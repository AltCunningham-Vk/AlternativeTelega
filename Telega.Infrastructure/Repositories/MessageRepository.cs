using Microsoft.EntityFrameworkCore;
using Telega.Application.Repositories;
using Telega.Domain.Entities;
using Telega.Infrastructure.Data;

namespace Telega.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _dbContext;

        public MessageRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Message message)
        {
            var existingChat = await _dbContext.Chats.FindAsync(message.ChatId);
            if (existingChat != null)
            {
                // Если Chat уже есть в базе, используем его вместо переданного экземпляра
                message.SetChat(existingChat);
            }
            else
            {
                // Прикрепляем Chat, если он ещё не отслеживается
                _dbContext.Attach(message.Chat);
            }

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<(IReadOnlyCollection<Message> Messages, int TotalCount)> GetByChatIdAsync(Guid chatId, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _dbContext.Messages
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.CreatedAt);

            var totalCount = await query.CountAsync();
            var messages = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (messages.AsReadOnly(), totalCount);
        }
        public async Task<Message> GetByIdAsync(Guid messageId)
        {
            return await _dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId)
                ?? throw new ArgumentException($"Message with ID {messageId} not found.");
        }
        public async Task UpdateAsync(Message message)
        {
            _dbContext.Messages.Update(message);
            await _dbContext.SaveChangesAsync();
        }
        public async Task RemoveExpiredAsync()
        {
            var expiredMessages = await _dbContext.Messages
                .Where(m => m.ExpirationDate.HasValue && m.ExpirationDate <= DateTime.UtcNow)
                .ToListAsync();
            if (expiredMessages.Any())
            {
                _dbContext.Messages.RemoveRange(expiredMessages);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task RemoveAsync(Guid messageId)
        {
            var message = await _dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null)
                throw new ArgumentException($"Message with ID {messageId} not found.");
            _dbContext.Messages.Remove(message);
            await _dbContext.SaveChangesAsync();
        }

    }
}
