using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Enums.Clan;

namespace Sunrise.Shared.Database.Models.Clan;

[Table("clan_join_request")]
public class ClanJoinRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ClanId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public ClanJoinRequestStatus Status { get; set; } = ClanJoinRequestStatus.Pending;

    public int RequestedBy { get; set; }
    public int? ActionedBy { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(ClanId))]
    public Clan? Clan { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}


