using static Telega.Application.DTOs.DTO;

namespace Telega.Application.Services
{
    public interface IChatNotificationService
    {
        Task NotifyMessageReceivedAsync(Guid chatId, MessageDto message);
        Task NotifyMessageReadAsync(Guid chatId, MessageDto message);
        Task NotifyMessageRemovedAsync(Guid chatId, Guid messageId);
    }
}
