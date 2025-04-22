using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telega.Application.Repositories;
using Telega.Application.Services;
using Telega.Domain.Entities;
using Telega.Infrastructure.Data;
using Telega.Infrastructure.Repositories;
using Telega.Infrastructure.Services;
using Xunit;
using Microsoft.EntityFrameworkCore.InMemory;
using static Telega.Application.DTOs.DTO;

namespace Telega.Tests.Integration
{
    public class IntegrationTests : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AppDbContext _dbContext;

        public IntegrationTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<IChatRepository, ChatRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IFileStorageService, StubFileStorageService>();
            services.AddScoped<ICacheService, StubCacheService>();
            services.AddScoped<IEncryptionService, StubEncryptionService>();
            services.AddScoped<IMessageAuditLogRepository, MessageAuditLogRepository>();
            services.AddScoped<IChatNotificationService, StubChatNotificationService>();
            services.AddLogging();

            _serviceProvider = services.BuildServiceProvider();
            _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        }

        [Fact]
        public async Task SendMessageAsync_Integration_SavesToDb()
        {
            // Arrange
            var messageService = _serviceProvider.GetRequiredService<IMessageService>();
            var chat = new Chat("Test Chat", ChatType.Regular);
            var user = new User("testuser", "test@example.com", "hash", "Test User");
            _dbContext.Chats.Add(chat);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await messageService.SendMessageAsync(chat.Id, user.Id, "Hello", MessageContentType.Text);

            // Assert
            var message = await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == result.Id);
            Assert.NotNull(message);
            Assert.Equal("Hello", message.Content);
            Assert.Equal(chat.Id, message.ChatId);
            Assert.Equal(user.Id, message.SenderId);
        }
        [Fact]
        public async Task SendMediaMessageAsync_Integration_CallsFileStorage()
        {
            // Arrange
            var messageService = _serviceProvider.GetRequiredService<IMessageService>();
            var chat = new Chat("Media Chat", ChatType.Regular);
            var user = new User("mediauser", "media@example.com", "hash", "Media User");

            _dbContext.Chats.Add(chat);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("test file"));
            var fileName = "test.txt";

            // Act
            var result = await messageService.SendMediaMessageAsync(chat.Id, user.Id, fileStream, fileName);

            // Assert
            var message = await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == result.Id);
            Assert.NotNull(message);
            Assert.Equal(MessageContentType.Document, message.ContentType);
            Assert.Contains(fileName, message.Content); // Уникальное имя содержит исходное
        }

        [Fact]
        public async Task RemoveMessageAsync_Integration_RemovesFromDb()
        {
            // Arrange
            var messageService = _serviceProvider.GetRequiredService<IMessageService>();
            var chat = new Chat("Test Chat", ChatType.Regular);
            var user = new User("testuser", "test@example.com", "hash", "Test User");
            var message = new Message(chat, user, MessageContentType.Text, "Hi");

            _dbContext.Chats.Add(chat);
            _dbContext.Users.Add(user);
            _dbContext.ChatMembers.Add(new ChatMember(user, chat)); // Добавляем пользователя в чат
            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            // Act
            await messageService.RemoveMessageAsync(message.Id, user.Id);

            // Assert
            var removedMessage = await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == message.Id);
            Assert.Null(removedMessage);

            var auditLog = await _dbContext.MessageAuditLogs.FirstOrDefaultAsync(a => a.MessageId == message.Id);
            Assert.NotNull(auditLog);
            Assert.Equal(AuditAction.Removed, auditLog.Action);
        }
        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }

    public class StubFileStorageService : IFileStorageService
    {
        public Task UploadFileAsync(string fileName, Stream fileStream) => Task.CompletedTask;
        public Task<Stream> DownloadFileAsync(string fileName) => Task.FromResult<Stream>(new MemoryStream());
        public Task RemoveFileAsync(string fileName) => Task.CompletedTask;
        public Task<bool> ObjectExistsAsync(string fileName) => Task.FromResult(true);
        public Task<string> GetPresignedUrlAsync(string fileName, int expirySeconds = 3600) => Task.FromResult("stub-url");
        public void Dispose() { }
    }

    public class StubCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) => Task.CompletedTask;
        public Task RemoveAsync(string key) => Task.CompletedTask;
    }

    public class StubEncryptionService : IEncryptionService
    {
        public string Encrypt(string content) => $"Encrypted_{content}";
        public string Decrypt(string encryptedContent) => encryptedContent.Replace("Encrypted_", "");
    }

    public class StubChatNotificationService : IChatNotificationService
    {
        public Task NotifyMessageReceivedAsync(Guid chatId, MessageDto message) => Task.CompletedTask;
        public Task NotifyMessageReadAsync(Guid chatId, MessageDto message) => Task.CompletedTask;
        public Task NotifyMessageRemovedAsync(Guid chatId, Guid messageId) => Task.CompletedTask;
    }
}