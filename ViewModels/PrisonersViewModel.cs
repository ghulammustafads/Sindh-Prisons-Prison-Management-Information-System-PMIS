using System.ComponentModel.DataAnnotations;
using PrisonManagementSystem.Models;
using PrisonManagementSystem.Services;

namespace PrisonManagementSystem.ViewModels;

public class RegisterPrisonerForm
{
    public int PrisonerID { get; set; }

    [Display(Name = "Prisoner number (optional, auto-generated if blank)")]
    public string? PrisonerNumber { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Father name (optional)")]
    public string? FatherName { get; set; }

    [Display(Name = "Date of birth (optional)")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = "Male";

    [Display(Name = "Identification (optional)")]
    public string? CNIC { get; set; }

    [Display(Name = "Address (optional)")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Admission date is required.")]
    [Display(Name = "Admission date")]
    public DateOnly AdmissionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Barrack")]
    public int? BarrackID { get; set; }

    [Display(Name = "Cell")]
    public int? CellID { get; set; }

    [Display(Name = "Emergency contact")]
    public string? EmergencyContact { get; set; }

    [Display(Name = "Emergency contact phone")]
    public string? EmergencyContactPhone { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class AddPrisonerCaseForm
{
    [Required]
    public int PrisonerID { get; set; }

    [Required(ErrorMessage = "Case number is required.")]
    [Display(Name = "Case number")]
    public string CaseNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Case year is required.")]
    [Display(Name = "Case year")]
    public int CaseYear { get; set; } = DateTime.Now.Year;

    [Required(ErrorMessage = "Case type is required.")]
    [Display(Name = "Case type")]
    public string CaseType { get; set; } = "Criminal";

    [Required(ErrorMessage = "Court name is required.")]
    [Display(Name = "Court name")]
    public string CourtName { get; set; } = string.Empty;

    [Required(ErrorMessage = "FIR number is required.")]
    [Display(Name = "FIR number")]
    public string FIRNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Police station is required.")]
    [Display(Name = "Police station")]
    public string PoliceStation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Alleged offence is required.")]
    [Display(Name = "Alleged offence")]
    public string AllegedOffence { get; set; } = string.Empty;

    [Display(Name = "Lawyer name")]
    public string? LawyerName { get; set; }

    [Required(ErrorMessage = "Please select responsible staff.")]
    [Display(Name = "Responsible staff")]
    public int ResponsibleStaffID { get; set; }

    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }
}

public class PrisonersViewModel
{
    public string ActiveTab { get; set; } = "records"; // "records" or "cases"
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; } = "All statuses";

    public List<PrisonerListItemDto> Prisoners { get; set; } = new();
    public PrisonerDetailDto? SelectedPrisoner { get; set; }

    public RegisterPrisonerForm RegisterForm { get; set; } = new();
    public AddPrisonerCaseForm NewCaseForm { get; set; } = new();

    public List<Barrack> AvailableBarracks { get; set; } = new();
    public List<Cell> AvailableCells { get; set; } = new();
    public List<Staff> AvailableStaff { get; set; } = new();

    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
