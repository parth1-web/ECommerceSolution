using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(
            ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<List<CategoryDto>> GetAllAsync()
        {
            var categories =
                await _categoryRepository.GetAllAsync();

            return categories.Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            }).ToList();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category =
                await _categoryRepository.GetByIdAsync(id);

            if (category == null)
                return null;

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };
        }

        public async Task<CategoryDto> CreateAsync(
            CreateCategoryDto dto)
        {
            var existingCategory =
                await _categoryRepository.GetByNameAsync(
                    dto.Name);

            if (existingCategory != null)
            {
                throw new InvalidOperationException(
                    "A category with this name already exists.");
            }

            var category = new Category
            {
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createdCategory =
                await _categoryRepository.CreateAsync(category);

            return new CategoryDto
            {
                Id = createdCategory.Id,
                Name = createdCategory.Name,
                Description = createdCategory.Description,
                IsActive = createdCategory.IsActive,
                CreatedAt = createdCategory.CreatedAt
            };
        }

        public async Task<bool> UpdateAsync(
            int id,
            UpdateCategoryDto dto)
        {
            var category =
                await _categoryRepository.GetByIdAsync(id);

            if (category == null)
                return false;

            category.Name = dto.Name.Trim();

            category.Description =
                dto.Description.Trim();

            category.IsActive = dto.IsActive;

            category.UpdatedAt = DateTime.UtcNow;

            return await _categoryRepository
                .UpdateAsync(category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _categoryRepository
                .DeleteAsync(id);
        }
    }
}