using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sunrise.Shared.Database.Models.Users;

namespace Sunrise.Shared.Database.Models.Clan;

[Table("clan")]
public class Clan
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(6)]
    public string Tag { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    [Required]
    public int OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public User? Owner { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}


