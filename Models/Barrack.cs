using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrisonManagementSystem.Models;

[Table("Barracks", Schema = "dbo")]
public class Barrack
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BarrackID { get; set; }

    [Required]
    [MaxLength(20)]
    public string BarrackCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string BarrackName { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Category { get; set; } = string.Empty;

    public int? InChargeStaffID { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(InChargeStaffID))]
    public virtual Staff? InChargeStaff { get; set; }

    public virtual ICollection<Staff> AssignedStaff { get; set; } = new List<Staff>();
    public virtual ICollection<Cell> Cells { get; set; } = new List<Cell>();
    public virtual ICollection<JailTimetable> TimetableEntries { get; set; } = new List<JailTimetable>();
}
