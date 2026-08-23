using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces
{
    public interface ICurrentUserService
    {
        int? UserId { get; }

        string? Email { get; }

        string? Role { get; }

        bool IsAuthenticated { get; }

        bool IsAdmin { get; }
    }
}