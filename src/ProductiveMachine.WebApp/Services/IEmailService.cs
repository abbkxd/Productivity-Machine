using ProductiveMachine.WebApp.Models;

namespace ProductiveMachine.WebApp.Services;

public interface IEmailService
{
    // Self-destructing email operations
    Task<SelfDestructEmail> CreateSelfDestructEmailAsync(SelfDestructEmail email);
    Task<SelfDestructEmail?> GetEmailByIdAsync(int id, string userId);
    Task<SelfDestructEmail?> GetEmailByGuidAsync(Guid accessGuid);
    Task<IEnumerable<SelfDestructEmail>> GetEmailsAsync(string userId);
    Task<bool> DeleteEmailAsync(int id, string userId);
    
    // Email access and delivery
    Task<bool> MarkEmailAsAccessedAsync(Guid accessGuid);
    Task<bool> SendEmailAsync(int emailId, string userId);
    Task<bool> ResendEmailAsync(int emailId, string userId);
    
    // Clean up operations
    Task CleanupExpiredEmailsAsync();
    
    // Statistics
    Task<int> GetSentEmailsCountAsync(string userId, DateTime since);
    Task<int> GetAccessedEmailsCountAsync(string userId, DateTime since);
} 