using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context
                .Set<Category>()
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Set<Category>()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Category?> GetByNameAsync(
            string name)
        {
            return await _context.Set<Category>()
                .FirstOrDefaultAsync(c => c.Name == name);
        }

        public async Task<Category> CreateAsync(
            Category category)
        {
            await _context.Set<Category>().AddAsync(category);

            await _context.SaveChangesAsync();

            return category;
        }

        public async Task<bool> UpdateAsync(
            Category category)
        {
            _context.Set<Category>().Update(category);

            var result =
                await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Set<Category>().FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return false;

            _context.Set<Category>().Remove(category);

            var result =
                await _context.SaveChangesAsync();

            return result > 0;
        }
    }
}