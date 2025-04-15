using System.ComponentModel.DataAnnotations;

namespace ProductiveMachine.WebApp.Models;

public class SelfDestructEmail
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    [StringLength(10000)]
    public string Body { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string RecipientEmail { get; set; } = string.Empty;
    
    [Required]
    public Guid AccessGuid { get; set; } = Guid.NewGuid();
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(2); // Default 2-day expiration
    
    public DateTime? SentAt { get; set; }
    
    public DateTime? AccessedAt { get; set; }
    
    public bool WasAccessed { get; set; } = false;
    
    public bool WasDeleted { get; set; } = false;
    
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    
    // Foreign key
    public string UserId { get; set; } = string.Empty;
    
    // Navigation property
    public virtual ApplicationUser User { get; set; } = null!;
    
    // Helper method to generate access URL
    public string GetAccessUrl(string baseUrl)
    {
        return $"{baseUrl}/Email/View/{AccessGuid}";
    }
}

public enum EmailStatus
{
    Pending,
    Sent,
    Accessed,
    Expired,
    Failed
} 