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

        /// <summary>
        /// Sends a message in direct chat mode or workflow mode.
        /// </summary>
        [HttpPost("sendChat")]
        public async Task<IActionResult> Send([FromBody] ChatRequestDto request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                var response = await service.SendAsync(userId, request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lists conversations for the authenticated user.
        /// </summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            var conversations = await service.GetConversationsAsync(userId);
            return Ok(conversations);
        }

        /// <summary>
        /// Loads a conversation with ordered messages and responses.
        /// </summary>
        [HttpGet("conversations/{id:int}")]
        public async Task<IActionResult> GetConversation(int id)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                var conversation = await service.GetConversationAsync(userId, id);
                return Ok(conversation);
            }
            catch (Exception ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Renames a conversation owned by the authenticated user.
        /// </summary>
        [HttpPatch("conversations/{id:int}")]
        public async Task<IActionResult> RenameConversation(int id, [FromBody] RenameConversationRequest request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                var conversation = await service.RenameConversationAsync(userId, id, request.Title);
                return Ok(conversation);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Soft-deletes a conversation owned by the authenticated user.
        /// </summary>
        [HttpDelete("conversations/{id:int}")]
        public async Task<IActionResult> DeleteConversation(int id)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                await service.DeleteConversationAsync(userId, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
