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

    // Hex color in form #RRGGBB (optional)
    [StringLength(7)] public string? ColorHex { get; set; }

    // Icon identifier string (e.g., lucide-react icon name in kebab-case, like "paw-print")
    [StringLength(32)] public string? Icon { get; set; }

    // Icon type: "lucide" (default), "emoji", "url"
    [StringLength(16)] public string? IconType { get; set; }
}


