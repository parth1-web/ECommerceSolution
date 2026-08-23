using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(
            string token);

        Task<RefreshToken> CreateAsync(
            RefreshToken refreshToken);

        Task RevokeAsync(
            RefreshToken refreshToken);

        Task SaveChangesAsync();
    }
}