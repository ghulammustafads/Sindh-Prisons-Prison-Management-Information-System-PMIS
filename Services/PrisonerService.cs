using System.Data;
using Microsoft.EntityFrameworkCore;
using PrisonManagementSystem.Data;
using PrisonManagementSystem.Models;

namespace PrisonManagementSystem.Services;

public class PrisonerListItemDto
{
    public int PrisonerID { get; set; }
    public string PrisonerNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly AdmissionDate { get; set; }
    public string BarrackCellLocation { get; set; } = string.Empty;
    public string CustodyStatus { get; set; } = string.Empty;
    public byte[]? RowVersion { get; set; }
}

public class PrisonerDetailDto
{
    public int PrisonerID { get; set; }
    public string PrisonerNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? CNIC { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? PhotoReference { get; set; }
    public DateOnly AdmissionDate { get; set; }
    public string CustodyStatus { get; set; } = "In Custody";
    public int? BarrackID { get; set; }
    public int? CellID { get; set; }
    public string? CellNumber { get; set; }
    public string? BarrackName { get; set; }
    public byte[]? RowVersion { get; set; }

    public List<CourtCaseDto> Cases { get; set; } = new();
}

public class CourtCaseDto
{
    public int CaseID { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public short CaseYear { get; set; }
    public string CaseType { get; set; } = string.Empty;
    public string CourtName { get; set; } = string.Empty;
    public string? FIRNumber { get; set; }
    public string? PoliceStation { get; set; }
    public string? AllegedOffence { get; set; }
    public string CaseStatus { get; set; } = string.Empty;
    public string? LawyerName { get; set; }
    public string ResponsibleStaffName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public byte[]? RowVersion { get; set; }

    public List<HearingDto> LinkedHearings { get; set; } = new();
}

public class PrisonerResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ConcurrencyConflict { get; set; }
    public int PrisonerId { get; set; }
}

public interface IPrisonerService
{
    Task<List<PrisonerListItemDto>> GetPrisonersAsync(string? search = null, string? statusFilter = null, CancellationToken cancellationToken = default);
    Task<PrisonerDetailDto?> GetPrisonerDetailAsync(int prisonerId, CancellationToken cancellationToken = default);
    Task<List<Barrack>> GetActiveBarracksWithCellsAsync(CancellationToken cancellationToken = default);
    Task<List<Cell>> GetCellsByBarrackAsync(int barrackId, CancellationToken cancellationToken = default);
    Task<PrisonerResult> RegisterPrisonerAsync(Prisoner prisoner, int registeredByStaffId, CancellationToken cancellationToken = default);
    Task<PrisonerResult> UpdatePrisonerAsync(Prisoner prisoner, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task<PrisonerResult> AddCaseAsync(CourtCase courtCase, int responsibleStaffId, CancellationToken cancellationToken = default);
}

public class PrisonerService : IPrisonerService
{
    private readonly PrisonDbContext _context;
    private readonly ILogger<PrisonerService> _logger;

