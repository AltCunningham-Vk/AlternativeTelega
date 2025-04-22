using Telega.Domain.Entities;
using static Telega.Application.DTOs.DTO;

namespace Telega.Application.Services
{
    public interface IChatService
    {
        Task<ChatDto> CreateChatAsync(string name, ChatType type, Guid creatorId);
        Task AddUserToChatAsync(Guid chatId, Guid userId);
        Task<IReadOnlyCollection<ChatDto>> GetUserChatsAsync(Guid userId);
    }
}
