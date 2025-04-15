using Microsoft.EntityFrameworkCore;
using ProductiveMachine.WebApp.Data;
using ProductiveMachine.WebApp.Models;

namespace ProductiveMachine.WebApp.Services;

public class JournalService : IJournalService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<JournalService> _logger;

    public JournalService(ApplicationDbContext context, ILogger<JournalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Basic CRUD Operations

    public async Task<JournalEntry?> GetJournalEntryByIdAsync(int id, string userId)
    {
        return await _context.JournalEntries
            .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);
    }

    public async Task<IEnumerable<JournalEntry>> GetJournalEntriesAsync(string userId, int count = 10, int skip = 0)
    {
        return await _context.JournalEntries
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .Skip(skip)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<JournalEntry>> GetJournalEntriesByDateAsync(string userId, DateTime date)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        return await _context.JournalEntries
            .Where(j => j.UserId == userId && 
                      j.CreatedAt >= startDate && 
                      j.CreatedAt < endDate)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<JournalEntry>> GetJournalEntriesByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
    {
        return await _context.JournalEntries
            .Where(j => j.UserId == userId && 
                      j.CreatedAt >= startDate.Date && 
                      j.CreatedAt < endDate.Date.AddDays(1))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<JournalEntry>> SearchJournalEntriesAsync(string userId, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Array.Empty<JournalEntry>();
        }

        searchTerm = searchTerm.ToLower();

        return await _context.JournalEntries
            .Where(j => j.UserId == userId && 
                      (j.Title.ToLower().Contains(searchTerm) || 
                       j.Content.ToLower().Contains(searchTerm) || 
                       j.Tags!.ToLower().Contains(searchTerm)))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry)
    {
        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created JournalEntry {Id} for user {UserId}", entry.Id, entry.UserId);
        return entry;
    }

    public async Task<JournalEntry?> UpdateJournalEntryAsync(JournalEntry entry)
    {
        var existingEntry = await _context.JournalEntries
            .FirstOrDefaultAsync(j => j.Id == entry.Id && j.UserId == entry.UserId);
            
        if (existingEntry == null)
        {
            return null;
        }

        existingEntry.Title = entry.Title;
        existingEntry.Content = entry.Content;
        existingEntry.Tags = entry.Tags;
        existingEntry.Mood = entry.Mood;
        existingEntry.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated JournalEntry {Id} for user {UserId}", entry.Id, entry.UserId);
        
        return existingEntry;
    }

    public async Task<bool> DeleteJournalEntryAsync(int id, string userId)
    {
        var entry = await _context.JournalEntries
            .FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);
            
        if (entry == null)
        {
            return false;
        }

        _context.JournalEntries.Remove(entry);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted JournalEntry {Id} for user {UserId}", id, userId);
        return true;
    }

    #endregion

    #region Tag Operations

    public async Task<IEnumerable<string>> GetAllTagsAsync(string userId)
    {
        var allTags = new HashSet<string>();
        
        var entriesWithTags = await _context.JournalEntries
            .Where(j => j.UserId == userId && !string.IsNullOrEmpty(j.Tags))
            .Select(j => j.Tags)
            .ToListAsync();
            
        foreach (var tagStr in entriesWithTags)
        {
            if (string.IsNullOrEmpty(tagStr))
                continue;
                
            var tags = tagStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim());
                
            foreach (var tag in tags)
            {
                allTags.Add(tag);
            }
        }
        
        return allTags.OrderBy(t => t);
    }

    public async Task<IEnumerable<JournalEntry>> GetJournalEntriesByTagAsync(string userId, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return Array.Empty<JournalEntry>();
        }

        tag = tag.Trim();
        
        // This is a simple implementation. For better performance in a production environment,
        // consider using a separate Tags table with a many-to-many relationship
        return await _context.JournalEntries
            .Where(j => j.UserId == userId && 
                      !string.IsNullOrEmpty(j.Tags) &&
                      (j.Tags.Contains($"{tag},") || 
                       j.Tags.Contains($",{tag}") || 
                       j.Tags == tag || 
                       j.Tags.EndsWith($",{tag}")))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    #endregion

    #region Analytics

    public async Task<int> GetJournalCountAsync(string userId, DateTime since)
    {
        return await _context.JournalEntries
            .CountAsync(j => j.UserId == userId && j.CreatedAt >= since);
    }

    public async Task<IDictionary<DateTime, int>> GetJournalEntriesCountByDayAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var entriesByDay = await _context.JournalEntries
            .Where(j => j.UserId == userId && 
                      j.CreatedAt >= startDate.Date && 
                      j.CreatedAt < endDate.Date.AddDays(1))
            .GroupBy(j => j.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Date, g => g.Count);
            
        // Fill in missing days with zero entries
        var result = new Dictionary<DateTime, int>();
        for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
        {
            result[day] = entriesByDay.ContainsKey(day) ? entriesByDay[day] : 0;
        }
        
        return result;
    }

    public async Task<IDictionary<JournalMood, int>> GetMoodDistributionAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var moodCounts = await _context.JournalEntries
            .Where(j => j.UserId == userId && 
                      j.CreatedAt >= startDate.Date && 
                      j.CreatedAt < endDate.Date.AddDays(1) &&
                      j.Mood.HasValue)
            .GroupBy(j => j.Mood!.Value)
            .Select(g => new { Mood = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Mood, g => g.Count);
            
        // Initialize all mood values
        var result = new Dictionary<JournalMood, int>
        {
            { JournalMood.VeryNegative, 0 },
            { JournalMood.Negative, 0 },
            { JournalMood.Neutral, 0 },
            { JournalMood.Positive, 0 },
            { JournalMood.VeryPositive, 0 }
        };
        
        // Update with actual counts
        foreach (var mood in moodCounts.Keys)
        {
            result[mood] = moodCounts[mood];
        }
        
        return result;
    }

    #endregion
} 