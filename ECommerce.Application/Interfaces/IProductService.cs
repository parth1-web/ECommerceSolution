using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Interfaces
{
    public interface IProductService
    {
        Task<PagedResult<ProductDto>> SearchAsync(
            ProductQueryDto query);

        Task<ProductDto?> GetByIdAsync(int id);

        Task<ProductDto> CreateAsync(
            CreateProductDto dto);

        Task<bool> UpdateAsync(
            int id,
            UpdateProductDto dto);

        Task<bool> DeleteAsync(int id);
    }
}