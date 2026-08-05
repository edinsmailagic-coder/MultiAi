using Microsoft.AspNetCore.Mvc;
using MultiAI.Authorization;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [Route("api/usage")]
    [ApiController]
    public class UsageStatsController : ControllerBase
    {
        private readonly IUsageStatsService statsService;

        public UsageStatsController(IUsageStatsService statsService)
        {
            this.statsService = statsService;
        }

        [HasPermission(AppPermissions.UsageRead)]
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
