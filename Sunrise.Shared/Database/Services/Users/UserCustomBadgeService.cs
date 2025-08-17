using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Database.Models.Users;

namespace Sunrise.Shared.Database.Services.Users;

public class UserCustomBadgeService(SunriseDbContext dbContext)
{
    public async Task<List<string>> GetBadges(int userId, CancellationToken ct = default)
    {
        return await dbContext.UserCustomBadges
            .Where(x => x.UserId == userId)
            .Select(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<Result> AddBadges(int userId, IEnumerable<string> badges, CancellationToken ct = default)
    {
        var normalized = badges
            .Select(b => b.Trim())
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0) return Result.Success();

        var existing = await dbContext.UserCustomBadges
            .Where(x => x.UserId == userId && normalized.Contains(x.Name))
            .Select(x => x.Name)
            .ToListAsync(ct);

        var toInsert = normalized
            .Except(existing, StringComparer.OrdinalIgnoreCase)
            .Select(name => new UserCustomBadge { UserId = userId, Name = name })
            .ToList();

        await dbContext.UserCustomBadges.AddRangeAsync(toInsert, ct);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveBadges(int userId, IEnumerable<string> badges, CancellationToken ct = default)
    {
        var normalized = badges
            .Select(b => b.Trim())
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0) return Result.Success();

        var toDelete = await dbContext.UserCustomBadges
            .Where(x => x.UserId == userId && normalized.Contains(x.Name))
            .ToListAsync(ct);

        dbContext.UserCustomBadges.RemoveRange(toDelete);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}


