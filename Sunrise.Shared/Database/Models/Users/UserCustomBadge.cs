using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sunrise.Shared.Database.Models.Users;

[Table("user_custom_badge")]
public class UserCustomBadge
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey("UserId")] public User User { get; set; }

    [StringLength(32)] public string Name { get; set; } = string.Empty;
}


