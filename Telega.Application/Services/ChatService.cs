using Telega.Application.Repositories;
using Telega.Domain.Entities;
using Telega.Application.DTOs;
using static Telega.Application.DTOs.DTO;

namespace Telega.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cache;

        public ChatService(IChatRepository chatRepository, IUserRepository userRepository, ICacheService cache)
        {
            _chatRepository = chatRepository;
            _userRepository = userRepository;   
            _cache = cache;
        }

        //public async Task<ChatDto> CreateChatAsync(string name, ChatType type, Guid creatorId)
        //{
        //    var creator = await _userRepository.GetByIdAsync(creatorId)
        //        ?? throw new ArgumentException("Пользователь не найден.");

        //    var chat = new Chat(name, type);
        //    await _chatRepository.AddAsync(chat);

        //    var membership = new ChatMember(creator, chat, isAdmin: true);
        //    await _chatRepository.AddMemberAsync(membership);

        //    var chatDto = MapToChatDto(chat);
        //    await _cache.SetAsync($"chat:{chat.Id}", chatDto, TimeSpan.FromHours(1));
        //    return chatDto;
        //}
        public async Task<ChatDto> CreateChatAsync(string name, ChatType type, Guid userId)
        {
            var chat = new Chat(name, type);
            await _chatRepository.AddAsync(chat);

            // Добавляем пользователя как участника
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new ArgumentException("User not found.");
            var chatMember = new ChatMember(user, chat, isAdmin: true); // Создатель — админ
            await _chatRepository.AddMemberAsync(chatMember);

            // Очищаем кэш для пользователя
            var cacheKey = $"user:chats:{userId}";
            await _cache.RemoveAsync(cacheKey);

            return MapToChatDto(chat);
        }

        public async Task AddUserToChatAsync(Guid chatId, Guid userId)
        {
            var chat = await _chatRepository.GetByIdAsync(chatId)
                ?? throw new ArgumentException("Чат не найден.");
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new ArgumentException("Пользователь не найден.");
                
            var exists = await _chatRepository.MemberExistsAsync(chatId, userId);
            //Console.WriteLine($"Checking membership: ChatId={chatId}, UserId={userId}, Exists={exists}");
            //if (exists)
            if (await _chatRepository.MemberExistsAsync(chatId, userId))
                throw new InvalidOperationException("Пользователь уже находится в чате.");

            var membership = new ChatMember(user, chat);
            await _chatRepository.AddMemberAsync(membership);

            await _cache.RemoveAsync($"user:chats:{userId}");
        }

        public async Task<IReadOnlyCollection<ChatDto>> GetUserChatsAsync(Guid userId)
        {
            var cacheKey = $"user:chats:{userId}";
            var cachedChats = await _cache.GetAsync<List<ChatDto>>(cacheKey);
            if (cachedChats != null)
                return cachedChats;

            var chats = await _chatRepository.GetUserChatsAsync(userId);
            var chatDtos = chats.Select(MapToChatDto).ToList();

            await _cache.SetAsync(cacheKey, chatDtos, TimeSpan.FromHours(1));
            return chatDtos;
        }

        private static ChatDto MapToChatDto(Chat chat) =>
            new ChatDto(chat.Id, chat.Name, chat.Type, chat.IsEncrypted);
    }
}
