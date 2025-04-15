using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;
using ProductiveMachine.WebApp.Services;
using Xunit;

namespace ProductiveMachine.Tests.Services;

public class EmailServiceTests
{
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IConfigurationSection> _emailConfigSectionMock;
    
    public EmailServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestEmailDb_" + Guid.NewGuid())
            .Options;
            
        _loggerMock = new Mock<ILogger<EmailService>>();
        
        // Setup configuration mock
        _configMock = new Mock<IConfiguration>();
        _emailConfigSectionMock = new Mock<IConfigurationSection>();
        
        _configMock.Setup(c => c["ApplicationUrl"]).Returns("http://localhost:5000");
        _configMock.Setup(c => c["Email:SmtpServer"]).Returns("smtp.example.com");
        _configMock.Setup(c => c["Email:SmtpPort"]).Returns("587");
        _configMock.Setup(c => c["Email:SmtpUsername"]).Returns("test@example.com");
        _configMock.Setup(c => c["Email:SmtpPassword"]).Returns("password");
        _configMock.Setup(c => c["Email:SenderEmail"]).Returns("test@example.com");
        _configMock.Setup(c => c["Email:UseSsl"]).Returns("true");
    }
    
    [Fact]
    public async Task CreateSelfDestructEmailAsync_Should_Add_Email_To_Database()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new EmailService(context, _loggerMock.Object, _configMock.Object);
        
        var userId = "user-123";
        var email = new SelfDestructEmail
        {
            Subject = "Test Email",
            Body = "This is a test email",
            RecipientEmail = "recipient@example.com",
            UserId = userId
        };
        
        // Act
        var result = await service.CreateSelfDestructEmailAsync(email);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Email", result.Subject);
        Assert.Equal(userId, result.UserId);
        Assert.NotEqual(Guid.Empty, result.AccessGuid);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        
        // Verify it's in the database
        var emailFromDb = await context.SelfDestructEmails.FindAsync(result.Id);
        Assert.NotNull(emailFromDb);
        Assert.Equal("Test Email", emailFromDb.Subject);
    }
    
    [Fact]
    public async Task GetEmailByGuidAsync_Should_Return_Email_With_Matching_Guid()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new EmailService(context, _loggerMock.Object, _configMock.Object);
        
        var userId = "user-123";
        var accessGuid = Guid.NewGuid();
        
        var email = new SelfDestructEmail
        {
            Subject = "Test Email",
            Body = "This is a test email",
            RecipientEmail = "recipient@example.com",
            UserId = userId,
            AccessGuid = accessGuid,
            WasDeleted = false
        };
        
        context.SelfDestructEmails.Add(email);
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.GetEmailByGuidAsync(accessGuid);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Email", result.Subject);
        Assert.Equal(accessGuid, result.AccessGuid);
    }
    
    [Fact]
    public async Task GetEmailByGuidAsync_Should_Return_Null_For_Deleted_Email()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new EmailService(context, _loggerMock.Object, _configMock.Object);
        
        var userId = "user-123";
        var accessGuid = Guid.NewGuid();
        
        var email = new SelfDestructEmail
        {
            Subject = "Test Email",
            Body = "This is a test email",
            RecipientEmail = "recipient@example.com",
            UserId = userId,
            AccessGuid = accessGuid,
            WasDeleted = true // Email is marked as deleted
        };
        
        context.SelfDestructEmails.Add(email);
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.GetEmailByGuidAsync(accessGuid);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public async Task MarkEmailAsAccessedAsync_Should_Update_Email_Properties()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new EmailService(context, _loggerMock.Object, _configMock.Object);
        
        var userId = "user-123";
        var accessGuid = Guid.NewGuid();
        
        var email = new SelfDestructEmail
        {
            Subject = "Test Email",
            Body = "This is a test email",
            RecipientEmail = "recipient@example.com",
            UserId = userId,
            AccessGuid = accessGuid,
            WasAccessed = false,
            AccessedAt = null,
            Status = EmailStatus.Sent
        };
        
        context.SelfDestructEmails.Add(email);
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.MarkEmailAsAccessedAsync(accessGuid);
        
        // Assert
        Assert.True(result);
        
        var updatedEmail = await context.SelfDestructEmails.FindAsync(email.Id);
        Assert.NotNull(updatedEmail);
        Assert.True(updatedEmail.WasAccessed);
        Assert.NotNull(updatedEmail.AccessedAt);
        Assert.Equal(EmailStatus.Accessed, updatedEmail.Status);
    }
    
    [Fact]
    public async Task CleanupExpiredEmailsAsync_Should_Mark_Expired_Emails_As_Deleted()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new EmailService(context, _loggerMock.Object, _configMock.Object);
        
        var userId = "user-123";
        var now = DateTime.UtcNow;
        
        // Create some expired and non-expired emails
        await context.SelfDestructEmails.AddRangeAsync(
            new SelfDestructEmail { 
                Subject = "Expired Email 1", 
                Body = "Test", 
                UserId = userId, 
                ExpiresAt = now.AddDays(-1),
                WasDeleted = false 
            },
            new SelfDestructEmail { 
                Subject = "Expired Email 2", 
                Body = "Test", 
                UserId = userId, 
                ExpiresAt = now.AddHours(-2),
                WasDeleted = false 
            },
            new SelfDestructEmail { 
                Subject = "Valid Email", 
                Body = "Test", 
                UserId = userId, 
                ExpiresAt = now.AddDays(1),
                WasDeleted = false 
            }
        );
        await context.SaveChangesAsync();
        
        // Act
        await service.CleanupExpiredEmailsAsync();
        
        // Assert
        var emails = await context.SelfDestructEmails.ToListAsync();
        
        // Verify expired emails are marked as deleted
        var expiredEmail1 = emails.First(e => e.Subject == "Expired Email 1");
        var expiredEmail2 = emails.First(e => e.Subject == "Expired Email 2");
        var validEmail = emails.First(e => e.Subject == "Valid Email");
        
        Assert.True(expiredEmail1.WasDeleted);
        Assert.Equal(EmailStatus.Expired, expiredEmail1.Status);
        
        Assert.True(expiredEmail2.WasDeleted);
        Assert.Equal(EmailStatus.Expired, expiredEmail2.Status);
        
        // Non-expired email should remain unchanged
        Assert.False(validEmail.WasDeleted);
        Assert.NotEqual(EmailStatus.Expired, validEmail.Status);
    }
} 