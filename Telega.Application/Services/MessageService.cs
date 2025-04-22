using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Telega.Application.Repositories;
using Telega.Domain.Entities;
using static Telega.Application.DTOs.DTO;

namespace Telega.Application.Services

{   
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IChatRepository _chatRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFileStorageService _fileStorage;
        private readonly ICacheService _cache;
        private readonly IEncryptionService _encryptionService;
        private readonly IMessageAuditLogRepository _auditLogRepository;
        private readonly ILogger<MessageService> _logger;
        private readonly IChatNotificationService _notificationService;
       
        public MessageService(
            IMessageRepository messageRepository, 
            IChatRepository chatRepository,
            IUserRepository userRepository, 
            IFileStorageService fileStorage, 
            ICacheService cache, 
            IEncryptionService encryptionService, 
            IMessageAuditLogRepository auditLogRepository, 
            ILogger<MessageService> logger,
            IChatNotificationService notificationService)
        {
            _messageRepository = messageRepository;
            _chatRepository = chatRepository;
            _userRepository = userRepository;
            _fileStorage = fileStorage;
            _cache = cache;
            _encryptionService = encryptionService;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<IReadOnlyCollection<MessageDto>> BroadcastMessageAsync(Guid senderId, IEnumerable<Guid> chatIds, string content, MessageContentType contentType, TimeSpan? timeToLive = null)
        {
            var sender = await _userRepository.GetByIdAsync(senderId)
                ?? throw new ArgumentException("User not found.");
            var chats = new List<Chat>();
            foreach (var chatId in chatIds.Distinct())
            {
                var chat = await _chatRepository.GetByIdAsync(chatId);
                if (chat != null && await _chatRepository.MemberExistsAsync(chatId, senderId))
                {
                    chats.Add(chat);
                }
            }

            if (!chats.Any())
                throw new ArgumentException("No valid chats found for broadcast.");

            var messageDtos = new List<MessageDto>();

            foreach (var chat in chats)
            {
                string? encryptedContent = chat.IsEncrypted ? _encryptionService.Encrypt(content) : null;
                var message = new Message(chat, sender, contentType, content, encryptedContent, timeToLive);

                await _messageRepository.AddAsync(message); // Теперь это не вызовет ошибку
                await _cache.RemoveAsync($"chat:messages:{chat.Id}");
                await _cache.RemoveAsync($"chat:{chat.Id}");

                _logger.LogInformation("Broadcast message {MessageId} created in chat {ChatId} by user {SenderId}", message.Id, chat.Id, senderId);
                await _auditLogRepository.AddAsync(new MessageAuditLog(message.Id, senderId, AuditAction.Created));

                var messageDto = MapToMessageDto(message, chat.IsEncrypted);
                messageDtos.Add(messageDto);
                await _notificationService.NotifyMessageReceivedAsync(chat.Id, messageDto);
            }

            return messageDtos.AsReadOnly();
        }

        public async Task<MessageDto> SendMessageAsync(Guid chatId, Guid senderId, string content, MessageContentType contentType, TimeSpan? timeToLive = null)
        {
            var chat = await _chatRepository.GetByIdAsync(chatId)
                ?? throw new ArgumentException("Chat not found.");
            var sender = await _userRepository.GetByIdAsync(senderId)
                ?? throw new ArgumentException("User not found.");

            string? encryptedContent = chat.IsEncrypted ? _encryptionService.Encrypt(content) : null;
            var message = new Message(chat, sender, contentType, content, encryptedContent, timeToLive);

            await _messageRepository.AddAsync(message);
            await _cache.RemoveAsync($"chat:messages:{chatId}");
            await _cache.RemoveAsync($"chat:{chatId }");
            
            _logger.LogInformation("Сообщение {MessageId}, созданное в чате {chatId} пользователем {SenderID}", message.Id, chatId, senderId); 
            await _auditLogRepository.AddAsync(new MessageAuditLog(message.Id, senderId, AuditAction.Created));

            var messageDto = MapToMessageDto(message, chat.IsEncrypted);
            await _notificationService.NotifyMessageReceivedAsync(chatId, messageDto);

            return MapToMessageDto(message, chat.IsEncrypted);
        }

        public async Task<MessageDto> SendMediaMessageAsync(Guid chatId, Guid senderId, Stream fileStream, string fileName, TimeSpan? timeToLive = null)
        {
            var chat = await _chatRepository.GetByIdAsync(chatId)
                ?? throw new ArgumentException("Chat not found.");
            var sender = await _userRepository.GetByIdAsync(senderId)
                ?? throw new ArgumentException("User not found.");

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            await _fileStorage.UploadFileAsync(uniqueFileName, fileStream);

            var contentType = DetermineContentType(uniqueFileName);
            string? encryptedContent = chat.IsEncrypted ? _encryptionService.Encrypt(uniqueFileName) : null;
            var message = new Message(chat, sender, contentType, uniqueFileName, encryptedContent, timeToLive);

            await _messageRepository.AddAsync(message);

            await _cache.RemoveAsync($"chat:messages:{chatId}");

            _logger.LogInformation("Медиасообщение {MessageId}, созданное в чате {chatId} пользователем {SenderID}", message.Id, chatId, senderId);
            await _auditLogRepository.AddAsync(new MessageAuditLog(message.Id, senderId, AuditAction.Created));

            var messageDto = MapToMessageDto(message, chat.IsEncrypted);
            await _notificationService.NotifyMessageReceivedAsync(chatId, messageDto);

            return MapToMessageDto(message, chat.IsEncrypted);
        }

        public async Task<(IReadOnlyCollection<MessageDto> Messages, int TotalCount)> GetChatMessagesAsync(Guid chatId, int page = 1, int pageSize = 20)
        {
            var cacheKey = $"chat:messages:{chatId}:page:{page}:size:{pageSize}";
            var cachedResult = await _cache.GetAsync<(List<MessageDto> Messages, int TotalCount)>(cacheKey);
            if (cachedResult.Messages != null)
            {
                return (cachedResult.Messages.AsReadOnly(), cachedResult.TotalCount);
            }

            var chat = await _chatRepository.GetByIdAsync(chatId)
                ?? throw new ArgumentException("Chat not found.");
            var (messages, totalCount) = await _messageRepository.GetByChatIdAsync(chatId, page, pageSize);
            var messageDtos = messages
                .Where(m => !m.IsExpired())
                .Select(m => MapToMessageDto(m, chat.IsEncrypted))
                .ToList();

            await _cache.SetAsync(cacheKey, (messageDtos, totalCount), TimeSpan.FromMinutes(15));
            return (messageDtos.AsReadOnly(), totalCount);
        }
        public async Task<(Stream Stream, string FileName, string ContentType)> DownloadMediaMessageAsync(Guid messageId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId)
                ?? throw new ArgumentException("Сообщение не найдено.");
            var chat = await _chatRepository.GetByIdAsync(message.ChatId)
                ?? throw new ArgumentException("Чат не найден.");


            var fileName = chat.IsEncrypted ? _encryptionService.Decrypt(message.EncryptedContent) : message.Content;
            var stream = await _fileStorage.DownloadFileAsync(fileName);
            var contentType = GetMimeType(fileName);

            _logger.LogDebug("Media message {MessageId} downloaded", messageId);
            return (stream, Path.GetFileName(fileName), contentType);
        }
        public async Task<MessageDto> GetMessageByIdAsync(Guid messageId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message == null) return null;

