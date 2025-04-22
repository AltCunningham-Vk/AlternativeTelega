using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telega.Domain.Entities
{
    //Пользователи системы
    public class User : BaseEntity
    {
        public string Login { get; private set; } //Уникальный логин
        public string PasswordHash { get; private set; }//Хэш пароля
        public string Email { get; private set; }//Почта
        public string DisplayName { get; private set; }//Отображаемое имя
        public string? AvatarUrl { get; private set; }//Ссылка на аватар
        // Связь многие-ко-многим с чатами
        private readonly List<ChatMember> _chatMemberships = new();
        public IReadOnlyCollection<ChatMember> ChatMemberships => _chatMemberships.AsReadOnly();

        private User()
        {
        }
        public User(string login, string email, string passwordHash, string displayName)
        {
            Login = login ?? throw new ArgumentNullException(nameof(login));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }

        public void UpdateProfile(string displayName, string? avatarUrl)
        {
            DisplayName = displayName;
            AvatarUrl = avatarUrl;
            UpdateTimeStamp();
        }
    }
}
