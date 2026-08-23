using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Tests.IntegrationTests;

public class AuthIntegrationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ============================================================
    // TEST 1: Registration
    // ============================================================

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        var request = new
        {
            username = $"testuser_{Guid.NewGuid():N}",
            email = $"test_{Guid.NewGuid():N}@example.com",
            password = "Test@12345",
            firstName = "Integration",
            lastName = "Test"
        };

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/Auth/register",
                request);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Created);
    }


    // ============================================================
    // TEST 2: Login
    // ============================================================

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        var email =
            $"login_{Guid.NewGuid():N}@example.com";

        var password = "Test@12345";

        // Register user first
        var registerRequest = new
        {
            username = $"loginuser_{Guid.NewGuid():N}",
            email = email,
            password = password,
            firstName = "Login",
            lastName = "Test"
        };

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/register",
                registerRequest);

        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.OK ||
            registerResponse.StatusCode == HttpStatusCode.Created);

        // Login
        var loginRequest = new
        {
            email = email,
            password = password
        };

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                loginRequest);

        // Assert login succeeded
        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        // Read response
        var loginJson =
            await loginResponse.Content
                .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(loginJson);

        var root =
            document.RootElement;

        // Your API returns the JWT in "accessToken"
        var accessToken =
            root.GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));
    }


    // ============================================================
    // TEST 3: Authenticated User Can Access Protected Endpoint
    // ============================================================

    [Fact]
    public async Task AuthenticatedUser_CanAccessProtectedEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient();

        var email =
            $"protected_{Guid.NewGuid():N}@example.com";

        var password = "Test@12345";

        // --------------------------------------------------------
        // Register user
        // --------------------------------------------------------

        var registerRequest = new
        {
            username =
                $"protecteduser_{Guid.NewGuid():N}",

            email = email,

            password = password,

            firstName = "Protected",

            lastName = "Test"
        };

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/register",
                registerRequest);

        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.OK ||
            registerResponse.StatusCode == HttpStatusCode.Created);


        // --------------------------------------------------------
        // Login
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
        // Read JWT
        // --------------------------------------------------------

        var loginJson =
            await loginResponse.Content
                .ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(loginJson);

        var root =
            document.RootElement;

        // IMPORTANT:
        // Your API returns the actual JWT in "accessToken"
        var accessToken =
            root.GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));


        // --------------------------------------------------------
        // Add JWT to Authorization header
        // --------------------------------------------------------

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);


        // --------------------------------------------------------
        // Access protected endpoint
        // --------------------------------------------------------

        var response =
            await client.GetAsync(
                "/api/Orders");


        // --------------------------------------------------------
        // Assert
        // --------------------------------------------------------

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }


    // ============================================================
    // TEST 4: Anonymous User Cannot Access Protected Endpoint
    // ============================================================

    [Fact]
    public async Task AnonymousUser_CannotAccessProtectedEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                "/api/Orders");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


}