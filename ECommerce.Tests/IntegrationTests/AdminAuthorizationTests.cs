using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Tests.IntegrationTests;

public class AdminAuthorizationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminAuthorizationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ============================================================
    // TEST 1: Anonymous user cannot access Admin Orders
    // ============================================================

    [Fact]
    public async Task AnonymousUser_CannotAccessAdminOrders()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                "/api/admin/orders");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // TEST 2: Normal Customer cannot access Admin Orders
    // ============================================================

    [Fact]
    public async Task NormalUser_CannotAccessAdminOrders()
    {
        // Arrange
        var client = _factory.CreateClient();

        var email =
            $"customer_{Guid.NewGuid():N}@example.com";

        var password =
            "Test@12345";


        // --------------------------------------------------------
        // Register Customer
        // --------------------------------------------------------

        var registerRequest = new
        {
            username =
                $"customer_{Guid.NewGuid():N}",

            email = email,

            password = password,

            firstName = "Normal",

            lastName = "Customer"
        };

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/register",
                registerRequest);

        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.OK ||
            registerResponse.StatusCode == HttpStatusCode.Created);


        // --------------------------------------------------------
        // Login Customer
        // --------------------------------------------------------

        var loginRequest = new
        {
            email = email,
            password = password
        };

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);


        // --------------------------------------------------------
        // Read Login Response
        // --------------------------------------------------------

        var loginJson =
            await loginResponse.Content
                .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(loginJson);

        var root =
            document.RootElement;


        // --------------------------------------------------------
        // Verify Customer Role
        // --------------------------------------------------------

        var role =
            root.GetProperty("role")
                .GetString();

        Assert.Equal(
            "Customer",
            role);


        // --------------------------------------------------------
        // Extract JWT
        // --------------------------------------------------------

        var accessToken =
            root.GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));


        // --------------------------------------------------------
        // Add JWT to Authorization Header
        // --------------------------------------------------------

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);


        // --------------------------------------------------------
        // Access Admin Endpoint
        // --------------------------------------------------------

        var response =
            await client.GetAsync(
                "/api/admin/orders");


        // --------------------------------------------------------
        // Customer should receive 403 Forbidden
        // --------------------------------------------------------

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }


    // ============================================================
    // TEST 3: Admin user can access Admin Orders
    // ============================================================

    [Fact]
    public async Task AdminUser_CanAccessAdminOrders()
    {
        // Arrange
        var client = _factory.CreateClient();

        const string adminEmail =
            "admin@ecommerce.local";

        const string adminPassword =
            "Admin123!ChangeMe";


        // --------------------------------------------------------
        // Login using seeded Admin account
        // --------------------------------------------------------

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email = adminEmail,
                    password = adminPassword
                });


        // --------------------------------------------------------
        // Login should succeed
        // --------------------------------------------------------

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);


        // --------------------------------------------------------
        // Read Login Response
        // --------------------------------------------------------

        var loginJson =
            await loginResponse.Content
                .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(loginJson);

        var root =
            document.RootElement;


        // --------------------------------------------------------
        // Verify Admin Role
        // --------------------------------------------------------

        var role =
            root.GetProperty("role")
                .GetString();

        Assert.Equal(
            "Admin",
            role);


        // --------------------------------------------------------
        // Extract JWT
        // --------------------------------------------------------

        var accessToken =
            root.GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));


        // --------------------------------------------------------
        // Add JWT to Authorization Header
        // --------------------------------------------------------

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);


        // --------------------------------------------------------
        // Access Admin Orders Endpoint
        // --------------------------------------------------------

        var response =
            await client.GetAsync(
                "/api/admin/orders");


        // --------------------------------------------------------
        // Admin should receive 200 OK
        // --------------------------------------------------------

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }
}