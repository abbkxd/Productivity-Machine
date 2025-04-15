using System.ComponentModel.DataAnnotations;

namespace ProductiveMachine.WebApp.Models;

public class JournalEntry
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(10000)]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ModifiedAt { get; set; }
    
    // Tags can be stored as comma-separated values
    [StringLength(500)]
    public string? Tags { get; set; }
    
    // Mood tracking (optional feature)
    public JournalMood? Mood { get; set; }
    
    // Foreign key
    public string UserId { get; set; } = string.Empty;
    
    // Navigation property
    public virtual ApplicationUser User { get; set; } = null!;
}

public enum JournalMood
{
    VeryNegative,
    Negative,
    Neutral,
    Positive,
    VeryPositive
} 