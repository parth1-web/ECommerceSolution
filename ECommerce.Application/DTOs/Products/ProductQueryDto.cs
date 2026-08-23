namespace ECommerce.Application.DTOs.Products
{
    public class ProductQueryDto
    {
        public string? Search { get; set; }

        public int? CategoryId { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public bool? IsActive { get; set; }

        public string SortBy { get; set; } = "createdAt";

        public string SortOrder { get; set; } = "desc";

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}