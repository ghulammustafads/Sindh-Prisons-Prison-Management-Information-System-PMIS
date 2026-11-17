using System.ComponentModel.DataAnnotations;
using PrisonManagementSystem.Models;

namespace PrisonManagementSystem.ViewModels;

public class NewGateEntryForm
{
    [Required(ErrorMessage = "Visitor name is required.")]
    [Display(Name = "Visitor name")]
    public string VisitorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Visit type is required.")]
    [Display(Name = "Visit type")]
    public string VisitType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Visit purpose is required.")]
    [Display(Name = "Visit purpose")]
    public string VisitPurpose { get; set; } = string.Empty;

    [Display(Name = "Vehicle details (optional)")]
    public string? VehicleDetails { get; set; }

    [Required(ErrorMessage = "Number of people is required.")]
    [Range(1, 500, ErrorMessage = "Number of people must be at least 1.")]
    [Display(Name = "Number of people")]
    public int NumberOfPeople { get; set; } = 1;

    [Required(ErrorMessage = "Entry time is required.")]
    [Display(Name = "Entry time")]
    public DateTime EntryTime { get; set; } = DateTime.Now;
}

public class GateRegisterViewModel
{
    public string ActiveTab { get; set; } = "all"; // "all" or "inside"
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public List<GateRegister> Entries { get; set; } = new();

    public NewGateEntryForm NewEntry { get; set; } = new();

    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
