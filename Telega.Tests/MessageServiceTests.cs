using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Telega.Application.DTOs;
using Telega.Application.Repositories;
using Telega.Application.Services;
using Telega.Domain.Entities;
using Xunit;
using static Telega.Application.DTOs.DTO;

namespace Telega.Tests.Unit
{
    public class MessageServiceTests
    {
        private readonly Mock<IMessageRepository> _messageRepositoryMock;
        private readonly Mock<IChatRepository> _chatRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IFileStorageService> _fileStorageMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IEncryptionService> _encryptionMock;
        private readonly Mock<IMessageAuditLogRepository> _auditLogRepositoryMock;
        private readonly Mock<IChatNotificationService> _notificationServiceMock;
        private readonly Mock<ILogger<MessageService>> _loggerMock;
        private readonly MessageService _service;

        public MessageServiceTests()
        {
            _messageRepositoryMock = new Mock<IMessageRepository>();
            _chatRepositoryMock = new Mock<IChatRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _fileStorageMock = new Mock<IFileStorageService>();
            _cacheMock = new Mock<ICacheService>();
            _encryptionMock = new Mock<IEncryptionService>();
            _auditLogRepositoryMock = new Mock<IMessageAuditLogRepository>();
            _notificationServiceMock = new Mock<IChatNotificationService>();
            _loggerMock = new Mock<ILogger<MessageService>>();

            _service = new MessageService(
                _messageRepositoryMock.Object,
                _chatRepositoryMock.Object,
                _userRepositoryMock.Object,
                _fileStorageMock.Object,
                _cacheMock.Object,
                _encryptionMock.Object,
                _auditLogRepositoryMock.Object,
                _loggerMock.Object,
                _notificationServiceMock.Object
            );
        }

        [Fact]
        public async Task SendMessageAsync_RegularChat_Success()
        {
            // Arrange
            var chatId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var content = "Hello";
            var chat = new Chat("Test Chat", ChatType.Regular) { Id = chatId };
            var user = new User("testuser", "test@example.com", "hash", "Test User") { Id = senderId };

            _chatRepositoryMock.Setup(r => r.GetByIdAsync(chatId)).ReturnsAsync(chat);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(senderId)).ReturnsAsync(user);
            _messageRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.SendMessageAsync(chatId, senderId, content, MessageContentType.Text);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(chatId, result.ChatId);
            Assert.Equal(senderId, result.SenderId);
            Assert.Equal(content, result.Content);
            Assert.False(result.IsRead);
            _cacheMock.Verify(c => c.RemoveAsync($"chat:messages:{chatId}"), Times.Once);
            _notificationServiceMock.Verify(n => n.NotifyMessageReceivedAsync(chatId, It.IsAny<MessageDto>()), Times.Once);
            _auditLogRepositoryMock.Verify(a => a.AddAsync(It.IsAny<MessageAuditLog>()), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_SecretChat_EncryptsContent()
        {
            // Arrange
            var chatId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var content = "Secret message";
            var encryptedContent = "EncryptedSecret";
            var chat = new Chat("Secret Chat", ChatType.Secret) { Id = chatId };
            var user = new User("testuser", "test@example.com", "hash", "Test User") { Id = senderId };

            _chatRepositoryMock.Setup(r => r.GetByIdAsync(chatId)).ReturnsAsync(chat);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(senderId)).ReturnsAsync(user);
            _encryptionMock.Setup(e => e.Encrypt(content)).Returns(encryptedContent);
            _encryptionMock.Setup(e => e.Decrypt(encryptedContent)).Returns(content);
            _messageRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.SendMessageAsync(chatId, senderId, content, MessageContentType.Text);

            // Assert
            Assert.Equal(content, result.Content); // Декодированный контент
            _encryptionMock.Verify(e => e.Encrypt(content), Times.Once);
            _notificationServiceMock.Verify(n => n.NotifyMessageReceivedAsync(chatId, It.IsAny<MessageDto>()), Times.Once);
        }

        [Fact]
        public async Task MarkMessageAsReadAsync_Success()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var chatId = Guid.NewGuid();
            var chat = new Chat("Test Chat", ChatType.Regular) { Id = chatId };
            var user = new User("test", "test@example.com", "hash", "Test") { Id = userId };
            var message = new Message(chat, user, MessageContentType.Text, "Hi") { Id = messageId };

            _messageRepositoryMock.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(message);
            _chatRepositoryMock.Setup(r => r.GetByIdAsync(chatId)).ReturnsAsync(chat);
            _chatRepositoryMock.Setup(r => r.MemberExistsAsync(chatId, userId)).ReturnsAsync(true);
            _messageRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);

            // Act
            await _service.MarkMessageAsReadAsync(messageId, userId);

            // Assert
            Assert.True(message.IsRead);
            _cacheMock.Verify(c => c.RemoveAsync($"chat:messages:{chatId}"), Times.Once);
            _notificationServiceMock.Verify(n => n.NotifyMessageReadAsync(chatId, It.IsAny<MessageDto>()), Times.Once);
        }

        [Fact]
        public async Task RemoveMessageAsync_MediaMessage_DeletesFile()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var chatId = Guid.NewGuid();
            var chat = new Chat("Test Chat", ChatType.Regular) { Id = chatId };
            var user = new User("test", "test@example.com", "hash", "Test") { Id = userId };
            var message = new Message(chat, user, MessageContentType.Image, "file.jpg") { Id = messageId };

            _messageRepositoryMock.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(message);
            _chatRepositoryMock.Setup(r => r.GetByIdAsync(chatId)).ReturnsAsync(chat);
            _chatRepositoryMock.Setup(r => r.MemberExistsAsync(chatId, userId)).ReturnsAsync(true);
            _messageRepositoryMock.Setup(r => r.RemoveAsync(messageId)).Returns(Task.CompletedTask);
            _fileStorageMock.Setup(f => f.RemoveFileAsync("file.jpg")).Returns(Task.CompletedTask);

            // Act
            await _service.RemoveMessageAsync(messageId, userId);

            // Assert
            _fileStorageMock.Verify(f => f.RemoveFileAsync("file.jpg"), Times.Once);
            _cacheMock.Verify(c => c.RemoveAsync($"chat:messages:{chatId}"), Times.Once);
            _notificationServiceMock.Verify(n => n.NotifyMessageRemovedAsync(chatId, messageId), Times.Once);
        }
    }
}