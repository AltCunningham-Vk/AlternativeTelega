using Microsoft.AspNetCore.SignalR;
using Telega.Application.Services;
using Telega.Presentation.Hubs;
using static Telega.Application.DTOs.DTO;

namespace Telega.Presentation.Services
{
    public class ChatNotificationService : IChatNotificationService
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatNotificationService(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public async Task NotifyMessageReceivedAsync(Guid chatId, MessageDto message)
        {
            await _hubContext.Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", message);
        }

        public async Task NotifyMessageReadAsync(Guid chatId, MessageDto message)
        {
            await _hubContext.Clients.Group(chatId.ToString()).SendAsync("MessageRead", message);
        }

        public async Task NotifyMessageRemovedAsync(Guid chatId, Guid messageId)
        {
            await _hubContext.Clients.Group(chatId.ToString()).SendAsync("MessageRemoved", messageId);
        }
    }
}
