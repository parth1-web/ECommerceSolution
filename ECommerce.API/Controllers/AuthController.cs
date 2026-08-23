using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        private readonly ICurrentUserService
            _currentUserService;

        public AuthController(
            IAuthService authService,
            ICurrentUserService currentUserService)
        {
            _authService = authService;

            _currentUserService =
                currentUserService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            RegisterDto dto)
        {
            try
            {
                var result =
                    await _authService
                        .RegisterAsync(dto);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            LoginDto dto)
        {
            try
            {
                var result =
                    await _authService
                        .LoginAsync(dto);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
            RefreshTokenDto dto)
        {
            try
            {
                var result =
                    await _authService
                        .RefreshTokenAsync(dto);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            RefreshTokenDto dto)
        {
            await _authService
                .LogoutAsync(dto.RefreshToken);

            return Ok(new
            {
                message = "Logged out successfully."
            });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                userId =
                    _currentUserService.UserId,

                email =
                    _currentUserService.Email,

                role =
                    _currentUserService.Role,

                isAdmin =
                    _currentUserService.IsAdmin
            });
        }
    }
}