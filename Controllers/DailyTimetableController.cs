using Microsoft.AspNetCore.Mvc;
using PrisonManagementSystem.Authorization;
using PrisonManagementSystem.Models;
using PrisonManagementSystem.Services;
using PrisonManagementSystem.ViewModels;

namespace PrisonManagementSystem.Controllers;

[EnforceAssignedRolePage(RoleConstants.TimetableOfficer)]
public class DailyTimetableController : Controller
{
    private readonly ITimetableService _timetableService;

    public DailyTimetableController(ITimetableService timetableService)
    {
        _timetableService = timetableService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? day, int? editId, string? status, string? error)
    {
        var model = new TimetableViewModel
        {
            SelectedDay = day,
            StatusMessage = status,
            ErrorMessage = error,
            Activities = await _timetableService.GetActivitiesAsync(day),
            AvailableBarracks = await _timetableService.GetActiveBarracksAsync(),
            AvailableStaff = await _timetableService.GetActiveStaffAsync()
        };

        if (editId.HasValue)
        {
            var activity = await _timetableService.GetActivityByIdAsync(editId.Value);
            if (activity != null)
            {
                model.Form = new TimetableActivityForm
                {
                    TimetableID = activity.TimetableID,
                    ActivityName = activity.ActivityName,
                    DayOfWeek = activity.DayOfWeek,
                    StartTime = activity.StartTime,
                    EndTime = activity.EndTime,
                    BarrackID = activity.BarrackID,
                    ResponsibleStaffID = activity.ResponsibleStaffID,
                    Instructions = activity.Instructions,
                    RowVersion = activity.RowVersion
                };
            }
        }

        return View(model);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Save(TimetableViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Activities = await _timetableService.GetActivitiesAsync(model.SelectedDay);
            model.AvailableBarracks = await _timetableService.GetActiveBarracksAsync();
            model.AvailableStaff = await _timetableService.GetActiveStaffAsync();
            return View("Index", model);
        }

        var activity = new JailTimetable
        {
            TimetableID = model.Form.TimetableID,
            ActivityName = model.Form.ActivityName,
            DayOfWeek = (byte?)model.Form.DayOfWeek,
            StartTime = model.Form.StartTime,
            EndTime = model.Form.EndTime,
            BarrackID = model.Form.BarrackID,
            ResponsibleStaffID = model.Form.ResponsibleStaffID,
            Instructions = model.Form.Instructions
        };

        var result = await _timetableService.SaveActivityAsync(activity, model.Form.RowVersion);
        if (!result.Succeeded)
        {
            model.ErrorMessage = result.ErrorMessage;
            model.Activities = await _timetableService.GetActivitiesAsync(model.SelectedDay);
            model.AvailableBarracks = await _timetableService.GetActiveBarracksAsync();
            model.AvailableStaff = await _timetableService.GetActiveStaffAsync();
            return View("Index", model);
        }

        return RedirectToAction("Index", new { day = model.SelectedDay, status = "Activity saved successfully." });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Delete(int id, int? day)
    {
        var result = await _timetableService.DeleteActivityAsync(id);
        if (!result.Succeeded)
        {
            return RedirectToAction("Index", new { day, error = result.ErrorMessage });
        }

        return RedirectToAction("Index", new { day, status = "Activity removed successfully." });
    }
}
