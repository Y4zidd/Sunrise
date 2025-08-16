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

    // EF/LINQ variants for experimentation: may be less performant on large datasets
    public async Task<(double TotalPp, double AveragePp, long RankedScore, double Accuracy)> GetClanStatsEf(GameMode mode, int clanId, CancellationToken ct = default)
    {
        var usersQuery = dbContext.Set<User>().Where(u => u.ClanId == clanId);
        var statsQuery = dbContext.Set<UserStats>().Where(s => s.GameMode == mode).Join(usersQuery, s => s.UserId, u => u.Id, (s, u) => s);

        var totalPp = await statsQuery.SumAsync(s => (double?)s.PerformancePoints, ct) ?? 0d;
        var averagePp = await statsQuery.AverageAsync(s => (double?)s.PerformancePoints, ct) ?? 0d;
        var rankedScore = await statsQuery.SumAsync(s => (long?)s.RankedScore, ct) ?? 0L;
        var accuracy = await statsQuery.AverageAsync(s => (double?)s.Accuracy, ct) ?? 0d;

        return (totalPp, averagePp, rankedScore, accuracy);
    }

    public async Task<int?> GetClanRankEf(ClanLeaderboardMetric metric, GameMode mode, int clanId, CancellationToken ct = default)
    {
        var statSelector = metric switch
        {
            ClanLeaderboardMetric.TotalPP => new Func<UserStats, double>(s => s.PerformancePoints),
            ClanLeaderboardMetric.AveragePP => new Func<UserStats, double>(s => s.PerformancePoints),
            ClanLeaderboardMetric.RankedScore => new Func<UserStats, double>(s => s.RankedScore),
            ClanLeaderboardMetric.Accuracy => new Func<UserStats, double>(s => s.Accuracy),
            _ => new Func<UserStats, double>(s => s.PerformancePoints)
        };

        // Build aggregation per clan
        var aggregated = await dbContext.Set<User>()
            .GroupJoin(dbContext.Set<UserStats>().Where(s => s.GameMode == mode), u => u.Id, s => s.UserId,
                (u, stats) => new { u.ClanId, Stats = stats })
            .Where(x => x.ClanId != 0)
            .GroupBy(x => x.ClanId)
            .Select(g => new
            {
                ClanId = g.Key,
                TotalPp = g.SelectMany(x => x.Stats).Sum(s => s.PerformancePoints),
                AveragePp = g.SelectMany(x => x.Stats).Average(s => s.PerformancePoints),
                RankedScore = g.SelectMany(x => x.Stats).Sum(s => (long)s.RankedScore),
                Accuracy = g.SelectMany(x => x.Stats).Average(s => s.Accuracy)
            })
            .ToListAsync(ct);

        if (aggregated.Count == 0) return null;

        IEnumerable<(int ClanId, double Value)> orderSource = metric switch
        {
            ClanLeaderboardMetric.TotalPP => aggregated.Select(a => (a.ClanId, a.TotalPp)),
            ClanLeaderboardMetric.AveragePP => aggregated.Select(a => (a.ClanId, a.AveragePp)),
            ClanLeaderboardMetric.RankedScore => aggregated.Select(a => (a.ClanId, (double)a.RankedScore)),
            ClanLeaderboardMetric.Accuracy => aggregated.Select(a => (a.ClanId, a.Accuracy)),
            _ => aggregated.Select(a => (a.ClanId, a.TotalPp))
        };

        var ranked = orderSource
            .OrderByDescending(x => x.Value)
            .Select((x, index) => new { x.ClanId, Rank = index + 1 })
            .ToDictionary(x => x.ClanId, x => x.Rank);

        return ranked.TryGetValue(clanId, out var rank) ? rank : null;
    }

    public async Task<(int XH, int X, int SH, int S, int A)> GetClanGradesEf(GameMode mode, int clanId, CancellationToken ct = default)
    {
        var usersQuery = dbContext.Set<User>().Where(u => u.ClanId == clanId);
        var gradesQuery = dbContext.Set<UserGrades>().Where(g => g.GameMode == mode).Join(usersQuery, g => g.UserId, u => u.Id, (g, u) => g);

        var result = await gradesQuery
            .GroupBy(g => 1)
            .Select(g => new
            {
                XH = g.Sum(x => x.CountXH),
                X = g.Sum(x => x.CountX),
                SH = g.Sum(x => x.CountSH),
                S = g.Sum(x => x.CountS),
                A = g.Sum(x => x.CountA)
            })
            .FirstOrDefaultAsync(ct);

        return result == null ? (0, 0, 0, 0, 0) : (result.XH, result.X, result.SH, result.S, result.A);
    }
}


