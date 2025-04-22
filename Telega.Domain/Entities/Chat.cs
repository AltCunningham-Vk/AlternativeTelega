using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telega.Domain.Entities
{
    //Тип чата(Обычный или секретный)
    public enum ChatType
    {
        Regular,
        Secret
    }
    //Чаты
    public class Chat : BaseEntity
    {
        public string Name { get; private set; }//Название чата
        public ChatType Type { get; private set; }//Тип чата
        public bool IsEncrypted => Type == ChatType.Secret;//Зашифрован ли чат
        // Связь многие-ко-многим с пользователями
        private readonly List<ChatMember> _members = new();
        public IReadOnlyCollection<ChatMember> Members => _members.AsReadOnly();
        // Связь один-ко-многим с сообщениями
        private readonly List<Message> _messages = new();
        public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

        private Chat()
        {
        }
        public Chat(string name, ChatType type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
        }
        public void UpdateName(string name)
        {
            Name = name;
            UpdateTimeStamp();
        }

    }
}
