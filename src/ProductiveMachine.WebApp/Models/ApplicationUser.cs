using Microsoft.AspNetCore.Identity;

namespace ProductiveMachine.WebApp.Models;

public class ApplicationUser : IdentityUser
{
    // Two-factor authentication properties
    public bool IsTwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecretKey { get; set; }
    
    // User preferences
    public string? TimeZone { get; set; }
    public bool EnableEmailNotifications { get; set; } = true;
    public bool EnablePushNotifications { get; set; } = false;
    
    // Navigation properties
    public virtual ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
    public virtual ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    public virtual ICollection<TodoCategory> TodoCategories { get; set; } = new List<TodoCategory>();
    public virtual ICollection<SelfDestructEmail> SelfDestructEmails { get; set; } = new List<SelfDestructEmail>();
} 