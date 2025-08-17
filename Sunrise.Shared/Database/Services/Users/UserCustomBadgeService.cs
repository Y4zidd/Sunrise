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

    public record DetailedBadge(string Name, string? ColorHex, string? Icon, string? IconType);

    public async Task<List<DetailedBadge>> GetBadgesDetailed(int userId, CancellationToken ct = default)
    {
        return await dbContext.UserCustomBadges
            .Where(x => x.UserId == userId)
            .Select(x => new DetailedBadge(x.Name, x.ColorHex, x.Icon, x.IconType))
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

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(color, "^#([0-9a-fA-F]{6}|[0-9a-fA-F]{3})$");
    }

    private static bool IsValidIcon(string icon)
    {
        if (string.IsNullOrWhiteSpace(icon)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(icon, "^[a-z0-9-]{1,32}$");
    }

    public async Task<Result> SetBadgeColor(int userId, string name, string colorHex, CancellationToken ct = default)
    {
        if (!IsValidHexColor(colorHex)) return Result.Failure("Invalid color hex; use #RRGGBB or #RGB");
        var lowered = name.Trim().ToLower();
        var badge = await dbContext.UserCustomBadges
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name.ToLower() == lowered, ct);
        if (badge == null) return Result.Failure("Badge not found");
        badge.ColorHex = colorHex;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetBadgeIcon(int userId, string name, string icon, string? iconType = null, CancellationToken ct = default)
    {
        // longgar: izinkan kebab-case lucide, emoji unicode, atau URL; FE yang menerjemahkan
        var lowered = name.Trim().ToLower();
        var badge = await dbContext.UserCustomBadges
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name.ToLower() == lowered, ct);
        if (badge == null) return Result.Failure("Badge not found");
        badge.Icon = icon;
        if (!string.IsNullOrWhiteSpace(iconType)) badge.IconType = iconType;
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}


