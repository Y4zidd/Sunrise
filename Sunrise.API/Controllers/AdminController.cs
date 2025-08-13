using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Sunrise.Shared.Application;

namespace Sunrise.API.Controllers;

[ApiController]
[Route("admin")] 
public class AdminController : ControllerBase
{
    // Simple protected endpoint to enqueue PP recalculation by score hash
    // Usage (from VPS):
    // curl -X POST "http://localhost:5148/admin/enqueue/recalcpp?hash=<SCORE_HASH>&token=<API_KEY>"
    [HttpPost("enqueue/recalcpp")]
    public IActionResult EnqueueRecalcPp([FromQuery] string hash, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return BadRequest(new { status = 400, message = "hash is required" });

        if (string.IsNullOrWhiteSpace(token) || !string.Equals(token, Configuration.ObservatoryApiKey, StringComparison.Ordinal))
            return Unauthorized(new { status = 401, message = "invalid token" });

        BackgroundJob.Enqueue(() => Sunrise.Shared.Jobs.RecalcScorePpJob.ProcessByScoreHash(hash));
        return Ok(new { status = 200, message = "enqueued", hash });
    }
}