    public PrisonerService(PrisonDbContext context, ILogger<PrisonerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PrisonerListItemDto>> GetPrisonersAsync(string? search = null, string? statusFilter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Prisoners
                .AsNoTracking()
                .Include(p => p.Cell)
                    .ThenInclude(c => c!.Barrack)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.FullName.ToLower().Contains(term) ||
                                         p.PrisonerNumber.ToLower().Contains(term) ||
                                         (p.CNIC != null && p.CNIC.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All statuses")
            {
                query = query.Where(p => p.CustodyStatus == statusFilter);
            }

            return await query
                .OrderByDescending(p => p.AdmissionDate)
                .Select(p => new PrisonerListItemDto
                {
                    PrisonerID = p.PrisonerID,
                    PrisonerNumber = p.PrisonerNumber,
                    FullName = p.FullName,
                    AdmissionDate = p.AdmissionDate,
                    BarrackCellLocation = p.Cell != null ? $"{p.Cell.Barrack.BarrackName} - Cell {p.Cell.CellNumber}" : "Unassigned",
                    CustodyStatus = p.CustodyStatus,
                    RowVersion = p.RowVersion
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load prisoners list.");
            return new List<PrisonerListItemDto>();
        }
    }

    public async Task<PrisonerDetailDto?> GetPrisonerDetailAsync(int prisonerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var prisoner = await _context.Prisoners
                .AsNoTracking()
                .Include(p => p.Cell)
                    .ThenInclude(c => c!.Barrack)
                .Include(p => p.CourtCases)
                    .ThenInclude(c => c.ResponsibleStaff)
                .Include(p => p.CourtCases)
                    .ThenInclude(c => c.CourtTrials)
                        .ThenInclude(t => t.ResponsibleStaff)
                .FirstOrDefaultAsync(p => p.PrisonerID == prisonerId, cancellationToken);

            if (prisoner == null) return null;

            return new PrisonerDetailDto
            {
                PrisonerID = prisoner.PrisonerID,
                PrisonerNumber = prisoner.PrisonerNumber,
                FullName = prisoner.FullName,
                FatherName = prisoner.FatherName,
                DateOfBirth = prisoner.DateOfBirth,
                Gender = prisoner.Gender,
                CNIC = prisoner.CNIC,
                Address = prisoner.Address,
                EmergencyContactName = prisoner.EmergencyContactName,
                EmergencyContactPhone = prisoner.EmergencyContactPhone,
                PhotoReference = prisoner.PhotoReference,
                AdmissionDate = prisoner.AdmissionDate,
                CustodyStatus = prisoner.CustodyStatus,
                CellID = prisoner.CellID,
                BarrackID = prisoner.Cell?.BarrackID,
                BarrackName = prisoner.Cell?.Barrack.BarrackName,
                CellNumber = prisoner.Cell?.CellNumber,
                RowVersion = prisoner.RowVersion,
                Cases = prisoner.CourtCases.Select(c => new CourtCaseDto
                {
                    CaseID = c.CaseID,
                    CaseNumber = c.CaseNumber,
                    CaseYear = c.CaseYear,
                    CaseType = c.CaseType,
                    CourtName = c.CourtName,
                    FIRNumber = c.FIRNumber,
                    PoliceStation = c.PoliceStation,
                    AllegedOffence = c.AllegedOffence,
                    CaseStatus = c.CaseStatus,
                    LawyerName = c.LawyerName,
                    ResponsibleStaffName = c.ResponsibleStaff.FullName,
                    Remarks = c.Remarks,
                    RowVersion = c.RowVersion,
                    LinkedHearings = c.CourtTrials.Select(t => new HearingDto
                    {
                        TrialID = t.TrialID,
                        CaseID = t.CaseID,
                        PrisonerName = prisoner.FullName,
                        CaseReference = c.CaseNumber,
                        HearingDate = t.HearingAt,
                        HearingType = t.HearingType,
                        AttendanceStatus = t.AttendanceStatus,
                        AttendanceMethod = t.AttendanceMethod ?? "In Person",
                        HearingStatus = t.HearingStatus,
                        Outcome = t.Outcome,
                        ResponsibleStaffName = t.ResponsibleStaff.FullName,
                        Remarks = t.Remarks,
                        RowVersion = t.RowVersion
                    }).ToList()
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load prisoner detail for {PrisonerId}.", prisonerId);
            return null;
        }
    }

    public async Task<List<Barrack>> GetActiveBarracksWithCellsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Barracks
                .AsNoTracking()
                .Include(b => b.Cells.Where(c => c.IsActive))
                .Where(b => b.IsActive)
                .OrderBy(b => b.BarrackName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load active barracks with cells.");
            return new List<Barrack>();
        }
    }

    public async Task<List<Cell>> GetCellsByBarrackAsync(int barrackId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Cells
                .AsNoTracking()
                .Where(c => c.BarrackID == barrackId && c.IsActive)
                .OrderBy(c => c.CellNumber)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cells for barrack {BarrackId}.", barrackId);
            return new List<Cell>();
        }
    }

    public async Task<PrisonerResult> RegisterPrisonerAsync(Prisoner prisoner, int registeredByStaffId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prisoner.FullName))
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Full name is required." };
        if (string.IsNullOrWhiteSpace(prisoner.FatherName))
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Father name is required." };
        if (string.IsNullOrWhiteSpace(prisoner.Gender))
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Gender is required." };
        if (prisoner.DateOfBirth == default)
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Date of birth is required." };
        if (prisoner.AdmissionDate == default)
            prisoner.AdmissionDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Concurrency-safe prisoner number generation if not manually specified
        if (string.IsNullOrWhiteSpace(prisoner.PrisonerNumber))
        {
            var year = DateTime.UtcNow.Year;
            var randSuffix = Random.Shared.Next(10000, 99999);
            prisoner.PrisonerNumber = $"SP-{year}-{randSuffix}";
        }

        // Validate uniqueness of PrisonerNumber
        var exists = await _context.Prisoners.AnyAsync(p => p.PrisonerNumber == prisoner.PrisonerNumber, cancellationToken);
        if (exists)
        {
            return new PrisonerResult { Succeeded = false, ErrorMessage = $"Prisoner number '{prisoner.PrisonerNumber}' is already in use." };
        }

        prisoner.RegisteredByStaffID = registeredByStaffId;
        prisoner.CreatedAtUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(prisoner.CustodyStatus))
        {
            prisoner.CustodyStatus = "In Custody";
        }

        // Validate cell capacity with SQL Server locking within a transaction
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (prisoner.CellID.HasValue)
            {
                Cell? cell;
                if (_context.Database.IsSqlServer())
                {
                    cell = await _context.Cells
                        .FromSqlInterpolated($"SELECT * FROM dbo.Cells WITH (UPDLOCK, HOLDLOCK) WHERE CellID = {prisoner.CellID.Value} AND IsActive = 1")
                        .FirstOrDefaultAsync(cancellationToken);
                }
                else
                {
                    cell = await _context.Cells
                        .FirstOrDefaultAsync(c => c.CellID == prisoner.CellID.Value && c.IsActive, cancellationToken);
                }

                if (cell == null)
                {
                    return new PrisonerResult { Succeeded = false, ErrorMessage = "Selected cell is invalid or inactive." };
                }

                var currentOccupancy = await _context.Prisoners
                    .Where(p => p.CellID == cell.CellID && p.CustodyStatus == "In Custody")
                    .CountAsync(cancellationToken);

                if (currentOccupancy >= cell.Capacity)
                {
                    return new PrisonerResult { Succeeded = false, ErrorMessage = $"Cell {cell.CellNumber} has reached its maximum capacity of {cell.Capacity}." };
                }
            }

            _context.Prisoners.Add(prisoner);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new PrisonerResult { Succeeded = true, PrisonerId = prisoner.PrisonerID };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to register prisoner.");
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Database error. Could not register prisoner." };
        }
    }

