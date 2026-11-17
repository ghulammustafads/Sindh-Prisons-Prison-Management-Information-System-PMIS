using System.ComponentModel.DataAnnotations;
using PrisonManagementSystem.Models;

namespace PrisonManagementSystem.ViewModels;

public class TimetableActivityForm
{
    public int TimetableID { get; set; }

    [Required(ErrorMessage = "Activity name is required.")]
    [Display(Name = "Activity name")]
    public string ActivityName { get; set; } = string.Empty;

    [Display(Name = "Day")]
    public int? DayOfWeek { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    [Display(Name = "Start time")]
    public TimeSpan StartTime { get; set; } = new TimeSpan(8, 0, 0);

    [Required(ErrorMessage = "End time is required.")]
    [Display(Name = "End time")]
    public TimeSpan EndTime { get; set; } = new TimeSpan(9, 0, 0);

    [Display(Name = "Barrack")]
    public int? BarrackID { get; set; }

    [Display(Name = "Responsible staff")]
    public int? ResponsibleStaffID { get; set; }

    [Display(Name = "Instructions")]
    public string? Instructions { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class TimetableViewModel
{
    public int? SelectedDay { get; set; }
    public List<JailTimetable> Activities { get; set; } = new();
    public List<Barrack> AvailableBarracks { get; set; } = new();
    public List<Staff> AvailableStaff { get; set; } = new();

    public TimetableActivityForm Form { get; set; } = new();

    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
