namespace ECommerce.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public string AccessToken { get; internal set; }
        public DateTime AccessTokenExpiresAt { get; internal set; }
        public string RefreshToken { get; internal set; }
        public DateTime RefreshTokenExpiresAt { get; internal set; }
    }
}