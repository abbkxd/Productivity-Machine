using Microsoft.EntityFrameworkCore;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;

namespace ProductiveMachine.WebApp.Services;

public class TodoService : ITodoService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TodoService> _logger;

    public TodoService(ApplicationDbContext context, ILogger<TodoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Basic CRUD Operations

    public async Task<TodoItem?> GetTodoByIdAsync(int id, string userId)
    {
        return await _context.TodoItems
            .Include(t => t.Category)
            .Include(t => t.RecurrenceSchedule)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    }

    public async Task<IEnumerable<TodoItem>> GetTodoItemsAsync(string userId, bool includeCompleted = false)
    {
        var query = _context.TodoItems
            .Include(t => t.Category)
            .Include(t => t.RecurrenceSchedule)
            .Where(t => t.UserId == userId);

        if (!includeCompleted)
        {
            query = query.Where(t => t.Status != TodoStatus.Completed && t.Status != TodoStatus.Cancelled);
        }

        return await query.OrderBy(t => t.DueDate).ThenBy(t => t.Priority).ToListAsync();
    }

    public async Task<IEnumerable<TodoItem>> GetTodoItemsForCategoryAsync(int categoryId, string userId)
    {
        return await _context.TodoItems
            .Include(t => t.Category)
            .Include(t => t.RecurrenceSchedule)
            .Where(t => t.CategoryId == categoryId && t.UserId == userId)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Priority)
            .ToListAsync();
    }

    public async Task<IEnumerable<TodoItem>> GetDueTodayAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.TodoItems
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && 
                        t.DueDate.HasValue && 
                        t.DueDate.Value.Date == today &&
                        t.Status != TodoStatus.Completed && 
                        t.Status != TodoStatus.Cancelled)
            .OrderBy(t => t.Priority)
            .ToListAsync();
    }

    public async Task<IEnumerable<TodoItem>> GetOverdueAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.TodoItems
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && 
                        t.DueDate.HasValue && 
                        t.DueDate.Value.Date < today &&
                        t.Status != TodoStatus.Completed && 
                        t.Status != TodoStatus.Cancelled)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Priority)
            .ToListAsync();
    }

    public async Task<TodoItem> CreateTodoAsync(TodoItem todo)
    {
        _context.TodoItems.Add(todo);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created TodoItem {Id} for user {UserId}", todo.Id, todo.UserId);
        return todo;
    }

    public async Task<TodoItem?> UpdateTodoAsync(TodoItem todo)
    {
        var existingTodo = await _context.TodoItems
            .FirstOrDefaultAsync(t => t.Id == todo.Id && t.UserId == todo.UserId);
            
        if (existingTodo == null)
        {
            return null;
        }

        // Update properties
        existingTodo.Title = todo.Title;
        existingTodo.Description = todo.Description;
        existingTodo.Status = todo.Status;
        existingTodo.Priority = todo.Priority;
        existingTodo.DueDate = todo.DueDate;
        existingTodo.CategoryId = todo.CategoryId;
        existingTodo.EstimatedMinutes = todo.EstimatedMinutes;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated TodoItem {Id} for user {UserId}", todo.Id, todo.UserId);
        
        return existingTodo;
    }

    public async Task<bool> DeleteTodoAsync(int id, string userId)
    {
        var todo = await _context.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            
        if (todo == null)
        {
            return false;
        }

        _context.TodoItems.Remove(todo);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted TodoItem {Id} for user {UserId}", id, userId);
        return true;
    }

    #endregion

    #region Status Management

    public async Task<TodoItem?> CompleteTodoAsync(int id, string userId, int? actualMinutes = null)
    {
        var todo = await _context.TodoItems
            .Include(t => t.RecurrenceSchedule)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            
        if (todo == null)
        {
            return null;
        }

        todo.Status = TodoStatus.Completed;
        todo.CompletedAt = DateTime.UtcNow;
        todo.ActualMinutes = actualMinutes;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Completed TodoItem {Id} for user {UserId}", id, userId);

        // If this is a recurring task, generate the next occurrence
        if (todo.RecurrenceSchedule != null && todo.GenerateNextOnComplete)
        {
            await GenerateNextRecurringTodoAsync(todo.Id, userId);
        }

        return todo;
    }

    public async Task<TodoItem?> StartTodoAsync(int id, string userId)
    {
        var todo = await _context.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            
        if (todo == null)
        {
            return null;
        }

        todo.Status = TodoStatus.InProgress;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Started TodoItem {Id} for user {UserId}", id, userId);
        return todo;
    }

    public async Task<TodoItem?> CancelTodoAsync(int id, string userId)
    {
        var todo = await _context.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            
        if (todo == null)
        {
            return null;
        }

        todo.Status = TodoStatus.Cancelled;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Cancelled TodoItem {Id} for user {UserId}", id, userId);
        return todo;
    }

    public async Task<TodoItem?> DeferTodoAsync(int id, string userId, DateTime newDueDate)
    {
        var todo = await _context.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            
        if (todo == null)
        {
            return null;
        }

        todo.Status = TodoStatus.Deferred;
        todo.DueDate = newDueDate;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deferred TodoItem {Id} for user {UserId} to {NewDueDate}", 
            id, userId, newDueDate);
        return todo;
    }

    #endregion

    #region Recurrence Management

    public async Task<TodoItem?> CreateRecurringTodoAsync(TodoItem todo, RecurrenceSchedule schedule)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // First save the recurrence schedule
            _context.RecurrenceSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            // Now create the todo with the schedule ID
            todo.RecurrenceScheduleId = schedule.Id;
            _context.TodoItems.Add(todo);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            
            _logger.LogInformation("Created recurring TodoItem {Id} with schedule {ScheduleId}", 
                todo.Id, schedule.Id);
            return todo;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating recurring todo");
            throw;
        }
    }

    public async Task<TodoItem?> UpdateRecurrenceScheduleAsync(int todoId, RecurrenceSchedule schedule, string userId)
    {
        var todo = await _context.TodoItems
            .Include(t => t.RecurrenceSchedule)
            .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);
            
        if (todo == null)
        {
            return null;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (todo.RecurrenceSchedule == null)
            {
                // Create a new schedule
                _context.RecurrenceSchedules.Add(schedule);
                await _context.SaveChangesAsync();
                
                todo.RecurrenceScheduleId = schedule.Id;
            }
            else
            {
                // Update existing schedule
                todo.RecurrenceSchedule.Type = schedule.Type;
                todo.RecurrenceSchedule.Interval = schedule.Interval;
                todo.RecurrenceSchedule.DaysOfWeek = schedule.DaysOfWeek;
                todo.RecurrenceSchedule.MonthlyDay = schedule.MonthlyDay;
                todo.RecurrenceSchedule.StartDate = schedule.StartDate;
                todo.RecurrenceSchedule.EndDate = schedule.EndDate;
                todo.RecurrenceSchedule.MaxOccurrences = schedule.MaxOccurrences;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation("Updated recurrence schedule for TodoItem {Id}", todoId);
            return todo;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating recurrence schedule");
            throw;
        }
    }

    public async Task<TodoItem?> GenerateNextRecurringTodoAsync(int completedTodoId, string userId)
    {
        var completedTodo = await _context.TodoItems
            .Include(t => t.RecurrenceSchedule)
            .FirstOrDefaultAsync(t => t.Id == completedTodoId && t.UserId == userId);
            
        if (completedTodo == null || completedTodo.RecurrenceSchedule == null)
        {
            return null;
        }

        // Check if recurrence has ended
        if (completedTodo.RecurrenceSchedule.EndDate.HasValue && 
            completedTodo.RecurrenceSchedule.EndDate.Value < DateTime.UtcNow)
        {
            _logger.LogInformation("Recurrence has ended for TodoItem {Id}", completedTodoId);
            return null;
        }

        // Calculate next occurrence date
        var baseDate = completedTodo.DueDate ?? completedTodo.CompletedAt ?? DateTime.UtcNow;
        var nextDueDate = completedTodo.RecurrenceSchedule.CalculateNextOccurrence(baseDate);

        if (!nextDueDate.HasValue)
        {
            _logger.LogWarning("Could not calculate next occurrence for TodoItem {Id}", completedTodoId);
            return null;
        }

        // Create the next todo item
        var nextTodo = new TodoItem
        {
            Title = completedTodo.Title,
            Description = completedTodo.Description,
            Status = TodoStatus.NotStarted,
            Priority = completedTodo.Priority,
            DueDate = nextDueDate,
            UserId = completedTodo.UserId,
            CategoryId = completedTodo.CategoryId,
            RecurrenceScheduleId = completedTodo.RecurrenceScheduleId,
            EstimatedMinutes = completedTodo.EstimatedMinutes,
            ParentTodoId = completedTodo.Id,
            GenerateNextOnComplete = completedTodo.GenerateNextOnComplete
        };

        _context.TodoItems.Add(nextTodo);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Generated next recurring TodoItem {NewId} from completed TodoItem {OldId}", 
            nextTodo.Id, completedTodoId);
        return nextTodo;
    }

    #endregion

    #region Category Management

    public async Task<IEnumerable<TodoCategory>> GetCategoriesAsync(string userId)
    {
        return await _context.TodoCategories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<TodoCategory> CreateCategoryAsync(TodoCategory category)
    {
        _context.TodoCategories.Add(category);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created TodoCategory {Id} for user {UserId}", category.Id, category.UserId);
        return category;
    }

    public async Task<TodoCategory?> UpdateCategoryAsync(TodoCategory category)
    {
        var existingCategory = await _context.TodoCategories
            .FirstOrDefaultAsync(c => c.Id == category.Id && c.UserId == category.UserId);
            
        if (existingCategory == null)
        {
            return null;
        }

        existingCategory.Name = category.Name;
        existingCategory.Color = category.Color;
        existingCategory.Icon = category.Icon;
        existingCategory.Description = category.Description;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated TodoCategory {Id} for user {UserId}", category.Id, category.UserId);
        
        return existingCategory;
    }

    public async Task<bool> DeleteCategoryAsync(int id, string userId)
    {
        var category = await _context.TodoCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            
        if (category == null)
        {
            return false;
        }

        // Update all tasks using this category to have no category
        var tasksWithCategory = await _context.TodoItems
            .Where(t => t.CategoryId == id && t.UserId == userId)
            .ToListAsync();
            
        foreach (var task in tasksWithCategory)
        {
            task.CategoryId = null;
        }

        _context.TodoCategories.Remove(category);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted TodoCategory {Id} for user {UserId}", id, userId);
        return true;
    }

    #endregion

    #region Analytics

    public async Task<int> GetCompletedCountAsync(string userId, DateTime since)
    {
        return await _context.TodoItems
            .CountAsync(t => t.UserId == userId && 
                          t.Status == TodoStatus.Completed && 
                          t.CompletedAt.HasValue && 
                          t.CompletedAt >= since);
    }

    public async Task<int> GetCreatedCountAsync(string userId, DateTime since)
    {
        return await _context.TodoItems
            .CountAsync(t => t.UserId == userId && t.CreatedAt >= since);
    }

    public async Task<double> GetCompletionRateAsync(string userId, DateTime since)
    {
        var created = await GetCreatedCountAsync(userId, since);
        var completed = await GetCompletedCountAsync(userId, since);
        
        return created > 0 ? (double)completed / created : 0;
    }

    public async Task<IDictionary<TodoStatus, int>> GetStatusDistributionAsync(string userId)
    {
        return await _context.TodoItems
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count);
    }

    public async Task<IDictionary<TodoCategory, int>> GetCategoryDistributionAsync(string userId)
    {
        var results = await _context.TodoItems
            .Where(t => t.UserId == userId && t.CategoryId != null)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key!.Value, Count = g.Count() })
            .ToListAsync();

        var categories = await _context.TodoCategories
            .Where(c => c.UserId == userId)
            .ToListAsync();

        return results
            .Join(categories, r => r.CategoryId, c => c.Id, (r, c) => new { Category = c, Count = r.Count })
            .ToDictionary(x => x.Category, x => x.Count);
    }

    #endregion
} 