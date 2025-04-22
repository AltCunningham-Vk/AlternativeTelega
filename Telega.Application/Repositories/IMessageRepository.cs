using Telega.Domain.Entities;

namespace Telega.Application.Repositories
{
    public interface IMessageRepository
    {
        Task AddAsync(Message message);
        Task<(IReadOnlyCollection<Message> Messages, int TotalCount)> GetByChatIdAsync(Guid chatId, int page = 1, int pageSize = 20);
        Task<Message> GetByIdAsync(Guid messageid);
        Task UpdateAsync(Message message);
        Task RemoveExpiredAsync();
        Task RemoveAsync(Guid messageId);

    }
}
