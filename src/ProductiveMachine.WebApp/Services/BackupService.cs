using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;

namespace ProductiveMachine.WebApp.Services;

public class BackupService : IBackupService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;
    private readonly string _backupDirectory;
    private readonly string _dataDirectory;
    private readonly string _rcloneConfigPath;

    public BackupService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<BackupService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        
        // Get configuration values
        _backupDirectory = _configuration["Backup:Directory"] ?? "../backups";
        _dataDirectory = _configuration["Backup:DataDirectory"] ?? "../data";
        _rcloneConfigPath = _configuration["Backup:RcloneConfigPath"] ?? "/etc/rclone/rclone.conf";
        
        // Ensure directories exist
        Directory.CreateDirectory(_backupDirectory);
        Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<BackupLog> CreateBackupAsync()
    {
        // Create a backup log entry
        var backupLog = new BackupLog
        {
            StartTime = DateTime.UtcNow,
            Status = BackupStatus.InProgress
        };
        
        _context.BackupLogs.Add(backupLog);
        await _context.SaveChangesAsync();
        
        try
        {
            // Generate backup filename
            var filename = await GetBackupFilenameAsync();
            var backupPath = Path.Combine(_backupDirectory, filename);
            
            // Create the database backup
            var connectionString = _context.Database.GetConnectionString();
            var databasePath = GetDatabasePathFromConnectionString(connectionString);
            
            if (File.Exists(databasePath))
            {
                // For SQLite, we can simply copy the database file
                File.Copy(databasePath, backupPath, true);
                
                // Get the file size
                var fileInfo = new FileInfo(backupPath);
                backupLog.FileSize = fileInfo.Length;
                backupLog.FileName = filename;
                
                // Encrypt the backup if configured
                if (bool.Parse(_configuration["Backup:EncryptBackups"] ?? "true"))
                {
                    await EncryptBackupAsync(backupPath);
                    backupLog.FileName = $"{filename}.gpg";
                }
                
                // Upload to cloud if configured
                if (bool.Parse(_configuration["Backup:UploadToCloud"] ?? "false"))
                {
                    var destinationPath = await UploadToCloudAsync(backupPath);
                    backupLog.DestinationPath = destinationPath;
                }
                
                // Mark backup as successful
                backupLog.Status = BackupStatus.Completed;
                backupLog.EndTime = DateTime.UtcNow;
                backupLog.Details = "Backup completed successfully";
                
                _logger.LogInformation("Backup completed successfully: {FileName}, size: {FileSize} bytes", 
                    backupLog.FileName, backupLog.FileSize);
            }
            else
            {
                // Database file not found
                backupLog.Status = BackupStatus.Failed;
                backupLog.EndTime = DateTime.UtcNow;
                backupLog.Details = $"Database file not found: {databasePath}";
                
                _logger.LogError("Backup failed: Database file not found at {DatabasePath}", databasePath);
            }
        }
        catch (Exception ex)
        {
            // Mark backup as failed
            backupLog.Status = BackupStatus.Failed;
            backupLog.EndTime = DateTime.UtcNow;
            backupLog.Details = $"Backup failed: {ex.Message}";
            
            _logger.LogError(ex, "Backup failed");
        }
        
        // Update the backup log
        await _context.SaveChangesAsync();
        return backupLog;
    }

    public async Task<IEnumerable<BackupLog>> GetBackupLogsAsync(int count = 10, int skip = 0)
    {
        return await _context.BackupLogs
            .OrderByDescending(b => b.StartTime)
            .Skip(skip)
            .Take(count)
            .ToListAsync();
    }

    public async Task<BackupLog?> GetBackupLogByIdAsync(int id)
    {
        return await _context.BackupLogs.FindAsync(id);
    }

    public async Task<DateTime?> GetLastSuccessfulBackupTimeAsync()
    {
        var lastBackup = await _context.BackupLogs
            .Where(b => b.Status == BackupStatus.Completed)
            .OrderByDescending(b => b.EndTime)
            .FirstOrDefaultAsync();
            
        return lastBackup?.EndTime;
    }

    public async Task<bool> IsBackupDueAsync()
    {
        var backupFrequencyHours = int.Parse(_configuration["Backup:FrequencyHours"] ?? "24");
        var lastBackupTime = await GetLastSuccessfulBackupTimeAsync();
        
        if (!lastBackupTime.HasValue)
        {
            // No successful backup ever, so one is due
            return true;
        }
        
        // Check if the time since the last backup exceeds the frequency
        var hoursSinceLastBackup = (DateTime.UtcNow - lastBackupTime.Value).TotalHours;
        return hoursSinceLastBackup >= backupFrequencyHours;
    }

    public async Task<string> GetBackupFilenameAsync()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var dbName = Path.GetFileNameWithoutExtension(GetDatabaseName());
        return $"{dbName}_backup_{timestamp}.db";
    }

    public async Task<long> GetDatabaseSizeAsync()
    {
        var connectionString = _context.Database.GetConnectionString();
        var databasePath = GetDatabasePathFromConnectionString(connectionString);
        
        if (File.Exists(databasePath))
        {
            var fileInfo = new FileInfo(databasePath);
            return fileInfo.Length;
        }
        
        return 0;
    }

    private string GetDatabasePathFromConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            // Default path if connection string is not available
            return Path.Combine(_dataDirectory, "productive_machine.db");
        }
        
        // For SQLite, extract the Data Source path
        var builder = new SqliteConnectionStringBuilder(connectionString);
        return builder.DataSource;
    }

    private string GetDatabaseName()
    {
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            return "productive_machine";
        }
        
        var builder = new SqliteConnectionStringBuilder(connectionString);
        return Path.GetFileName(builder.DataSource);
    }

    private async Task EncryptBackupAsync(string backupPath)
    {
        var encryptedPath = $"{backupPath}.gpg";
        var recipient = _configuration["Backup:GPGRecipient"];
        
        if (string.IsNullOrEmpty(recipient))
        {
            _logger.LogWarning("GPG recipient not configured, skipping encryption");
            return;
        }
        
        try
        {
            // Use GPG to encrypt the backup
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gpg",
                    Arguments = $"--output {encryptedPath} --encrypt --recipient {recipient} {backupPath}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                // Delete the unencrypted file if encryption was successful
                File.Delete(backupPath);
                _logger.LogInformation("Backup encrypted successfully: {EncryptedPath}", encryptedPath);
                return;
            }
            
            var error = await process.StandardError.ReadToEndAsync();
            _logger.LogError("GPG encryption failed: {Error}", error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt backup");
        }
    }

    private async Task<string?> UploadToCloudAsync(string backupPath)
    {
        var cloudProvider = _configuration["Backup:CloudProvider"] ?? "googledrive";
        var remoteDir = _configuration["Backup:CloudDirectory"] ?? "productive_machine_backups";
        var remotePath = $"{cloudProvider}:{remoteDir}/{Path.GetFileName(backupPath)}";
        
        try
        {
            // Use rclone to upload the backup
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "rclone",
                    Arguments = $"copy {backupPath} {remotePath} --config {_rcloneConfigPath}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Backup uploaded to cloud: {RemotePath}", remotePath);
                return remotePath;
            }
            
            var error = await process.StandardError.ReadToEndAsync();
            _logger.LogError("rclone upload failed: {Error}", error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload backup to cloud");
            return null;
        }
    }
} 