            var chat = await _chatRepository.GetByIdAsync(message.ChatId)
                ?? throw new ArgumentException("Чат не найден.");

            return MapToMessageDto(message, chat.IsEncrypted);
        }

        public async Task<string> GetMediaPreviewUrlAsync(Guid messageId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId)
                ?? throw new ArgumentException("Сообщение не найдено.");
            var chat = await _chatRepository.GetByIdAsync(message.ChatId)
                ?? throw new ArgumentException("Чат не найден.");

            if (message.ContentType != MessageContentType.Image)
                throw new ArgumentException("Графические сообщения не имеют URL-адреса для предварительного просмотра.");

            var cacheKey = $"media:preview:{messageId}";
            var cachedUrl = await _cache.GetAsync<string>(cacheKey);
            if (cachedUrl != null)
                return cachedUrl;

            var fileName = chat.IsEncrypted ? _encryptionService.Decrypt(message.EncryptedContent) : message.Content;
            var url = await _fileStorage.GetPresignedUrlAsync(fileName);

            await _cache.SetAsync(cacheKey, url, TimeSpan.FromSeconds(3600)); // Кэшируем на то же время
            return url;
        }

        public async Task MarkMessageAsReadAsync(Guid messageId, Guid userId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId)
                ?? throw new ArgumentException("Сообщение не найдено.");
            var chat = await _chatRepository.GetByIdAsync(message.ChatId);
            if (!await _chatRepository.MemberExistsAsync(chat.Id, userId))
                throw new UnauthorizedAccessException("Пользователь не является участником этого чата.");
            if (message.IsRead)
                return;// Уже прочитано, ничего не делаем

            message.MarkAsRead();
            await _messageRepository.UpdateAsync(message);

            await _cache.RemoveAsync($"chat:messages:{message.ChatId}");

            _logger.LogInformation("Сообщение {MessageId} помечено как прочитанное пользователем {userId}", messageId, userId);
            await _auditLogRepository.AddAsync(new MessageAuditLog(messageId, userId, AuditAction.MarkedAsRead));

            var messageDto = MapToMessageDto(message, chat.IsEncrypted);
            await _notificationService.NotifyMessageReadAsync(chat.Id, messageDto);
        }
        public async Task RemoveMessageAsync(Guid messageId, Guid userId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId)
                ?? throw new ArgumentException("Сообщение не найдено.");
            var chat = await _chatRepository.GetByIdAsync(message.ChatId)
                ?? throw new ArgumentException("Чат не найден.");

            if (!await _chatRepository.MemberExistsAsync(chat.Id, userId))
                throw new UnauthorizedAccessException("Пользователь не является участником этого чата.");

            // Если это медиа, удаляем файл из Minio
            if (message.ContentType != MessageContentType.Text)
            {
                var fileName = chat.IsEncrypted ? _encryptionService.Decrypt(message.EncryptedContent) : message.Content;
                await _fileStorage.RemoveFileAsync(fileName);
            }

            // Удаляем сообщение из базы
            await _messageRepository.RemoveAsync(messageId);

            // Очищаем кэш
            await _cache.RemoveAsync($"chat:messages:{chat.Id}");
            await _cache.RemoveAsync($"media:preview:{messageId}");

            _logger.LogInformation("Сообщение {MessageId} удалено пользователем {userId}", messageId, userId);
            await _auditLogRepository.AddAsync(new MessageAuditLog(messageId, userId, AuditAction.Removed));

            await _notificationService.NotifyMessageRemovedAsync(chat.Id, messageId);
        }
        
        

        private MessageDto MapToMessageDto(Message message, bool isEncrypted)
        {
            var content = isEncrypted && message.EncryptedContent != null
                ? _encryptionService.Decrypt(message.EncryptedContent)
                : message.Content;

            return new MessageDto(
                message.Id,
                message.ChatId,
                message.SenderId,
                content,
                message.ContentType,
                message.CreatedAt,
                message.IsRead
            );
        }

        private static string Encrypt(string content) => content; // Заглушка

        private static MessageContentType DetermineContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".jpg" or ".png" => MessageContentType.Image,
                ".mp4" => MessageContentType.Video,
                ".mp3" => MessageContentType.Audio,
                ".pdf" or ".docx" => MessageContentType.Document,
                _ => MessageContentType.Document
            };
        }
        private static string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".png" => "image/png",
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }

        public async Task<IReadOnlyCollection<MessageDto>> SendMultipleMediaMessagesAsync(Guid chatId, Guid senderId, IEnumerable<(Stream FileStream, string FileName)> files, TimeSpan? timeToLive = null)
        {
            if (files == null || !files.Any())
                throw new ArgumentException("At least one file is required.");

            var chat = await _chatRepository.GetByIdAsync(chatId)
                ?? throw new ArgumentException("Chat not found.");
            var sender = await _userRepository.GetByIdAsync(senderId)
                ?? throw new ArgumentException("User not found.");

            if (!await _chatRepository.MemberExistsAsync(chatId, senderId))
                throw new UnauthorizedAccessException("User is not a member of this chat.");

            var messageDtos = new List<MessageDto>();

            foreach (var (fileStream, fileName) in files)
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                await _fileStorage.UploadFileAsync(uniqueFileName, fileStream);

                var contentType = DetermineContentType(uniqueFileName);
                string? encryptedContent = chat.IsEncrypted ? _encryptionService.Encrypt(uniqueFileName) : null;
                var message = new Message(chat, sender, contentType, uniqueFileName, encryptedContent, timeToLive);

                await _messageRepository.AddAsync(message);
                await _cache.RemoveAsync($"chat:messages:{chatId}");
                await _cache.RemoveAsync($"chat:{chatId}");

                _logger.LogInformation("Media message {MessageId} created in chat {ChatId} by user {SenderId}", message.Id, chatId, senderId);
                await _auditLogRepository.AddAsync(new MessageAuditLog(message.Id, senderId, AuditAction.Created));

                var messageDto = MapToMessageDto(message, chat.IsEncrypted);
                messageDtos.Add(messageDto);
                await _notificationService.NotifyMessageReceivedAsync(chatId, messageDto);
            }

            return messageDtos.AsReadOnly();
        }
    }
}
