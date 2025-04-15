using System.ComponentModel.DataAnnotations;

namespace ProductiveMachine.WebApp.Models;

public class RecurrenceSchedule
{
    public int Id { get; set; }
    
    public RecurrenceType Type { get; set; } = RecurrenceType.Daily;
    
    public int Interval { get; set; } = 1; // Every X days/weeks/months
    
    [StringLength(50)]
    public string? DaysOfWeek { get; set; } // Comma-separated days of week (0=Sunday, 1=Monday, etc.)
    
    [StringLength(10)]
    public string? MonthlyDay { get; set; } // Day of month (1-31) or "last"
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public int? MaxOccurrences { get; set; }
    
    // Helper method to parse days of week
    public List<DayOfWeek> GetDaysOfWeek()
    {
        var result = new List<DayOfWeek>();
        if (string.IsNullOrEmpty(DaysOfWeek))
            return result;
            
        foreach (var dayStr in DaysOfWeek.Split(','))
        {
            if (int.TryParse(dayStr, out int day) && day >= 0 && day <= 6)
            {
                result.Add((DayOfWeek)day);
            }
        }
        
        return result;
    }
    
    // Helper method to set days of week
    public void SetDaysOfWeek(IEnumerable<DayOfWeek> days)
    {
        DaysOfWeek = string.Join(",", days.Select(d => (int)d));
    }
    
    // Helper method to calculate the next occurrence from a given date
    public DateTime? CalculateNextOccurrence(DateTime fromDate)
    {
        DateTime baseDate = fromDate.Date;
        
        switch (Type)
        {
            case RecurrenceType.Daily:
                return baseDate.AddDays(Interval);
                
            case RecurrenceType.Weekly:
                var daysOfWeek = GetDaysOfWeek();
                if (!daysOfWeek.Any())
                    return baseDate.AddDays(7 * Interval);
                    
                // Find the next matching day of week
                for (int i = 1; i <= 7; i++)
                {
                    var nextDay = baseDate.AddDays(i);
                    if (daysOfWeek.Contains(nextDay.DayOfWeek))
                    {
                        return nextDay;
                    }
                }
                
                // If no day found in the next week, jump to the next interval
                var firstDayNextWeek = baseDate.AddDays(7 * Interval);
                for (int i = 0; i < 7; i++)
                {
                    var checkDay = firstDayNextWeek.AddDays(i);
                    if (daysOfWeek.Contains(checkDay.DayOfWeek))
                    {
                        return checkDay;
                    }
                }
                break;
                
            case RecurrenceType.Monthly:
                // Handle "last day of month" case
                if (MonthlyDay == "last")
                {
                    return new DateTime(
                        baseDate.AddMonths(Interval).Year, 
                        baseDate.AddMonths(Interval).Month, 
                        DateTime.DaysInMonth(
                            baseDate.AddMonths(Interval).Year, 
                            baseDate.AddMonths(Interval).Month));
                }
                
                // Handle specific day of month
                if (int.TryParse(MonthlyDay, out int dayOfMonth) && dayOfMonth >= 1 && dayOfMonth <= 31)
                {
                    var nextMonth = baseDate.AddMonths(Interval);
                    var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                    
                    // Adjust day if it exceeds the days in the month
                    var actualDay = Math.Min(dayOfMonth, daysInMonth);
                    return new DateTime(nextMonth.Year, nextMonth.Month, actualDay);
                }
                break;
                
            case RecurrenceType.Yearly:
                return baseDate.AddYears(Interval);
        }
        
        // Default fallback
        return baseDate.AddDays(1);
    }
}

public enum RecurrenceType
{
    Daily,
    Weekly,
    Monthly,
    Yearly
} 