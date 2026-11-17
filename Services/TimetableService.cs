using Microsoft.EntityFrameworkCore;
using PrisonManagementSystem.Data;
using PrisonManagementSystem.Models;

namespace PrisonManagementSystem.Services;

public class TimetableResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ConcurrencyConflict { get; set; }
}

public interface ITimetableService
{
    Task<List<JailTimetable>> GetActivitiesAsync(int? dayOfWeek = null, CancellationToken cancellationToken = default);
    Task<JailTimetable?> GetActivityByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Barrack>> GetActiveBarracksAsync(CancellationToken cancellationToken = default);
    Task<List<Staff>> GetActiveStaffAsync(CancellationToken cancellationToken = default);
    Task<TimetableResult> SaveActivityAsync(JailTimetable activity, byte[]? originalRowVersion = null, CancellationToken cancellationToken = default);
    Task<TimetableResult> DeleteActivityAsync(int id, CancellationToken cancellationToken = default);
}

public class TimetableService : ITimetableService
{
    private readonly PrisonDbContext _context;
    private readonly ILogger<TimetableService> _logger;

    public TimetableService(PrisonDbContext context, ILogger<TimetableService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<JailTimetable>> GetActivitiesAsync(int? dayOfWeek = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.JailTimetable
                .AsNoTracking()
                .Include(t => t.Barrack)
                .Include(t => t.ResponsibleStaff)
                .Where(t => t.IsActive);

            if (dayOfWeek.HasValue)
            {
                byte targetDay = (byte)dayOfWeek.Value;
                query = query.Where(t => t.DayOfWeek == null || t.DayOfWeek == targetDay);
            }

            var items = await query.ToListAsync(cancellationToken);
            return items.OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load timetable activities from database.");
            return new List<JailTimetable>();
        }
    }

    public async Task<JailTimetable?> GetActivityByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.JailTimetable
                .Include(t => t.Barrack)
                .Include(t => t.ResponsibleStaff)
                .FirstOrDefaultAsync(t => t.TimetableID == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch activity {Id}.", id);
            return null;
        }
    }

    public async Task<List<Barrack>> GetActiveBarracksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Barracks
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.BarrackName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load active barracks.");
            return new List<Barrack>();
        }
    }

    public async Task<List<Staff>> GetActiveStaffAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Staff
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.FullName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load active staff.");
            return new List<Staff>();
        }
    }

    public async Task<TimetableResult> SaveActivityAsync(JailTimetable activity, byte[]? originalRowVersion = null, CancellationToken cancellationToken = default)
    {
        // Validation: EndTime > StartTime
        if (activity.EndTime <= activity.StartTime)
        {
            return new TimetableResult { Succeeded = false, ErrorMessage = "End time must be later than start time." };
        }

        if (string.IsNullOrWhiteSpace(activity.ActivityName))
        {
            return new TimetableResult { Succeeded = false, ErrorMessage = "Activity name is required." };
        }

        // Validate DayOfWeek range
        if (activity.DayOfWeek.HasValue && (activity.DayOfWeek.Value < 1 || activity.DayOfWeek.Value > 7))
        {
            return new TimetableResult { Succeeded = false, ErrorMessage = "Day must be between Monday (1) and Sunday (7), or empty for Daily." };
        }

        // Validate foreign keys
        if (activity.BarrackID.HasValue)
        {
            var barrackExists = await _context.Barracks.AnyAsync(b => b.BarrackID == activity.BarrackID.Value && b.IsActive, cancellationToken);
            if (!barrackExists)
            {
                return new TimetableResult { Succeeded = false, ErrorMessage = "Selected barrack does not exist or is inactive." };
            }
        }

        if (activity.ResponsibleStaffID.HasValue)
        {
            var staffExists = await _context.Staff.AnyAsync(s => s.StaffID == activity.ResponsibleStaffID.Value && s.IsActive, cancellationToken);
            if (!staffExists)
            {
                return new TimetableResult { Succeeded = false, ErrorMessage = "Selected responsible staff member does not exist or is inactive." };
            }
        }

        try
        {
            if (activity.TimetableID == 0)
            {
                // New activity
                activity.IsActive = true;
                _context.JailTimetable.Add(activity);
            }
            else
            {
                // Update existing activity
                var existing = await _context.JailTimetable.FindAsync(new object[] { activity.TimetableID }, cancellationToken);
                if (existing == null)
                {
                    return new TimetableResult { Succeeded = false, ErrorMessage = "Activity not found." };
                }

                if (originalRowVersion != null)
                {
                    _context.Entry(existing).Property(e => e.RowVersion).OriginalValue = originalRowVersion;
                }

                existing.ActivityName = activity.ActivityName;
                existing.DayOfWeek = activity.DayOfWeek;
                existing.StartTime = activity.StartTime;
                existing.EndTime = activity.EndTime;
                existing.BarrackID = activity.BarrackID;
                existing.ResponsibleStaffID = activity.ResponsibleStaffID;
                existing.Instructions = activity.Instructions;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new TimetableResult { Succeeded = true };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while saving timetable activity.");
            return new TimetableResult
            {
                Succeeded = false,
                ConcurrencyConflict = true,
                ErrorMessage = "This activity was modified by another user. Please reload the page."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving timetable activity.");
            return new TimetableResult
            {
                Succeeded = false,
                ErrorMessage = "Database connection error. Unable to save activity to SQL Server."
            };
        }
    }

    public async Task<TimetableResult> DeleteActivityAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var activity = await _context.JailTimetable.FindAsync(new object[] { id }, cancellationToken);
            if (activity == null)
            {
                return new TimetableResult { Succeeded = false, ErrorMessage = "Activity not found." };
            }

            // Soft-delete
            activity.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
            return new TimetableResult { Succeeded = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting timetable activity.");
            return new TimetableResult { Succeeded = false, ErrorMessage = "Database connection error." };
        }
    }
}
