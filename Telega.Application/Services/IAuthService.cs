using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Telega.Application.DTOs.DTO;

namespace Telega.Application.Services
{
    public interface IAuthService
    {
        Task<UserDto> RegisterAsync(RegisterRequestDto request);
        Task<UserDto> LoginAsync(LoginRequestDto request);

    }
}
