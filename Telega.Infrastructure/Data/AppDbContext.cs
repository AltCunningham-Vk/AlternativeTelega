using Microsoft.EntityFrameworkCore;
using Telega.Domain.Entities;

namespace Telega.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ChatMember> ChatMembers { get; set; }
        public DbSet<MessageAuditLog> MessageAuditLogs { get; set; } // Добавлено для логов аудита сообщений

        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Login).
                    IsRequired().
                    HasMaxLength(50);
                entity.Property(e => e.PasswordHash).
                    IsRequired();
                entity.Property(e => e.Email).
                    IsRequired().
                    HasMaxLength(100);
                entity.Property(e => e.DisplayName).
                    IsRequired().
                    HasMaxLength(100);
                entity.Property(e => e.AvatarUrl).
                    HasMaxLength(500);


                //индексы
                entity.HasIndex(e => e.Login).
                    IsUnique();
                entity.HasIndex(e => e.Email).
                    IsUnique();

            });

            //Настройка сущности Chat
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).
                   HasMaxLength(100);
                entity.Property(e => e.Type).
                    IsRequired().
                    HasConversion<int>();

            });

            //Настройка сущности ChatMember
            modelBuilder.Entity<ChatMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                //Связь с User
                entity.HasOne(e => e.User).
                    WithMany(e => e.ChatMemberships).
                    HasForeignKey(e => e.UserId).
                    OnDelete(DeleteBehavior.Cascade);
                //Связь с Chat
                entity.HasOne(e => e.Chat).
                    WithMany(e => e.Members).
                    HasForeignKey(e => e.ChatId).
                    OnDelete(DeleteBehavior.Cascade);


                //индексы
                entity.HasIndex(e => new { e.ChatId, e.UserId }).
                    IsUnique();
            });
            //Настройка сущности Message
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                //Связь с Chat
                entity.HasOne(e => e.Chat).
                    WithMany(e => e.Messages).
                    HasForeignKey(e => e.ChatId).
                    OnDelete(DeleteBehavior.Cascade);
                //Связь с User
                entity.HasOne(e => e.Sender).
                    WithMany().
                    HasForeignKey(e => e.SenderId).
                    OnDelete(DeleteBehavior.Restrict);


                entity.Property(e => e.Content).
                    IsRequired();
                entity.Property(e => e.ContentType).
                    IsRequired().
                    HasConversion<int>();
                entity.Property(e => e.EncryptedContent).
                    HasMaxLength(5000);
                entity.Property(e => e.CreatedAt).
                    HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).
                    HasDefaultValue(null);
            });
            
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            //Обновление времени изменения для всех сущностей
            var entries = ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.State == EntityState.Modified);
            foreach (var entry in entries)
            {
                entry.Entity.UpdateTimeStamp();
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
