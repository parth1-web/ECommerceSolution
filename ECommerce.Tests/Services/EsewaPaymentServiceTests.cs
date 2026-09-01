using ECommerce.Application.Configuration;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace ECommerce.Tests.Services;

public class EsewaPaymentServiceTests
{
    [Fact]
    public async Task VerifyPaymentAsync_WhenPaymentDoesNotExist_ReturnsFalse()
    {
        // Arrange

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString())
            .Options;

        await using var context =
            new AppDbContext(options);

        var httpClient = new HttpClient();

        var settings = Options.Create(
            new ESewaSettings
            {
                ProductCode = "EPAYTEST",
                SecretKey = "test-secret",
                StatusUrl = "https://example.com/status"
            });

        var logger = NullLogger<EsewaPaymentService>.Instance;

        var service = new EsewaPaymentService(
            httpClient,
            settings,
            context,
            logger);

        // Act

        var result =
            await service.VerifyPaymentAsync(
                1,
                "transaction-123");

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenPaymentMethodIsNotEsewa_ReturnsFalse()
    {
        // Arrange

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString())
            .Options;

        await using var context =
            new AppDbContext(options);

        var payment = new Payment
        {
            OrderId = 1,
            Amount = 1000,
            Method = PaymentMethod.Khalti,
            Status = PaymentStatus.Pending,
            TransactionId = "transaction-123",
            PaymentDate = DateTime.UtcNow
        };

        context.Payments.Add(payment);

        await context.SaveChangesAsync();

        var settings = Options.Create(
            new ESewaSettings
            {
                ProductCode = "EPAYTEST",
                SecretKey = "test-secret",
                StatusUrl = "https://example.com/status"
            });

        var logger = NullLogger<EsewaPaymentService>.Instance;

        var service = new EsewaPaymentService(
            new HttpClient(),
            settings,
            context,
            logger);

        // Act

        var result =
            await service.VerifyPaymentAsync(
                1,
                "transaction-123");

        // Assert

        Assert.False(result);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenPaymentAlreadyPaid_ReturnsTrue()
    {
        // Arrange

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context =
            new AppDbContext(options);

        var transactionUuid = "transaction-123";

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            TotalAmount = 1000,
            Status = OrderStatus.Confirmed,
            OrderDate = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = 1,
            OrderId = 1,
            Amount = 1000,
            Method = PaymentMethod.ESewa,
            Status = PaymentStatus.Paid,

            // IMPORTANT:
            // Must match transactionUuid below
            TransactionId = transactionUuid,

            PaymentDate = DateTime.UtcNow,
            Order = order
        };

        context.Orders.Add(order);
        context.Payments.Add(payment);

        await context.SaveChangesAsync();

        var settings = Options.Create(
            new ESewaSettings
            {
                ProductCode = "EPAYTEST",
                SecretKey = "test-secret",
                StatusUrl = "https://example.com/status"
            });

        var service = new EsewaPaymentService(
            new HttpClient(),
            settings,
            context,
            NullLogger<EsewaPaymentService>.Instance);

        // Act

        var result =
            await service.VerifyPaymentAsync(
                1,
                transactionUuid);

        // Assert

        Assert.True(result);

