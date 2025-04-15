using ProductiveMachine.WebApp.Models;

namespace ProductiveMachine.WebApp.Services;

public interface IJournalService
{
    // Basic CRUD operations
    Task<JournalEntry?> GetJournalEntryByIdAsync(int id, string userId);
    Task<IEnumerable<JournalEntry>> GetJournalEntriesAsync(string userId, int count = 10, int skip = 0);
    Task<IEnumerable<JournalEntry>> GetJournalEntriesByDateAsync(string userId, DateTime date);
    Task<IEnumerable<JournalEntry>> GetJournalEntriesByDateRangeAsync(string userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<JournalEntry>> SearchJournalEntriesAsync(string userId, string searchTerm);
    Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry);
    Task<JournalEntry?> UpdateJournalEntryAsync(JournalEntry entry);
    Task<bool> DeleteJournalEntryAsync(int id, string userId);
    
    // Tag operations
    Task<IEnumerable<string>> GetAllTagsAsync(string userId);
    Task<IEnumerable<JournalEntry>> GetJournalEntriesByTagAsync(string userId, string tag);
    
    // Analytics
    Task<int> GetJournalCountAsync(string userId, DateTime since);
    Task<IDictionary<DateTime, int>> GetJournalEntriesCountByDayAsync(string userId, DateTime startDate, DateTime endDate);
    Task<IDictionary<JournalMood, int>> GetMoodDistributionAsync(string userId, DateTime startDate, DateTime endDate);
} 