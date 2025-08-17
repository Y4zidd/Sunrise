using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Sunrise.API.Extensions;
using Sunrise.API.Serializable.Response;
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
public class ClanController(DatabaseService database, ClanService clanService, ClanRepository clanRepository) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, [FromQuery] GameMode mode = GameMode.Standard, [FromQuery] bool useEf = false, CancellationToken ct = default)
    {
        var clan = await clanRepository.GetById(id, ct);
        if (clan == null) return NotFound(new ErrorResponse("Clan not found"));

        // Members are optional; frontend can handle empty lists
        var members = await clanRepository.GetMembers(id, ct);
        var owner = await database.Users.GetUser(id: clan.OwnerId, ct: ct);
<<<<<<< HEAD
        // Compute all clan ranks for different metrics
=======
>>>>>>> bef4f18 (feat(clan): tambahkan perhitungan peringkat klan untuk metrik TotalPP, AveragePP, RankedScore, dan Accuracy; perbarui respons API untuk menyertakan semua peringkat tersebut.)
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
<<<<<<< HEAD
        var statsAgg = useEf
            ? await clanRepository.GetClanStatsEf(mode, id, ct)
            : await clanRepository.GetClanStats(mode, id, ct);

        // Grades aggregation available only with EF path for now
        var grades = useEf ? await clanRepository.GetClanGradesEf(mode, id, ct) : (XH: 0, X: 0, SH: 0, S: 0, A: 0);
=======

        var statsAgg = useEf
            ? await clanRepository.GetClanStatsEf(mode, id, ct)
            : await clanRepository.GetClanStats(mode, id, ct);
        // Always compute grades to show in UI
        var grades = await clanRepository.GetClanGradesEf(mode, id, ct);
>>>>>>> bef4f18 (feat(clan): tambahkan perhitungan peringkat klan untuk metrik TotalPP, AveragePP, RankedScore, dan Accuracy; perbarui respons API untuk menyertakan semua peringkat tersebut.)

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
<<<<<<< HEAD
            // return each rank metric explicitly
=======
            rank = rankTotalPp,
>>>>>>> bef4f18 (feat(clan): tambahkan perhitungan peringkat klan untuk metrik TotalPP, AveragePP, RankedScore, dan Accuracy; perbarui respons API untuk menyertakan semua peringkat tersebut.)
            rankTotalPp,
            rankAveragePp,
            rankRankedScore,
            rankAccuracy,
            totalPp = statsAgg.TotalPp,
            averagePp = statsAgg.AveragePp,
            rankedScore = statsAgg.RankedScore,
            accuracy = statsAgg.Accuracy,
            // aggregated grades for clan members
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

    // Join via approval on web only; direct join disabled.

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

    // Request join (web approval flow)
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


