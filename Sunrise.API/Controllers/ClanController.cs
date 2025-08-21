using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Sunrise.API.Extensions;
using Sunrise.API.Serializable.Response;
using Sunrise.API.Services;
using Sunrise.Shared.Attributes;
using Sunrise.Shared.Database;
using Sunrise.Shared.Services;
using Sunrise.Shared.Database.Repositories;
using Sunrise.Shared.Enums.Beatmaps;
using Sunrise.Shared.Enums.Clan;
using Sunrise.Shared.Database.Models.Users;
 

namespace Sunrise.API.Controllers;

[Route("/clan")]
[Subdomain("api")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
public class ClanController(DatabaseService database, ClanService clanService, ClanRepository clanRepository, AssetService assetService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, [FromQuery] GameMode mode = GameMode.Standard, [FromQuery] bool useEf = false, CancellationToken ct = default)
    {
        var clan = await clanRepository.GetById(id, ct);
        if (clan == null) return NotFound(new ErrorResponse("Clan not found"));

        var members = await clanRepository.GetMembers(id, ct);
        var owner = await database.Users.GetUser(id: clan.OwnerId, ct: ct);
        var rankTotalPp = useEf
            ? await clanRepository.GetClanRankEf(ClanLeaderboardMetric.TotalPP, mode, id, ct)
            : await clanRepository.GetClanRank(ClanLeaderboardMetric.TotalPP, mode, id, ct);
        var rankAveragePp = useEf
            ? await clanRepository.GetClanRankEf(ClanLeaderboardMetric.AveragePP, mode, id, ct)
            : await clanRepository.GetClanRank(ClanLeaderboardMetric.AveragePP, mode, id, ct);
        var rankRankedScore = useEf
            ? await clanRepository.GetClanRankEf(ClanLeaderboardMetric.RankedScore, mode, id, ct)
            : await clanRepository.GetClanRank(ClanLeaderboardMetric.RankedScore, mode, id, ct);
        var rankAccuracy = useEf
            ? await clanRepository.GetClanRankEf(ClanLeaderboardMetric.Accuracy, mode, id, ct)
            : await clanRepository.GetClanRank(ClanLeaderboardMetric.Accuracy, mode, id, ct);
        var statsAgg = useEf
            ? await clanRepository.GetClanStatsEf(mode, id, ct)
            : await clanRepository.GetClanStats(mode, id, ct);

        var grades = useEf ? await clanRepository.GetClanGradesEf(mode, id, ct) : (XH: 0, X: 0, SH: 0, S: 0, A: 0);

        return Ok(new
        {
            id = clan.Id,
            tag = clan.Tag,
            name = clan.Name,
            ownerId = clan.OwnerId,
            createdAt = clan.CreatedAt,
            memberCount = members.Count,
            members = members.Select(m => new
            {
                id = m.Id,
                name = m.Username,
                country = m.Country.ToString(),
                rank = m.ClanPriv switch
                {
                    3 => "Owner",
                    2 => "Officer",
                    1 => "Member",
                    _ => (string?)null
                }
            }),
            owner = owner == null ? null : new { id = owner.Id, name = owner.Username },
            ownerLastActive = owner?.LastOnlineTime,
            avatarUrl = $"https://a.{Shared.Application.Configuration.Domain}/clan/avatar/{id}",
            bannerUrl = $"https://a.{Shared.Application.Configuration.Domain}/clan/banner/{id}",
            rankTotalPp,
            rankAveragePp,
            rankRankedScore,
            rankAccuracy,
            totalPp = statsAgg.TotalPp,
            averagePp = statsAgg.AveragePp,
            rankedScore = statsAgg.RankedScore,
            accuracy = statsAgg.Accuracy,
            grades = useEf ? new { xh = grades.XH, x = grades.X, sh = grades.SH, s = grades.S, a = grades.A } : null
        });
    }
    
    [HttpPost("create")]
    [ProducesResponseType(typeof(ClanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateClanRequest req, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));

        var result = await clanService.CreateClan(user.Id, req.Name, req.Tag, req.Description, ct);
        if (result.IsFailure) return BadRequest(new ErrorResponse(result.Error));
        return Ok(ClanResponse.FromEntity(result.Value));
    }

    [HttpPost("leave")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Leave(CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));

        var result = await clanService.LeaveClan(user.Id, ct);
        if (result.IsFailure) return BadRequest(new ErrorResponse(result.Error));
        return Ok(new OperationResponse("Left clan"));
    }

    public record ClanRequestJoin(int ClanId);
    [HttpPost("request")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestJoin([FromBody] ClanRequestJoin body, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));
        if (body.ClanId <= 0) return BadRequest(new ErrorResponse("Invalid clanId"));
        var res = await clanService.RequestJoin(user.Id, body.ClanId, ct);
        if (res.IsFailure) return BadRequest(new ErrorResponse(res.Error));
        return Ok(new OperationResponse("Request submitted"));
    }

    [HttpGet("{id:int}/request/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRequestStatus([FromRoute] int id, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));
        var has = await clanRepository.HasPendingRequest(id, user.Id, ct);
        return Ok(new { pending = has });
    }

    [HttpGet("{id:int}/requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRequests([FromRoute] int id, [FromQuery] ClanJoinRequestStatus status = ClanJoinRequestStatus.Pending, [FromQuery] int page = 0, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));

        var clan = await clanRepository.GetByOwner(user.Id, ct);
        if (clan == null || clan.Id != id)
            return Forbid();

        var items = await clanRepository.GetRequests(id, status, page, pageSize, ct);
        return Ok(new { items, page, pageSize });
    }

    public record ClanRevokeJoin(int ClanId);
    [HttpPost("request/revoke")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeJoin([FromBody] ClanRevokeJoin body, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));
        if (body.ClanId <= 0) return BadRequest(new ErrorResponse("Invalid clanId"));
        var res = await clanService.RevokeJoinRequest(user.Id, body.ClanId, ct);
        if (res.IsFailure) return BadRequest(new ErrorResponse(res.Error));
        return Ok(new OperationResponse("Request revoked"));
    }

    public record ClanApprove(int RequestId, int TargetUserId);
    [HttpPost("requests/approve")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve([FromBody] ClanApprove body, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));
        if (body.RequestId <= 0 || body.TargetUserId <= 0) return BadRequest(new ErrorResponse("Invalid payload"));
        var res = await clanService.ApproveRequest(user.Id, body.RequestId, body.TargetUserId, ct);
        if (res.IsFailure) return BadRequest(new ErrorResponse(res.Error));
        return Ok(new OperationResponse("Request approved"));
    }

    public record ClanDeny(int RequestId);
    [HttpPost("requests/deny")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deny([FromBody] ClanDeny body, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));
        if (body.RequestId <= 0) return BadRequest(new ErrorResponse("Invalid payload"));
        var res = await clanService.DenyRequest(user.Id, body.RequestId, ct);
        if (res.IsFailure) return BadRequest(new ErrorResponse(res.Error));
        return Ok(new OperationResponse("Request denied"));
    }

    [HttpPost("{id:int}/upload/avatar")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadClanAvatar([FromRoute] int id)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));
        if (Request.HasFormContentType == false) return BadRequest(new ErrorResponse("Invalid content type"));
        if (Request.Form.Files.Count == 0) return BadRequest(new ErrorResponse("No files were uploaded"));

        var clan = await clanRepository.GetByOwner(user.Id);
        if (clan == null || clan.Id != id) return Forbid();

        var file = Request.Form.Files[0];
        await using var stream = file.OpenReadStream();
        var (ok, err) = await assetService.SetClanAvatar(id, stream);
        if (!ok) return BadRequest(new ErrorResponse(err ?? "Failed to set clan avatar"));
        return Ok(new OperationResponse("Clan avatar updated"));
    }

    [HttpPost("{id:int}/upload/banner")]
    [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadClanBanner([FromRoute] int id)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));
        if (Request.HasFormContentType == false) return BadRequest(new ErrorResponse("Invalid content type"));
        if (Request.Form.Files.Count == 0) return BadRequest(new ErrorResponse("No files were uploaded"));

        var clan = await clanRepository.GetByOwner(user.Id);
        if (clan == null || clan.Id != id) return Forbid();

        var file = Request.Form.Files[0];
        await using var stream = file.OpenReadStream();
        var (ok, err) = await assetService.SetClanBanner(id, stream);
        if (!ok) return BadRequest(new ErrorResponse(err ?? "Failed to set clan banner"));
        return Ok(new OperationResponse("Clan banner updated"));
    }

    [HttpGet("leaderboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Leaderboard([FromQuery] ClanLeaderboardMetric metric, [FromQuery] GameMode mode, [FromQuery] int page = 0, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        if (page < 0 || pageSize is < 1 or > 100) return BadRequest(new ErrorResponse("Invalid pagination parameters"));
        var items = await clanRepository.GetClanLeaderboard(metric, mode, page, pageSize, ct);
        return Ok(new { items, page, pageSize });
    }
}

public record StatsSnapshotsResponse(List<StatsSnapshot> Snapshots);

public record CreateClanRequest(string Name, string Tag, string? Description);


