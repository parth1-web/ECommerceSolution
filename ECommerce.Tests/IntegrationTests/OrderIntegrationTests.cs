using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Tests.IntegrationTests;

public class OrderIntegrationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ============================================================
    // TEST 1
    // Anonymous user cannot create an order
    // ============================================================

    [Fact]
    public async Task AnonymousUser_CannotCreateOrder()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response =
            await client.PostAsync(
                "/api/Orders",
                null);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // TEST 2
    // Authenticated customer can create an order from cart
    // ============================================================

    [Fact]
    public async Task AuthenticatedUser_CanCreateOrderFromCart()
    {
        // Arrange
        var client = _factory.CreateClient();

        var email =
            $"order_{Guid.NewGuid():N}@example.com";

        var password =
            "Test@12345";


        // --------------------------------------------------------
        // Register customer
        // --------------------------------------------------------

        var registerRequest = new
        {
            username =
                $"orderuser_{Guid.NewGuid():N}",

            email = email,

            password = password,

            firstName = "Order",

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
        // Read login response
        // --------------------------------------------------------

        var loginJson =
            await loginResponse.Content
                .ReadAsStringAsync();

        using var loginDocument =
            JsonDocument.Parse(loginJson);

        var loginRoot =
            loginDocument.RootElement;


        // --------------------------------------------------------
        // Verify Customer role
        // --------------------------------------------------------

        var role =
            loginRoot
                .GetProperty("role")
                .GetString();

        Assert.Equal(
            "Customer",
            role);


        // --------------------------------------------------------
        // Extract access token
        // --------------------------------------------------------

        var accessToken =
            loginRoot
                .GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));


        // --------------------------------------------------------
        // Add JWT
        // --------------------------------------------------------

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);


        // --------------------------------------------------------
        // Existing product
        // --------------------------------------------------------
        //
        // Your database already contains products.
        //
        // Change this ID if product 1 does not exist.
        // --------------------------------------------------------

        const int productId = 1;


        // --------------------------------------------------------
        // Add product to cart
        // --------------------------------------------------------

        var addCartItemRequest = new
        {
            productId = productId,
            quantity = 1
        };

        var cartResponse =
            await client.PostAsJsonAsync(
                "/api/Cart/items",
                addCartItemRequest);


        // --------------------------------------------------------
        // Read response for useful diagnostics
        // --------------------------------------------------------

        var cartResponseBody =
            await cartResponse.Content
                .ReadAsStringAsync();


        // --------------------------------------------------------
        // Verify cart request succeeded
        // --------------------------------------------------------

        cartResponse.Headers.TryGetValues("Allow", out var allowValues);

        Assert.True(
            cartResponse.StatusCode == HttpStatusCode.OK ||
            cartResponse.StatusCode == HttpStatusCode.Created,
            $"Cart request failed.\n" +
            $"Status: {(int)cartResponse.StatusCode} " +
            $"{cartResponse.StatusCode}\n" +
            $"Allow: {(allowValues != null ? string.Join(", ", allowValues) : "(none)")}\n" +
            $"Response: {cartResponseBody}");


        // --------------------------------------------------------
        // Create order
        // --------------------------------------------------------

        var orderResponse =
            await client.PostAsync(
                "/api/Orders",
                null);


        // --------------------------------------------------------
        // Read order response
        // --------------------------------------------------------

        var orderResponseBody =
            await orderResponse.Content
                .ReadAsStringAsync();


        // --------------------------------------------------------
        // Verify order creation
        // --------------------------------------------------------

        Assert.Equal(
            HttpStatusCode.Created,
            orderResponse.StatusCode);


        // --------------------------------------------------------
        // Parse order
        // --------------------------------------------------------

        using var orderDocument =
            JsonDocument.Parse(orderResponseBody);

        var orderRoot =
            orderDocument.RootElement;


        // --------------------------------------------------------
        // Verify order ID
        // --------------------------------------------------------

        Assert.True(
            orderRoot.TryGetProperty(
                "id",
                out var orderIdElement));

        var orderId =
            orderIdElement.GetInt32();

        Assert.True(
            orderId > 0);


        // --------------------------------------------------------
        // Verify response contains total amount
        // --------------------------------------------------------

        Assert.True(
            orderRoot.TryGetProperty(
                "totalAmount",
                out _));
    }


    // ============================================================
    // TEST 3
    // Authenticated customer can retrieve own orders
    // ============================================================

    [Fact]
    public async Task AuthenticatedUser_CanGetMyOrders()
    {
        // Arrange
        var client = _factory.CreateClient();

        var email =
            $"myorders_{Guid.NewGuid():N}@example.com";

        var password =
            "Test@12345";


        // --------------------------------------------------------
        // Register
        // --------------------------------------------------------

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/register",
                new
                {
                    username =
                        $"myorders_{Guid.NewGuid():N}",

                    email = email,

                    password = password,

                    firstName = "My",

                    lastName = "Orders"
                });

        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.OK ||
            registerResponse.StatusCode == HttpStatusCode.Created);


        // --------------------------------------------------------
        // Login
        // --------------------------------------------------------

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email = email,
                    password = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);


        // --------------------------------------------------------
        // Extract JWT
        // --------------------------------------------------------

        var loginJson =
            await loginResponse.Content
                .ReadAsStringAsync();

        using var loginDocument =
            JsonDocument.Parse(loginJson);

        var accessToken =
            loginDocument.RootElement
                .GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));


        // --------------------------------------------------------
        // Add JWT
        // --------------------------------------------------------

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);


        // --------------------------------------------------------
        // Get customer's orders
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


        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.False(
            string.IsNullOrWhiteSpace(responseBody));
    }


    // ============================================================
    // TEST 4
    // Authenticated customer gets 404 for nonexistent order
    // ============================================================

    [Fact]
    public async Task AuthenticatedUser_GetNonexistentOrder_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        var email =
            $"notfound_{Guid.NewGuid():N}@example.com";

        var password =
            "Test@12345";


        // --------------------------------------------------------
        // Register
        // --------------------------------------------------------

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/register",
                new
                {
                    username =
                        $"notfound_{Guid.NewGuid():N}",

                    email = email,

                    password = password,

                    firstName = "Not",

                    lastName = "Found"
                });

        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.OK ||
            registerResponse.StatusCode == HttpStatusCode.Created);


        // --------------------------------------------------------
        // Login
        // --------------------------------------------------------

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email = email,
                    password = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);


        // --------------------------------------------------------
        // Extract JWT
        // --------------------------------------------------------

        var loginJson =
            await loginResponse.Content
                .ReadAsStringAsync();

        using var loginDocument =
            JsonDocument.Parse(loginJson);

        var accessToken =
            loginDocument.RootElement
                .GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));


        // --------------------------------------------------------
        // Add JWT
        // --------------------------------------------------------

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);


        // --------------------------------------------------------
        // Request nonexistent order
        // --------------------------------------------------------

        var response =
            await client.GetAsync(
                "/api/Orders/999999999");


        // --------------------------------------------------------
        // Assert
        // --------------------------------------------------------

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }


    // ============================================================
    // TEST 5
    // User cannot access another user's order
    // ============================================================

    [Fact]
    public async Task User_CannotAccessAnotherUsersOrder()
    {
        // Arrange
        var client = _factory.CreateClient();

        var email =
            $"another_{Guid.NewGuid():N}@example.com";

        var password =
            "Test@12345";


        // --------------------------------------------------------
        // Register
        // --------------------------------------------------------

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/register",
                new
                {
                    username =
                        $"another_{Guid.NewGuid():N}",

                    email = email,

                    password = password,

                    firstName = "Another",

                    lastName = "User"
                });

        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.OK ||
            registerResponse.StatusCode == HttpStatusCode.Created);


        // --------------------------------------------------------
        // Login
        // --------------------------------------------------------

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email = email,
                    password = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);


        // --------------------------------------------------------
        // Extract JWT
        // --------------------------------------------------------

        var loginJson =
            await loginResponse.Content
                .ReadAsStringAsync();

        using var loginDocument =
            JsonDocument.Parse(loginJson);

        var accessToken =
            loginDocument.RootElement
                .GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));


        // --------------------------------------------------------
        // Add JWT
        // --------------------------------------------------------

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);


        // --------------------------------------------------------
        // Order 1 belongs to another user
        // --------------------------------------------------------

        var response =
            await client.GetAsync(
                "/api/Orders/1");


        // --------------------------------------------------------
        // Because GetById filters by UserId,
        // another user's order should not be returned.
        // --------------------------------------------------------

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}