    public async Task<PrisonerResult> UpdatePrisonerAsync(Prisoner prisoner, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var existing = await _context.Prisoners.FindAsync(new object[] { prisoner.PrisonerID }, cancellationToken);
            if (existing == null)
            {
                return new PrisonerResult { Succeeded = false, ErrorMessage = "Prisoner not found." };
            }

            if (rowVersion != null)
            {
                _context.Entry(existing).Property(p => p.RowVersion).OriginalValue = rowVersion;
            }

            // Cell capacity check if changing cell
            if (prisoner.CellID.HasValue && prisoner.CellID != existing.CellID)
            {
                Cell? cell;
                if (_context.Database.IsSqlServer())
                {
                    cell = await _context.Cells
                        .FromSqlInterpolated($"SELECT * FROM dbo.Cells WITH (UPDLOCK, HOLDLOCK) WHERE CellID = {prisoner.CellID.Value} AND IsActive = 1")
                        .FirstOrDefaultAsync(cancellationToken);
                }
                else
                {
                    cell = await _context.Cells
                        .FirstOrDefaultAsync(c => c.CellID == prisoner.CellID.Value && c.IsActive, cancellationToken);
                }

                if (cell == null)
                {
                    return new PrisonerResult { Succeeded = false, ErrorMessage = "Selected cell is invalid or inactive." };
                }

                var currentOccupancy = await _context.Prisoners
                    .Where(p => p.CellID == cell.CellID && p.CustodyStatus == "In Custody" && p.PrisonerID != prisoner.PrisonerID)
                    .CountAsync(cancellationToken);

                if (currentOccupancy >= cell.Capacity)
                {
                    return new PrisonerResult { Succeeded = false, ErrorMessage = $"Cell {cell.CellNumber} is at capacity ({cell.Capacity}/{cell.Capacity})." };
                }

                existing.CellID = prisoner.CellID;
            }
            else if (!prisoner.CellID.HasValue)
            {
                existing.CellID = null;
            }

            existing.FullName = prisoner.FullName;
            existing.FatherName = prisoner.FatherName;
            existing.DateOfBirth = prisoner.DateOfBirth;
            existing.Gender = prisoner.Gender;
            existing.CNIC = prisoner.CNIC;
            existing.Address = prisoner.Address;
            existing.EmergencyContactName = prisoner.EmergencyContactName;
            existing.EmergencyContactPhone = prisoner.EmergencyContactPhone;
            existing.Remarks = prisoner.Remarks;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new PrisonerResult { Succeeded = true, PrisonerId = existing.PrisonerID };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Concurrency conflict updating prisoner.");
            return new PrisonerResult
            {
                Succeeded = false,
                ConcurrencyConflict = true,
                ErrorMessage = "This prisoner record was modified by another user. Please reload."
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating prisoner.");
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Database error." };
        }
    }

    public async Task<PrisonerResult> AddCaseAsync(CourtCase courtCase, int responsibleStaffId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(courtCase.CaseNumber))
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Case number is required." };
        if (string.IsNullOrWhiteSpace(courtCase.CourtName))
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Court name is required." };
        if (string.IsNullOrWhiteSpace(courtCase.FIRNumber))
            return new PrisonerResult { Succeeded = false, ErrorMessage = "FIR number is required." };
        if (string.IsNullOrWhiteSpace(courtCase.PoliceStation))
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Police station is required." };
        if (string.IsNullOrWhiteSpace(courtCase.AllegedOffence))
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Alleged offence is required." };

        courtCase.ResponsibleStaffID = responsibleStaffId;
        courtCase.CreatedAtUtc = DateTime.UtcNow;
        if (courtCase.CaseYear <= 0)
        {
            courtCase.CaseYear = (short)DateTime.UtcNow.Year;
        }

        try
        {
            _context.CourtCases.Add(courtCase);
            await _context.SaveChangesAsync(cancellationToken);
            return new PrisonerResult { Succeeded = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add court case.");
            return new PrisonerResult { Succeeded = false, ErrorMessage = "Database error adding court case." };
        }
    }
}
