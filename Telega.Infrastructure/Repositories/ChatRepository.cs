using Microsoft.EntityFrameworkCore;
using Telega.Application.DTOs;
using Telega.Application.Repositories;
using Telega.Application.Services;
using Telega.Domain.Entities;
using static Telega.Application.DTOs.DTO;
using Telega.Infrastructure.Data;

namespace Telega.Infrastructure.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly ICacheService _cacheService;

        public ChatRepository(AppDbContext dbContext, ICacheService cacheService)
        {
            _dbContext = dbContext;
            _cacheService = cacheService;
        }

        public async Task<Chat> GetByIdAsync(Guid id)
        {
            var cacheKey = $"chat:{id}";
            var cachedChatDto = await _cacheService.GetAsync<ChatCacheDto>(cacheKey);
            if (cachedChatDto != null)
            {
                var chat = new Chat(cachedChatDto.Name, cachedChatDto.Type)
                {
                    Id = cachedChatDto.Id,
                    CreatedAt = cachedChatDto.CreatedAt,
                    UpdatedAt = cachedChatDto.UpdatedAt
                };
                return chat;
            }

            var chatFromDb = await _dbContext.Chats
                .Include(c => c.Members)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chatFromDb != null)
            {
                var chatDto = new ChatCacheDto(chatFromDb.Id, chatFromDb.Name, chatFromDb.Type, chatFromDb.CreatedAt, chatFromDb.UpdatedAt);
                await _cacheService.SetAsync(cacheKey, chatDto, TimeSpan.FromMinutes(15));
            }

            return chatFromDb;
        }

        public async Task AddAsync(Chat chat)
        {
            _dbContext.Chats.Add(chat);
            await _dbContext.SaveChangesAsync();

            var cacheKey = $"chat:{chat.Id}";
            var chatDto = new ChatCacheDto(chat.Id, chat.Name, chat.Type, chat.CreatedAt, chat.UpdatedAt);
            await _cacheService.SetAsync(cacheKey, chatDto, TimeSpan.FromMinutes(15));

            foreach (var member in chat.Members)
            {
                await _cacheService.RemoveAsync($"user:chats:{member.UserId}");
            }
        }

        public async Task AddMemberAsync(ChatMember member)
        {
            _dbContext.ChatMembers.Add(member);
            await _dbContext.SaveChangesAsync();

            await _cacheService.RemoveAsync($"chat:{member.ChatId}");
            await _cacheService.RemoveAsync($"user:chats:{member.UserId}");
        }

        public async Task<bool> MemberExistsAsync(Guid chatId, Guid userId)
        {
            return await _dbContext.ChatMembers
                .AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);
        }

        public async Task<IReadOnlyCollection<Chat>> GetUserChatsAsync(Guid userId)
        {
            var cacheKey = $"user:chats:{userId}";
            var cachedChatsDto = await _cacheService.GetAsync<List<ChatCacheDto>>(cacheKey);
            if (cachedChatsDto != null)
            {
                var chats = cachedChatsDto.Select(dto => new Chat(dto.Name, dto.Type)
                {
                    Id = dto.Id,
                    CreatedAt = dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt
                }).ToList();
                return chats.AsReadOnly();
            }

            var chatsFromDb = await _dbContext.ChatMembers
                .Where(cm => cm.UserId == userId)
                .Select(cm => cm.Chat)
                .ToListAsync();

            var chatsDto = chatsFromDb.Select(c => new ChatCacheDto(c.Id, c.Name, c.Type, c.CreatedAt, c.UpdatedAt)).ToList();
            await _cacheService.SetAsync(cacheKey, chatsDto, TimeSpan.FromMinutes(15));
            return chatsFromDb.AsReadOnly();
        }
        public async Task<IReadOnlyCollection<Chat>> GetAllAsync()
        {
            var chats = await _dbContext.Chats
                .Include(c => c.Members)
                .ToListAsync();
            return chats.AsReadOnly();
        }
    }
}