using Telega.Domain.Entities;

namespace Telega.Application.Repositories
{
    public interface IChatRepository
    {
        Task<Chat> GetByIdAsync(Guid id);
        Task AddAsync(Chat chat);
        Task AddMemberAsync(ChatMember member);
        Task<bool> MemberExistsAsync(Guid chatId, Guid userId);
        Task<IReadOnlyCollection<Chat>> GetUserChatsAsync(Guid userId);
        Task<IReadOnlyCollection<Chat>> GetAllAsync();
    }
}
