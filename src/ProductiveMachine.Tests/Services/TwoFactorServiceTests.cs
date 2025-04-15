using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;
using ProductiveMachine.WebApp.Services;
using Xunit;

namespace ProductiveMachine.Tests.Services;

public class TwoFactorServiceTests
{
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ILogger<TwoFactorService>> _loggerMock;
    
    public TwoFactorServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestTwoFactorDb_" + Guid.NewGuid())
            .Options;
            
        _loggerMock = new Mock<ILogger<TwoFactorService>>();
        
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);
    }
    
    [Fact]
    public void GenerateSecretKey_Should_Return_Valid_Base32_Key()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new TwoFactorService(_userManagerMock.Object, context, _loggerMock.Object);
        
        // Act
        var key = service.GenerateSecretKey();
        
        // Assert
        Assert.NotNull(key);
        Assert.NotEmpty(key);
        
        // Base32 uses only uppercase letters A-Z and digits 2-7
        Assert.Matches("^[A-Z2-7]+$", key);
    }
    
    [Fact]
    public void GenerateQrCode_Should_Return_Non_Empty_Byte_Array()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new TwoFactorService(_userManagerMock.Object, context, _loggerMock.Object);
        
        var secretKey = "ABCDEFGHIJKLMNOPQRST";
        var email = "test@example.com";
        
        // Act
        var qrCode = service.GenerateQrCode(secretKey, email);
        
        // Assert
        Assert.NotNull(qrCode);
        Assert.True(qrCode.Length > 0);
    }
    
    [Fact]
    public void ValidateToken_Should_Return_False_For_Invalid_Token()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new TwoFactorService(_userManagerMock.Object, context, _loggerMock.Object);
        
        var secretKey = service.GenerateSecretKey();
        
        // Act & Assert
        
        // Invalid format
        Assert.False(service.ValidateToken(secretKey, "12345")); // Too short
        Assert.False(service.ValidateToken(secretKey, "1234567")); // Too long
        Assert.False(service.ValidateToken(secretKey, "abcdef")); // Not numeric
        
        // Valid format but almost certainly wrong code (unless we get extremely lucky)
        Assert.False(service.ValidateToken(secretKey, "123456"));
    }
    
    [Fact]
    public async Task EnableTwoFactorAsync_Should_Update_User_Properties()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        
        string capturedUserId = null;
        ApplicationUser capturedUser = null;
        
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => {
                capturedUserId = id;
                return new ApplicationUser { Id = id };
            });
            
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(user => capturedUser = user);
            
        var service = new TwoFactorService(_userManagerMock.Object, context, _loggerMock.Object);
        
        var userId = "user-123";
        var secretKey = "ABCDEFGHIJKLMNOPQRST";
        
        // Act
        var result = await service.EnableTwoFactorAsync(userId, secretKey);
        
        // Assert
        Assert.True(result);
        
        // Verify the user was retrieved by the correct ID
        Assert.Equal(userId, capturedUserId);
        
        // Verify user properties were updated correctly
        Assert.NotNull(capturedUser);
        Assert.True(capturedUser.TwoFactorEnabled);
        Assert.True(capturedUser.IsTwoFactorEnabled);
        Assert.Equal(secretKey, capturedUser.TwoFactorSecretKey);
    }
    
    [Fact]
    public async Task DisableTwoFactorAsync_Should_Update_User_Properties()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        
        string capturedUserId = null;
        ApplicationUser capturedUser = null;
        
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => {
                capturedUserId = id;
                return new ApplicationUser { 
                    Id = id,
                    TwoFactorEnabled = true,
                    IsTwoFactorEnabled = true,
                    TwoFactorSecretKey = "SECRETKEY"
                };
            });
            
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(user => capturedUser = user);
            
        var service = new TwoFactorService(_userManagerMock.Object, context, _loggerMock.Object);
        
        var userId = "user-123";
        
        // Act
        var result = await service.DisableTwoFactorAsync(userId);
        
        // Assert
        Assert.True(result);
        
        // Verify the user was retrieved by the correct ID
        Assert.Equal(userId, capturedUserId);
        
        // Verify user properties were updated correctly
        Assert.NotNull(capturedUser);
        Assert.False(capturedUser.TwoFactorEnabled);
        Assert.False(capturedUser.IsTwoFactorEnabled);
        Assert.Null(capturedUser.TwoFactorSecretKey);
    }
} 