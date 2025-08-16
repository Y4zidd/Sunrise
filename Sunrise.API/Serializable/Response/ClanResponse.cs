using Sunrise.Shared.Database.Models.Clan;

namespace Sunrise.API.Serializable.Response;

public record ClanResponse(int Id, string Tag, string Name, int OwnerId, DateTime CreatedAt)
{
    public static ClanResponse FromEntity(Clan clan)
        => new(clan.Id, clan.Tag, clan.Name, clan.OwnerId, clan.CreatedAt);
}


