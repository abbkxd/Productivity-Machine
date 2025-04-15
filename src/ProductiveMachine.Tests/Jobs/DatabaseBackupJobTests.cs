using Microsoft.Extensions.Logging;
using Moq;
using ProductiveMachine.Jobs;
using ProductiveMachine.WebApp.Models;
using ProductiveMachine.WebApp.Services;
using Xunit;

namespace ProductiveMachine.Tests.Jobs;

public class DatabaseBackupJobTests
{
    private readonly Mock<IBackupService> _backupServiceMock;
    private readonly Mock<ILogger<DatabaseBackupJob>> _loggerMock;
    
    public DatabaseBackupJobTests()
    {
        _backupServiceMock = new Mock<IBackupService>();
        _loggerMock = new Mock<ILogger<DatabaseBackupJob>>();
    }
    
    [Fact]
    public async Task CreateBackupAsync_Should_Return_Backup_Log_When_Successful()
    {
        // Arrange
        var job = new DatabaseBackupJob(_backupServiceMock.Object, _loggerMock.Object);
        
        var backupLog = new BackupLog
        {
            Id = 1,
            FileName = "backup_20230101_120000.db",
            Status = BackupStatus.Completed,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow
        };
        
        // Setup the backup service to return a successful backup
        _backupServiceMock
            .Setup(x => x.CreateBackupAsync())
            .ReturnsAsync(backupLog);
        
        // Act
        var result = await job.CreateBackupAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(backupLog.Id, result.Id);
        Assert.Equal(BackupStatus.Completed, result.Status);
        
        // Verify that the service was called
        _backupServiceMock.Verify(x => x.CreateBackupAsync(), Times.Once);
    }
    
    [Fact]
    public async Task CreateBackupAsync_Should_Handle_Failed_Backup()
    {
        // Arrange
        var job = new DatabaseBackupJob(_backupServiceMock.Object, _loggerMock.Object);
        
        var backupLog = new BackupLog
        {
            Id = 1,
            Status = BackupStatus.Failed,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            Details = "Backup failed: Access denied"
        };
        
        // Setup the backup service to return a failed backup
        _backupServiceMock
            .Setup(x => x.CreateBackupAsync())
            .ReturnsAsync(backupLog);
        
        // Act
        var result = await job.CreateBackupAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(BackupStatus.Failed, result.Status);
        
        // Verify that the service was called
        _backupServiceMock.Verify(x => x.CreateBackupAsync(), Times.Once);
        
        // Verify that a warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
    
    [Fact]
    public async Task CreateBackupAsync_Should_Handle_Exceptions()
    {
        // Arrange
        var job = new DatabaseBackupJob(_backupServiceMock.Object, _loggerMock.Object);
        
        // Setup the backup service to throw an exception
        _backupServiceMock
            .Setup(x => x.CreateBackupAsync())
            .ThrowsAsync(new Exception("Test exception"));
        
        // Act & Assert - Should rethrow the exception
        await Assert.ThrowsAsync<Exception>(() => job.CreateBackupAsync());
        
        // Verify that the service was called
        _backupServiceMock.Verify(x => x.CreateBackupAsync(), Times.Once);
        
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