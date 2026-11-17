using Microsoft.AspNetCore.Mvc;
using PrisonManagementSystem.Authorization;
using PrisonManagementSystem.Services;
using PrisonManagementSystem.ViewModels;

namespace PrisonManagementSystem.Controllers;

[EnforceAssignedRolePage(RoleConstants.JailSuperintendent)]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var data = await _dashboardService.GetDashboardDataAsync();

        var viewModel = new DashboardViewModel
        {
            IsDatabaseAvailable = data.IsDatabaseAvailable,
            DatabaseErrorMessage = data.DatabaseErrorMessage,
            PrisonersInCustody = data.PrisonersInCustody,
            ActiveStaff = data.ActiveStaff,
            HearingsToday = data.HearingsToday,
            VisitorsInside = data.VisitorsInside,
            UpcomingHearings = data.UpcomingHearings,
            BarracksAndCells = data.BarracksAndCells,
            DailyActivities = data.DailyActivities,
            RecentGateActivity = data.RecentGateActivity
        };

        return View(viewModel);
    }
}
