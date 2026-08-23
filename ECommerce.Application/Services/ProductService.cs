using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<List<ProductDto>> GetAllAsync()
        {
            var products =
                await _productRepository.GetAllAsync();

            return products.Select(MapToDto).ToList();
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product =
                await _productRepository.GetByIdAsync(id);

            if (product == null)
                return null;

            return MapToDto(product);
        }

        public async Task<ProductDto> CreateAsync(
            CreateProductDto dto)
        {
            var category =
                await _categoryRepository
                    .GetByIdAsync(dto.CategoryId);

            if (category == null)
            {
                throw new InvalidOperationException(
                    "The selected category does not exist.");
            }

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                Price = dto.Price,
                Stock = dto.Stock,
                ImageUrl = dto.ImageUrl.Trim(),
                CategoryId = dto.CategoryId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createdProduct =
                await _productRepository
                    .CreateAsync(product);

            return MapToDto(createdProduct);
        }

        public async Task<bool> UpdateAsync(
            int id,
            UpdateProductDto dto)
        {
            var product =
                await _productRepository
                    .GetByIdAsync(id);

            if (product == null)
                return false;

            var category =
                await _categoryRepository
                    .GetByIdAsync(dto.CategoryId);

            if (category == null)
            {
                throw new InvalidOperationException(
                    "The selected category does not exist.");
            }

            product.Name = dto.Name.Trim();
            product.Description = dto.Description.Trim();
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.ImageUrl = dto.ImageUrl.Trim();
            product.CategoryId = dto.CategoryId;
            product.IsActive = dto.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            return await _productRepository
                .UpdateAsync(product);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _productRepository
                .DeleteAsync(id);
        }

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                CategoryId = product.CategoryId,
                CategoryName =
                    product.Category?.Name ?? string.Empty
            };
        }
        public async Task<PagedResult<ProductDto>> SearchAsync(
    ProductQueryDto query)
        {
            // Protect the API from invalid pagination values

            if (query.Page < 1)
                query.Page = 1;

            if (query.PageSize < 1)
                query.PageSize = 10;

            if (query.PageSize > 100)
                query.PageSize = 100;

            var result =
                await _productRepository.SearchAsync(query);

            return new PagedResult<ProductDto>
            {
                Items = result.Items
                    .Select(MapToDto)
                    .ToList(),

                Page = query.Page,

                PageSize = query.PageSize,

                TotalItems = result.TotalItems,

                TotalPages =
                    (int)Math.Ceiling(
                        result.TotalItems /
                        (double)query.PageSize)
            };
        }
    }
}