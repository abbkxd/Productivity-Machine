using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductiveMachine.Jobs;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;
using ProductiveMachine.WebApp.Services;
using Xunit;

namespace ProductiveMachine.Tests.Jobs;

public class TaskMaintenanceJobTests
{
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly Mock<ITodoService> _todoServiceMock;
    private readonly Mock<ILogger<TaskMaintenanceJob>> _loggerMock;
    
    public TaskMaintenanceJobTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestTaskMaintenanceDb_" + Guid.NewGuid())
            .Options;
            
        _todoServiceMock = new Mock<ITodoService>();
        _loggerMock = new Mock<ILogger<TaskMaintenanceJob>>();
    }
    
    [Fact]
    public async Task ProcessTasksAsync_Should_Complete_Due_Recurring_Tasks()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var job = new TaskMaintenanceJob(context, _todoServiceMock.Object, _loggerMock.Object);
        
        var userId = "user-123";
        var today = DateTime.UtcNow.Date;
        
        // Create a recurrence schedule
        var schedule = new RecurrenceSchedule
        {
            Type = RecurrenceType.Daily,
            Interval = 1
        };
        context.RecurrenceSchedules.Add(schedule);
        await context.SaveChangesAsync();
        
        // Create a recurring task that is due today
        var recurringTask = new TodoItem
        {
            Title = "Recurring Task",
            UserId = userId,
            Status = TodoStatus.NotStarted,
            DueDate = today,
            RecurrenceScheduleId = schedule.Id
        };
        context.TodoItems.Add(recurringTask);
        await context.SaveChangesAsync();
        
        // Setup the todo service to return successfully when completing the task
        _todoServiceMock
            .Setup(x => x.CompleteTodoAsync(recurringTask.Id, userId))
            .ReturnsAsync(new TodoItem { 
                Id = recurringTask.Id, 
                Title = recurringTask.Title, 
                UserId = userId, 
                Status = TodoStatus.Completed,
                CompletedAt = DateTime.UtcNow
            });
        
        // Act
        await job.ProcessTasksAsync();
        
        // Assert
        // Verify that the service was called to complete the task
        _todoServiceMock.Verify(x => x.CompleteTodoAsync(recurringTask.Id, userId), Times.Once);
    }
    
    [Fact]
    public async Task ProcessTasksAsync_Should_Skip_Tasks_With_No_RecurrenceSchedule()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var job = new TaskMaintenanceJob(context, _todoServiceMock.Object, _loggerMock.Object);
        
        var userId = "user-123";
        var today = DateTime.UtcNow.Date;
        
        // Create a non-recurring task that is due today
        var nonRecurringTask = new TodoItem
        {
            Title = "Non-Recurring Task",
            UserId = userId,
            Status = TodoStatus.NotStarted,
            DueDate = today,
            RecurrenceScheduleId = null
        };
        context.TodoItems.Add(nonRecurringTask);
        await context.SaveChangesAsync();
        
        // Act
        await job.ProcessTasksAsync();
        
        // Assert
        // Verify that the service was not called to complete the task
        _todoServiceMock.Verify(x => x.CompleteTodoAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task ProcessTasksAsync_Should_Handle_Exceptions()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var job = new TaskMaintenanceJob(context, _todoServiceMock.Object, _loggerMock.Object);
        
        var userId = "user-123";
        var today = DateTime.UtcNow.Date;
        
        // Create a recurrence schedule
        var schedule = new RecurrenceSchedule
        {
            Type = RecurrenceType.Daily,
            Interval = 1
        };
        context.RecurrenceSchedules.Add(schedule);
        await context.SaveChangesAsync();
        
        // Create a recurring task that is due today
        var recurringTask = new TodoItem
        {
            Title = "Recurring Task",
            UserId = userId,
            Status = TodoStatus.NotStarted,
            DueDate = today,
            RecurrenceScheduleId = schedule.Id
        };
        context.TodoItems.Add(recurringTask);
        await context.SaveChangesAsync();
        
        // Setup the todo service to throw an exception
        _todoServiceMock
            .Setup(x => x.CompleteTodoAsync(recurringTask.Id, userId))
            .ThrowsAsync(new Exception("Test exception"));
        
        // Act & Assert - Should not throw an exception
        await job.ProcessTasksAsync();
        
        // Verify that an error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }
} 