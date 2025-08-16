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

    public async Task<Clan?> GetByOwner(int ownerId, CancellationToken ct = default)
        => await dbContext.Set<Clan>().FirstOrDefaultAsync(c => c.OwnerId == ownerId, ct);

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

    public async Task<List<User>> GetMembers(int clanId, CancellationToken ct = default)
        => await dbContext.Set<User>().Where(u => u.ClanId == clanId).ToListAsync(ct);

    public async Task SetOwner(int clanId, int newOwnerId, CancellationToken ct = default)
    {
        var clan = await dbContext.Set<Clan>().FirstOrDefaultAsync(c => c.Id == clanId, ct);
        if (clan == null) return;
        clan.OwnerId = newOwnerId;
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteClan(int clanId, CancellationToken ct = default)
    {
        var clan = await dbContext.Set<Clan>().FirstOrDefaultAsync(c => c.Id == clanId, ct);
        if (clan == null) return;
        dbContext.Set<Clan>().Remove(clan);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<List<Clan>> GetClans(int? page = null, int? pageSize = null, CancellationToken ct = default)
    {
        IQueryable<Clan> query = dbContext.Set<Clan>().OrderBy(c => c.Id);
        if (page != null && pageSize != null)
            query = query.Skip(page.Value * pageSize.Value).Take(pageSize.Value);
        return await query.ToListAsync(ct);
    }

    public async Task<int> CountClans(CancellationToken ct = default)
        => await dbContext.Set<Clan>().CountAsync(ct);

    public async Task SetUserClanPrivilege(int userId, byte clanPriv, CancellationToken ct = default)
    {
        var user = await dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return;
        user.ClanPriv = clanPriv;
        await dbContext.SaveChangesAsync(ct);
    }
}


