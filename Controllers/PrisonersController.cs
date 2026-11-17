using Microsoft.AspNetCore.Mvc;
using PrisonManagementSystem.Authorization;
using PrisonManagementSystem.Models;
using PrisonManagementSystem.Services;
using PrisonManagementSystem.ViewModels;

namespace PrisonManagementSystem.Controllers;

[EnforceAssignedRolePage(RoleConstants.PrisonerRecordsOfficer)]
public class PrisonersController : Controller
{
    private readonly IPrisonerService _prisonerService;
    private readonly ICourtService _courtService;

    public PrisonersController(IPrisonerService prisonerService, ICourtService courtService)
    {
        _prisonerService = prisonerService;
        _courtService = courtService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? tab = "records",
        string? search = null,
        string? status = "All statuses",
        int? selectedPrisonerId = null,
        string? successMsg = null,
        string? errorMsg = null)
    {
        var activeTab = string.Equals(tab, "cases", StringComparison.OrdinalIgnoreCase) ? "cases" : "records";

        var prisonersList = await _prisonerService.GetPrisonersAsync(search, status);
        int targetPrisonerId = selectedPrisonerId ?? (prisonersList.FirstOrDefault()?.PrisonerID ?? 0);

        PrisonerDetailDto? selectedDetail = null;
        if (targetPrisonerId > 0)
        {
            selectedDetail = await _prisonerService.GetPrisonerDetailAsync(targetPrisonerId);
        }

        var barracks = await _prisonerService.GetActiveBarracksWithCellsAsync();
        var staff = await _courtService.GetStaffForDropdownAsync();

        var model = new PrisonersViewModel
        {
            ActiveTab = activeTab,
            SearchTerm = search,
            StatusFilter = status,
            Prisoners = prisonersList,
            SelectedPrisoner = selectedDetail,
            AvailableBarracks = barracks,
            AvailableStaff = staff,
            StatusMessage = successMsg,
            ErrorMessage = errorMsg
        };

        if (selectedDetail != null)
        {
            model.NewCaseForm.PrisonerID = selectedDetail.PrisonerID;
        }

        return View(model);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Register(PrisonersViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Prisoners = await _prisonerService.GetPrisonersAsync(model.SearchTerm, model.StatusFilter);
            model.AvailableBarracks = await _prisonerService.GetActiveBarracksWithCellsAsync();
            model.AvailableStaff = await _courtService.GetStaffForDropdownAsync();
            return View("Index", model);
        }

        var staffIdClaim = User.FindFirst("StaffID")?.Value;
        if (!int.TryParse(staffIdClaim, out var registeredByStaffId))
        {
            model.ErrorMessage = "Unable to determine authenticated staff recorder identity.";
            model.Prisoners = await _prisonerService.GetPrisonersAsync(model.SearchTerm, model.StatusFilter);
            model.AvailableBarracks = await _prisonerService.GetActiveBarracksWithCellsAsync();
            model.AvailableStaff = await _courtService.GetStaffForDropdownAsync();
            return View("Index", model);
        }

        var prisoner = new Prisoner
        {
            PrisonerID = model.RegisterForm.PrisonerID,
            PrisonerNumber = model.RegisterForm.PrisonerNumber?.Trim() ?? string.Empty,
            FullName = model.RegisterForm.FullName.Trim(),
            FatherName = model.RegisterForm.FatherName?.Trim(),
            DateOfBirth = model.RegisterForm.DateOfBirth,
            Gender = model.RegisterForm.Gender.Trim(),
            CNIC = model.RegisterForm.CNIC?.Trim(),
            Address = model.RegisterForm.Address?.Trim(),
            EmergencyContactName = model.RegisterForm.EmergencyContact?.Trim(),
            EmergencyContactPhone = model.RegisterForm.EmergencyContactPhone?.Trim(),
            AdmissionDate = model.RegisterForm.AdmissionDate,
            CellID = model.RegisterForm.CellID,
            CustodyStatus = "In Custody"
        };

        PrisonerResult result;
        if (prisoner.PrisonerID == 0)
        {
            result = await _prisonerService.RegisterPrisonerAsync(prisoner, registeredByStaffId);
        }
        else
        {
            result = await _prisonerService.UpdatePrisonerAsync(prisoner, model.RegisterForm.RowVersion);
        }

        if (!result.Succeeded)
        {
            model.ErrorMessage = result.ErrorMessage;
            model.Prisoners = await _prisonerService.GetPrisonersAsync(model.SearchTerm, model.StatusFilter);
            model.AvailableBarracks = await _prisonerService.GetActiveBarracksWithCellsAsync();
            model.AvailableStaff = await _courtService.GetStaffForDropdownAsync();
            return View("Index", model);
        }

        return RedirectToAction("Index", new
        {
            tab = "records",
            search = model.SearchTerm,
            status = model.StatusFilter,
            selectedPrisonerId = result.PrisonerId,
            successMsg = "Prisoner registered successfully."
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddCase(PrisonersViewModel model)
    {
        var courtCase = new CourtCase
        {
            PrisonerID = model.NewCaseForm.PrisonerID,
            CaseNumber = model.NewCaseForm.CaseNumber.Trim(),
            CaseYear = (short)model.NewCaseForm.CaseYear,
            CaseType = model.NewCaseForm.CaseType.Trim(),
            CourtName = model.NewCaseForm.CourtName.Trim(),
            FIRNumber = model.NewCaseForm.FIRNumber?.Trim(),
            PoliceStation = model.NewCaseForm.PoliceStation?.Trim(),
            AllegedOffence = model.NewCaseForm.AllegedOffence?.Trim(),
            CaseStatus = "Pending",
            LawyerName = model.NewCaseForm.LawyerName?.Trim(),
            Remarks = model.NewCaseForm.Remarks?.Trim()
        };

        var result = await _prisonerService.AddCaseAsync(courtCase, model.NewCaseForm.ResponsibleStaffID);
        if (!result.Succeeded)
        {
            return RedirectToAction("Index", new
            {
                tab = "cases",
                selectedPrisonerId = model.NewCaseForm.PrisonerID,
                errorMsg = result.ErrorMessage
            });
        }

        return RedirectToAction("Index", new
        {
            tab = "cases",
            selectedPrisonerId = model.NewCaseForm.PrisonerID,
            successMsg = "Court case added for prisoner."
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetCellsForBarrack(int barrackId)
    {
        var cells = await _prisonerService.GetCellsByBarrackAsync(barrackId);
        return Json(cells.Select(c => new { c.CellID, c.CellNumber, c.Capacity }));
    }
}
