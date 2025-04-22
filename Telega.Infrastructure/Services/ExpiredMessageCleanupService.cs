using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Telega.Application.Repositories;
using Telega.Application.Services;
using Telega.Domain.Entities;

namespace Telega.Infrastructure.Services
{
    public class ExpiredMessageCleanupService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;

        public ExpiredMessageCleanupService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var messageRepository = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
                    var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
                    var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
                    var chatRepository = scope.ServiceProvider.GetRequiredService<IChatRepository>();

                    var chats = await chatRepository.GetAllAsync();
                    foreach (var chat in chats)
                    {
                        int page = 1;
                        const int pageSize = 1000;
                        while (true)
                        {
                            var (messages, totalCount) = await messageRepository.GetByChatIdAsync(chat.Id, page, pageSize);
                            var expired = messages.Where(m => m.IsExpired()).ToList();

                            foreach (var message in expired)
                            {
                                if (message.ContentType != MessageContentType.Text)
                                {
                                    var filename = chat.IsEncrypted && message.EncryptedContent != null
                                        ? encryptionService.Decrypt(message.EncryptedContent)
                                        : message.Content;
                                    await fileStorage.RemoveFileAsync(filename);
                                }
                                await messageRepository.RemoveAsync(message.Id);
                            }

                            if (page * pageSize >= totalCount) break;
                            page++;
                        }
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
