using Microsoft.AspNetCore.Http;
using Telega.Domain.Entities;

namespace Telega.Application.DTOs
{
    public class DTO
    {
        public record UserDto(Guid Id, string Login, string Email, string DisplayName, string? AvatarUrl);
        public record ChatDto(Guid Id, string Name, ChatType Type, bool IsEncrypted);
        public record MessageDto(Guid Id, Guid ChatId, Guid SenderId, string Content, MessageContentType ContentType, DateTime CreatedAt, bool IsRead);
        public record LoginRequestDto(string LoginOrEmail, string Password);
        public record RegisterRequestDto(string Login, string Email, string Password, string DisplayName);
        public record CreateChatRequestDto(string Name, ChatType Type);
        public record AddUserToChatRequestDto(Guid UserId);
        public record SendTextMessageRequestDto(Guid ChatId, string Content, MessageContentType ContentType, TimeSpan? TimeToLive = null);
        public record SendMediaMessageRequestDto(Guid ChatId, IFormFile File, TimeSpan? TimeToLive = null);
        public record BroadcastMessageRequestDto(IEnumerable<Guid> ChatIds, string Content, MessageContentType ContentType, TimeSpan? TimeToLive = null);
        public record ChatCacheDto(Guid Id, string Name, ChatType Type, DateTime CreatedAt, DateTime? UpdatedAt);
        public record SendMultipleMediaMessageRequestDto(Guid ChatId, IFormFileCollection Files, TimeSpan? TimeToLive = null);
        public record PaginatedMessagesResponseDto
        {
            public IReadOnlyCollection<MessageDto> Messages { get; init; }
            public int TotalCount { get; init; }
            public int Page { get; init; }
            public int PageSize { get; init; }
            public int TotalPages { get; init; }
        }
    }
}
