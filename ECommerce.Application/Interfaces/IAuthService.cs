using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(
            RegisterDto dto);

        Task<AuthResponseDto> LoginAsync(
            LoginDto dto);

        Task<AuthResponseDto> RefreshTokenAsync(
            RefreshTokenDto dto);

        Task LogoutAsync(
            string refreshToken);
    }
}