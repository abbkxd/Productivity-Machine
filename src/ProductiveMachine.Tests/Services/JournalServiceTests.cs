using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;
using ProductiveMachine.WebApp.Services;
using Xunit;

namespace ProductiveMachine.Tests.Services;

public class JournalServiceTests
{
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly Mock<ILogger<JournalService>> _loggerMock;
    
    public JournalServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestJournalDb_" + Guid.NewGuid())
            .Options;
            
        _loggerMock = new Mock<ILogger<JournalService>>();
    }
    
    [Fact]
    public async Task CreateJournalEntryAsync_Should_Add_Entry_To_Database()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new JournalService(context, _loggerMock.Object);
        
        var userId = "user-123";
        var entry = new JournalEntry
        {
            Title = "Test Journal Entry",
            Content = "This is a test journal entry.",
            UserId = userId,
            Tags = "test,journal,entry"
        };
        
        // Act
        var result = await service.CreateJournalEntryAsync(entry);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Journal Entry", result.Title);
        Assert.Equal(userId, result.UserId);
        
        // Verify it's in the database
        var entryFromDb = await context.JournalEntries.FindAsync(result.Id);
        Assert.NotNull(entryFromDb);
        Assert.Equal("Test Journal Entry", entryFromDb.Title);
    }
    
    [Fact]
    public async Task GetJournalEntriesAsync_Should_Return_User_Entries_In_Descending_Order()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new JournalService(context, _loggerMock.Object);
        
        var userId = "user-123";
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
        
        await context.JournalEntries.AddRangeAsync(
            new JournalEntry { Title = "Newest Entry", Content = "Test", UserId = userId, CreatedAt = DateTime.UtcNow },
            new JournalEntry { Title = "Yesterday Entry", Content = "Test", UserId = userId, CreatedAt = yesterday },
            new JournalEntry { Title = "Oldest Entry", Content = "Test", UserId = userId, CreatedAt = twoDaysAgo }
        );
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.GetJournalEntriesAsync(userId, 10, 0);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        
        // Should be in descending order by CreatedAt
        var resultList = result.ToList();
        Assert.Equal("Newest Entry", resultList[0].Title);
        Assert.Equal("Yesterday Entry", resultList[1].Title);
        Assert.Equal("Oldest Entry", resultList[2].Title);
    }
    
    [Fact]
    public async Task GetJournalEntriesByDateRangeAsync_Should_Return_Entries_Within_Range()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new JournalService(context, _loggerMock.Object);
        
        var userId = "user-123";
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var twoDaysAgo = today.AddDays(-2);
        var threeDaysAgo = today.AddDays(-3);
        
        await context.JournalEntries.AddRangeAsync(
            new JournalEntry { Title = "Today Entry", Content = "Test", UserId = userId, CreatedAt = today.AddHours(12) },
            new JournalEntry { Title = "Yesterday Entry", Content = "Test", UserId = userId, CreatedAt = yesterday.AddHours(12) },
            new JournalEntry { Title = "Two Days Ago Entry", Content = "Test", UserId = userId, CreatedAt = twoDaysAgo.AddHours(12) },
            new JournalEntry { Title = "Three Days Ago Entry", Content = "Test", UserId = userId, CreatedAt = threeDaysAgo.AddHours(12) }
        );
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.GetJournalEntriesByDateRangeAsync(userId, yesterday, today);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        
        var titles = result.Select(e => e.Title).ToList();
        Assert.Contains("Today Entry", titles);
        Assert.Contains("Yesterday Entry", titles);
        Assert.DoesNotContain("Two Days Ago Entry", titles);
        Assert.DoesNotContain("Three Days Ago Entry", titles);
    }
    
    [Fact]
    public async Task SearchJournalEntriesAsync_Should_Find_Entries_Matching_Search_Term()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new JournalService(context, _loggerMock.Object);
        
        var userId = "user-123";
        
        await context.JournalEntries.AddRangeAsync(
            new JournalEntry { Title = "Meeting Notes", Content = "Discussed project deadlines", UserId = userId },
            new JournalEntry { Title = "Shopping List", Content = "Buy milk and bread", UserId = userId },
            new JournalEntry { Title = "Project Ideas", Content = "New app concept", UserId = userId, Tags = "project,ideas,app" }
        );
        await context.SaveChangesAsync();
        
        // Act
        var result1 = await service.SearchJournalEntriesAsync(userId, "project");
        var result2 = await service.SearchJournalEntriesAsync(userId, "milk");
        var result3 = await service.SearchJournalEntriesAsync(userId, "ideas");
        
        // Assert
        Assert.Equal(2, result1.Count()); // Should find "Project Ideas" title and "Discussed project deadlines" content
        Assert.Single(result2); // Should find "Shopping List" by content
        Assert.Single(result3); // Should find "Project Ideas" by tag
    }
    
    [Fact]
    public async Task GetAllTagsAsync_Should_Return_Unique_Tags()
    {
        // Arrange
        using var context = new ApplicationDbContext(_dbContextOptions);
        var service = new JournalService(context, _loggerMock.Object);
        
        var userId = "user-123";
        
        await context.JournalEntries.AddRangeAsync(
            new JournalEntry { Title = "Entry 1", Content = "Test", UserId = userId, Tags = "tag1,tag2,tag3" },
            new JournalEntry { Title = "Entry 2", Content = "Test", UserId = userId, Tags = "tag2,tag4,tag5" },
            new JournalEntry { Title = "Entry 3", Content = "Test", UserId = userId, Tags = "tag1,tag5,tag6" }
        );
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.GetAllTagsAsync(userId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.Count());
        
        var tags = result.ToHashSet();
        Assert.Contains("tag1", tags);
        Assert.Contains("tag2", tags);
        Assert.Contains("tag3", tags);
        Assert.Contains("tag4", tags);
        Assert.Contains("tag5", tags);
        Assert.Contains("tag6", tags);
    }
} 