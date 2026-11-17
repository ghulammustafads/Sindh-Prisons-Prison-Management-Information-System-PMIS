using Microsoft.EntityFrameworkCore;
using PrisonManagementSystem.Data;
using PrisonManagementSystem.Models;

namespace PrisonManagementSystem.Services;

public class HearingDto
{
    public int TrialID { get; set; }
    public int CaseID { get; set; }
    public string PrisonerName { get; set; } = string.Empty;
    public string CaseReference { get; set; } = string.Empty;
    public DateTime HearingDate { get; set; }
    public string HearingType { get; set; } = string.Empty;
    public string AttendanceStatus { get; set; } = string.Empty;
    public string AttendanceMethod { get; set; } = string.Empty;
    public string HearingStatus { get; set; } = string.Empty;
    public string? Outcome { get; set; }
    public string ResponsibleStaffName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CaseLookupDto
{
    public int CaseID { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}

public class CourtResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ConcurrencyConflict { get; set; }
}

public interface ICourtService
{
    Task<List<HearingDto>> GetHearingsAsync(string? searchTerm = null, DateTime? dateFilter = null, CancellationToken cancellationToken = default);
    Task<List<CaseLookupDto>> GetCasesForDropdownAsync(CancellationToken cancellationToken = default);
    Task<List<Staff>> GetStaffForDropdownAsync(CancellationToken cancellationToken = default);
    Task<CourtResult> ScheduleHearingAsync(int caseId, DateTime hearingAt, string hearingType, int responsibleStaffId, int recordedByStaffId, string? remarks, CancellationToken cancellationToken = default);
    Task<CourtResult> RecordOutcomeAsync(int trialId, string hearingStatus, string attendanceStatus, string? outcome, string? remarks, byte[]? rowVersion, CancellationToken cancellationToken = default);
}

public class CourtService : ICourtService
{
    private readonly PrisonDbContext _context;
    private readonly ILogger<CourtService> _logger;

    public CourtService(PrisonDbContext context, ILogger<CourtService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<HearingDto>> GetHearingsAsync(string? searchTerm = null, DateTime? dateFilter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.CourtTrials
                .AsNoTracking()
                .Include(t => t.Case)
                    .ThenInclude(c => c.Prisoner)
                .Include(t => t.ResponsibleStaff)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(t => t.Case.CaseNumber.ToLower().Contains(term) ||
                                         t.Case.Prisoner.FullName.ToLower().Contains(term) ||
                                         t.Case.Prisoner.PrisonerNumber.ToLower().Contains(term));
            }

            if (dateFilter.HasValue)
            {
                var start = dateFilter.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(t => t.HearingAt >= start && t.HearingAt < end);
            }

            return await query
                .OrderByDescending(t => t.HearingAt)
                .Select(t => new HearingDto
                {
                    TrialID = t.TrialID,
                    CaseID = t.CaseID,
                    PrisonerName = t.Case.Prisoner.FullName,
                    CaseReference = $"{t.Case.CaseNumber} ({t.Case.CourtName})",
                    HearingDate = t.HearingAt,
                    HearingType = t.HearingType,
                    AttendanceStatus = t.AttendanceStatus,
                    AttendanceMethod = t.AttendanceMethod ?? "In Person",
                    HearingStatus = t.HearingStatus,
                    Outcome = t.Outcome,
                    ResponsibleStaffName = t.ResponsibleStaff.FullName,
                    Remarks = t.Remarks,
                    RowVersion = t.RowVersion
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load court hearings.");
            return new List<HearingDto>();
        }
    }

    public async Task<List<CaseLookupDto>> GetCasesForDropdownAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.CourtCases
                .AsNoTracking()
                .Include(c => c.Prisoner)
                .OrderByDescending(c => c.CaseYear)
                .ThenBy(c => c.CaseNumber)
                .Select(c => new CaseLookupDto
                {
                    CaseID = c.CaseID,
                    DisplayText = $"{c.Prisoner.FullName} ({c.Prisoner.PrisonerNumber}) — Case {c.CaseNumber} ({c.CourtName})"
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cases for dropdown.");
            return new List<CaseLookupDto>();
        }
    }

    public async Task<List<Staff>> GetStaffForDropdownAsync(CancellationToken cancellationToken = default)
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
            _logger.LogError(ex, "Failed to load staff for dropdown.");
            return new List<Staff>();
        }
    }

    public async Task<CourtResult> ScheduleHearingAsync(int caseId, DateTime hearingAt, string hearingType, int responsibleStaffId, int recordedByStaffId, string? remarks, CancellationToken cancellationToken = default)
    {
        if (caseId <= 0)
        {
            return new CourtResult { Succeeded = false, ErrorMessage = "Please select a prisoner court case." };
        }

        if (string.IsNullOrWhiteSpace(hearingType))
        {
            return new CourtResult { Succeeded = false, ErrorMessage = "Hearing type is required." };
        }

        var caseExists = await _context.CourtCases.AnyAsync(c => c.CaseID == caseId, cancellationToken);
        if (!caseExists)
        {
            return new CourtResult { Succeeded = false, ErrorMessage = "Selected case does not exist." };
        }

        var staffExists = await _context.Staff.AnyAsync(s => s.StaffID == responsibleStaffId && s.IsActive, cancellationToken);
        if (!staffExists)
        {
            return new CourtResult { Succeeded = false, ErrorMessage = "Selected responsible staff does not exist or is inactive." };
        }

        var recorderExists = await _context.Staff.AnyAsync(s => s.StaffID == recordedByStaffId && s.IsActive, cancellationToken);
        if (!recorderExists)
        {
            return new CourtResult { Succeeded = false, ErrorMessage = "Authenticated staff recorder identity is invalid." };
        }

        try
        {
            var trial = new CourtTrial
            {
                CaseID = caseId,
                HearingAt = hearingAt,
                HearingType = hearingType.Trim(),
                HearingStatus = "Scheduled",
                AttendanceStatus = "Pending",
                AttendanceMethod = "In Person",
                ResponsibleStaffID = responsibleStaffId,
                RecordedByStaffID = recordedByStaffId,
                Remarks = remarks,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.CourtTrials.Add(trial);
            await _context.SaveChangesAsync(cancellationToken);
            return new CourtResult { Succeeded = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule court hearing.");
            return new CourtResult { Succeeded = false, ErrorMessage = "Database error. Could not record hearing in SQL Server." };
        }
    }

    public async Task<CourtResult> RecordOutcomeAsync(int trialId, string hearingStatus, string attendanceStatus, string? outcome, string? remarks, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var trial = await _context.CourtTrials.FindAsync(new object[] { trialId }, cancellationToken);
            if (trial == null)
            {
                return new CourtResult { Succeeded = false, ErrorMessage = "Hearing record not found." };
            }

            if (rowVersion != null)
            {
                _context.Entry(trial).Property(t => t.RowVersion).OriginalValue = rowVersion;
            }

            trial.HearingStatus = hearingStatus;
            trial.AttendanceStatus = attendanceStatus;
            trial.Outcome = outcome;
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                trial.Remarks = remarks;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new CourtResult { Succeeded = true };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating hearing outcome.");
            return new CourtResult
            {
                Succeeded = false,
                ConcurrencyConflict = true,
                ErrorMessage = "This hearing was modified by another user. Please refresh and try again."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update hearing outcome.");
            return new CourtResult { Succeeded = false, ErrorMessage = "Database error." };
        }
    }
}
