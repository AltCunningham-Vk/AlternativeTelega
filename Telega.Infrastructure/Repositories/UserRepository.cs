using Microsoft.EntityFrameworkCore;
using Telega.Application.Repositories;
using Telega.Domain.Entities;
using Telega.Infrastructure.Data;

namespace Telega.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _dbContext;

        public UserRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User> GetByLoginOrEmailAsync(string loginOrEmail) =>
            await _dbContext.Users.FirstOrDefaultAsync(u => u.Login == loginOrEmail || u.Email == loginOrEmail);

        public async Task<bool> ExistsByLoginOrEmailAsync(string login, string email) =>
            await _dbContext.Users.AnyAsync(u => u.Login == login || u.Email == email);

        public async Task AddAsync(User user)
        {
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<User> GetByIdAsync(Guid id) =>
            await _dbContext.Users.FindAsync(id);
    }
}
