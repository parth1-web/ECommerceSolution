
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.Tests.IntegrationTests;

public class PaymentIntegrationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PaymentIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ============================================================
    // Helper: Register + Login
    // ============================================================

    private async Task<string> CreateAuthenticatedClientAsync(
        HttpClient client)
    {
        var email =
            $"payment_{Guid.NewGuid():N}@example.com";

        var password =
            "Test@12345";

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/Auth/register",
                new
                {
                    username =
                        $"paymentuser_{Guid.NewGuid():N}",

                    email = email,

                    password = password,

                    firstName = "Payment",

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

        var accessToken =
            document.RootElement
                .GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return accessToken!;
    }


    // ============================================================
    // Helper: Add product to cart and create order
    // ============================================================

    private async Task<int> CreateOrderForCurrentUserAsync(
        HttpClient client)
    {
        // --------------------------------------------------------
        // Product ID
        //
        // Your existing integration tests already use product 1.
        // Change this only if product 1 does not exist.
        // --------------------------------------------------------

        const int productId = 1;


        // --------------------------------------------------------
        // Add product to cart
        // --------------------------------------------------------

        var cartRequest = new
        {
            productId = productId,
            quantity = 1
        };

        var cartResponse =
            await client.PostAsJsonAsync(
                "/api/Cart/items",
                cartRequest);

        var cartBody =
            await cartResponse.Content
                .ReadAsStringAsync();

        Assert.True(
            cartResponse.StatusCode == HttpStatusCode.OK ||
            cartResponse.StatusCode == HttpStatusCode.Created,
            $"Adding product to cart failed.\n" +
            $"Status: {cartResponse.StatusCode}\n" +
            $"Response: {cartBody}");


        // --------------------------------------------------------
        // Create order
        // --------------------------------------------------------

        var orderResponse =
            await client.PostAsync(
                "/api/Orders",
                null);

        var orderBody =
            await orderResponse.Content
                .ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.Created,
            orderResponse.StatusCode);

        using var orderDocument =
            JsonDocument.Parse(orderBody);

        var orderRoot =
            orderDocument.RootElement;

        Assert.True(
            orderRoot.TryGetProperty(
                "id",
                out var orderIdElement),
            $"Order response did not contain an id.\n" +
            $"Response: {orderBody}");

        var orderId =
            orderIdElement.GetInt32();

        Assert.True(
            orderId > 0);

        return orderId;
    }


    // ============================================================
    // TEST 1
    // Anonymous user cannot create a payment
    // ============================================================

    [Fact]
    public async Task AnonymousUser_CannotCreatePayment()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        var request = new
        {
            orderId = 1,
            method = PaymentMethod.CashOnDelivery
        };


        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/Payments",
                request);


        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // TEST 2
    // Anonymous user cannot get payment
    // ============================================================

    [Fact]
    public async Task AnonymousUser_CannotGetPaymentByOrderId()
    {
        // Arrange
        var client =
            _factory.CreateClient();


        // Act
        var response =
            await client.GetAsync(
                "/api/Payments/order/1");


        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    // ============================================================
    // TEST 3
    // Authenticated customer can create COD payment
    // ============================================================

    [Fact]
    public async Task AuthenticatedUser_CanCreateCashOnDeliveryPayment()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await CreateAuthenticatedClientAsync(
            client);


        // --------------------------------------------------------
        // Create an order belonging to this user
        // --------------------------------------------------------

        var orderId =
            await CreateOrderForCurrentUserAsync(
                client);


        // --------------------------------------------------------
        // Create Cash On Delivery payment
        // --------------------------------------------------------

        var paymentRequest = new
        {
            orderId = orderId,

            method =
                PaymentMethod.CashOnDelivery
        };

        var paymentResponse =
            await client.PostAsJsonAsync(
                "/api/Payments",
                paymentRequest);


        // --------------------------------------------------------
        // Read response
        // --------------------------------------------------------

        var responseBody =
            await paymentResponse.Content
                .ReadAsStringAsync();


        // --------------------------------------------------------
        // Assert
        // --------------------------------------------------------

        Assert.True(
            paymentResponse.StatusCode == HttpStatusCode.OK ||
            paymentResponse.StatusCode == HttpStatusCode.Created,
            $"Payment creation failed.\n" +
            $"Status: {(int)paymentResponse.StatusCode} " +
            $"{paymentResponse.StatusCode}\n" +
            $"Response: {responseBody}");


        // --------------------------------------------------------
        // Verify payment response
        // --------------------------------------------------------

        using var document =
            JsonDocument.Parse(responseBody);

        var root =
            document.RootElement;


        Assert.True(
            root.TryGetProperty(
                "orderId",
                out var responseOrderId));

        Assert.Equal(
            orderId,
            responseOrderId.GetInt32());
    }


    // ============================================================
    // TEST 4
    // Customer can retrieve their payment
    // ============================================================

    [Fact]
    public async Task AuthenticatedUser_CanGetOwnPayment()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await CreateAuthenticatedClientAsync(
            client);


        // --------------------------------------------------------
        // Create order for this user
        // --------------------------------------------------------

        var orderId =
            await CreateOrderForCurrentUserAsync(
                client);


        // --------------------------------------------------------
        // Create payment
        // --------------------------------------------------------

        var paymentRequest = new
        {
            orderId = orderId,

            method =
                PaymentMethod.CashOnDelivery
        };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/Payments",
                paymentRequest);

        var createBody =
            await createResponse.Content
                .ReadAsStringAsync();

        Assert.True(
            createResponse.StatusCode == HttpStatusCode.OK ||
            createResponse.StatusCode == HttpStatusCode.Created,
            $"Payment creation failed.\n" +
            $"Status: {createResponse.StatusCode}\n" +
            $"Response: {createBody}");


        // --------------------------------------------------------
        // Get payment
        // --------------------------------------------------------

        var response =
            await client.GetAsync(
                $"/api/Payments/order/{orderId}");


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


        using var document =
            JsonDocument.Parse(responseBody);

        var root =
            document.RootElement;


        Assert.True(
            root.TryGetProperty(
                "orderId",
                out var responseOrderId));

        Assert.Equal(
            orderId,
            responseOrderId.GetInt32());
    }


    // ============================================================
    // TEST 5
    // User cannot access another user's payment
    // ============================================================

    [Fact]
    public async Task User_CannotGetAnotherUsersPayment()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await CreateAuthenticatedClientAsync(
            client);


        // --------------------------------------------------------
        // Order 1 belongs to another user in the existing
        // database, so it should not be exposed.
        // --------------------------------------------------------

        var response =
            await client.GetAsync(
                "/api/Payments/order/1");


        // --------------------------------------------------------
        // Assert
        // --------------------------------------------------------

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }


    // ============================================================
    // TEST 6
    // Wrong payment method rejected by Khalti endpoint
    // ============================================================

    [Fact]
    public async Task InitiateKhaltiPayment_WithWrongMethod_ReturnsBadRequest()
    {
        // Arrange
        var client =
            _factory.CreateClient();

        await CreateAuthenticatedClientAsync(
            client);


        // --------------------------------------------------------
        // Send COD instead of Khalti
        // --------------------------------------------------------

        var request = new
        {
            orderId = 1,

            method =
                PaymentMethod.CashOnDelivery
        };


        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/Payments/khalti/initiate",
                request);


        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }


    // ============================================================
    // TEST 7
    // eSewa failure callback without data
    // ============================================================

    [Fact]
    public async Task EsewaFailure_WithoutData_RedirectsToMvcFailure()
    {
        // Arrange
        //
        // Disable automatic redirects so we can verify the
        // response returned directly by the API.
        //
        var client =
            _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });


        // Act
        var response =
            await client.GetAsync(
                "/api/Payments/esewa/failure");


        // Assert
        //
        // Missing eSewa callback data should redirect the
        // customer to the MVC payment failure page.
        //
        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);


        // --------------------------------------------------------
        // Verify redirect location
        // --------------------------------------------------------

        Assert.NotNull(
            response.Headers.Location);


        var location =
            response.Headers.Location!.ToString();


        Assert.Contains(
            "/Payment/Failure",
            location,
            StringComparison.OrdinalIgnoreCase);


        // --------------------------------------------------------
        // Verify failure message
        // --------------------------------------------------------

        Assert.Contains(
            "cancelled",
            location,
            StringComparison.OrdinalIgnoreCase);
    }


    // ============================================================
    // TEST 8
    // Khalti callback without pidx
    // ============================================================

    [Fact]
    public async Task KhaltiCallback_WithoutPidx_ReturnsBadRequest()
    {
        // Arrange
        var client =
            _factory.CreateClient();


        // Act
        var response =
            await client.GetAsync(
                "/api/Payments/khalti/callback");


        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);


        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "missing",
            responseBody,
            StringComparison.OrdinalIgnoreCase);
    }
}

