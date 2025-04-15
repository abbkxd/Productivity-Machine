using Microsoft.Extensions.Logging;
using ProductiveMachine.WebApp.Models;
using ProductiveMachine.WebApp.Services;

namespace ProductiveMachine.Jobs;

public class DatabaseBackupJob
{
    private readonly IBackupService _backupService;
    private readonly ILogger<DatabaseBackupJob> _logger;

    public DatabaseBackupJob(
        IBackupService backupService,
        ILogger<DatabaseBackupJob> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<BackupLog> CreateBackupAsync()
    {
        _logger.LogInformation("Starting database backup");
        
        try
        {
            var backupLog = await _backupService.CreateBackupAsync();
            
            if (backupLog.Status == BackupStatus.Completed)
            {
                _logger.LogInformation("Database backup completed successfully: {FileName}", backupLog.FileName);
            }
            else
            {
                _logger.LogWarning("Database backup failed: {Details}", backupLog.Details);
            }
            
            return backupLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database backup");
            throw;
        }
    }
} 