using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> CreateAsync(
            Product product)
        {
            await _context.Products.AddAsync(product);

            await _context.SaveChangesAsync();

            return product;
        }

        public async Task<bool> UpdateAsync(
            Product product)
        {
            _context.Products.Update(product);

            var result =
                await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var product =
                await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return false;

            _context.Products.Remove(product);

            var result =
                await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<(List<Product> Items, int TotalItems)>
            SearchAsync(ProductQueryDto query)
        {
            IQueryable<Product> products =
                _context.Products
                    .Include(p => p.Category)
                    .AsNoTracking();

            // Search
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search =
                    query.Search.Trim();

                products = products.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description.Contains(search));
            }

            // Category filter
            if (query.CategoryId.HasValue)
            {
                products = products.Where(p =>
                    p.CategoryId ==
                    query.CategoryId.Value);
            }

            // Minimum price
            if (query.MinPrice.HasValue)
            {
                products = products.Where(p =>
                    p.Price >= query.MinPrice.Value);
            }

            // Maximum price
            if (query.MaxPrice.HasValue)
            {
                products = products.Where(p =>
                    p.Price <= query.MaxPrice.Value);
            }

            // Active filter
            if (query.IsActive.HasValue)
            {
                products = products.Where(p =>
                    p.IsActive ==
                    query.IsActive.Value);
            }

            // Count before pagination
            var totalItems =
                await products.CountAsync();

            // Sorting
            products = query.SortBy.ToLower()
                switch
            {
                "name" =>
                    query.SortOrder.ToLower() == "asc"
                        ? products.OrderBy(p => p.Name)
                        : products.OrderByDescending(p => p.Name),

                "price" =>
                    query.SortOrder.ToLower() == "asc"
                        ? products.OrderBy(p => p.Price)
                        : products.OrderByDescending(p => p.Price),

                "stock" =>
                    query.SortOrder.ToLower() == "asc"
                        ? products.OrderBy(p => p.Stock)
                        : products.OrderByDescending(p => p.Stock),

                _ =>
                    query.SortOrder.ToLower() == "asc"
                        ? products.OrderBy(p => p.CreatedAt)
                        : products.OrderByDescending(p => p.CreatedAt)
            };

            // Pagination
            var items =
                await products
                    .Skip(
                        (query.Page - 1)
                        * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

            return (items, totalItems);
        }
    }
}