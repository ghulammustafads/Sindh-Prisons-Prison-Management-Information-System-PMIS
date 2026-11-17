using Microsoft.AspNetCore.Mvc;
using PrisonManagementSystem.Authorization;
using PrisonManagementSystem.Services;
using PrisonManagementSystem.ViewModels;

namespace PrisonManagementSystem.Controllers;

[EnforceAssignedRolePage(RoleConstants.CourtRecordsOfficer)]
public class CourtHearingsController : Controller
{
    private readonly ICourtService _courtService;

    public CourtHearingsController(ICourtService courtService)
    {
        _courtService = courtService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, DateTime? date, string? status, string? error)
    {
        var model = new CourtHearingsViewModel
        {
            SearchCase = search,
            DateFilter = date,
            StatusMessage = status,
            ErrorMessage = error,
            Hearings = await _courtService.GetHearingsAsync(search, date),
            AvailableCases = await _courtService.GetCasesForDropdownAsync(),
            AvailableStaff = await _courtService.GetStaffForDropdownAsync()
        };

        return View(model);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Schedule(CourtHearingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Hearings = await _courtService.GetHearingsAsync(model.SearchCase, model.DateFilter);
            model.AvailableCases = await _courtService.GetCasesForDropdownAsync();
            model.AvailableStaff = await _courtService.GetStaffForDropdownAsync();
            return View("Index", model);
        }

        // Authenticated user's staff ID
        var staffIdClaim = User.FindFirst("StaffID")?.Value;
        if (!int.TryParse(staffIdClaim, out var recordedByStaffId))
        {
            model.ErrorMessage = "Unable to determine authenticated staff identity.";
            model.Hearings = await _courtService.GetHearingsAsync(model.SearchCase, model.DateFilter);
            model.AvailableCases = await _courtService.GetCasesForDropdownAsync();
            model.AvailableStaff = await _courtService.GetStaffForDropdownAsync();
            return View("Index", model);
        }

        var hearingDateTime = model.ScheduleForm.HearingDate.ToDateTime(TimeOnly.FromTimeSpan(model.ScheduleForm.HearingTime));

        var result = await _courtService.ScheduleHearingAsync(
            model.ScheduleForm.CaseID,
            hearingDateTime,
            model.ScheduleForm.HearingType,
            model.ScheduleForm.ResponsibleStaffID,
            recordedByStaffId,
            model.ScheduleForm.Remarks);

        if (!result.Succeeded)
        {
            model.ErrorMessage = result.ErrorMessage;
            model.Hearings = await _courtService.GetHearingsAsync(model.SearchCase, model.DateFilter);
            model.AvailableCases = await _courtService.GetCasesForDropdownAsync();
            model.AvailableStaff = await _courtService.GetStaffForDropdownAsync();
            return View("Index", model);
        }

        return RedirectToAction("Index", new
        {
            search = model.SearchCase,
            date = model.DateFilter?.ToString("yyyy-MM-dd"),
            status = "Hearing scheduled successfully."
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RecordOutcome(RecordOutcomeForm form, string? search, DateTime? date)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Index", new { search, date = date?.ToString("yyyy-MM-dd"), error = "Please provide valid outcome information." });
        }

        var result = await _courtService.RecordOutcomeAsync(
            form.TrialID,
            form.HearingStatus,
            form.AttendanceStatus,
            form.Outcome,
            form.Remarks,
            form.RowVersion);

        if (!result.Succeeded)
        {
            return RedirectToAction("Index", new { search, date = date?.ToString("yyyy-MM-dd"), error = result.ErrorMessage });
        }

        return RedirectToAction("Index", new { search, date = date?.ToString("yyyy-MM-dd"), status = "Hearing outcome recorded." });
    }
}
