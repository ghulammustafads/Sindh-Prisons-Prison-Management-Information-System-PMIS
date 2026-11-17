using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrisonManagementSystem.Models;

[Table("Staff", Schema = "dbo")]
public class Staff
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int StaffID { get; set; }

    [Required]
    [MaxLength(30)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? OfficialRank { get; set; }

    public int RoleID { get; set; }

    public int? BarrackID { get; set; }

    [MaxLength(15)]
    [Column(TypeName = "varchar(15)")]
    public string? CNIC { get; set; }

    [MaxLength(25)]
    [Column(TypeName = "varchar(25)")]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public DateOnly JoiningDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [ForeignKey(nameof(RoleID))]
    public virtual StaffRole Role { get; set; } = null!;

    [ForeignKey(nameof(BarrackID))]
    public virtual Barrack? Barrack { get; set; }

    public virtual ManagementAccount? ManagementAccount { get; set; }
}
