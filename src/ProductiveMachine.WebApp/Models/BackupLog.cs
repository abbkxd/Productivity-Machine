using System.ComponentModel.DataAnnotations;

namespace ProductiveMachine.WebApp.Models;

public class BackupLog
{
    public int Id { get; set; }
    
    [Required]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    
    public DateTime? EndTime { get; set; }
    
    [Required]
    public BackupStatus Status { get; set; } = BackupStatus.InProgress;
    
    [StringLength(2000)]
    public string? Details { get; set; }
    
    public string? FileName { get; set; }
    
    public long? FileSize { get; set; } // Size in bytes
    
    public string? DestinationPath { get; set; }
    
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
    
    // Helper method to complete the backup log
    public void Complete(bool successful, string details = null)
    {
        EndTime = DateTime.UtcNow;
        Status = successful ? BackupStatus.Completed : BackupStatus.Failed;
        
        if (!string.IsNullOrEmpty(details))
        {
            Details = details;
        }
    }
}

public enum BackupStatus
{
    InProgress,
    Completed,
    Failed,
    Cancelled
} 