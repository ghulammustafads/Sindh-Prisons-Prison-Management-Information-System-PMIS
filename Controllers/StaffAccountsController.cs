using Microsoft.AspNetCore.Mvc;
using PrisonManagementSystem.Authorization;
using PrisonManagementSystem.Models;
using PrisonManagementSystem.Services;
using PrisonManagementSystem.ViewModels;

namespace PrisonManagementSystem.Controllers;

[EnforceAssignedRolePage(RoleConstants.SystemAdministrator)]
public class StaffAccountsController : Controller
{
    private readonly IStaffService _staffService;

    public StaffAccountsController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? tab = "staff",
        string? search = null,
        string? status = null,
        string? error = null)
    {
        var activeTab = tab?.ToLower() switch
        {
            "roles" => "roles",
            "accounts" => "accounts",
            _ => "staff"
        };

        var model = new StaffAccountsViewModel
        {
            ActiveTab = activeTab,
            SearchTerm = search,
            StatusMessage = status,
            ErrorMessage = error,
            StaffList = await _staffService.GetStaffListAsync(search),
            RolesList = await _staffService.GetRolesAsync(),
            AccountsList = await _staffService.GetAccountsAsync(),
            AvailableRoles = await _staffService.GetRolesAsync(),
            AvailableBarracks = await _staffService.GetActiveBarracksAsync(),
            AvailableStaffWithoutAccounts = await _staffService.GetStaffWithoutAccountsAsync()
        };

        return View(model);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveStaff(StaffAccountsViewModel model)
    {
        var staff = new Staff
        {
            StaffID = model.StaffForm.StaffID,
            FullName = model.StaffForm.FullName.Trim(),
            EmployeeNumber = model.StaffForm.EmployeeNumber.Trim(),
            OfficialRank = model.StaffForm.OfficialRank.Trim(),
            RoleID = model.StaffForm.RoleID,
            BarrackID = model.StaffForm.BarrackID,
            Phone = model.StaffForm.Phone.Trim(),
            JoiningDate = model.StaffForm.JoiningDate,
            DateOfBirth = model.StaffForm.DateOfBirth,
            CNIC = model.StaffForm.CNIC.Trim(),
            Address = model.StaffForm.Address.Trim()
        };

        StaffResult result;
        if (staff.StaffID == 0)
        {
            result = await _staffService.CreateStaffAsync(staff);
        }
        else
        {
            result = await _staffService.UpdateStaffAsync(staff, model.StaffForm.RowVersion);
        }

        if (!result.Succeeded)
        {
            return RedirectToAction("Index", new { tab = "staff", error = result.ErrorMessage });
        }

        return RedirectToAction("Index", new { tab = "staff", status = "Staff member saved successfully." });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ToggleStaffStatus(int staffId)
    {
        var result = await _staffService.ToggleStaffStatusAsync(staffId);
        if (!result.Succeeded)
        {
            return RedirectToAction("Index", new { tab = "staff", error = result.ErrorMessage });
        }

        return RedirectToAction("Index", new { tab = "staff", status = "Staff status updated." });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveRole(StaffAccountsViewModel model)
    {
        StaffResult result;
        if (model.RoleForm.RoleID == 0)
        {
            result = await _staffService.CreateRoleAsync(model.RoleForm.RoleName, model.RoleForm.Description);
        }
        else
        {
            result = await _staffService.UpdateRoleAsync(model.RoleForm.RoleID, model.RoleForm.RoleName, model.RoleForm.Description, true);
        }

        if (!result.Succeeded)
        {
            return RedirectToAction("Index", new { tab = "roles", error = result.ErrorMessage });
        }

        return RedirectToAction("Index", new { tab = "roles", status = "Role saved successfully." });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveAccount(StaffAccountsViewModel model)
    {
        var result = await _staffService.CreateAccountAsync(
            model.AccountForm.StaffID,
            model.AccountForm.Username,
            model.AccountForm.Password);

        if (!result.Succeeded)
        {
            return RedirectToAction("Index", new { tab = "accounts", error = result.ErrorMessage });
        }

        return RedirectToAction("Index", new { tab = "accounts", status = "Management account created successfully." });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ChangePassword(StaffAccountsViewModel model)
    {
        var result = await _staffService.UpdatePasswordAsync(
            model.PasswordForm.AccountID,
            model.PasswordForm.NewPassword,
            model.PasswordForm.RowVersion);

        if (!result.Succeeded)
        {
            return RedirectToAction("Index", new { tab = "accounts", error = result.ErrorMessage });
        }

        return RedirectToAction("Index", new { tab = "accounts", status = "Password updated securely." });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ToggleAccountStatus(int accountId)
    {
        var result = await _staffService.ToggleAccountStatusAsync(accountId);
        if (!result.Succeeded)
        {
            return RedirectToAction("Index", new { tab = "accounts", error = result.ErrorMessage });
        }

        return RedirectToAction("Index", new { tab = "accounts", status = "Account status updated." });
    }
}
