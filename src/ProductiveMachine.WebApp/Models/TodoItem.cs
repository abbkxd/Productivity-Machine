using System.ComponentModel.DataAnnotations;

namespace ProductiveMachine.WebApp.Models;

public class TodoItem
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? Description { get; set; }
    
    public TodoStatus Status { get; set; } = TodoStatus.NotStarted;
    
    public TodoPriority Priority { get; set; } = TodoPriority.Medium;
    
    public DateTime? DueDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    // Foreign keys
    public string UserId { get; set; } = string.Empty;
    
    public int? CategoryId { get; set; }
    
    public int? RecurrenceScheduleId { get; set; }
    
    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    
    public virtual TodoCategory? Category { get; set; }
    
    public virtual RecurrenceSchedule? RecurrenceSchedule { get; set; }
    
    // Properties for task completion metrics
    public int? EstimatedMinutes { get; set; }
    
    public int? ActualMinutes { get; set; }
    
    // Properties for recurrence management
    public int? ParentTodoId { get; set; }
    
    public bool GenerateNextOnComplete { get; set; } = true;
}

public enum TodoStatus
{
    NotStarted,
    InProgress,
    Completed,
    Cancelled,
    Deferred
}

public enum TodoPriority
{
    Low,
    Medium,
    High,
    Urgent
} 