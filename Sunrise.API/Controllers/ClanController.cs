using Microsoft.AspNetCore.Mvc;
using Sunrise.API.Extensions;
using Sunrise.API.Serializable.Response;
using Sunrise.Shared.Attributes;
using Sunrise.Shared.Database;
using Sunrise.Shared.Services;
using Sunrise.Shared.Database.Repositories;

namespace Sunrise.API.Controllers;

[Route("/clan")]
[Subdomain("api")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
public class ClanController(DatabaseService database, ClanService clanService, ClanRepository clanRepository) : ControllerBase
{
    [HttpGet("list")]
    [EndpointDescription("List clans (paginated)")]
    [ProducesResponseType(typeof(ClansResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] int page = 0, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        if (page < 0 || pageSize is < 1 or > 100)
            return BadRequest(new ErrorResponse("Invalid pagination parameters"));

        var total = await clanRepository.CountClans(ct);
        var items = await clanRepository.GetClans(page, pageSize, ct);

        return Ok(new ClansResponse(items.Select(ClanResponse.FromEntity).ToList(), total, page, pageSize));
    }

    [HttpGet("{id:int}")]
    [EndpointDescription("Get clan by id")]
    [ProducesResponseType(typeof(ClanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        if (id <= 0) return BadRequest(new ErrorResponse("Invalid id parameter"));
        var clan = await clanRepository.GetById(id, ct);
        if (clan == null) return NotFound(new ErrorResponse("Clan not found"));
        return Ok(ClanResponse.FromEntity(clan));
    }

    [HttpGet("by-tag/{tag}")]
    [EndpointDescription("Get clan by tag")]
    [ProducesResponseType(typeof(ClanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTag(string tag, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tag)) return BadRequest(new ErrorResponse("Invalid tag parameter"));
        var clan = await clanRepository.GetByTag(tag, ct);
        if (clan == null) return NotFound(new ErrorResponse("Clan not found"));
        return Ok(ClanResponse.FromEntity(clan));
    }
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateClanRequest req, CancellationToken ct = default)
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null) return Unauthorized(new ErrorResponse("Unauthorized"));

        var result = await clanService.CreateClan(user.Id, req.Name, req.Tag, req.Description, ct);
        if (result.IsFailure) return BadRequest(new ErrorResponse(result.Error));
        return Ok(ClanResponse.FromEntity(result.Value));
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


