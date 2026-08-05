using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [Route("api/usage")]
    [ApiController]
    [Authorize]
    public class UsageStatsController : ControllerBase
    {
        private readonly IUsageStatsService statsService;

        public UsageStatsController(IUsageStatsService statsService)
        {
            this.statsService = statsService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var stats = await statsService.GetDashboardStatsAsync(userId, cancellationToken);
            return Ok(stats);
        }
    }
}
