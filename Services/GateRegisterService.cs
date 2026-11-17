using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PrisonManagementSystem.Data;
using PrisonManagementSystem.Models;

namespace PrisonManagementSystem.Services;

public class GateRegisterResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IGateRegisterService
{
    Task<List<GateRegister>> GetEntriesAsync(DateTime? fromDate = null, DateTime? toDate = null, bool stillInsideOnly = false, CancellationToken cancellationToken = default);
    Task<GateRegisterResult> RecordEntryAsync(GateRegister entry, CancellationToken cancellationToken = default);
    Task<GateRegisterResult> RecordExitAsync(int entryId, DateTime? exitTime = null, CancellationToken cancellationToken = default);
}

public class GateRegisterService : IGateRegisterService
{
    private readonly PrisonDbContext _context;
    private readonly ILogger<GateRegisterService> _logger;

    public GateRegisterService(PrisonDbContext context, ILogger<GateRegisterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<GateRegister>> GetEntriesAsync(DateTime? fromDate = null, DateTime? toDate = null, bool stillInsideOnly = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.GateRegister.AsNoTracking();

            if (stillInsideOnly)
            {
                query = query.Where(g => g.ExitTime == null);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(g => g.EntryTime >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1);
                query = query.Where(g => g.EntryTime < endOfDay);
            }

            return await query.OrderByDescending(g => g.EntryTime).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load gate register entries.");
            return new List<GateRegister>();
        }
    }

    public async Task<GateRegisterResult> RecordEntryAsync(GateRegister entry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entry.VisitorName))
        {
            return new GateRegisterResult { Succeeded = false, ErrorMessage = "Visitor name is required." };
        }

        if (string.IsNullOrWhiteSpace(entry.VisitType))
        {
            return new GateRegisterResult { Succeeded = false, ErrorMessage = "Visit type is required." };
        }

        if (string.IsNullOrWhiteSpace(entry.VisitPurpose))
        {
            return new GateRegisterResult { Succeeded = false, ErrorMessage = "Visit purpose is required." };
        }

        if (entry.NumberOfPeople <= 0)
        {
            return new GateRegisterResult { Succeeded = false, ErrorMessage = "Number of people must be greater than zero." };
        }

        if (entry.EntryTime == default)
        {
            entry.EntryTime = DateTime.Now;
        }

        try
        {
            _context.GateRegister.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);
            return new GateRegisterResult { Succeeded = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record gate entry.");
            return new GateRegisterResult { Succeeded = false, ErrorMessage = "Database error. Could not record entry into SQL Server." };
        }
    }

    public async Task<GateRegisterResult> RecordExitAsync(int entryId, DateTime? exitTime = null, CancellationToken cancellationToken = default)
    {
        var effectiveExit = exitTime ?? DateTime.Now;

        try
        {
            var entry = await _context.GateRegister.FindAsync(new object[] { entryId }, cancellationToken);
            if (entry == null)
            {
                return new GateRegisterResult { Succeeded = false, ErrorMessage = "Entry record not found." };
            }
            if (entry.ExitTime != null)
            {
                return new GateRegisterResult { Succeeded = false, ErrorMessage = "Visitor departure has already been recorded." };
            }
            if (effectiveExit < entry.EntryTime)
            {
                return new GateRegisterResult { Succeeded = false, ErrorMessage = "Exit time cannot precede entry time." };
            }

            entry.ExitTime = effectiveExit;
            await _context.SaveChangesAsync(cancellationToken);

            return new GateRegisterResult { Succeeded = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record gate exit for entry {EntryId}.", entryId);
            return new GateRegisterResult { Succeeded = false, ErrorMessage = "Database error. Could not update departure time." };
        }
    }
}
