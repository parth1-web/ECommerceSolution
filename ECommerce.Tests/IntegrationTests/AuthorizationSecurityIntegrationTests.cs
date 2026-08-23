
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Tests.IntegrationTests;

public class AuthorizationSecurityIntegrationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationSecurityIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ============================================================
    // CUSTOMER AUTHENTICATION
    // ============================================================

    private async Task AuthenticateAsCustomerAsync(
        HttpClient client)
    {
        var email =
            $"security_{Guid.NewGuid():N}@example.com";

        const string password =
            "Test@12345";

        var registerRequest = new
        {
            username =
                $"securityuser_{Guid.NewGuid():N}",

            email,

            password,

            firstName = "Security",

            lastName = "Test"
        };

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/register",
                registerRequest);

        var registerBody =
            await registerResponse.Content
                .ReadAsStringAsync();

        Assert.True(
            registerResponse.StatusCode ==
                HttpStatusCode.OK ||
            registerResponse.StatusCode ==
                HttpStatusCode.Created,
            $"Registration failed.\n" +
            $"Status: {registerResponse.StatusCode}\n" +
            $"Response: {registerBody}");

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email,
                    password
                });

        var loginBody =
            await loginResponse.Content
                .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using var document =
            JsonDocument.Parse(loginBody);

        var token =
            document.RootElement
                .GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(token),
            $"Access token was empty.\n{loginBody}");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }


    // ============================================================
    // ANONYMOUS ACCESS
    // ============================================================

    [Fact]
    public async Task AnonymousUser_CannotAccessAdminOrders()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                "/api/admin/orders");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task AnonymousUser_CannotCreateProduct()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        var request = new
        {
            name =
                $"Unauthorized Product {Guid.NewGuid():N}",

            description =
                "Should not be created.",

            price = 100m,

            stock = 10,

            imageUrl =
                "https://example.com/test.jpg",

            categoryId = 1
        };

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/Products",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task AnonymousUser_CannotUpdateProduct()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        var request = new
        {
            name = "Unauthorized Update",

            description =
                "Should not be allowed.",

            price = 200m,

            stock = 10,

            imageUrl = "",

            isActive = true,

            categoryId = 1
        };

        // Act
        var response =
            await client.PutAsJsonAsync(
                "/api/Products/1",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task AnonymousUser_CannotDeleteProduct()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        // Act
        var response =
            await client.DeleteAsync(
                "/api/Products/1");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task AnonymousUser_CannotCreateCategory()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        var request = new
        {
            name =
                $"Unauthorized Category {Guid.NewGuid():N}",

            description =
                "Should not be created."
        };

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/Categories",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // CUSTOMER ACCESS
    // ============================================================

    [Fact]
    public async Task Customer_CannotAccessAdminOrders()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsCustomerAsync(
            client);

        // Act
        var response =
            await client.GetAsync(
                "/api/admin/orders");

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }


    [Fact]
    public async Task Customer_CannotCreateProduct()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsCustomerAsync(
            client);

        var request = new
        {
            name =
                $"Customer Product {Guid.NewGuid():N}",

            description =
                "Customer must not create products.",

            price = 100m,

            stock = 10,

            imageUrl =
                "https://example.com/customer.jpg",

            categoryId = 1
        };

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/Products",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }


    [Fact]
    public async Task Customer_CannotUpdateProduct()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsCustomerAsync(
            client);

        var request = new
        {
            name = "Customer Update",

            description =
                "Customer must not update products.",

            price = 100m,

            stock = 10,

            imageUrl = "",

            isActive = true,

            categoryId = 1
        };

        // Act
        var response =
            await client.PutAsJsonAsync(
                "/api/Products/1",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }


    [Fact]
    public async Task Customer_CannotDeleteProduct()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsCustomerAsync(
            client);

        // Act
        var response =
            await client.DeleteAsync(
                "/api/Products/1");

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }


    [Fact]
    public async Task Customer_CannotCreateCategory()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsCustomerAsync(
            client);

        var request = new
        {
            name =
                $"Customer Category {Guid.NewGuid():N}",

            description =
                "Customer must not create categories."
        };

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/Categories",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }


    [Fact]
    public async Task Customer_CannotUpdateCategory()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsCustomerAsync(
            client);

        var request = new
        {
            name = "Customer Update Category",

            description =
                "Customer must not update categories.",

            isActive = true
        };

        // Act
        var response =
            await client.PutAsJsonAsync(
                "/api/Categories/1",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }


    [Fact]
    public async Task Customer_CannotDeleteCategory()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsCustomerAsync(
            client);

        // Act
        var response =
            await client.DeleteAsync(
                "/api/Categories/1");

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }


    // ============================================================
    // PUBLIC ENDPOINTS
    // ============================================================

    [Fact]
    public async Task AnonymousUser_CanAccessProducts()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                "/api/Products/1");

        // Assert
        Assert.NotEqual(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.NotEqual(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }


    [Fact]
    public async Task AnonymousUser_CanAccessCategories()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                "/api/Categories");

        // Assert
        Assert.NotEqual(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.NotEqual(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}

