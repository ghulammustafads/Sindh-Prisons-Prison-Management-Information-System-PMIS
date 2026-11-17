using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrisonManagementSystem.Models;

[Table("Cells", Schema = "dbo")]
public class Cell
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CellID { get; set; }

    public int BarrackID { get; set; }

    [Required]
    [MaxLength(20)]
    public string CellNumber { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(BarrackID))]
    public virtual Barrack Barrack { get; set; } = null!;

    public virtual ICollection<Prisoner> Prisoners { get; set; } = new List<Prisoner>();
}
