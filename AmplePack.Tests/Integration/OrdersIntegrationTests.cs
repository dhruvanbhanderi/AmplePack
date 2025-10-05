using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using AmplePack.Models;
using AmplePack.Data;
using AmplePack.Tests.Infrastructure;

namespace AmplePack.Tests.Integration
{
    [Trait("Category", "Integration")]
    public class OrdersIntegrationTests : IClassFixture<AmplePackWebApplicationFactory<Program>>
    {
        private readonly AmplePackWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public OrdersIntegrationTests(AmplePackWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Get_Orders_GetCustomerDetails_Should_Return_Json()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = context.Customers.First();

            // Act
            var response = await _client.GetAsync($"/Orders/GetCustomerDetails?id={customer.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            result.TryGetProperty("success", out var successProperty).Should().BeTrue();
            successProperty.GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task Get_Orders_GetCustomerDetails_With_Invalid_Id_Should_Return_Error_Json()
        {
            // Act
            var response = await _client.GetAsync("/Orders/GetCustomerDetails?id=999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            result.TryGetProperty("success", out var successProperty).Should().BeTrue();
            successProperty.GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task Get_Orders_GetCustomerProducts_Should_Return_Json()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = context.Customers.First();

            // Act
            var response = await _client.GetAsync($"/Orders/GetCustomerProducts?customerId={customer.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Orders_API_Performance_Test_Should_Complete_Within_Time_Limit()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = context.Customers.First();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync($"/Orders/GetCustomerDetails?id={customer.Id}");

            // Assert
            stopwatch.Stop();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
        }

        [Fact]
        public async Task Orders_Database_Operations_Should_Maintain_Data_Integrity()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var initialOrderCount = context.Orders.Count();
            var customer = context.Customers.First();

            // Verify initial state
            initialOrderCount.Should().BeGreaterThan(0, "Should have seeded test data");

            // Act - Get customer details (read operation)
            var response = await _client.GetAsync($"/Orders/GetCustomerDetails?id={customer.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Verify data integrity maintained
            using var verifyScope = _factory.Services.CreateScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var finalOrderCount = verifyContext.Orders.Count();
            
            finalOrderCount.Should().Be(initialOrderCount, "Order count should remain unchanged after read operations");
        }

        [Fact]
        public async Task Orders_Multiple_Concurrent_API_Calls_Should_Handle_Load()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = context.Customers.First();

            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Send 5 concurrent requests
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(_client.GetAsync($"/Orders/GetCustomerDetails?id={customer.Id}"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotBeEmpty();
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Orders_GetCustomerDetails_Should_Handle_Different_Customer_Ids(int customerId)
        {
            // Act
            var response = await _client.GetAsync($"/Orders/GetCustomerDetails?id={customerId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            result.TryGetProperty("success", out var successProperty).Should().BeTrue();
            
            // Should succeed for customer IDs 1 and 2 (seeded test data)
            successProperty.GetBoolean().Should().BeTrue();
        }
    }
}