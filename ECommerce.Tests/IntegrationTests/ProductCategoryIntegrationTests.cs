
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Tests.IntegrationTests;

public class ProductCategoryIntegrationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductCategoryIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ============================================================
    // Helper: Register + Login
    // ============================================================

    private async Task AuthenticateAsUserAsync(
        HttpClient client)
    {
        var email =
            $"catalog_{Guid.NewGuid():N}@example.com";

        var password =
            "Test@12345";

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/register",
                new
                {
                    username =
                        $"cataloguser_{Guid.NewGuid():N}",

                    email = email,

                    password = password,

                    firstName = "Catalog",

                    lastName = "Test"
                });

        var registerBody =
            await registerResponse.Content
                .ReadAsStringAsync();

        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.OK ||
            registerResponse.StatusCode == HttpStatusCode.Created,
            $"Registration failed.\n" +
            $"Status: {registerResponse.StatusCode}\n" +
            $"Response: {registerBody}");

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email = email,
                    password = password
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
            string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }


    // ============================================================
    // PRODUCT TESTS
    // ============================================================

    [Fact]
    public async Task GetProductById_WithExistingProduct_ReturnsOk()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        const int productId = 1;

        // Act
        var response =
            await client.GetAsync(
                $"/api/Products/{productId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.False(
            string.IsNullOrWhiteSpace(body));
    }


    [Fact]
    public async Task GetProductById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        const int invalidProductId =
            999999;

        // Act
        var response =
            await client.GetAsync(
                $"/api/Products/{invalidProductId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }


    [Fact]
    public async Task SearchProducts_ReturnsOk()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                "/api/Products/search");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.False(
            string.IsNullOrWhiteSpace(body));
    }


    [Fact]
    public async Task SearchProducts_WithPagination_ReturnsOk()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                "/api/Products/search?page=1&pageSize=10");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.False(
            string.IsNullOrWhiteSpace(body));
    }


    // ============================================================
    // CATEGORY TESTS
    // ============================================================

    [Fact]
    public async Task GetAllCategories_ReturnsOk()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                "/api/Categories");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.False(
            string.IsNullOrWhiteSpace(body));
    }


    [Fact]
    public async Task GetCategoryById_WithExistingCategory_ReturnsOk()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        const int categoryId = 1;

        // Act
        var response =
            await client.GetAsync(
                $"/api/Categories/{categoryId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadAsStringAsync();

        Assert.False(
            string.IsNullOrWhiteSpace(body));
    }


    [Fact]
    public async Task GetCategoryById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        const int invalidCategoryId =
            999999;

        // Act
        var response =
            await client.GetAsync(
                $"/api/Categories/{invalidCategoryId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }


    // ============================================================
    // AUTHORIZATION TESTS
    // ============================================================

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
                "Test product",

            price = 100,

            stockQuantity = 10,

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
            name =
                "Unauthorized Update",

            description =
                "Test",

            price = 100,

            stockQuantity = 10,

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
                "Test category"
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


    [Fact]
    public async Task AnonymousUser_CannotUpdateCategory()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        var request = new
        {
            name =
                "Unauthorized Category Update",

            description =
                "Test"
        };

        // Act
        var response =
            await client.PutAsJsonAsync(
                "/api/Categories/1",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task AnonymousUser_CannotDeleteCategory()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        // Act
        var response =
            await client.DeleteAsync(
                "/api/Categories/1");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // CUSTOMER AUTHORIZATION
    // ============================================================

    [Fact]
    public async Task AuthenticatedCustomer_CannotCreateProduct()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsUserAsync(
            client);

        var request = new
        {
            name =
                $"Customer Product {Guid.NewGuid():N}",

            description =
                "Customer should not create this",

            price = 100,

            stockQuantity = 10,

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
    public async Task AuthenticatedCustomer_CannotCreateCategory()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await AuthenticateAsUserAsync(
            client);

        var request = new
        {
            name =
                $"Customer Category {Guid.NewGuid():N}",

            description =
                "Customer should not create this"
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
}

