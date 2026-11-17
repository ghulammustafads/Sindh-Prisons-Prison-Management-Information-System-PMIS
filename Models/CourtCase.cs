using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrisonManagementSystem.Models;

[Table("CourtCases", Schema = "dbo")]
public class CourtCase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CaseID { get; set; }

    public int PrisonerID { get; set; }

    [Required]
    [MaxLength(50)]
    public string CaseNumber { get; set; } = string.Empty;

    public short CaseYear { get; set; }

    [Required]
    [MaxLength(50)]
    public string CaseType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CourtName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? FIRNumber { get; set; }

    [MaxLength(150)]
    public string? PoliceStation { get; set; }

    [MaxLength(500)]
    public string? AllegedOffence { get; set; }

    [Required]
    [MaxLength(30)]
    public string CaseStatus { get; set; } = "Pending";

    [MaxLength(150)]
    public string? LawyerName { get; set; }

    public int ResponsibleStaffID { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [ForeignKey(nameof(PrisonerID))]
    public virtual Prisoner Prisoner { get; set; } = null!;

    [ForeignKey(nameof(ResponsibleStaffID))]
    public virtual Staff ResponsibleStaff { get; set; } = null!;

    public virtual ICollection<CourtTrial> CourtTrials { get; set; } = new List<CourtTrial>();
}
