using Telega.Domain.Entities;
namespace Telega.Application.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByLoginOrEmailAsync(string loginOrEmail);
        Task<bool> ExistsByLoginOrEmailAsync(string login, string email);
        Task AddAsync(User user);
        Task<User> GetByIdAsync(Guid id);
    }
}
