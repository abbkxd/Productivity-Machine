using ProductiveMachine.WebApp.Models;

namespace ProductiveMachine.WebApp.Services;

public interface IBackupService
{
    // Create and execute a backup
    Task<BackupLog> CreateBackupAsync();
    
    // Get backup logs
    Task<IEnumerable<BackupLog>> GetBackupLogsAsync(int count = 10, int skip = 0);
    Task<BackupLog?> GetBackupLogByIdAsync(int id);
    
    // Check backup status
    Task<DateTime?> GetLastSuccessfulBackupTimeAsync();
    Task<bool> IsBackupDueAsync();
    
    // Backup helper methods
    Task<string> GetBackupFilenameAsync();
    Task<long> GetDatabaseSizeAsync();
} 