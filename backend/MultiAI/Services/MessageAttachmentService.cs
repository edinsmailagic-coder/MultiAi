using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.Entities;
using MultiAI.Models.Response;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class MessageAttachmentService : IMessageAttachmentService
    {
        public const long MaxFileBytes = 10 * 1024 * 1024;
        public const int MaxAttachmentsPerMessage = 5;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx", ".xlsx", ".csv", ".txt", ".md",
            ".png", ".jpg", ".jpeg", ".webp", ".gif"
        };

        private readonly AppDbContext context;
        private readonly IAttachmentTextExtractor extractor;

        public MessageAttachmentService(AppDbContext context, IAttachmentTextExtractor extractor)
        {
            this.context = context;
            this.extractor = extractor;
        }

        public async Task<AttachmentDto> UploadAsync(string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File is required.");

            if (file.Length > MaxFileBytes)
                throw new Exception("File exceeds the 10MB limit.");

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                throw new Exception("Unsupported file type. Allowed: pdf, docx, xlsx, csv, txt, images.");

            if (extension.Equals(".doc", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".xls", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Legacy .doc/.xls are not supported. Use .docx/.xlsx.");

            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            await using var memory = new MemoryStream();
            await file.CopyToAsync(memory);
            var data = memory.ToArray();
            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;
            var isImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                          extension is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif";

            string? extracted = null;
            if (!isImage)
                extracted = extractor.Extract(file.FileName, contentType, data);

            var attachment = new MessageAttachment
            {
                OwnerUserId = userId,
                UserMessageId = null,
                FileName = Path.GetFileName(file.FileName),
                ContentType = contentType,
                SizeBytes = data.LongLength,
                Data = data,
                ExtractedText = extracted,
                IsImage = isImage,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = user.UserName,
                IsDeleted = 0
            };

            context.MessageAttachments.Add(attachment);
            await context.SaveChangesAsync();

            return Map(attachment);
        }

        public async Task<List<MessageAttachment>> GetOwnedPendingAsync(string userId, IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0)
                return [];

            return await context.MessageAttachments
                .Where(x =>
                    idList.Contains(x.Id) &&
                    x.OwnerUserId == userId &&
                    x.UserMessageId == null &&
                    x.IsDeleted == 0)
                .ToListAsync();
        }

        public async Task LinkToMessageAsync(string userId, int messageId, IEnumerable<int> attachmentIds)
        {
            var idList = attachmentIds.Distinct().ToList();
            if (idList.Count == 0)
                return;

            if (idList.Count > MaxAttachmentsPerMessage)
                throw new Exception($"Maximum {MaxAttachmentsPerMessage} attachments per message.");

            var attachments = await GetOwnedPendingAsync(userId, idList);
            if (attachments.Count != idList.Count)
                throw new Exception("One or more attachments were not found.");

            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            foreach (var attachment in attachments)
            {
                attachment.UserMessageId = messageId;
                attachment.UpdatedAt = DateTime.UtcNow;
                attachment.UpdatedBy = user.UserName;
            }

            await context.SaveChangesAsync();
        }

        public async Task<List<AttachmentDto>> GetForMessageAsync(int messageId)
        {
            return await context.MessageAttachments
                .AsNoTracking()
                .Where(x => x.UserMessageId == messageId && x.IsDeleted == 0)
                .OrderBy(x => x.Id)
                .Select(x => new AttachmentDto
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    ContentType = x.ContentType,
                    SizeBytes = x.SizeBytes,
                    IsImage = x.IsImage,
                    ExtractedTextPreview = x.ExtractedText == null
                        ? null
                        : (x.ExtractedText.Length > 200 ? x.ExtractedText.Substring(0, 200) : x.ExtractedText)
                })
                .ToListAsync();
        }

        private static AttachmentDto Map(MessageAttachment x) => new()
        {
            Id = x.Id,
            FileName = x.FileName,
            ContentType = x.ContentType,
            SizeBytes = x.SizeBytes,
            IsImage = x.IsImage,
            ExtractedTextPreview = x.ExtractedText == null
                ? null
                : (x.ExtractedText.Length > 200 ? x.ExtractedText[..200] : x.ExtractedText)
        };
    }
}
