using Microsoft.Extensions.Logging;
using Moq;
using ProductiveMachine.Jobs;
using ProductiveMachine.WebApp.Services;
using Xunit;

namespace ProductiveMachine.Tests.Jobs;

public class EmailCleanupJobTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<EmailCleanupJob>> _loggerMock;
    
    public EmailCleanupJobTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<EmailCleanupJob>>();
    }
    
    [Fact]
    public async Task CleanupExpiredEmailsAsync_Should_Call_Email_Service()
    {
        // Arrange
        var job = new EmailCleanupJob(_emailServiceMock.Object, _loggerMock.Object);
        
        // Setup the email service to return successfully
        _emailServiceMock
            .Setup(x => x.CleanupExpiredEmailsAsync())
            .Returns(Task.CompletedTask);
        
        // Act
        await job.CleanupExpiredEmailsAsync();
        
        // Assert
        _emailServiceMock.Verify(x => x.CleanupExpiredEmailsAsync(), Times.Once);
    }
    
    [Fact]
    public async Task CleanupExpiredEmailsAsync_Should_Handle_Exceptions()
    {
        // Arrange
        var job = new EmailCleanupJob(_emailServiceMock.Object, _loggerMock.Object);
        
        // Setup the email service to throw an exception
        _emailServiceMock
            .Setup(x => x.CleanupExpiredEmailsAsync())
            .ThrowsAsync(new Exception("Test exception"));
        
        // Act & Assert - Should not throw an exception
        await job.CleanupExpiredEmailsAsync();
        
        // Verify that the service was called
        _emailServiceMock.Verify(x => x.CleanupExpiredEmailsAsync(), Times.Once);
        
        // Verify that an error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
} 