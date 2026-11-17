using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrisonManagementSystem.Models;

[Table("Prisoners", Schema = "dbo")]
public class Prisoner
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PrisonerID { get; set; }

    [Required]
    [MaxLength(30)]
    public string PrisonerNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? FatherName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(15)]
    [Column(TypeName = "varchar(15)")]
    public string? CNIC { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(150)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(25)]
    [Column(TypeName = "varchar(25)")]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(400)]
    public string? PhotoReference { get; set; }

    public DateOnly AdmissionDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string CustodyStatus { get; set; } = "In Custody";

    public int? CellID { get; set; }

    public int RegisteredByStaffID { get; set; }

    public DateOnly? ActualReleaseDate { get; set; }

    [MaxLength(100)]
    public string? ReleaseOrderReference { get; set; }

    public int? ReleaseRecordedByStaffID { get; set; }

    public DateOnly? TransferDate { get; set; }

    [MaxLength(200)]
    public string? TransferDestination { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [ForeignKey(nameof(CellID))]
    public virtual Cell? Cell { get; set; }

    [ForeignKey(nameof(RegisteredByStaffID))]
    public virtual Staff RegisteredByStaff { get; set; } = null!;

    [ForeignKey(nameof(ReleaseRecordedByStaffID))]
    public virtual Staff? ReleaseRecordedByStaff { get; set; }

    public virtual ICollection<CourtCase> CourtCases { get; set; } = new List<CourtCase>();
}
