using Microsoft.Extensions.Logging;
using ProductiveMachine.WebApp.Services;

namespace ProductiveMachine.Jobs;

public class EmailCleanupJob
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailCleanupJob> _logger;

    public EmailCleanupJob(
        IEmailService emailService,
        ILogger<EmailCleanupJob> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task CleanupExpiredEmailsAsync()
    {
        _logger.LogInformation("Starting cleanup of expired emails");
        
        try
        {
            await _emailService.CleanupExpiredEmailsAsync();
            _logger.LogInformation("Expired emails cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean up expired emails");
        }
    }
} 