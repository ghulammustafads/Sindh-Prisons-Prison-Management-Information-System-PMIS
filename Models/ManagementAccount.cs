using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrisonManagementSystem.Models;

[Table("ManagementAccounts", Schema = "dbo")]
public class ManagementAccount
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AccountID { get; set; }

    public int StaffID { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAtUtc { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    [ForeignKey(nameof(StaffID))]
    public virtual Staff Staff { get; set; } = null!;
}