        Assert.Equal(
            PaymentStatus.Paid,
            payment.Status);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenEsewaReturnsComplete_MarksPaymentPaid()
    {
        // Arrange

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context =
            new AppDbContext(options);

        const int orderId = 1;
        const string transactionUuid = "transaction-123";

        var order = new Order
        {
            Id = orderId,
            UserId = 1,
            TotalAmount = 1000m,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = 1,
            OrderId = orderId,
            Amount = 1000m,
            Method = PaymentMethod.ESewa,
            Status = PaymentStatus.Pending,

            // Must match the transaction UUID passed
            // to VerifyPaymentAsync()
            TransactionId = transactionUuid,

            PaymentDate = DateTime.UtcNow
        };

        context.Orders.Add(order);
        context.Payments.Add(payment);

        await context.SaveChangesAsync();

        // Simulated eSewa status API response
        var responseJson = """
    {
        "status": "COMPLETE",
        "transaction_uuid": "transaction-123",
        "total_amount": "1000.00",
        "transaction_code": "ESW123456"
    }
    """;

        var handler = new FakeHttpMessageHandler(
            responseJson);

        var httpClient = new HttpClient(handler);

        var settings = Options.Create(
            new ESewaSettings
            {
                ProductCode = "EPAYTEST",
                SecretKey = "test-secret",
                StatusUrl = "https://example.com/status"
            });

        var service = new EsewaPaymentService(
            httpClient,
            settings,
            context,
            NullLogger<EsewaPaymentService>.Instance);

        // Act

        var result =
            await service.VerifyPaymentAsync(
                orderId,
                transactionUuid);

        // Assert

        Assert.True(result);

        Assert.Equal(
            PaymentStatus.Paid,
            payment.Status);

        Assert.Equal(
    transactionUuid,
    payment.TransactionId);

        Assert.Equal(
            "ESW123456",
            payment.ESewaTransactionCode);

        Assert.Equal(
            OrderStatus.Confirmed,
            order.Status);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenTransactionUuidDoesNotMatch_ReturnsFalse()
    {
        // Arrange

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context =
            new AppDbContext(options);

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            TotalAmount = 1000,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = 1,
            OrderId = 1,
            Amount = 1000,
            Method = PaymentMethod.ESewa,
            Status = PaymentStatus.Pending,
            TransactionId = "transaction-123",
            PaymentDate = DateTime.UtcNow,
            Order = order
        };

        context.Orders.Add(order);
        context.Payments.Add(payment);

        await context.SaveChangesAsync();

        var responseJson = """
    {
        "status": "COMPLETE",
        "transaction_uuid": "transaction-999",
        "total_amount": "1000.00",
        "transaction_code": "ESW123456"
    }
    """;

        var handler =
            new FakeHttpMessageHandler(responseJson);

        var service =
     new EsewaPaymentService(
         new HttpClient(handler),
         Options.Create(
             new ESewaSettings
             {
                 ProductCode = "EPAYTEST",
                 SecretKey = "test-secret",
                 StatusUrl = "https://example.com/status"
             }),
         context,
         NullLogger<EsewaPaymentService>.Instance);

        // Act

        var result =
            await service.VerifyPaymentAsync(
                1,
                "transaction-123");

        // Assert

        Assert.False(result);

        Assert.Equal(
            PaymentStatus.Failed,
            payment.Status);

        Assert.Equal(
            OrderStatus.Pending,
            order.Status);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenAmountDoesNotMatch_ReturnsFalse()
    {
        // Arrange

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context =
            new AppDbContext(options);

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            TotalAmount = 1000,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = 1,
            OrderId = 1,
            Amount = 1000,
            Method = PaymentMethod.ESewa,
            Status = PaymentStatus.Pending,
            TransactionId = "transaction-123",
            PaymentDate = DateTime.UtcNow,
            Order = order
        };

        context.Orders.Add(order);
        context.Payments.Add(payment);

        await context.SaveChangesAsync();

        var responseJson = """
    {
        "status": "COMPLETE",
        "transaction_uuid": "transaction-123",
        "total_amount": "2000.00",
        "transaction_code": "ESW123456"
    }
    """;

        var handler =
            new FakeHttpMessageHandler(responseJson);

        var service =
    new EsewaPaymentService(
        new HttpClient(handler),
        Options.Create(
            new ESewaSettings
            {
                ProductCode = "EPAYTEST",
                SecretKey = "test-secret",
                StatusUrl = "https://example.com/status"
            }),
        context,
        NullLogger<EsewaPaymentService>.Instance);

        // Act

        var result =
            await service.VerifyPaymentAsync(
                1,
                "transaction-123");

        // Assert

        Assert.False(result);

        Assert.Equal(
            PaymentStatus.Failed,
            payment.Status);

        Assert.Equal(
            OrderStatus.Pending,
            order.Status);
    }

    [Fact]
    public async Task VerifyPaymentAsync_WhenEsewaStatusIsNotComplete_ReturnsFalse()
    {
        // Arrange

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context =
            new AppDbContext(options);

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            TotalAmount = 1000,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Id = 1,
            OrderId = 1,
            Amount = 1000,
            Method = PaymentMethod.ESewa,
            Status = PaymentStatus.Pending,
            TransactionId = "transaction-123",
            PaymentDate = DateTime.UtcNow,
            Order = order
        };

        context.Orders.Add(order);
        context.Payments.Add(payment);

        await context.SaveChangesAsync();

        var responseJson = """
    {
        "status": "PENDING",
        "transaction_uuid": "transaction-123",
        "total_amount": "1000.00"
    }
    """;

        var handler =
            new FakeHttpMessageHandler(responseJson);

        var service =
    new EsewaPaymentService(
        new HttpClient(handler),
        Options.Create(
            new ESewaSettings
            {
                ProductCode = "EPAYTEST",
                SecretKey = "test-secret",
                StatusUrl = "https://example.com/status"
            }),
        context,
        NullLogger<EsewaPaymentService>.Instance);

        // Act

        var result =
            await service.VerifyPaymentAsync(
                1,
                "transaction-123");

        // Assert

        Assert.False(result);

        Assert.Equal(
            PaymentStatus.Failed,
            payment.Status);
    }





    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(
            string responseContent,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(
                    _responseContent,
                    Encoding.UTF8,
                    "application/json")
            };

            return Task.FromResult(response);
        }
    }
}