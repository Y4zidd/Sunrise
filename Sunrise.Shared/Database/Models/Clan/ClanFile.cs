using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Database.Models.Clan;
using Sunrise.Shared.Enums;

namespace Sunrise.Shared.Database.Models.Clan;

[Table("clan_file")]
[Index(nameof(OwnerId))]
[Index(nameof(OwnerId), nameof(Type))]
public class ClanFile
{
    public int Id { get; set; }

    [ForeignKey(nameof(OwnerId))]
    public Clan Clan { get; set; }

    public int OwnerId { get; set; }
    public string Path { get; set; }
    public FileType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
