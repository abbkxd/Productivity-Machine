using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;

namespace ProductiveMachine.WebApp.Services;

public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(
        ApplicationDbContext context, 
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    #region Self-Destructing Email Operations

    public async Task<SelfDestructEmail> CreateSelfDestructEmailAsync(SelfDestructEmail email)
    {
        // Ensure GUID is set
        if (email.AccessGuid == Guid.Empty)
        {
            email.AccessGuid = Guid.NewGuid();
        }
        
        // Set default expiration if not provided
        if (email.ExpiresAt == default)
        {
            email.ExpiresAt = DateTime.UtcNow.AddDays(2); // Default 2-day expiration
        }
        
        _context.SelfDestructEmails.Add(email);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created SelfDestructEmail {Id} for user {UserId}", email.Id, email.UserId);
        return email;
    }

    public async Task<SelfDestructEmail?> GetEmailByIdAsync(int id, string userId)
    {
        return await _context.SelfDestructEmails
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
    }

    public async Task<SelfDestructEmail?> GetEmailByGuidAsync(Guid accessGuid)
    {
        return await _context.SelfDestructEmails
            .FirstOrDefaultAsync(e => e.AccessGuid == accessGuid && !e.WasDeleted);
    }

    public async Task<IEnumerable<SelfDestructEmail>> GetEmailsAsync(string userId)
    {
        return await _context.SelfDestructEmails
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteEmailAsync(int id, string userId)
    {
        var email = await _context.SelfDestructEmails
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            
        if (email == null)
        {
            return false;
        }

        email.WasDeleted = true;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted SelfDestructEmail {Id} for user {UserId}", id, userId);
        return true;
    }

    #endregion

    #region Email Access and Delivery

    public async Task<bool> MarkEmailAsAccessedAsync(Guid accessGuid)
    {
        var email = await _context.SelfDestructEmails
            .FirstOrDefaultAsync(e => e.AccessGuid == accessGuid && !e.WasDeleted);
            
        if (email == null)
        {
            return false;
        }

        email.WasAccessed = true;
        email.AccessedAt = DateTime.UtcNow;
        email.Status = EmailStatus.Accessed;
        
        await _context.SaveChangesAsync();
        _logger.LogInformation("SelfDestructEmail {Id} was accessed", email.Id);
        
        return true;
    }

    public async Task<bool> SendEmailAsync(int emailId, string userId)
    {
        var email = await _context.SelfDestructEmails
            .FirstOrDefaultAsync(e => e.Id == emailId && e.UserId == userId && !e.WasDeleted);
            
        if (email == null)
        {
            return false;
        }

        try
        {
            // Get SMTP settings from configuration
            var smtpServer = _configuration["Email:SmtpServer"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "25");
            var smtpUsername = _configuration["Email:SmtpUsername"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var senderEmail = _configuration["Email:SenderEmail"];
            var useSsl = bool.Parse(_configuration["Email:UseSsl"] ?? "true");
            var applicationUrl = _configuration["ApplicationUrl"] ?? "http://localhost:5000";

            // Create email message
            var message = new MailMessage
            {
                From = new MailAddress(senderEmail!),
                Subject = email.Subject,
                IsBodyHtml = true
            };
            
            message.To.Add(email.RecipientEmail);
            
            // Build email body with access link
            var accessUrl = email.GetAccessUrl(applicationUrl);
            var emailBody = @$"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }}
                        .btn {{ display: inline-block; background-color: #007bff; color: white; text-decoration: none; padding: 10px 20px; border-radius: 5px; }}
                        .warning {{ color: #dc3545; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Secure Message</h2>
                        </div>
                        <div class='content'>
                            <p>You have received a secure message that will self-destruct once read or after {email.ExpiresAt.ToString("MMMM dd, yyyy")}.</p>
                            <p>Click the button below to view the message:</p>
                            <p style='text-align: center;'>
                                <a href='{accessUrl}' class='btn'>View Secure Message</a>
                            </p>
                            <p class='warning'>Warning: This message can only be viewed once and will expire automatically.</p>
                        </div>
                        <div class='footer'>
                            <p>This message was sent from the Productive Machine system.</p>
                            <p>If you didn't expect this message, please disregard it.</p>
                        </div>
                    </div>
                </body>
                </html>";
                
            message.Body = emailBody;

            // Send email using SMTP
            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = useSsl,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };
            
            await client.SendMailAsync(message);

            // Update email status
            email.SentAt = DateTime.UtcNow;
            email.Status = EmailStatus.Sent;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("SelfDestructEmail {Id} sent to {Recipient}", 
                email.Id, email.RecipientEmail);
                
            return true;
        }
        catch (Exception ex)
        {
            email.Status = EmailStatus.Failed;
            email.Details = ex.Message;
            await _context.SaveChangesAsync();
            
            _logger.LogError(ex, "Failed to send SelfDestructEmail {Id}", email.Id);
            return false;
        }
    }

    public async Task<bool> ResendEmailAsync(int emailId, string userId)
    {
        var email = await _context.SelfDestructEmails
            .FirstOrDefaultAsync(e => e.Id == emailId && e.UserId == userId && !e.WasDeleted);
            
        if (email == null)
        {
            return false;
        }
        
        // Reset access status
        email.WasAccessed = false;
        email.AccessedAt = null;
        
        // Extend expiration date
        email.ExpiresAt = DateTime.UtcNow.AddDays(2);
        
        await _context.SaveChangesAsync();
        
        // Send the email again
        return await SendEmailAsync(emailId, userId);
    }

    #endregion

    #region Cleanup Operations

    public async Task CleanupExpiredEmailsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredEmails = await _context.SelfDestructEmails
            .Where(e => !e.WasDeleted && e.ExpiresAt < now)
            .ToListAsync();
            
        if (!expiredEmails.Any())
        {
            return;
        }
        
        foreach (var email in expiredEmails)
        {
            email.WasDeleted = true;
            email.Status = EmailStatus.Expired;
        }
        
        await _context.SaveChangesAsync();
        _logger.LogInformation("Cleaned up {Count} expired emails", expiredEmails.Count);
    }

    #endregion

    #region Statistics

    public async Task<int> GetSentEmailsCountAsync(string userId, DateTime since)
    {
        return await _context.SelfDestructEmails
            .CountAsync(e => e.UserId == userId && 
                          e.SentAt.HasValue && 
                          e.SentAt >= since);
    }

    public async Task<int> GetAccessedEmailsCountAsync(string userId, DateTime since)
    {
        return await _context.SelfDestructEmails
            .CountAsync(e => e.UserId == userId && 
                          e.WasAccessed && 
                          e.AccessedAt.HasValue && 
                          e.AccessedAt >= since);
    }

    #endregion
} 