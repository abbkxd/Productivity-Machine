using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;
using ProductiveMachine.WebApp.Services;
using Xunit;

namespace ProductiveMachine.Tests.Services;

public class BackupServiceTests
{
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly Mock<ILogger<BackupService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    
    public BackupServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestBackupDb_" + Guid.NewGuid())
            .Options;
            
        _loggerMock = new Mock<ILogger<BackupService>>();
        
        // Setup configuration mock
        _configMock = new Mock<IConfiguration>();
        
        _configMock.Setup(c => c["Backup:Directory"]).Returns("../backups");
        _configMock.Setup(c => c["Backup:DataDirectory"]).Returns("../data");
        _configMock.Setup(c => c["Backup:RcloneConfigPath"]).Returns("/etc/rclone/rclone.conf");
        _configMock.Setup(c => c["Backup:EncryptBackups"]).Returns("false"); // Disable for testing
        _configMock.Setup(c => c["Backup:UploadToCloud"]).Returns("false"); // Disable for testing
        _configMock.Setup(c => c["Backup:FrequencyHours"]).Returns("24");
    }
    
    [Fact]
    public async Task GetBackupLogsAsync_Should_Return_Logs_In_Descending_Order()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new BackupService(context, _configMock.Object, _loggerMock.Object);
        
        var now = DateTime.UtcNow;
        
        await context.BackupLogs.AddRangeAsync(
            new BackupLog { 
                StartTime = now.AddDays(-3), 
                EndTime = now.AddDays(-3).AddMinutes(5), 
                Status = BackupStatus.Completed,
                FileName = "backup_3_days_ago.db" 
            },
            new BackupLog { 
                StartTime = now.AddDays(-2), 
                EndTime = now.AddDays(-2).AddMinutes(5), 
                Status = BackupStatus.Completed,
                FileName = "backup_2_days_ago.db" 
            },
            new BackupLog { 
                StartTime = now.AddDays(-1), 
                EndTime = now.AddDays(-1).AddMinutes(5), 
                Status = BackupStatus.Completed,
                FileName = "backup_yesterday.db" 
            }
        );
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.GetBackupLogsAsync(10, 0);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        
        // Should be in descending order by StartTime
        var resultList = result.ToList();
        Assert.Equal("backup_yesterday.db", resultList[0].FileName);
        Assert.Equal("backup_2_days_ago.db", resultList[1].FileName);
        Assert.Equal("backup_3_days_ago.db", resultList[2].FileName);
    }
    
    [Fact]
    public async Task GetLastSuccessfulBackupTimeAsync_Should_Return_Most_Recent_Completed_Backup()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new BackupService(context, _configMock.Object, _loggerMock.Object);
        
        var now = DateTime.UtcNow;
        
        await context.BackupLogs.AddRangeAsync(
            new BackupLog { 
                StartTime = now.AddDays(-3), 
                EndTime = now.AddDays(-3).AddMinutes(5), 
                Status = BackupStatus.Completed 
            },
            new BackupLog { 
                StartTime = now.AddDays(-2), 
                EndTime = now.AddDays(-2).AddMinutes(5), 
                Status = BackupStatus.Failed 
            },
            new BackupLog { 
                StartTime = now.AddDays(-1), 
                EndTime = now.AddDays(-1).AddMinutes(5), 
                Status = BackupStatus.Completed 
            }
        );
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.GetLastSuccessfulBackupTimeAsync();
        
        // Assert
        Assert.NotNull(result);
        
        // Should be the most recent completed backup (yesterday)
        var expectedDate = now.AddDays(-1).AddMinutes(5).Date;
        Assert.Equal(expectedDate, result.Value.Date);
    }
    
    [Fact]
    public async Task IsBackupDueAsync_Should_Return_True_When_No_Backups_Exist()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new BackupService(context, _configMock.Object, _loggerMock.Object);
        
        // No backups in the database
        
        // Act
        var result = await service.IsBackupDueAsync();
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public async Task IsBackupDueAsync_Should_Return_True_When_Last_Backup_Is_Old()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new BackupService(context, _configMock.Object, _loggerMock.Object);
        
        var now = DateTime.UtcNow;
        var oldBackupTime = now.AddHours(-25); // Older than the 24-hour frequency
        
        await context.BackupLogs.AddAsync(new BackupLog { 
            StartTime = oldBackupTime, 
            EndTime = oldBackupTime.AddMinutes(5), 
            Status = BackupStatus.Completed 
        });
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.IsBackupDueAsync();
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public async Task IsBackupDueAsync_Should_Return_False_When_Recent_Backup_Exists()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new BackupService(context, _configMock.Object, _loggerMock.Object);
        
        var now = DateTime.UtcNow;
        var recentBackupTime = now.AddHours(-12); // Newer than the 24-hour frequency
        
        await context.BackupLogs.AddAsync(new BackupLog { 
            StartTime = recentBackupTime, 
            EndTime = recentBackupTime.AddMinutes(5), 
            Status = BackupStatus.Completed 
        });
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.IsBackupDueAsync();
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task GetBackupFilenameAsync_Should_Return_Valid_Filename_With_Timestamp()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new BackupService(context, _configMock.Object, _loggerMock.Object);
        
        // Act
        var result = await service.GetBackupFilenameAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains("_backup_", result);
        Assert.EndsWith(".db", result);
        
        // Should contain today's date in format yyyyMMdd
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        Assert.Contains(today, result);
    }
} 