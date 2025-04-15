using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;
using ProductiveMachine.WebApp.Services;

namespace ProductiveMachine.Jobs;

public class TaskMaintenanceJob
{
    private readonly ApplicationDbContext _context;
    private readonly ITodoService _todoService;
    private readonly ILogger<TaskMaintenanceJob> _logger;

    public TaskMaintenanceJob(
        ApplicationDbContext context,
        ITodoService todoService,
        ILogger<TaskMaintenanceJob> logger)
    {
        _context = context;
        _todoService = todoService;
        _logger = logger;
    }

    public async Task ProcessTasksAsync()
    {
        _logger.LogInformation("Starting task maintenance job");
        
        try
        {
            await CheckRecurringTasksAsync();
            await CheckOverdueTasksAsync();
            
            _logger.LogInformation("Task maintenance completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during task maintenance");
        }
    }

    private async Task CheckRecurringTasksAsync()
    {
        try
        {
            // Get all active recurring tasks where next occurrence should be generated
            var recurringTasks = await _context.TodoItems
                .Include(t => t.RecurrenceSchedule)
                .Where(t => t.RecurrenceSchedule != null &&
                          t.Status == TodoStatus.NotStarted &&
                          t.DueDate.HasValue &&
                          t.DueDate.Value.Date <= DateTime.UtcNow.Date)
                .ToListAsync();
                
            _logger.LogInformation("Found {Count} recurring tasks to process", recurringTasks.Count);
            
            foreach (var task in recurringTasks)
            {
                if (task.RecurrenceSchedule == null)
                    continue;
                
                // Complete the current task automatically
                await _todoService.CompleteTodoAsync(task.Id, task.UserId);
                
                _logger.LogInformation("Processed recurring task: {TaskId} - {Title}", 
                    task.Id, task.Title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recurring tasks");
        }
    }

    private async Task CheckOverdueTasksAsync()
    {
        try
        {
            // Get all overdue tasks
            var today = DateTime.UtcNow.Date;
            var overdueTasks = await _context.TodoItems
                .Where(t => t.Status != TodoStatus.Completed && 
                           t.Status != TodoStatus.Cancelled &&
                           t.DueDate.HasValue && 
                           t.DueDate.Value.Date < today)
                .ToListAsync();
                
            _logger.LogInformation("Found {Count} overdue tasks", overdueTasks.Count);
            
            // Here you could implement logic to notify users about overdue tasks
            // or automatically reschedule/defer them based on your business rules
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking overdue tasks");
        }
    }
} 