using MultiAI.Models.Entities;
using MultiAI.Models.Response;

namespace MultiAI.Services.Interface
{
    public interface IMessageAttachmentService
    {
        Task<AttachmentDto> UploadAsync(string userId, IFormFile file);
        Task<List<MessageAttachment>> GetOwnedPendingAsync(string userId, IEnumerable<int> ids);
        Task LinkToMessageAsync(string userId, int messageId, IEnumerable<int> attachmentIds);
        Task<List<AttachmentDto>> GetForMessageAsync(int messageId);
    }
}
