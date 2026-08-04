using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiAI.Models.Request;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService service;

        public ChatController(IChatService service)
        {
            this.service = service;
        }
        [HttpPost("sendChat")]
        public async Task<IActionResult> Send(ChatRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            var response = await service.SendAsync(userId, request);

            return Ok(response);
        }
    }
}
