using Telega.Domain.Entities;
using static Telega.Application.DTOs.DTO;

namespace Telega.Application.Services
{
    public interface IMessageService
    {
        Task<MessageDto> SendMessageAsync(Guid chatId, Guid senderId, string content, MessageContentType contentType, TimeSpan? timeToLive = null);
        Task<MessageDto> SendMediaMessageAsync(Guid chatId, Guid senderId, Stream fileStream, string fileName, TimeSpan? timeToLive = null);
        Task<(IReadOnlyCollection<MessageDto> Messages, int TotalCount)> GetChatMessagesAsync(Guid chatId, int page = 1, int pageSize = 20);
        Task<(Stream Stream, string FileName, string ContentType)> DownloadMediaMessageAsync(Guid messageId);
        Task<MessageDto> GetMessageByIdAsync(Guid messageId);
        Task<string> GetMediaPreviewUrlAsync(Guid messageId);
        Task MarkMessageAsReadAsync(Guid messageId, Guid userId);
        Task RemoveMessageAsync(Guid messageId, Guid userId);
        Task<IReadOnlyCollection<MessageDto>> BroadcastMessageAsync(Guid senderId, IEnumerable<Guid> chatIds, string content, MessageContentType contentType, TimeSpan? timeToLive = null);
        Task<IReadOnlyCollection<MessageDto>> SendMultipleMediaMessagesAsync(Guid chatId, Guid senderId, IEnumerable<(Stream FileStream, string FileName)> files, TimeSpan? timeToLive = null);
    }
}
