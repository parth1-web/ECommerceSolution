
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Tests.IntegrationTests;

public class ValidationIntegrationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ValidationIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ============================================================
    // PRODUCT VALIDATION
    // ============================================================

    [Fact]
    public async Task CreateProduct_WithEmptyName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var request = new
        {
            name = "",
            description = "Invalid product",
            price = 100m,
            stock = 10,
            imageUrl = "",
            categoryId = 1
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Products",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task CreateProduct_WithNegativePrice_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var request = new
        {
            name = "Invalid Product",
            description = "Invalid price",
            price = -100m,
            stock = 10,
            imageUrl = "",
            categoryId = 1
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Products",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task CreateProduct_WithNegativeStock_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var request = new
        {
            name = "Invalid Product",
            description = "Invalid stock",
            price = 100m,
            stock = -10,
            imageUrl = "",
            categoryId = 1
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Products",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task CreateProduct_WithInvalidCategoryId_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var request = new
        {
            name = "Invalid Product",
            description = "Invalid category",
            price = 100m,
            stock = 10,
            imageUrl = "",
            categoryId = 0
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Products",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // CATEGORY VALIDATION
    // ============================================================

    [Fact]
    public async Task CreateCategory_WithEmptyName_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var request = new
        {
            name = "",
            description = "Invalid category"
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Categories",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // CART VALIDATION
    // ============================================================

    [Fact]
    public async Task AddCartItem_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var request = new
        {
            productId = 1,
            quantity = 1
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Cart/items",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task UpdateCartItem_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var request = new
        {
            quantity = 2
        };

        var response =
            await client.PutAsJsonAsync(
                "/api/Cart/items/1",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task RemoveCartItem_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response =
            await client.DeleteAsync(
                "/api/Cart/items/1");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // ORDER VALIDATION
    // ============================================================

    [Fact]
    public async Task CreateOrder_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response =
            await client.PostAsync(
                "/api/Orders",
                null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task GetOrderById_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/Orders/1");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task CancelOrder_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response =
            await client.PostAsync(
                "/api/Orders/1/cancel",
                null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // PAYMENT VALIDATION
    // ============================================================

    [Fact]
    public async Task CreatePayment_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var request = new
        {
            orderId = 1,
            method = 0
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Payments",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task GetPaymentByOrderId_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/Payments/order/1");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // INVALID ROUTES / RESOURCE IDs
    // ============================================================

    [Fact]
    public async Task GetProduct_WithNonExistingId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/Products/999999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }


    [Fact]
    public async Task GetCategory_WithNonExistingId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/Categories/999999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}

