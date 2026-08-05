using Microsoft.AspNetCore.Mvc;
using MultiAI.Authorization;
using MultiAI.Models.Request;
using MultiAI.Models.Response;
using MultiAI.Services.Interface;
using System.Security.Claims;

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
