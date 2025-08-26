using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Sunrise.Shared.Database;
using Sunrise.Shared.Database.Models.Clan;
using Sunrise.Shared.Database.Models.Users;
using Sunrise.Shared.Enums.Clan;
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

        var alreadyOwner = await clanRepository.GetByOwner(ownerId, ct);
        if (alreadyOwner != null)
        {
            // Ensure the owner user is attached to their owned clan
            var owner = await database.DbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == ownerId, ct);
            if (owner == null)
                return Result.Failure<Clan>("Owner user not found");

            if (owner.ClanId != alreadyOwner.Id || owner.ClanPriv != 3)
            {
                owner.ClanId = alreadyOwner.Id;
                owner.ClanPriv = 3;
                await database.DbContext.SaveChangesAsync(ct);
            }

            return Result.Success(alreadyOwner);
        }

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

        // Prevent owners from leaving without handing over or disbanding
        var ownedClan = await clanRepository.GetByOwner(userId, ct);
        if (ownedClan != null)
        {
            return Result.Failure(
                $"You are the owner of [{ownedClan.Tag}] {ownedClan.Name}. Transfer ownership with !clan transfer <userId> or disband the clan with !clan disband.");
        }

        await clanRepository.LeaveClan(userId, ct);
        return Result.Success();
    }

    public async Task<Result> TransferOwnership(int ownerId, int targetUserId, CancellationToken ct = default)
    {
        var clan = await clanRepository.GetByOwner(ownerId, ct);
        if (clan == null)
            return Result.Failure("You are not an owner of any clan");

        var members = await clanRepository.GetMembers(clan.Id, ct);
        if (members.All(m => m.Id != targetUserId))
            return Result.Failure("Target user is not a member of your clan");

        // downgrade current owner to member
        var owner = members.First(m => m.Id == ownerId);
        owner.ClanPriv = 1;
        await database.DbContext.SaveChangesAsync(ct);

        // upgrade target to owner
        var target = members.First(m => m.Id == targetUserId);
        target.ClanPriv = 3;
        await database.DbContext.SaveChangesAsync(ct);

        await clanRepository.SetOwner(clan.Id, targetUserId, ct);
        return Result.Success();
    }

    public async Task<Result> Disband(int ownerId, CancellationToken ct = default)
    {
        var clan = await clanRepository.GetByOwner(ownerId, ct);
        if (clan == null)
            return Result.Failure("You are not an owner of any clan");

        var members = await clanRepository.GetMembers(clan.Id, ct);
        foreach (var member in members)
        {
            member.ClanId = 0;
            member.ClanPriv = 0;
        }

        await database.DbContext.SaveChangesAsync(ct);
        await clanRepository.DeleteClan(clan.Id, ct);
        return Result.Success();
    }

    public async Task<Result> PromoteToOfficer(int ownerId, int targetUserId, CancellationToken ct = default)
    {
        var clan = await clanRepository.GetByOwner(ownerId, ct);
        if (clan == null)
            return Result.Failure("You are not an owner of any clan");

        var members = await clanRepository.GetMembers(clan.Id, ct);
        if (members.All(m => m.Id != targetUserId))
            return Result.Failure("Target user is not a member of your clan");

        if (targetUserId == ownerId)
            return Result.Failure("Owner already has the highest privilege");

        await clanRepository.SetUserClanPrivilege(targetUserId, 2, ct);
        return Result.Success();
    }

    public async Task<Result> DemoteToMember(int ownerId, int targetUserId, CancellationToken ct = default)
    {
        var clan = await clanRepository.GetByOwner(ownerId, ct);
        if (clan == null)
            return Result.Failure("You are not an owner of any clan");

        var members = await clanRepository.GetMembers(clan.Id, ct);
        if (members.All(m => m.Id != targetUserId))
            return Result.Failure("Target user is not a member of your clan");

        if (targetUserId == ownerId)
            return Result.Failure("Owner cannot be demoted");

        await clanRepository.SetUserClanPrivilege(targetUserId, 1, ct);
        return Result.Success();
    }

    public async Task<Result> RequestJoin(int userId, int clanId, CancellationToken ct = default)
    {
        var clan = await clanRepository.GetById(clanId, ct);
        if (clan == null) return Result.Failure("Clan not found");
        if (await clanRepository.IsUserInAnyClan(userId, ct)) return Result.Failure("User already in a clan");
        if (await clanRepository.HasPendingRequest(clanId, userId, ct)) return Result.Failure("You already have a pending request");
        await clanRepository.CreateRequest(clanId, userId, userId, ct);
        return Result.Success();
    }

    public async Task<Result> RevokeJoinRequest(int userId, int clanId, CancellationToken ct = default)
    {
        var pending = await clanRepository.GetRequests(clanId, ClanJoinRequestStatus.Pending, 0, 100, ct);
        var req = pending.FirstOrDefault(r => r.UserId == userId);
        if (req == null) return Result.Failure("No pending request found");
        req.Status = ClanJoinRequestStatus.Revoked;
        req.UpdatedAt = DateTime.UtcNow;
        req.ActionedBy = userId;
        await clanRepository.UpdateRequest(req, ct);
        return Result.Success();
    }

    public async Task<Result> ApproveRequest(int ownerId, int requestId, int targetUserId, CancellationToken ct = default)
    {
        var clan = await clanRepository.GetByOwner(ownerId, ct);
        if (clan == null) return Result.Failure("You are not an owner of any clan");
        var req = await clanRepository.GetRequest(requestId, ct);
        if (req == null || req.ClanId != clan.Id || req.UserId != targetUserId) return Result.Failure("Request not found");
        if (req.Status != ClanJoinRequestStatus.Pending) return Result.Failure("Request is not pending");

        await clanRepository.JoinClan(clan.Id, targetUserId, ct);

        req.Status = ClanJoinRequestStatus.Approved;
        req.UpdatedAt = DateTime.UtcNow;
        req.ActionedBy = ownerId;
        await clanRepository.UpdateRequest(req, ct);
        return Result.Success();
    }

    public async Task<Result> DenyRequest(int ownerId, int requestId, CancellationToken ct = default)
    {
        var clan = await clanRepository.GetByOwner(ownerId, ct);
        if (clan == null) return Result.Failure("You are not an owner of any clan");
        var req = await clanRepository.GetRequest(requestId, ct);
        if (req == null || req.ClanId != clan.Id) return Result.Failure("Request not found");
        if (req.Status != ClanJoinRequestStatus.Pending) return Result.Failure("Request is not pending");
        req.Status = ClanJoinRequestStatus.Denied;
        req.UpdatedAt = DateTime.UtcNow;
        req.ActionedBy = ownerId;
        await clanRepository.UpdateRequest(req, ct);
        return Result.Success();
    }
    
    public async Task<Result<Clan>> EditClan(int ownerId, string? newName, string? newTag, CancellationToken ct = default)
    {
        var clan = await clanRepository.GetByOwner(ownerId, ct);
        if (clan == null) return Result.Failure<Clan>("You are not an owner of any clan");

        var hasChanges = false;

        if (!string.IsNullOrWhiteSpace(newName))
        {
            var trimmedName = newName.Trim();
            if (trimmedName != clan.Name)
            {
                clan.Name = trimmedName;
                hasChanges = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(newTag))
        {
            var trimmedTag = newTag.Trim().ToUpperInvariant();
            if (trimmedTag.Length is < 2 or > 6)
                return Result.Failure<Clan>("Tag length must be 2-6 characters");
                
            if (trimmedTag != clan.Tag)
            {
                var existing = await clanRepository.GetByTag(trimmedTag, ct);
                if (existing != null && existing.Id != clan.Id)
                    return Result.Failure<Clan>("Tag already taken");
                    
                clan.Tag = trimmedTag;
                hasChanges = true;
            }
        }

        if (!hasChanges)
            return Result.Failure<Clan>("No changes provided");

        clan.UpdatedAt = DateTime.UtcNow;
        await database.DbContext.SaveChangesAsync(ct);
        return Result.Success(clan);
    }
}


