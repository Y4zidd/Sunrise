using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Database.Models.Clan;
using Sunrise.Shared.Database.Models.Users;

namespace Sunrise.Shared.Database.Repositories;

public class ClanRepository(SunriseDbContext dbContext)
{
    public async Task<Clan?> GetById(int id, CancellationToken ct = default)
        => await dbContext.Set<Clan>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Clan?> GetByTag(string tag, CancellationToken ct = default)
        => await dbContext.Set<Clan>().FirstOrDefaultAsync(c => c.Tag == tag, ct);

    public async Task<bool> IsUserInAnyClan(int userId, CancellationToken ct = default)
        => await dbContext.Set<User>().AnyAsync(u => u.Id == userId && u.ClanId != 0, ct);

    public async Task CreateClan(Clan clan, int ownerId, CancellationToken ct = default)
    {
        await dbContext.Set<Clan>().AddAsync(clan, ct);
        await dbContext.SaveChangesAsync(ct);
        var owner = await dbContext.Set<User>().FirstAsync(u => u.Id == ownerId, ct);
        owner.ClanId = clan.Id;
        owner.ClanPriv = 3; // owner/admin level
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task JoinClan(int clanId, int userId, CancellationToken ct = default)
    {
        var user = await dbContext.Set<User>().FirstAsync(u => u.Id == userId, ct);
        user.ClanId = clanId;
        user.ClanPriv = 1; // member
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task LeaveClan(int userId, CancellationToken ct = default)
    {
        var user = await dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return;
        user.ClanId = 0;
        user.ClanPriv = 0;
        await dbContext.SaveChangesAsync(ct);
    }
}


