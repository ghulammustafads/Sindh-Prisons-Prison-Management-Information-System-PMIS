using Microsoft.EntityFrameworkCore;
using PrisonManagementSystem.Data;
using PrisonManagementSystem.Models;

namespace PrisonManagementSystem.Services;

public class StaffDto
{
    public int StaffID { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? OfficialRank { get; set; }
    public int RoleID { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int? BarrackID { get; set; }
    public string? BarrackName { get; set; }
    public string? Phone { get; set; }
    public string? CNIC { get; set; }
    public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly JoiningDate { get; set; }
    public bool IsActive { get; set; }
    public bool HasAccount { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class RoleDto
{
    public int RoleID { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int StaffCount { get; set; }
}

public class AccountDto
{
    public int AccountID { get; set; }
    public int StaffID { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class StaffResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ConcurrencyConflict { get; set; }
}

public interface IStaffService
{
    Task<List<StaffDto>> GetStaffListAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<List<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<List<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<List<Barrack>> GetActiveBarracksAsync(CancellationToken cancellationToken = default);
    Task<List<Staff>> GetStaffWithoutAccountsAsync(CancellationToken cancellationToken = default);

    Task<StaffResult> CreateStaffAsync(Staff staff, CancellationToken cancellationToken = default);
    Task<StaffResult> UpdateStaffAsync(Staff staff, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task<StaffResult> ToggleStaffStatusAsync(int staffId, CancellationToken cancellationToken = default);

    Task<StaffResult> CreateRoleAsync(string roleName, string? description, CancellationToken cancellationToken = default);
    Task<StaffResult> UpdateRoleAsync(int roleId, string roleName, string? description, bool isActive, CancellationToken cancellationToken = default);

    Task<StaffResult> CreateAccountAsync(int staffId, string username, string password, CancellationToken cancellationToken = default);
    Task<StaffResult> UpdatePasswordAsync(int accountId, string newPassword, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task<StaffResult> ToggleAccountStatusAsync(int accountId, CancellationToken cancellationToken = default);
}

public class StaffService : IStaffService
{
    private readonly PrisonDbContext _context;
    private readonly IAuthService _authService;
    private readonly ILogger<StaffService> _logger;

    public StaffService(PrisonDbContext context, IAuthService authService, ILogger<StaffService> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    public async Task<List<StaffDto>> GetStaffListAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Staff
                .AsNoTracking()
                .Include(s => s.Role)
                .Include(s => s.Barrack)
                .Include(s => s.ManagementAccount)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s => s.FullName.ToLower().Contains(term) ||
                                         s.EmployeeNumber.ToLower().Contains(term) ||
                                         (s.OfficialRank != null && s.OfficialRank.ToLower().Contains(term)));
            }

            return await query
                .OrderBy(s => s.FullName)
                .Select(s => new StaffDto
                {
                    StaffID = s.StaffID,
                    EmployeeNumber = s.EmployeeNumber,
                    FullName = s.FullName,
                    OfficialRank = s.OfficialRank,
                    RoleID = s.RoleID,
                    RoleName = s.Role.RoleName,
                    BarrackID = s.BarrackID,
                    BarrackName = s.Barrack != null ? s.Barrack.BarrackName : null,
                    Phone = s.Phone,
                    CNIC = s.CNIC,
                    Address = s.Address,
                    DateOfBirth = s.DateOfBirth,
                    JoiningDate = s.JoiningDate,
                    IsActive = s.IsActive,
                    HasAccount = s.ManagementAccount != null,
                    RowVersion = s.RowVersion
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load staff list.");
            return new List<StaffDto>();
        }
    }

    public async Task<List<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.StaffRoles
                .AsNoTracking()
                .OrderBy(r => r.RoleName)
                .Select(r => new RoleDto
                {
                    RoleID = r.RoleID,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    StaffCount = r.StaffMembers.Count
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load roles.");
            return new List<RoleDto>();
        }
    }

    public async Task<List<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ManagementAccounts
                .AsNoTracking()
                .Include(a => a.Staff)
                    .ThenInclude(s => s.Role)
                .OrderBy(a => a.Username)
                .Select(a => new AccountDto
                {
                    AccountID = a.AccountID,
                    StaffID = a.StaffID,
                    StaffName = a.Staff.FullName,
                    EmployeeNumber = a.Staff.EmployeeNumber,
                    RoleName = a.Staff.Role.RoleName,
                    Username = a.Username,
                    IsActive = a.IsActive,
                    LastLoginAtUtc = a.LastLoginAtUtc,
                    RowVersion = a.RowVersion
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load accounts.");
            return new List<AccountDto>();
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
            _logger.LogError(ex, "Failed to load barracks.");
            return new List<Barrack>();
        }
    }

    public async Task<List<Staff>> GetStaffWithoutAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Staff
                .AsNoTracking()
                .Where(s => s.IsActive && s.ManagementAccount == null)
                .OrderBy(s => s.FullName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load staff without accounts.");
            return new List<Staff>();
        }
    }

    public async Task<StaffResult> CreateStaffAsync(Staff staff, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(staff.FullName))
            return new StaffResult { Succeeded = false, ErrorMessage = "Full name is required." };
        if (string.IsNullOrWhiteSpace(staff.EmployeeNumber))
            return new StaffResult { Succeeded = false, ErrorMessage = "Employee number is required." };
        if (string.IsNullOrWhiteSpace(staff.OfficialRank))
            return new StaffResult { Succeeded = false, ErrorMessage = "Official rank is required." };

        var exists = await _context.Staff.AnyAsync(s => s.EmployeeNumber == staff.EmployeeNumber.Trim(), cancellationToken);
        if (exists)
        {
            return new StaffResult { Succeeded = false, ErrorMessage = $"Employee number '{staff.EmployeeNumber}' already exists." };
        }

        var roleExists = await _context.StaffRoles.AnyAsync(r => r.RoleID == staff.RoleID && r.IsActive, cancellationToken);
        if (!roleExists)
        {
            return new StaffResult { Succeeded = false, ErrorMessage = "Selected role does not exist or is inactive." };
        }

        staff.EmployeeNumber = staff.EmployeeNumber.Trim();
        staff.CreatedAtUtc = DateTime.UtcNow;
        staff.IsActive = true;

        try
        {
            _context.Staff.Add(staff);
            await _context.SaveChangesAsync(cancellationToken);
            return new StaffResult { Succeeded = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating staff.");
            return new StaffResult { Succeeded = false, ErrorMessage = "Database error creating staff member." };
        }
    }

    public async Task<StaffResult> UpdateStaffAsync(Staff staff, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.Staff.FindAsync(new object[] { staff.StaffID }, cancellationToken);
            if (existing == null)
            {
                return new StaffResult { Succeeded = false, ErrorMessage = "Staff member not found." };
            }

            if (rowVersion != null)
            {
                _context.Entry(existing).Property(s => s.RowVersion).OriginalValue = rowVersion;
            }

            existing.FullName = staff.FullName;
            existing.OfficialRank = staff.OfficialRank;
            existing.RoleID = staff.RoleID;
            existing.BarrackID = staff.BarrackID;
            existing.Phone = staff.Phone;
            existing.CNIC = staff.CNIC;
            existing.Address = staff.Address;
            existing.DateOfBirth = staff.DateOfBirth;
            existing.JoiningDate = staff.JoiningDate;

            await _context.SaveChangesAsync(cancellationToken);
            return new StaffResult { Succeeded = true };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating staff.");
            return new StaffResult
            {
                Succeeded = false,
                ConcurrencyConflict = true,
                ErrorMessage = "Staff record was modified by another user."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update staff.");
            return new StaffResult { Succeeded = false, ErrorMessage = "Database error." };
        }
    }

    public async Task<StaffResult> ToggleStaffStatusAsync(int staffId, CancellationToken cancellationToken = default)
    {
        var staff = await _context.Staff.FindAsync(new object[] { staffId }, cancellationToken);
        if (staff == null) return new StaffResult { Succeeded = false, ErrorMessage = "Staff not found." };

        staff.IsActive = !staff.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return new StaffResult { Succeeded = true };
    }

    public async Task<StaffResult> CreateRoleAsync(string roleName, string? description, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return new StaffResult { Succeeded = false, ErrorMessage = "Role name is required." };

        var exists = await _context.StaffRoles.AnyAsync(r => r.RoleName == roleName.Trim(), cancellationToken);
        if (exists)
            return new StaffResult { Succeeded = false, ErrorMessage = $"Role '{roleName}' already exists." };

        var role = new StaffRole
        {
            RoleName = roleName.Trim(),
            Description = description?.Trim(),
            IsActive = true
        };

        _context.StaffRoles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return new StaffResult { Succeeded = true };
    }

    public async Task<StaffResult> UpdateRoleAsync(int roleId, string roleName, string? description, bool isActive, CancellationToken cancellationToken = default)
    {
        var role = await _context.StaffRoles.FindAsync(new object[] { roleId }, cancellationToken);
        if (role == null) return new StaffResult { Succeeded = false, ErrorMessage = "Role not found." };

        role.RoleName = roleName.Trim();
        role.Description = description?.Trim();
        role.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);
        return new StaffResult { Succeeded = true };
    }

    public async Task<StaffResult> CreateAccountAsync(int staffId, string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return new StaffResult { Succeeded = false, ErrorMessage = "Username is required." };
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return new StaffResult { Succeeded = false, ErrorMessage = "Password must be at least 6 characters." };

        var staffExists = await _context.Staff.AnyAsync(s => s.StaffID == staffId && s.IsActive, cancellationToken);
        if (!staffExists)
            return new StaffResult { Succeeded = false, ErrorMessage = "Staff member does not exist or is inactive." };

        var userExists = await _context.ManagementAccounts.AnyAsync(a => a.Username == username.Trim(), cancellationToken);
        if (userExists)
            return new StaffResult { Succeeded = false, ErrorMessage = $"Username '{username}' is already taken." };

        var accountExistsForStaff = await _context.ManagementAccounts.AnyAsync(a => a.StaffID == staffId, cancellationToken);
        if (accountExistsForStaff)
            return new StaffResult { Succeeded = false, ErrorMessage = "This staff member already has an account." };

        try
        {
            await _authService.CreateAccountAsync(staffId, username, password, cancellationToken);
            return new StaffResult { Succeeded = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create account.");
            return new StaffResult { Succeeded = false, ErrorMessage = "Database error creating account." };
        }
    }

    public async Task<StaffResult> UpdatePasswordAsync(int accountId, string newPassword, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return new StaffResult { Succeeded = false, ErrorMessage = "Password must be at least 6 characters." };

        var success = await _authService.UpdatePasswordAsync(accountId, newPassword, rowVersion, cancellationToken);
        return success
            ? new StaffResult { Succeeded = true }
            : new StaffResult { Succeeded = false, ErrorMessage = "Could not update password. Account not found or concurrency conflict." };
    }

    public async Task<StaffResult> ToggleAccountStatusAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await _context.ManagementAccounts.FindAsync(new object[] { accountId }, cancellationToken);
        if (account == null) return new StaffResult { Succeeded = false, ErrorMessage = "Account not found." };

        account.IsActive = !account.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return new StaffResult { Succeeded = true };
    }
}
