using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Categories
{
    public class CreateCategoryDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}