using System.ComponentModel.DataAnnotations;
using PrisonManagementSystem.Models;
using PrisonManagementSystem.Services;

namespace PrisonManagementSystem.ViewModels;

public class AddStaffForm
{
    public int StaffID { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Employee number is required.")]
    [Display(Name = "Employee number")]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Official rank is required.")]
    [Display(Name = "Official rank")]
    public string OfficialRank { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a role.")]
    [Display(Name = "Role")]
    public int RoleID { get; set; }

    [Display(Name = "Assigned barrack (optional)")]
    public int? BarrackID { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Joining date is required.")]
    [Display(Name = "Joining date")]
    public DateOnly JoiningDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Date of birth is required.")]
    [Display(Name = "Date of birth")]
    public DateOnly DateOfBirth { get; set; } = new DateOnly(1990, 1, 1);

    [Required(ErrorMessage = "CNIC is required.")]
    [Display(Name = "CNIC")]
    public string CNIC { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    public byte[]? RowVersion { get; set; }
}

public class AddRoleForm
{
    public int RoleID { get; set; }

    [Required(ErrorMessage = "Role name is required.")]
    [Display(Name = "Role name")]
    public string RoleName { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }
}

public class AddAccountForm
{
    [Required(ErrorMessage = "Please select a staff member.")]
    [Display(Name = "Staff member")]
    public int StaffID { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordForm
{
    [Required]
    public int AccountID { get; set; }

    [Required(ErrorMessage = "New password is required.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm password.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public byte[]? RowVersion { get; set; }
}

public class StaffAccountsViewModel
{
    public string ActiveTab { get; set; } = "staff"; // "staff", "roles", "accounts"
    public string? SearchTerm { get; set; }

    public List<StaffDto> StaffList { get; set; } = new();
    public List<RoleDto> RolesList { get; set; } = new();
    public List<AccountDto> AccountsList { get; set; } = new();

    public AddStaffForm StaffForm { get; set; } = new();
    public AddRoleForm RoleForm { get; set; } = new();
    public AddAccountForm AccountForm { get; set; } = new();
    public ChangePasswordForm PasswordForm { get; set; } = new();

    public List<RoleDto> AvailableRoles { get; set; } = new();
    public List<Barrack> AvailableBarracks { get; set; } = new();
    public List<Staff> AvailableStaffWithoutAccounts { get; set; } = new();

    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
