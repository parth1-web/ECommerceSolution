
using System.Text;

using ECommerce.API.Middleware;

using ECommerce.Application.Configuration;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Application.Validators;

using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;

using FluentValidation;
using FluentValidation.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


// ==========================================================
// Controllers
// ==========================================================

builder.Services.AddControllers();


// ==========================================================
// Swagger + OpenAPI
// ==========================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "ECommerce API",
            Version = "v1",
            Description =
                "E-Commerce Backend API built with ASP.NET Core 8, " +
                "Clean Architecture, Entity Framework Core and MySQL."
        });

    // ------------------------------------------------------
    // JWT Bearer Authentication
    // ------------------------------------------------------

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Enter your JWT access token.\n\n" +
                "Example:\n" +
                "Bearer eyJhbGciOiJIUzI1NiIs..."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,

                            Id = "Bearer"
                        }
                },

                Array.Empty<string>()
            }
        });
});


// ==========================================================
// Database
// ==========================================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection is missing from configuration.");
}

builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(
                connectionString));
    });


// ==========================================================
// JWT Configuration
// ==========================================================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

var jwtSettings =
    builder.Configuration
        .GetSection("Jwt")
        .Get<JwtSettings>();

if (jwtSettings == null)
{
    throw new InvalidOperationException(
        "JWT configuration is missing.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "JWT Key is missing.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
{
    throw new InvalidOperationException(
        "JWT Issuer is missing.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException(
        "JWT Audience is missing.");
}


// ==========================================================
// JWT Authentication
// ==========================================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    jwtSettings.Issuer,

                ValidAudience =
                    jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.Key))
            };
    });


// ==========================================================
// Authorization
// ==========================================================

builder.Services.AddAuthorization();


// ==========================================================
// HttpContext Accessor
// ==========================================================

builder.Services.AddHttpContextAccessor();


// ==========================================================
// Application Services
// ==========================================================

builder.Services.AddScoped<
    IProductService,
    ProductService>();

builder.Services.AddScoped<
    ICategoryService,
    CategoryService>();

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

builder.Services.AddScoped<
    ICurrentUserService,
    CurrentUserService>();

builder.Services.AddScoped<
    ICartService,
    CartService>();

builder.Services.AddScoped<
    IOrderService,
    OrderService>();

builder.Services.AddScoped<
    IPaymentService,
    PaymentService>();

builder.Services.AddScoped<
    IWishlistService,
    WishlistService>();


// ==========================================================
// Repositories
// ==========================================================

builder.Services.AddScoped<
    IProductRepository,
    ProductRepository>();

builder.Services.AddScoped<
    ICategoryRepository,
    CategoryRepository>();

builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

builder.Services.AddScoped<
    ICartRepository,
    CartRepository>();

builder.Services.AddScoped<
    IOrderRepository,
    OrderRepository>();

builder.Services.AddScoped<
    IPaymentRepository,
    PaymentRepository>();

builder.Services.AddScoped<
    IRefreshTokenRepository,
    RefreshTokenRepository>();

builder.Services.AddScoped<
    IWishlistRepository,
    WishlistRepository>();


// ==========================================================
// Khalti Payment
// ==========================================================

builder.Services.Configure<KhaltiSettings>(
    builder.Configuration.GetSection("Khalti"));

builder.Services.AddHttpClient<
    IKhaltiPaymentService,
    KhaltiPaymentService>();


// ==========================================================
// eSewa Payment
// ==========================================================

builder.Services.Configure<ESewaSettings>(
    builder.Configuration.GetSection("ESewa"));

builder.Services.AddHttpClient<
    IEsewaPaymentService,
    EsewaPaymentService>();


// ==========================================================
// FluentValidation
// ==========================================================

builder.Services
    .AddFluentValidationAutoValidation();

builder.Services.AddValidatorsFromAssemblyContaining<
    CreatePaymentDtoValidator>();


// ==========================================================
// Build Application
// ==========================================================

var app = builder.Build();


// ==========================================================
// Database Seeding
// ==========================================================

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    await context.Database.MigrateAsync();

    await AdminSeeder.SeedAsync(context);
}

// --------------------------------------------------
// Swagger
// --------------------------------------------------

app.UseSwagger();

app.UseSwaggerUI();



// ==========================================================
// HTTPS
// ==========================================================

app.UseHttpsRedirection();


// ==========================================================
// Global Exception Handling
// ==========================================================

app.UseMiddleware<GlobalExceptionMiddleware>();


// ==========================================================
// Authentication & Authorization
// ==========================================================

app.UseAuthentication();

app.UseAuthorization();


// ==========================================================
// Controllers
// ==========================================================

app.MapControllers();


// ==========================================================
// Run
// ==========================================================

app.Run();


// ==========================================================
// Required for Integration Tests
// ==========================================================

public partial class Program
{
}

