using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telega.Domain.Entities
{
    //Тип контента сообщения
    public enum MessageContentType
    {
        Text,
        Image,
        Video,
        Audio,
        Document
    }
    //Сообщения в чате
    public class Message : BaseEntity
    {
        public Guid ChatId { get; private set; }
        public Chat Chat { get; private set; }
        public Guid SenderId { get; private set; }
        public User Sender { get; private set; }
        public DateTime? ExpirationDate { get; private set; }
        public MessageContentType ContentType { get; private set; }
        public string Content { get; private set; }//текст или ссылка на файл
        public string? EncryptedContent { get; private set; }//зашифрованный контент
        public bool IsRead { get; private set; }


        private Message()
        {
        }
        public Message(Chat chat, User sender, MessageContentType contentType, string content, string? encryptedContent = null, TimeSpan? timeToLive = null)
        {
            Chat = chat ?? throw new ArgumentNullException(nameof(chat));
            ChatId = chat.Id;
            Sender = sender ?? throw new ArgumentNullException(nameof(sender));
            SenderId = sender.Id;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            ContentType = contentType;
            EncryptedContent = encryptedContent;
            ExpirationDate = timeToLive.HasValue && timeToLive.Value > TimeSpan.Zero
                ? DateTime.UtcNow + timeToLive.Value 
                : null;

        }
        public void SetChat(Chat chat)
        {
            Chat = chat ?? throw new ArgumentNullException(nameof(chat));
            ChatId = chat.Id;
        }
        public void MarkAsRead()
        {
            IsRead = true;
            UpdateTimeStamp();
        }
        public bool IsExpired() => ExpirationDate.HasValue && DateTime.UtcNow >= ExpirationDate.Value;
    }
}
