using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PrisonManagementSystem.Authorization;
using PrisonManagementSystem.Data;
using PrisonManagementSystem.Models;
using PrisonManagementSystem.Services;
using Xunit;

namespace PrisonManagementSystem.Tests;

public class SystemRulesTests
{
    private PrisonDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PrisonDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new PrisonDbContext(options);
    }

    [Fact]
    public void RolePageMap_Contains_AllSixMandatedRoles()
    {
        Assert.Equal("Dashboard", RoleConstants.GetAssignedController("Jail Superintendent"));
        Assert.Equal("DailyTimetable", RoleConstants.GetAssignedController("Timetable Officer"));
        Assert.Equal("GateRegister", RoleConstants.GetAssignedController("Gatekeeper"));
        Assert.Equal("CourtHearings", RoleConstants.GetAssignedController("Court Records Officer"));
        Assert.Equal("Prisoners", RoleConstants.GetAssignedController("Prisoner Records Officer"));
        Assert.Equal("StaffAccounts", RoleConstants.GetAssignedController("System Administrator"));

        Assert.Null(RoleConstants.GetAssignedController("RandomUnknownRole"));
    }

    [Fact]
    public async Task AuthService_AuthenticatesValidActiveAccount_UpdatesLastLogin()
    {
        using var context = CreateInMemoryContext(nameof(AuthService_AuthenticatesValidActiveAccount_UpdatesLastLogin));
        var authService = new AuthService(context, NullLogger<AuthService>.Instance);

        var role = new StaffRole { RoleID = 1, RoleName = RoleConstants.JailSuperintendent, IsActive = true };
        var staff = new Staff
        {
            StaffID = 1,
            FullName = "Major Tariq",
            EmployeeNumber = "SP-001",
            OfficialRank = "Superintendent",
            RoleID = 1,
            Role = role,
            CNIC = "42101-1234567-1",
            Phone = "03001234567",
            Address = "Central Jail Staff Colony",
            DateOfBirth = new DateOnly(1975, 5, 12),
            JoiningDate = new DateOnly(2005, 3, 1),
            IsActive = true
        };
        var account = new ManagementAccount
        {
            AccountID = 1,
            StaffID = 1,
            Staff = staff,
            Username = "superintendent",
            PasswordHash = authService.HashPassword("SecretPass123!"),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.StaffRoles.Add(role);
        context.Staff.Add(staff);
        context.ManagementAccounts.Add(account);
        await context.SaveChangesAsync();

        var result = await authService.AuthenticateAsync("superintendent", "SecretPass123!");

        Assert.True(result.Succeeded);
        Assert.Equal(RoleConstants.JailSuperintendent, result.RoleName);
        Assert.Equal("Dashboard", result.AssignedController);
        Assert.NotNull(result.Account?.LastLoginAtUtc);
    }

    [Fact]
    public async Task AuthService_RejectsWrongPassword()
    {
        using var context = CreateInMemoryContext(nameof(AuthService_RejectsWrongPassword));
        var authService = new AuthService(context, NullLogger<AuthService>.Instance);

        var role = new StaffRole { RoleID = 1, RoleName = RoleConstants.Gatekeeper, IsActive = true };
        var staff = new Staff
        {
            StaffID = 1,
            FullName = "Ahmed Khan",
            EmployeeNumber = "SP-002",
            OfficialRank = "Head Warden",
            RoleID = 1,
            Role = role,
            CNIC = "42101-1234567-2",
            Phone = "03001234568",
            Address = "Gate Quarters",
            DateOfBirth = new DateOnly(1985, 2, 10),
            JoiningDate = new DateOnly(2010, 8, 15),
            IsActive = true
        };
        var account = new ManagementAccount
        {
            AccountID = 1,
            StaffID = 1,
            Staff = staff,
            Username = "gatekeeper",
            PasswordHash = authService.HashPassword("CorrectPassword!"),
            IsActive = true
        };

        context.StaffRoles.Add(role);
        context.Staff.Add(staff);
        context.ManagementAccounts.Add(account);
        await context.SaveChangesAsync();

        var result = await authService.AuthenticateAsync("gatekeeper", "WrongPassword!");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthService_RejectsInactiveAccount()
    {
        using var context = CreateInMemoryContext(nameof(AuthService_RejectsInactiveAccount));
        var authService = new AuthService(context, NullLogger<AuthService>.Instance);

        var role = new StaffRole { RoleID = 1, RoleName = RoleConstants.TimetableOfficer, IsActive = true };
        var staff = new Staff { StaffID = 1, FullName = "Bilal", EmployeeNumber = "SP-003", OfficialRank = "Officer", RoleID = 1, Role = role, CNIC = "123", Phone = "123", Address = "Colony", IsActive = true };
        var account = new ManagementAccount
        {
            AccountID = 1,
            StaffID = 1,
            Staff = staff,
            Username = "timetable",
            PasswordHash = authService.HashPassword("Pass123!"),
            IsActive = false // Inactive account!
        };

        context.StaffRoles.Add(role);
        context.Staff.Add(staff);
        context.ManagementAccounts.Add(account);
        await context.SaveChangesAsync();

        var result = await authService.AuthenticateAsync("timetable", "Pass123!");

        Assert.False(result.Succeeded);
        Assert.Contains("inactive", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TimetableService_RejectsInvalidEndTime()
    {
        using var context = CreateInMemoryContext(nameof(TimetableService_RejectsInvalidEndTime));
        var service = new TimetableService(context, NullLogger<TimetableService>.Instance);

        var activity = new JailTimetable
        {
            ActivityName = "Morning Roll Call",
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(9, 0, 0), // End before Start!
            IsActive = true
        };

        var result = await service.SaveActivityAsync(activity);

        Assert.False(result.Succeeded);
        Assert.Equal("End time must be later than start time.", result.ErrorMessage);
    }

    [Fact]
    public async Task GateRegister_RejectsZeroNumberOfPeople()
    {
        using var context = CreateInMemoryContext(nameof(GateRegister_RejectsZeroNumberOfPeople));
        var service = new GateRegisterService(context, NullLogger<GateRegisterService>.Instance);

        var entry = new GateRegister
        {
            VisitorName = "Syed Ali",
            VisitType = "Legal / Advocate",
            VisitPurpose = "Case consultation",
            NumberOfPeople = 0, // Must be > 0
            EntryTime = DateTime.Now
        };

        var result = await service.RecordEntryAsync(entry);

        Assert.False(result.Succeeded);
        Assert.Contains("greater than zero", result.ErrorMessage);
    }
}
