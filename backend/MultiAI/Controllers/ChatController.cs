using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using MultiAI.Authorization;
using MultiAI.Models.Request;
using MultiAI.Models.Response;
using MultiAI.Services.Interface;
using System.Security.Claims;
using System.Text.Json;

namespace MultiAI.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService service;
        private readonly IConversationFolderService folderService;
        private readonly IMessageAttachmentService attachmentService;

        public ChatController(
            IChatService service,
            IConversationFolderService folderService,
            IMessageAttachmentService attachmentService)
        {
            this.service = service;
            this.folderService = folderService;
            this.attachmentService = attachmentService;
        }

        [HasPermission(AppPermissions.ChatWrite)]
        [HttpPost("sendChat")]
        public async Task<IActionResult> Send([FromBody] ChatRequestDto request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                return Ok(await service.SendAsync(userId, request));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HasPermission(AppPermissions.ChatWrite)]
        [HttpPost("stream")]
        public async Task Stream([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache, no-transform";
            Response.Headers.Connection = "keep-alive";
            Response.Headers.Append("X-Accel-Buffering", "no");

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            async Task EmitAsync(ChatStreamEventDto evt, CancellationToken ct)
            {
                var payload = JsonSerializer.Serialize(evt, jsonOptions);
                await Response.WriteAsync($"data: {payload}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            try
            {
                await service.StreamAsync(userId, request, EmitAsync, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Client disconnected.
            }
            catch (Exception ex)
            {
                if (!Response.HasStarted || !HttpContext.RequestAborted.IsCancellationRequested)
                {
                    try
                    {
                        await EmitAsync(
                            new ChatStreamEventDto { Type = "error", Message = ex.Message },
                            CancellationToken.None);
                    }
                    catch
                    {
                        // Response may already be closed.
                    }
                }
            }
        }

        [HasPermission(AppPermissions.ChatRead)]
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            return Ok(await service.GetConversationsAsync(userId));
        }

        [HasPermission(AppPermissions.ChatRead)]
        [HttpGet("conversations/{id:int}")]
        public async Task<IActionResult> GetConversation(int id)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                return Ok(await service.GetConversationAsync(userId, id));
            }
            catch (Exception ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HasPermission(AppPermissions.ChatWrite)]
        [HttpPatch("conversations/{id:int}")]
        public async Task<IActionResult> RenameConversation(int id, [FromBody] RenameConversationRequest request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                return Ok(await service.RenameConversationAsync(userId, id, request.Title));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HasPermission(AppPermissions.ChatWrite)]
        [HttpPatch("conversations/{id:int}/pin")]
        public async Task<IActionResult> PinConversation(int id, [FromBody] PinConversationRequest request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                return Ok(await service.SetPinnedAsync(userId, id, request.IsPinned));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HasPermission(AppPermissions.ChatWrite)]
        [HttpPatch("conversations/{id:int}/folder")]
        public async Task<IActionResult> AssignFolder(int id, [FromBody] AssignFolderRequest request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                return Ok(await service.AssignFolderAsync(userId, id, request.FolderId));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HasPermission(AppPermissions.ChatWrite)]
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

        [HasPermission(AppPermissions.ChatRead)]
        [HttpGet("folders")]
        public async Task<IActionResult> GetFolders()
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            return Ok(await folderService.GetAsync(userId));
        }

        [HasPermission(AppPermissions.ChatWrite)]
        [HttpPost("folders")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                return Ok(await folderService.CreateAsync(userId, request.Name));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HasPermission(AppPermissions.ChatWrite)]
        [HttpPatch("folders/{id:int}")]
        public async Task<IActionResult> RenameFolder(int id, [FromBody] RenameFolderRequest request)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                return Ok(await folderService.RenameAsync(userId, id, request.Name));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HasPermission(AppPermissions.ChatWrite)]
        [HttpDelete("folders/{id:int}")]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                await folderService.DeleteAsync(userId, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HasPermission(AppPermissions.ChatWrite)]
        [HttpPost("attachments")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadAttachment(IFormFile file)
        {
            var userId = GetUserId();
            if (userId is null)
                return Unauthorized();

            try
            {
                return Ok(await attachmentService.UploadAsync(userId, file));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
