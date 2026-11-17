using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrisonManagementSystem.Models;

[Table("GateRegister", Schema = "dbo")]
public class GateRegister
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EntryID { get; set; }

    [Required]
    [MaxLength(150)]
    public string VisitorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string VisitType { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? VisitPurpose { get; set; }

    [MaxLength(200)]
    public string? VehicleNumber { get; set; }

    [NotMapped]
    public string? VehicleDetails
    {
        get => VehicleNumber;
        set => VehicleNumber = value;
    }

    [Range(1, 1000)]
    public int NumberOfPeople { get; set; } = 1;

    public DateTime EntryTime { get; set; }

    public DateTime? ExitTime { get; set; }
}
