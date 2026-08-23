using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(
            string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == email);
        }

        public async Task<User?> GetByIdAsync(
            int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == id);
        }

        public async Task<User> CreateAsync(
            User user)
        {
            await _context.Users.AddAsync(user);

            await _context.SaveChangesAsync();

            return user;
        }
    }
}