using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.Entities;
using MultiAI.Models.Response;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class ConversationFolderService : IConversationFolderService
    {
        private readonly AppDbContext context;

        public ConversationFolderService(AppDbContext context)
        {
            this.context = context;
        }

        public async Task<List<ConversationFolderDto>> GetAsync(string userId)
        {
            return await context.ConversationFolders
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsDeleted == 0)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new ConversationFolderDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortOrder = x.SortOrder
                })
                .ToListAsync();
        }

        public async Task<ConversationFolderDto> CreateAsync(string userId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Folder name is required.");

            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            var maxOrder = await context.ConversationFolders
                .Where(x => x.UserId == userId && x.IsDeleted == 0)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync() ?? 0;

            var folder = new ConversationFolder
            {
                UserId = userId,
                Name = name.Trim(),
                SortOrder = maxOrder + 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = user.UserName,
                IsDeleted = 0
            };

            context.ConversationFolders.Add(folder);
            await context.SaveChangesAsync();

            return new ConversationFolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                SortOrder = folder.SortOrder
            };
        }

        public async Task<ConversationFolderDto> RenameAsync(string userId, int folderId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Folder name is required.");

            var folder = await GetOwnedAsync(userId, folderId);
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            folder.Name = name.Trim();
            folder.UpdatedAt = DateTime.UtcNow;
            folder.UpdatedBy = user.UserName;
            await context.SaveChangesAsync();

            return new ConversationFolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                SortOrder = folder.SortOrder
            };
        }

        public async Task DeleteAsync(string userId, int folderId)
        {
            var folder = await GetOwnedAsync(userId, folderId);
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            var conversations = await context.Conversations
                .Where(x => x.FolderId == folderId && x.UserId == userId && x.IsDeleted == 0)
                .ToListAsync();

            foreach (var conversation in conversations)
            {
                conversation.FolderId = null;
                conversation.UpdatedAt = DateTime.UtcNow;
                conversation.UpdatedBy = user.UserName;
            }

            folder.IsDeleted = 1;
            folder.DeletedAt = DateTime.UtcNow;
            folder.DeletedBy = user.UserName;
            folder.UpdatedAt = DateTime.UtcNow;
            folder.UpdatedBy = user.UserName;
            await context.SaveChangesAsync();
        }

        private async Task<ConversationFolder> GetOwnedAsync(string userId, int folderId)
        {
            return await context.ConversationFolders.FirstOrDefaultAsync(x =>
                       x.Id == folderId &&
                       x.UserId == userId &&
                       x.IsDeleted == 0)
                   ?? throw new Exception("Folder not found.");
        }
    }
}
