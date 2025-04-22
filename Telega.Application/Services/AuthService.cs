using Telega.Application.Repositories;
using Telega.Domain.Entities;
using static Telega.Application.DTOs.DTO;

namespace Telega.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cache;

        public AuthService(IUserRepository userRepository, ICacheService cache)
        {
            _userRepository = userRepository;
            _cache = cache;
        }

        public async Task<UserDto> RegisterAsync(RegisterRequestDto request)
        {
            if (await _userRepository.ExistsByLoginOrEmailAsync(request.Login, request.Email))
                throw new InvalidOperationException("User with this login or email already exists.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User(
                request.Login, 
                request.Email, 
                passwordHash, 
                request.DisplayName);

            await _userRepository.AddAsync(user);

            var userDto = MapToUserDto(user);
            await _cache.SetAsync($"user:{user.Id}", userDto, TimeSpan.FromHours(1));
            return userDto;
        }

        public async Task<UserDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByLoginOrEmailAsync(request.LoginOrEmail);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid login or password.");

            var userDto = MapToUserDto(user);
            await _cache.SetAsync($"user:{user.Id}", userDto, TimeSpan.FromHours(1));
            return userDto;
        }

        private static UserDto MapToUserDto(User user) =>
            new UserDto(user.Id, user.Login, user.Email, user.DisplayName, user.AvatarUrl);
    }
}
