using Microsoft.EntityFrameworkCore;
using PrisonManagementSystem.Data;
using PrisonManagementSystem.Models;

namespace PrisonManagementSystem.Services;

public class DashboardSummaryDto
{
    public bool IsDatabaseAvailable { get; set; } = true;
    public string? DatabaseErrorMessage { get; set; }

    public int PrisonersInCustody { get; set; }
    public int ActiveStaff { get; set; }
    public int HearingsToday { get; set; }
    public int VisitorsInside { get; set; }

    public List<UpcomingHearingDto> UpcomingHearings { get; set; } = new();
    public List<BarrackSummaryDto> BarracksAndCells { get; set; } = new();
    public List<TimetableActivityDto> DailyActivities { get; set; } = new();
    public List<GateActivityDto> RecentGateActivity { get; set; } = new();
}

public class UpcomingHearingDto
{
    public string PrisonerName { get; set; } = string.Empty;
    public string CaseReference { get; set; } = string.Empty;
    public DateTime HearingDate { get; set; }
    public string HearingType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class BarrackSummaryDto
{
    public string BarrackName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TotalCells { get; set; }
    public int TotalCapacity { get; set; }
    public int OccupiedCount { get; set; }
}

public class TimetableActivityDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
}

public class GateActivityDto
{
    public DateTime Time { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardDataAsync(CancellationToken cancellationToken = default);
}

public class DashboardService : IDashboardService
{
    private readonly PrisonDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(PrisonDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        var result = new DashboardSummaryDto();

        try
        {
            // Verify connection
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                result.IsDatabaseAvailable = false;
                result.DatabaseErrorMessage = "Database connection unavailable. Please check that the database service is running and accessible.";
                return result;
            }

            // 1. Prisoners in custody (strictly count from Prisoners table without joins)
            result.PrisonersInCustody = await _context.Prisoners
                .AsNoTracking()
                .Where(p => p.CustodyStatus == "In Custody")
                .CountAsync(cancellationToken);

            // 2. Active staff
            result.ActiveStaff = await _context.Staff
                .AsNoTracking()
                .Where(s => s.IsActive)
                .CountAsync(cancellationToken);

            // 3. Hearings today (using Asia/Karachi operational date safely)
            DateTime karachiTime;
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");
                karachiTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                karachiTime = DateTime.UtcNow.AddHours(5);
            }

            var todayStart = karachiTime.Date;
            var todayEnd = todayStart.AddDays(1);

            result.HearingsToday = await _context.CourtTrials
                .AsNoTracking()
                .Where(t => t.HearingAt >= todayStart && t.HearingAt < todayEnd)
                .CountAsync(cancellationToken);

            // 4. Visitors currently inside: SUM of NumberOfPeople for open gate entries (ExitTime IS NULL)
            result.VisitorsInside = await _context.GateRegister
                .AsNoTracking()
                .Where(g => g.ExitTime == null)
                .SumAsync(g => (int?)g.NumberOfPeople, cancellationToken) ?? 0;

            // 5. Upcoming court hearings
            var upcoming = await _context.CourtTrials
                .AsNoTracking()
                .Include(t => t.Case)
                    .ThenInclude(c => c.Prisoner)
                .Where(t => t.HearingAt >= todayStart)
                .OrderBy(t => t.HearingAt)
                .Take(5)
                .ToListAsync(cancellationToken);

            result.UpcomingHearings = upcoming.Select(t => new UpcomingHearingDto
            {
                PrisonerName = t.Case?.Prisoner?.FullName ?? "Unknown",
                CaseReference = t.Case?.CaseNumber ?? "Pending",
                HearingDate = t.HearingAt,
                HearingType = t.HearingType,
                Status = t.HearingStatus
            }).ToList();

            // 6. Barracks & cells summary - fully compatible with SQLite & SQL Server
            var barracks = await _context.Barracks
                .AsNoTracking()
                .Where(b => b.IsActive)
                .Include(b => b.Cells)
                .OrderBy(b => b.BarrackName)
                .ToListAsync(cancellationToken);

            // Count occupied prisoners grouped by Cell's BarrackID
            var occupiedCounts = await _context.Prisoners
                .AsNoTracking()
                .Where(p => p.CustodyStatus == "In Custody" && p.Cell != null)
                .GroupBy(p => p.Cell!.BarrackID)
                .Select(g => new { BarrackID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.BarrackID, g => g.Count, cancellationToken);

            result.BarracksAndCells = barracks.Select(b => new BarrackSummaryDto
            {
                BarrackName = b.BarrackName,
                Category = b.Category,
                TotalCells = b.Cells.Count(c => c.IsActive),
                TotalCapacity = b.Cells.Where(c => c.IsActive).Sum(c => c.Capacity),
                OccupiedCount = occupiedCounts.TryGetValue(b.BarrackID, out var count) ? count : 0
            }).ToList();

            // 7. Daily timetable activities
            int currentDayOfWeek = (int)karachiTime.DayOfWeek;
            byte convertedDay = (byte)(currentDayOfWeek == 0 ? 7 : currentDayOfWeek);

            var activities = await _context.JailTimetable
                .AsNoTracking()
                .Include(t => t.Barrack)
                .Where(t => t.IsActive && (t.DayOfWeek == null || t.DayOfWeek == convertedDay))
                .ToListAsync(cancellationToken);

            result.DailyActivities = activities
                .OrderBy(t => t.StartTime)
                .Take(5)
                .Select(t => new TimetableActivityDto
                {
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    ActivityName = t.ActivityName,
                    Location = t.Barrack != null ? t.Barrack.BarrackName : "Jail-wide",
                    Status = "Scheduled"
                })
                .ToList();

            // 8. Recent Gate activity
            result.RecentGateActivity = await _context.GateRegister
                .AsNoTracking()
                .OrderByDescending(g => g.EntryTime)
                .Take(5)
                .Select(g => new GateActivityDto
                {
                    Time = g.EntryTime,
                    Type = g.VisitType,
                    Details = $"{g.VisitorName} ({g.NumberOfPeople} {(g.NumberOfPeople == 1 ? "person" : "people")})",
                    Status = g.ExitTime == null ? "Inside" : "Departed"
                })
                .ToListAsync(cancellationToken);

            result.IsDatabaseAvailable = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dashboard data from database.");
            result.IsDatabaseAvailable = false;
            result.DatabaseErrorMessage = ex.Message;
        }

        return result;
    }
}
