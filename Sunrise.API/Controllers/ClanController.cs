using Microsoft.AspNetCore.Mvc;
using Sunrise.API.Extensions;
using Sunrise.API.Serializable.Response;
using Sunrise.Shared.Attributes;
using Sunrise.Shared.Database;
using Sunrise.Shared.Services;

namespace Sunrise.API.Controllers;

[Route("/clan")]
[Subdomain("api")]
public class ClanController(DatabaseService database, ClanService clanService) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateClanRequest req, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));

        var result = await clanService.CreateClan(user.Id, req.Name, req.Tag, req.Description, ct);
        if (result.IsFailure) return BadRequest(new ErrorResponse(result.Error));
        return Ok(new { id = result.Value.Id, name = result.Value.Name, tag = result.Value.Tag, description = result.Value.Description });
    }

    [HttpPost("join/{clanId:int}")]
    public async Task<IActionResult> Join([FromRoute] int clanId, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));

        var result = await clanService.JoinClan(user.Id, clanId, ct);
        if (result.IsFailure) return BadRequest(new ErrorResponse(result.Error));
        return Ok();
    }

    [HttpPost("leave")]
    public async Task<IActionResult> Leave(CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));

        var result = await clanService.LeaveClan(user.Id, ct);
        if (result.IsFailure) return BadRequest(new ErrorResponse(result.Error));
        return Ok();
    }
}

public record CreateClanRequest(string Name, string Tag, string? Description);


