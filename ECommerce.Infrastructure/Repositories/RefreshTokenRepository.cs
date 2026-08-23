using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories
{
    public class RefreshTokenRepository
        : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(
            string token)
        {
            return await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.Token == token);
        }

        public async Task<RefreshToken> CreateAsync(
            RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(
                refreshToken);

            return refreshToken;
        }

        public async Task RevokeAsync(
            RefreshToken refreshToken)
        {
            refreshToken.RevokedAt =
                DateTime.UtcNow;

            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}