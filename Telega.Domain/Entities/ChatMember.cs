using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telega.Domain.Entities
{
    
    //Связующая сущность для отношения многие-ко-многим между пользователями и чатами
    public class ChatMember : BaseEntity
    {
        public Guid ChatId { get; private set; }
        public Chat Chat { get; private set; } = default!;
        public Guid UserId { get; private set; }
        public User User { get; private set; } = default!;
        public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;
        public bool IsAdmin { get; private set; }
       
        private ChatMember()
        {
        }
        public ChatMember(User user, Chat chat, bool isAdmin = false)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            UserId = user.Id;
            Chat = chat ?? throw new ArgumentNullException(nameof(chat));
            ChatId = chat.Id;
            IsAdmin = isAdmin;
            
        }
       

    }
}
