using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Database.Models.Clan;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Enums.Clan;
using Sunrise.Shared.Database.Objects.Clan;
using GameMode = Sunrise.Shared.Enums.Beatmaps.GameMode;

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

    // Join requests
    public async Task<bool> HasPendingRequest(int clanId, int userId, CancellationToken ct = default)
        => await dbContext.Set<ClanJoinRequest>().AnyAsync(r => r.ClanId == clanId && r.UserId == userId && r.Status == ClanJoinRequestStatus.Pending, ct);

    public async Task<ClanJoinRequest> CreateRequest(int clanId, int userId, int requestedBy, CancellationToken ct = default)
    {
        var req = new ClanJoinRequest { ClanId = clanId, UserId = userId, RequestedBy = requestedBy, Status = ClanJoinRequestStatus.Pending };
        await dbContext.Set<ClanJoinRequest>().AddAsync(req, ct);
        await dbContext.SaveChangesAsync(ct);
        return req;
    }

    public async Task<ClanJoinRequest?> GetRequest(int requestId, CancellationToken ct = default)
        => await dbContext.Set<ClanJoinRequest>().FirstOrDefaultAsync(r => r.Id == requestId, ct);

    public async Task<List<ClanJoinRequest>> GetRequests(int clanId, ClanJoinRequestStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = dbContext.Set<ClanJoinRequest>().Where(r => r.ClanId == clanId);
        if (status != null) q = q.Where(r => r.Status == status);
        return await q.OrderByDescending(r => r.CreatedAt).Skip(page * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task UpdateRequest(ClanJoinRequest req, CancellationToken ct = default)
    {
        dbContext.Update(req);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<List<ClanLeaderboardItem>> GetClanLeaderboard(ClanLeaderboardMetric metric, GameMode mode, int page, int pageSize, CancellationToken ct = default)
    {
        // Using raw SQL to leverage window functions like Shiina
        string orderExpr = metric switch
        {
            ClanLeaderboardMetric.TotalPP => "COALESCE(SUM(s.PerformancePoints),0)",
            ClanLeaderboardMetric.AveragePP => "COALESCE(AVG(s.PerformancePoints),0)",
            ClanLeaderboardMetric.RankedScore => "COALESCE(SUM(s.RankedScore),0)",
            ClanLeaderboardMetric.Accuracy => "COALESCE(AVG(s.Accuracy),0)",
            _ => "COALESCE(SUM(s.PerformancePoints),0)"
        };

        var sql = $@"
            SELECT * FROM (
                SELECT c.Id AS ClanId, c.Name, c.Tag, c.OwnerId,
                       COUNT(u.Id) AS MemberCount,
                       {orderExpr} AS Value,
                       COALESCE(AVG(s.Accuracy),0) AS AvgAcc,
                       COALESCE(SUM(s.PlayCount),0) AS PlayCount,
                       RANK() OVER (ORDER BY {orderExpr} DESC) AS `Rank`
                FROM clan c
                LEFT JOIN user u ON u.ClanId = c.Id
                LEFT JOIN user_stats s ON s.UserId = u.Id AND s.GameMode = {{0}}
                GROUP BY c.Id
            ) t
            ORDER BY t.Rank ASC
            LIMIT {{1}} OFFSET {{2}}";

        var items = new List<ClanLeaderboardItem>();

        await using var conn = dbContext.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        // Inline values for execution (safe here since values are ints and validated)
        cmd.CommandText = sql.Replace("{0}", ((int)mode).ToString())
                             .Replace("{1}", pageSize.ToString())
                             .Replace("{2}", (page * pageSize).ToString());

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(new ClanLeaderboardItem
            {
                ClanId = reader.GetInt32(reader.GetOrdinal("ClanId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Tag = reader.GetString(reader.GetOrdinal("Tag")),
                OwnerId = reader.GetInt32(reader.GetOrdinal("OwnerId")),
                MemberCount = reader.GetInt32(reader.GetOrdinal("MemberCount")),
                Value = reader.GetDouble(reader.GetOrdinal("Value")),
                AvgAcc = reader.IsDBNull(reader.GetOrdinal("AvgAcc")) ? 0 : reader.GetDouble(reader.GetOrdinal("AvgAcc")),
                PlayCount = reader.IsDBNull(reader.GetOrdinal("PlayCount")) ? 0 : reader.GetInt64(reader.GetOrdinal("PlayCount")),
                Rank = reader.GetInt32(reader.GetOrdinal("Rank"))
            });
        }

        return items;
    }

    public async Task<ClanLeaderboardItem?> GetClanMetrics(int clanId, CancellationToken ct = default)
    {
        // This method can be expanded to return multiple metrics at once if needed by a custom DTO
        var clan = await GetById(clanId, ct);
        if (clan == null) return null;
        return new ClanLeaderboardItem { ClanId = clan.Id, Name = clan.Name, Tag = clan.Tag, OwnerId = clan.OwnerId };
    }

    public async Task<int?> GetClanRank(ClanLeaderboardMetric metric, GameMode mode, int clanId, CancellationToken ct = default)
    {
        string orderExpr = metric switch
        {
            ClanLeaderboardMetric.TotalPP => "COALESCE(SUM(s.PerformancePoints),0)",
            ClanLeaderboardMetric.AveragePP => "COALESCE(AVG(s.PerformancePoints),0)",
            ClanLeaderboardMetric.RankedScore => "COALESCE(SUM(s.RankedScore),0)",
            ClanLeaderboardMetric.Accuracy => "COALESCE(AVG(s.Accuracy),0)",
            _ => "COALESCE(SUM(s.PerformancePoints),0)"
        };

        var sql = $@"
            SELECT * FROM (
                SELECT c.Id AS ClanId,
                       RANK() OVER (ORDER BY {orderExpr} DESC) AS `Rank`
                FROM clan c
                LEFT JOIN user u ON u.ClanId = c.Id
                LEFT JOIN user_stats s ON s.UserId = u.Id AND s.GameMode = {{0}}
                GROUP BY c.Id
            ) t
            WHERE t.ClanId = {{1}}";

        await using var conn = dbContext.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql.Replace("{0}", ((int)mode).ToString())
                              .Replace("{1}", clanId.ToString());

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return reader.GetInt32(reader.GetOrdinal("Rank"));
        }

        return null;
    }

    public async Task<(double TotalPp, double AveragePp, long RankedScore, double Accuracy)> GetClanStats(GameMode mode, int clanId, CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                COALESCE(SUM(s.PerformancePoints),0) AS TotalPP,
                COALESCE(AVG(s.PerformancePoints),0) AS AveragePP,
                COALESCE(SUM(s.RankedScore),0) AS RankedScore,
                COALESCE(AVG(s.Accuracy),0) AS Accuracy
            FROM clan c
            LEFT JOIN user u ON u.ClanId = c.Id
            LEFT JOIN user_stats s ON s.UserId = u.Id AND s.GameMode = {0}
            WHERE c.Id = {1}";

        await using var conn = dbContext.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql.Replace("{0}", ((int)mode).ToString())
                              .Replace("{1}", clanId.ToString());

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var totalPp = reader.IsDBNull(reader.GetOrdinal("TotalPP")) ? 0d : reader.GetDouble(reader.GetOrdinal("TotalPP"));
            var avgPp = reader.IsDBNull(reader.GetOrdinal("AveragePP")) ? 0d : reader.GetDouble(reader.GetOrdinal("AveragePP"));
            var rankedScore = reader.IsDBNull(reader.GetOrdinal("RankedScore")) ? 0L : reader.GetInt64(reader.GetOrdinal("RankedScore"));
            var acc = reader.IsDBNull(reader.GetOrdinal("Accuracy")) ? 0d : reader.GetDouble(reader.GetOrdinal("Accuracy"));
            return (totalPp, avgPp, rankedScore, acc);
        }

        return (0, 0, 0, 0);
    }
}


