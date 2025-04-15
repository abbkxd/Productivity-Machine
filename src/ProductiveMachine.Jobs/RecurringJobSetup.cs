using Hangfire;

namespace ProductiveMachine.Jobs;

public static class RecurringJobSetup
{
    public static void ConfigureRecurringJobs()
    {
        // Email cleanup job - Run daily at 3 AM to clean up expired emails
        RecurringJob.AddOrUpdate<EmailCleanupJob>(
            "cleanup-expired-emails",
            job => job.CleanupExpiredEmailsAsync(),
            Cron.Daily(3));
            
        // Database backup job - Run daily at midnight
        RecurringJob.AddOrUpdate<DatabaseBackupJob>(
            "database-backup",
            job => job.CreateBackupAsync(),
            Cron.Daily(0));
            
        // Task maintenance job - Run hourly to check for overdue tasks and generate recurring tasks
        RecurringJob.AddOrUpdate<TaskMaintenanceJob>(
            "task-maintenance",
            job => job.ProcessTasksAsync(),
            Cron.Hourly());
    }
} 