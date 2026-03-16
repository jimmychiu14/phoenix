using Phoenix.Domain.Entities;

namespace Phoenix.Application.Services;

public interface IAuthService
{
    Task<User?> RegisterUserAsync(RegisterDto dto);
    Task<string?> LoginAsync(LoginDto dto);
}
