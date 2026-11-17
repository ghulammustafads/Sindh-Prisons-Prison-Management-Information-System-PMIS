using Microsoft.AspNetCore.Mvc;
using PrisonManagementSystem.Authorization;
using PrisonManagementSystem.Models;
using PrisonManagementSystem.Services;
using PrisonManagementSystem.ViewModels;

namespace PrisonManagementSystem.Controllers;

[EnforceAssignedRolePage(RoleConstants.Gatekeeper)]
public class GateRegisterController : Controller
{
    private readonly IGateRegisterService _gateService;

    public GateRegisterController(IGateRegisterService gateService)
    {
        _gateService = gateService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? tab = "all", DateTime? fromDate = null, DateTime? toDate = null, string? status = null, string? error = null)
    {
        var activeTab = string.Equals(tab, "inside", StringComparison.OrdinalIgnoreCase) ? "inside" : "all";
        var isStillInsideOnly = activeTab == "inside";

        var model = new GateRegisterViewModel
        {
            ActiveTab = activeTab,
            FromDate = fromDate,
            ToDate = toDate,
            StatusMessage = status,
            ErrorMessage = error,
            Entries = await _gateService.GetEntriesAsync(fromDate, toDate, isStillInsideOnly)
        };

        return View(model);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RecordEntry(GateRegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Entries = await _gateService.GetEntriesAsync(model.FromDate, model.ToDate, model.ActiveTab == "inside");
            return View("Index", model);
        }

        var entry = new GateRegister
        {
            VisitorName = model.NewEntry.VisitorName.Trim(),
            VisitType = model.NewEntry.VisitType.Trim(),
            VisitPurpose = model.NewEntry.VisitPurpose.Trim(),
            VehicleNumber = model.NewEntry.VehicleDetails?.Trim(),
            NumberOfPeople = model.NewEntry.NumberOfPeople,
            EntryTime = model.NewEntry.EntryTime
        };

        var result = await _gateService.RecordEntryAsync(entry);
        if (!result.Succeeded)
        {
            model.ErrorMessage = result.ErrorMessage;
            model.Entries = await _gateService.GetEntriesAsync(model.FromDate, model.ToDate, model.ActiveTab == "inside");
            return View("Index", model);
        }

        return RedirectToAction("Index", new
        {
            tab = model.ActiveTab,
            fromDate = model.FromDate?.ToString("yyyy-MM-dd"),
            toDate = model.ToDate?.ToString("yyyy-MM-dd"),
            status = "Visitor entry logged successfully."
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RecordExit(int entryId, string? tab, DateTime? fromDate, DateTime? toDate)
    {
        var result = await _gateService.RecordExitAsync(entryId);
        if (!result.Succeeded)
        {
            return RedirectToAction("Index", new
            {
                tab,
                fromDate = fromDate?.ToString("yyyy-MM-dd"),
                toDate = toDate?.ToString("yyyy-MM-dd"),
                error = result.ErrorMessage
            });
        }

        return RedirectToAction("Index", new
        {
            tab,
            fromDate = fromDate?.ToString("yyyy-MM-dd"),
            toDate = toDate?.ToString("yyyy-MM-dd"),
            status = "Visitor exit departure logged."
        });
    }
}
