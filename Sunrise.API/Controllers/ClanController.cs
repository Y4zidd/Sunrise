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
 

namespace Sunrise.API.Controllers;

[Route("/clan")]
[Subdomain("api")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
public class ClanController(DatabaseService database, ClanService clanService, ClanRepository clanRepository) : ControllerBase
{
    
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

public record CreateClanRequest(string Name, string Tag, string? Description);


