using PrisonManagementSystem.Services;

namespace PrisonManagementSystem.ViewModels;

public class DashboardViewModel
{
    public bool IsDatabaseAvailable { get; set; } = true;
    public string? DatabaseErrorMessage { get; set; }

    public int PrisonersInCustody { get; set; }
    public int ActiveStaff { get; set; }
    public int HearingsToday { get; set; }
    public int VisitorsInside { get; set; }

    public List<UpcomingHearingDto> UpcomingHearings { get; set; } = new();
    public List<BarrackSummaryDto> BarracksAndCells { get; set; } = new();
    public List<TimetableActivityDto> DailyActivities { get; set; } = new();
    public List<GateActivityDto> RecentGateActivity { get; set; } = new();
}
