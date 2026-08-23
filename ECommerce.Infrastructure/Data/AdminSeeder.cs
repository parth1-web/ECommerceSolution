using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data
{
    public static class AdminSeeder
    {
        public static async Task SeedAsync(
            AppDbContext context)
        {
            const string adminEmail =
                "admin@ecommerce.local";

            const string adminPassword =
                "Admin123!ChangeMe";

            var existingAdmin =
                await context.Users
                    .FirstOrDefaultAsync(
                        u => u.Email == adminEmail);

            if (existingAdmin != null)
                return;

            var passwordHasher =
                new PasswordHasher<User>();

            var admin = new User
            {
                FirstName = "System",
                LastName = "Admin",
                Email = adminEmail,
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            admin.PasswordHash =
                passwordHasher.HashPassword(
                    admin,
                    adminPassword);

            context.Users.Add(admin);

            await context.SaveChangesAsync();
        }
    }
}