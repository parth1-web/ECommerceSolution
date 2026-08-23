using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Configuration;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        private readonly IRefreshTokenRepository
            _refreshTokenRepository;

        private readonly JwtSettings _jwtSettings;

        private readonly PasswordHasher<User>
            _passwordHasher;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IOptions<JwtSettings> jwtSettings)
        {
            _userRepository = userRepository;

            _refreshTokenRepository =
                refreshTokenRepository;

            _jwtSettings = jwtSettings.Value;

            _passwordHasher =
                new PasswordHasher<User>();
        }

        public async Task<AuthResponseDto> RegisterAsync(
            RegisterDto dto)
        {
            var normalizedEmail =
                dto.Email.Trim().ToLowerInvariant();

            var existingUser =
                await _userRepository
                    .GetByEmailAsync(normalizedEmail);

            if (existingUser != null)
            {
                throw new InvalidOperationException(
                    "A user with this email already exists.");
            }

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),

                LastName = dto.LastName.Trim(),

                Email = normalizedEmail,

                Role = "Customer",

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    dto.Password);

            await _userRepository
                .CreateAsync(user);

            return await CreateAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> LoginAsync(
            LoginDto dto)
        {
            var normalizedEmail =
                dto.Email.Trim().ToLowerInvariant();

            var user =
                await _userRepository
                    .GetByEmailAsync(normalizedEmail);

            if (user == null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            var passwordResult =
                _passwordHasher
                    .VerifyHashedPassword(
                        user,
                        user.PasswordHash,
                        dto.Password);

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedAccessException(
                    "Invalid email or password.");
            }

            return await CreateAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto>
            RefreshTokenAsync(
                RefreshTokenDto dto)
        {
            var refreshToken =
                await _refreshTokenRepository
                    .GetByTokenAsync(
                        dto.RefreshToken);

            if (refreshToken == null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid refresh token.");
            }

            if (!refreshToken.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "Refresh token has expired or been revoked.");
            }

            if (refreshToken.User == null)
            {
                throw new UnauthorizedAccessException(
                    "User associated with token was not found.");
            }

            var user = refreshToken.User;

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            // Rotate old refresh token
            await _refreshTokenRepository
                .RevokeAsync(refreshToken);

            await _refreshTokenRepository
                .SaveChangesAsync();

            return await CreateAuthResponseAsync(user);
        }

        public async Task LogoutAsync(
            string refreshToken)
        {
            var token =
                await _refreshTokenRepository
                    .GetByTokenAsync(refreshToken);

            if (token == null)
                return;

            if (!token.IsRevoked)
            {
                await _refreshTokenRepository
                    .RevokeAsync(token);

                await _refreshTokenRepository
                    .SaveChangesAsync();
            }
        }

        private async Task<AuthResponseDto>
            CreateAuthResponseAsync(User user)
        {
            var accessToken =
                GenerateAccessToken(
                    user,
                    out var accessTokenExpiresAt);

            var refreshToken =
                GenerateRefreshToken();

            var refreshTokenEntity =
                new RefreshToken
                {
                    Token = refreshToken,

                    UserId = user.Id,

                    ExpiresAt =
                        DateTime.UtcNow.AddDays(
                            _jwtSettings
                                .RefreshTokenExpirationDays),

                    CreatedAt =
                        DateTime.UtcNow
                };

            await _refreshTokenRepository
                .CreateAsync(
                    refreshTokenEntity);

            await _refreshTokenRepository
                .SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,

                FirstName = user.FirstName,

                LastName = user.LastName,

                Email = user.Email,

                Role = user.Role,

                AccessToken = accessToken,

                AccessTokenExpiresAt =
                    accessTokenExpiresAt,

                RefreshToken = refreshToken,

                RefreshTokenExpiresAt =
                    refreshTokenEntity.ExpiresAt
            };
        }

        private string GenerateAccessToken(
            User user,
            out DateTime expiresAt)
        {
            expiresAt =
                DateTime.UtcNow.AddMinutes(
                    _jwtSettings
                        .AccessTokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.Email),

                new Claim(
                    ClaimTypes.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.Role)
            };

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _jwtSettings.Key));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token =
                new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,

                    audience: _jwtSettings.Audience,

                    claims: claims,

                    expires: expiresAt,

                    signingCredentials:
                        credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randomBytes =
                RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(
                randomBytes);
        }
    }
}