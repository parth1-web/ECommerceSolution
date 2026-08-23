
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Tests.IntegrationTests;

public class AdminProductCategoryIntegrationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminProductCategoryIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ============================================================
    // ADMIN AUTHENTICATION
    // ============================================================

    private async Task AuthenticateAsAdminAsync(
        HttpClient client)
    {
        const string email =
            "admin@ecommerce.local";

        const string password =
            "Admin123!ChangeMe";

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email,
                    password
                });

        var responseBody =
            await loginResponse.Content
                .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using var document =
            JsonDocument.Parse(responseBody);

        var root =
            document.RootElement;

        var token =
            root.GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(token),
            $"Admin access token was empty.\nResponse: {responseBody}");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }


    // ============================================================
    // CATEGORY CRUD
    // ============================================================

    [Fact]
    public async Task Admin_CanCreateCategory()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsAdminAsync(
            client);

        var categoryName =
            $"Admin Test Category {Guid.NewGuid():N}";

        var request = new
        {
            name = categoryName,

            description =
                "Category created by integration test."
        };

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/Categories",
                request);

        // Assert
        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.False(
            string.IsNullOrWhiteSpace(responseBody));

        using var document =
            JsonDocument.Parse(responseBody);

        var root =
            document.RootElement;

        Assert.True(
            root.GetProperty("id").GetInt32() > 0);

        Assert.Equal(
            categoryName,
            root.GetProperty("name").GetString());
    }


    [Fact]
    public async Task Admin_CanUpdateCategory()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsAdminAsync(
            client);

        var categoryName =
            $"Update Category {Guid.NewGuid():N}";

        var createRequest = new
        {
            name = categoryName,

            description =
                "Original category."
        };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/Categories",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        using var createDocument =
            JsonDocument.Parse(
                await createResponse.Content
                    .ReadAsStringAsync());

        var categoryId =
            createDocument.RootElement
                .GetProperty("id")
                .GetInt32();

        var updateRequest = new
        {
            name =
                $"Updated Category {Guid.NewGuid():N}",

            description =
                "Updated category.",

            isActive = true
        };

        // Act
        var response =
            await client.PutAsJsonAsync(
                $"/api/Categories/{categoryId}",
                updateRequest);

        // Assert
        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Contains(
            "updated successfully",
            responseBody,
            StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task Admin_CanDeleteCategory()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsAdminAsync(
            client);

        var createRequest = new
        {
            name =
                $"Delete Category {Guid.NewGuid():N}",

            description =
                "Category to delete."
        };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/Categories",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        using var document =
            JsonDocument.Parse(
                await createResponse.Content
                    .ReadAsStringAsync());

        var categoryId =
            document.RootElement
                .GetProperty("id")
                .GetInt32();

        // Act
        var response =
            await client.DeleteAsync(
                $"/api/Categories/{categoryId}");

        // Assert
        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Contains(
            "deleted successfully",
            responseBody,
            StringComparison.OrdinalIgnoreCase);
    }


    // ============================================================
    // PRODUCT CRUD
    // ============================================================

    [Fact]
    public async Task Admin_CanCreateProduct()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsAdminAsync(
            client);

        // Use an existing category.
        const int categoryId = 1;

        var productName =
            $"Admin Test Product {Guid.NewGuid():N}";

        var request = new
        {
            name = productName,

            description =
                "Product created by integration test.",

            price = 999.99m,

            stock = 25,

            imageUrl =
                "https://example.com/test-product.jpg",

            categoryId
        };

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/Products",
                request);

        // Assert
        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.False(
            string.IsNullOrWhiteSpace(responseBody));

        using var document =
            JsonDocument.Parse(responseBody);

        var root =
            document.RootElement;

        Assert.True(
            root.GetProperty("id").GetInt32() > 0);

        Assert.Equal(
            productName,
            root.GetProperty("name").GetString());
    }


    [Fact]
    public async Task Admin_CanUpdateProduct()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsAdminAsync(
            client);

        const int categoryId = 1;

        var createRequest = new
        {
            name =
                $"Product Before Update {Guid.NewGuid():N}",

            description =
                "Original product.",

            price = 500m,

            stock = 10,

            imageUrl =
                "https://example.com/original.jpg",

            categoryId
        };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/Products",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        using var createDocument =
            JsonDocument.Parse(
                await createResponse.Content
                    .ReadAsStringAsync());

        var productId =
            createDocument.RootElement
                .GetProperty("id")
                .GetInt32();

        var updateRequest = new
        {
            name =
                $"Updated Product {Guid.NewGuid():N}",

            description =
                "Updated product.",

            price = 750m,

            stock = 50,

            imageUrl =
                "https://example.com/updated.jpg",

            isActive = true,

            categoryId
        };

        // Act
        var response =
            await client.PutAsJsonAsync(
                $"/api/Products/{productId}",
                updateRequest);

        // Assert
        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Contains(
            "updated successfully",
            responseBody,
            StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task Admin_CanDeleteProduct()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsAdminAsync(
            client);

        const int categoryId = 1;

        var createRequest = new
        {
            name =
                $"Product To Delete {Guid.NewGuid():N}",

            description =
                "Product to delete.",

            price = 250m,

            stock = 5,

            imageUrl =
                "https://example.com/delete.jpg",

            categoryId
        };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/Products",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        using var document =
            JsonDocument.Parse(
                await createResponse.Content
                    .ReadAsStringAsync());

        var productId =
            document.RootElement
                .GetProperty("id")
                .GetInt32();

        // Act
        var response =
            await client.DeleteAsync(
                $"/api/Products/{productId}");

        // Assert
        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Contains(
            "deleted successfully",
            responseBody,
            StringComparison.OrdinalIgnoreCase);
    }
}

