using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;
using ProductiveMachine.WebApp.Services;
using Xunit;

namespace ProductiveMachine.Tests.Services;

public class TodoServiceTests
{
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly Mock<ILogger<TodoService>> _loggerMock;
    
    public TodoServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestTodoDb_" + Guid.NewGuid())
            .Options;
            
        _loggerMock = new Mock<ILogger<TodoService>>();
    }
    
    [Fact]
    public async Task CreateTodoAsync_Should_Add_TodoItem_To_Database()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new TodoService(context, _loggerMock.Object);
        
        var userId = "user-123";
        var todo = new TodoItem
        {
            Title = "Test Todo",
            Description = "Test Description",
            UserId = userId,
            Priority = TodoPriority.Medium
        };
        
        // Act
        var result = await service.CreateTodoAsync(todo);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Todo", result.Title);
        Assert.Equal(userId, result.UserId);
        
        // Verify it's in the database
        var todoFromDb = await context.TodoItems.FindAsync(result.Id);
        Assert.NotNull(todoFromDb);
        Assert.Equal("Test Todo", todoFromDb.Title);
    }
    
    [Fact]
    public async Task GetTodoItemsAsync_Should_Return_User_Todos()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new TodoService(context, _loggerMock.Object);
        
        var user1Id = "user-1";
        var user2Id = "user-2";
        
        await context.TodoItems.AddRangeAsync(
            new TodoItem { Title = "User 1 Todo 1", UserId = user1Id },
            new TodoItem { Title = "User 1 Todo 2", UserId = user1Id },
            new TodoItem { Title = "User 2 Todo", UserId = user2Id }
        );
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.GetTodoItemsAsync(user1Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, todo => Assert.Equal(user1Id, todo.UserId));
    }
    
    [Fact]
    public async Task CompleteTodoAsync_Should_Update_Status_And_Add_CompletionDate()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new TodoService(context, _loggerMock.Object);
        
        var userId = "user-123";
        var todo = new TodoItem
        {
            Title = "Test Todo",
            Description = "Test Description",
            UserId = userId,
            Status = TodoStatus.NotStarted
        };
        
        context.TodoItems.Add(todo);
        await context.SaveChangesAsync();
        
        // Act
        var completedTodo = await service.CompleteTodoAsync(todo.Id, userId);
        
        // Assert
        Assert.NotNull(completedTodo);
        Assert.Equal(TodoStatus.Completed, completedTodo.Status);
        Assert.NotNull(completedTodo.CompletedAt);
    }
    
    [Fact]
    public async Task GetOverdueAsync_Should_Return_Only_Overdue_Tasks()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new TodoService(context, _loggerMock.Object);
        
        var userId = "user-123";
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var tomorrow = DateTime.UtcNow.AddDays(1);
        
        await context.TodoItems.AddRangeAsync(
            new TodoItem { Title = "Overdue Todo", UserId = userId, DueDate = yesterday },
            new TodoItem { Title = "Future Todo", UserId = userId, DueDate = tomorrow },
            new TodoItem { Title = "No Due Date Todo", UserId = userId, DueDate = null }
        );
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.GetOverdueAsync(userId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Overdue Todo", result.First().Title);
    }
    
    [Fact]
    public async Task CreateRecurringTodoAsync_Should_Create_Todo_With_RecurrenceSchedule()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new TodoService(context, _loggerMock.Object);
        
        var userId = "user-123";
        var todo = new TodoItem
        {
            Title = "Recurring Todo",
            UserId = userId
        };
        
        var schedule = new RecurrenceSchedule
        {
            Type = RecurrenceType.Weekly,
            Interval = 1,
            DaysOfWeek = "1,3,5" // Monday, Wednesday, Friday
        };
        
        // Act
        var result = await service.CreateRecurringTodoAsync(todo, schedule);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Recurring Todo", result.Title);
        Assert.NotNull(result.RecurrenceScheduleId);
        
        // Verify the schedule was created
        var scheduleFromDb = await context.RecurrenceSchedules.FindAsync(result.RecurrenceScheduleId);
        Assert.NotNull(scheduleFromDb);
        Assert.Equal(RecurrenceType.Weekly, scheduleFromDb.Type);
        Assert.Equal("1,3,5", scheduleFromDb.DaysOfWeek);
    }
} 