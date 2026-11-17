using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrisonManagementSystem.Models;

[Table("CourtTrials", Schema = "dbo")]
public class CourtTrial
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TrialID { get; set; }

    public int CaseID { get; set; }

    public DateTime HearingAt { get; set; }

    [Required]
    [MaxLength(50)]
    public string HearingType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string HearingStatus { get; set; } = "Scheduled";

    [Required]
    [MaxLength(20)]
    public string AttendanceStatus { get; set; } = "Pending";

    [MaxLength(20)]
    public string? AttendanceMethod { get; set; }

    [MaxLength(500)]
    public string? Outcome { get; set; }

    public int ResponsibleStaffID { get; set; }

    public int RecordedByStaffID { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [ForeignKey(nameof(CaseID))]
    public virtual CourtCase Case { get; set; } = null!;

    [ForeignKey(nameof(ResponsibleStaffID))]
    public virtual Staff ResponsibleStaff { get; set; } = null!;

    [ForeignKey(nameof(RecordedByStaffID))]
    public virtual Staff RecordedByStaff { get; set; } = null!;
}
