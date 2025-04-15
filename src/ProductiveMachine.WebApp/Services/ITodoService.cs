using ProductiveMachine.WebApp.Models;

namespace ProductiveMachine.WebApp.Services;

public interface ITodoService
{
    // Basic CRUD operations
    Task<TodoItem?> GetTodoByIdAsync(int id, string userId);
    Task<IEnumerable<TodoItem>> GetTodoItemsAsync(string userId, bool includeCompleted = false);
    Task<IEnumerable<TodoItem>> GetTodoItemsForCategoryAsync(int categoryId, string userId);
    Task<IEnumerable<TodoItem>> GetDueTodayAsync(string userId);
    Task<IEnumerable<TodoItem>> GetOverdueAsync(string userId);
    Task<TodoItem> CreateTodoAsync(TodoItem todo);
    Task<TodoItem?> UpdateTodoAsync(TodoItem todo);
    Task<bool> DeleteTodoAsync(int id, string userId);
    
    // Status management
    Task<TodoItem?> CompleteTodoAsync(int id, string userId, int? actualMinutes = null);
    Task<TodoItem?> StartTodoAsync(int id, string userId);
    Task<TodoItem?> CancelTodoAsync(int id, string userId);
    Task<TodoItem?> DeferTodoAsync(int id, string userId, DateTime newDueDate);
    
    // Recurrence management
    Task<TodoItem?> CreateRecurringTodoAsync(TodoItem todo, RecurrenceSchedule schedule);
    Task<TodoItem?> UpdateRecurrenceScheduleAsync(int todoId, RecurrenceSchedule schedule, string userId);
    Task<TodoItem?> GenerateNextRecurringTodoAsync(int completedTodoId, string userId);
    
    // Category management
    Task<IEnumerable<TodoCategory>> GetCategoriesAsync(string userId);
    Task<TodoCategory> CreateCategoryAsync(TodoCategory category);
    Task<TodoCategory?> UpdateCategoryAsync(TodoCategory category);
    Task<bool> DeleteCategoryAsync(int id, string userId);
    
    // Analytics
    Task<int> GetCompletedCountAsync(string userId, DateTime since);
    Task<int> GetCreatedCountAsync(string userId, DateTime since);
    Task<double> GetCompletionRateAsync(string userId, DateTime since);
    Task<IDictionary<TodoStatus, int>> GetStatusDistributionAsync(string userId);
    Task<IDictionary<TodoCategory, int>> GetCategoryDistributionAsync(string userId);
} 