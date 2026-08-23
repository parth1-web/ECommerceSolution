using System.Security.Claims;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User =>
            _httpContextAccessor.HttpContext?.User;

        public int? UserId
        {
            get
            {
                var claim = User?.FindFirst(
                    ClaimTypes.NameIdentifier);

                if (claim == null)
                    return null;

                return int.TryParse(
                    claim.Value,
                    out var id)
                    ? id
                    : null;
            }
        }

        public string? Email =>
            User?.FindFirst(ClaimTypes.Email)?.Value;

        public string? Role =>
            User?.FindFirst(ClaimTypes.Role)?.Value;

        public bool IsAuthenticated =>
            User?.Identity?.IsAuthenticated ?? false;

        public bool IsAdmin =>
            User?.IsInRole("Admin") ?? false;
    }
}