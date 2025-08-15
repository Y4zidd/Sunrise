using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Database;
using Sunrise.Shared.Database.Models.Clan;
using Sunrise.Shared.Database.Repositories;

namespace Sunrise.Shared.Services;

public class ClanService(ClanRepository clanRepository, DatabaseService database)
{
    public async Task<Result<Clan>> CreateClan(int ownerId, string name, string tag, string? description, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(tag))
            return Result.Failure<Clan>("Name and tag are required");

        if (tag.Length is < 2 or > 6)
            return Result.Failure<Clan>("Tag length must be 2-6 characters");

        if (await clanRepository.IsUserInAnyClan(ownerId, ct))
            return Result.Failure<Clan>("User already in a clan");

        var existing = await clanRepository.GetByTag(tag, ct);
        if (existing != null)
            return Result.Failure<Clan>("Tag already taken");

        var clan = new Clan
        {
            Name = name.Trim(),
            Tag = tag.Trim(),
            Description = description?.Trim(),
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        await clanRepository.CreateClan(clan, ownerId, ct);
        return Result.Success(clan);
    }

    public async Task<Result> JoinClan(int userId, int clanId, CancellationToken ct = default)
    {
        if (await clanRepository.IsUserInAnyClan(userId, ct))
            return Result.Failure("User already in a clan");

        var clan = await clanRepository.GetById(clanId, ct);
        if (clan == null)
            return Result.Failure("Clan not found");

        await clanRepository.JoinClan(clanId, userId, ct);
        return Result.Success();
    }

    public async Task<Result> LeaveClan(int userId, CancellationToken ct = default)
    {
        if (!await clanRepository.IsUserInAnyClan(userId, ct))
            return Result.Failure("Not in a clan");

        await clanRepository.LeaveClan(userId, ct);
        return Result.Success();
    }
}


