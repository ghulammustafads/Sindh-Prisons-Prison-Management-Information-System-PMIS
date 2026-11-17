using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrisonManagementSystem.Models;

[Table("JailTimetable", Schema = "dbo")]
public class JailTimetable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TimetableID { get; set; }

    [Required]
    [MaxLength(100)]
    public string ActivityName { get; set; } = string.Empty;

    /// <summary>
    /// tinyint (1 = Monday through 7 = Sunday). NULL means daily activity.
    /// </summary>
    public byte? DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public int? BarrackID { get; set; }

    public int? ResponsibleStaffID { get; set; }

    [MaxLength(500)]
    public string? Instructions { get; set; }

    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [ForeignKey(nameof(BarrackID))]
    public virtual Barrack? Barrack { get; set; }

    [ForeignKey(nameof(ResponsibleStaffID))]
    public virtual Staff? ResponsibleStaff { get; set; }
}
