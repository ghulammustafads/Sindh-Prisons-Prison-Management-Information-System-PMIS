using System.ComponentModel.DataAnnotations;
using PrisonManagementSystem.Models;
using PrisonManagementSystem.Services;

namespace PrisonManagementSystem.ViewModels;

public class ScheduleHearingForm
{
    [Required(ErrorMessage = "Please select a prisoner court case.")]
    [Display(Name = "Prisoner case")]
    public int CaseID { get; set; }

    [Required(ErrorMessage = "Hearing date is required.")]
    [Display(Name = "Hearing date")]
    public DateOnly HearingDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Hearing time is required.")]
    [Display(Name = "Hearing time")]
    public TimeSpan HearingTime { get; set; } = new TimeSpan(10, 0, 0);

    [Required(ErrorMessage = "Hearing type is required.")]
    [Display(Name = "Hearing type")]
    public string HearingType { get; set; } = "Bail Hearing";

    [Required(ErrorMessage = "Please select a responsible staff member.")]
    [Display(Name = "Responsible staff")]
    public int ResponsibleStaffID { get; set; }

    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }
}

public class RecordOutcomeForm
{
    public int TrialID { get; set; }

    [Required]
    public string HearingStatus { get; set; } = "Completed";

    [Required]
    public string AttendanceStatus { get; set; } = "Attended";

    public string? Outcome { get; set; }
    public string? Remarks { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CourtHearingsViewModel
{
    public string? SearchCase { get; set; }
    public DateTime? DateFilter { get; set; }

    public List<HearingDto> Hearings { get; set; } = new();
    public List<CaseLookupDto> AvailableCases { get; set; } = new();
    public List<Staff> AvailableStaff { get; set; } = new();

    public ScheduleHearingForm ScheduleForm { get; set; } = new();

    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
