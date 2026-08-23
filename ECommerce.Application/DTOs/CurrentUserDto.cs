namespace ECommerce.Application.DTOs
{
    public class CurrentUserDto
    {
        public int UserId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }
}