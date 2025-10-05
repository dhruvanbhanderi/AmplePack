using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using AmplePack.Models;
using AmplePack.Tests.Infrastructure;

namespace AmplePack.Tests.Integration
{
    [Trait("Category", "Integration")]
    public class BoxCalculatorIntegrationTests : IClassFixture<AmplePackWebApplicationFactory<Program>>
    {
        private readonly AmplePackWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public BoxCalculatorIntegrationTests(AmplePackWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Get_BoxCalculator_TestVisualization_Should_Return_Success()
        {
            // This endpoint has [AllowAnonymous] so it should work
            // Act
            var response = await _client.GetAsync("/BoxCalculator/TestVisualization");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            result.TryGetProperty("success", out var successProperty).Should().BeTrue();
            successProperty.GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task Get_BoxCalculator_TestVisualizationSimple_Should_Return_Success()
        {
            // This endpoint has [AllowAnonymous] so it should work
            // Act
            var response = await _client.GetAsync("/BoxCalculator/TestVisualizationSimple");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            result.TryGetProperty("success", out var successProperty).Should().BeTrue();
            successProperty.GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task Post_BoxCalculator_GetLayoutVisualization_Should_Return_Json()
        {
            // This endpoint has [AllowAnonymous] so it should work
            // Arrange
            var request = new LayoutVisualizationRequest
            {
                Length = 10,
                Width = 8,
                Height = 6,
                SheetLength = 40,
                SheetWidth = 30,
                BoardGSM = 150,
                CompressionRatio = 1.0m,
                Quantity = 1000
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/BoxCalculator/GetLayoutVisualization", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().NotBeEmpty();

            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            result.TryGetProperty("success", out var successProperty).Should().BeTrue();
            successProperty.GetBoolean().Should().BeTrue();
        }

        [Theory]
        [InlineData(5, 3, 2)]
        [InlineData(10, 8, 6)]
        [InlineData(15, 12, 8)]
        public async Task BoxCalculator_Layout_Visualization_Should_Handle_Different_Box_Sizes(decimal length, decimal width, decimal height)
        {
            // This endpoint has [AllowAnonymous] so it should work
            // Arrange
            var request = new LayoutVisualizationRequest
            {
                Length = length,
                Width = width,
                Height = height,
                SheetLength = 40,
                SheetWidth = 30,
                BoardGSM = 150,
                CompressionRatio = 1.0m,
                Quantity = 1000
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/BoxCalculator/GetLayoutVisualization", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            result.TryGetProperty("success", out var successProperty).Should().BeTrue();
            
            if (successProperty.GetBoolean())
            {
                result.TryGetProperty("visualization", out var visualizationProperty).Should().BeTrue();
                visualizationProperty.TryGetProperty("totalBlanksPerSheet", out var blanksProperty).Should().BeTrue();
                blanksProperty.GetInt32().Should().BeGreaterThanOrEqualTo(0);
            }
        }

        [Fact]
        public async Task BoxCalculator_Performance_Test_Should_Complete_Within_Time_Limit()
        {
            // Arrange
            var request = new LayoutVisualizationRequest
            {
                Length = 10,
                Width = 8,
                Height = 6,
                SheetLength = 40,
                SheetWidth = 30,
                BoardGSM = 150,
                CompressionRatio = 1.0m,
                Quantity = 1000
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await _client.PostAsync("/BoxCalculator/GetLayoutVisualization", content);

            // Assert
            stopwatch.Stop();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
        }

        [Fact]
        public async Task BoxCalculator_Layout_Visualization_With_Invalid_Data_Should_Return_Error()
        {
            // Arrange - invalid data (zero dimensions)
            var request = new LayoutVisualizationRequest
            {
                Length = 0, // Invalid
                Width = 0,  // Invalid
                Height = 0, // Invalid
                SheetLength = 40,
                SheetWidth = 30,
                BoardGSM = 150,
                CompressionRatio = 1.0m,
                Quantity = 1000
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/BoxCalculator/GetLayoutVisualization", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            result.TryGetProperty("success", out var successProperty).Should().BeTrue();
            successProperty.GetBoolean().Should().BeFalse(); // Should fail for invalid data
        }

        [Fact]
        public async Task BoxCalculator_Multiple_Concurrent_Requests_Should_Handle_Load()
        {
            // Arrange
            var request = new LayoutVisualizationRequest
            {
                Length = 10,
                Width = 8,
                Height = 6,
                SheetLength = 40,
                SheetWidth = 30,
                BoardGSM = 150,
                CompressionRatio = 1.0m,
                Quantity = 1000
            };

            var json = JsonSerializer.Serialize(request);
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Send 5 concurrent requests
            for (int i = 0; i < 5; i++)
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                tasks.Add(_client.PostAsync("/BoxCalculator/GetLayoutVisualization", content));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().NotBeEmpty();
            }
        }
    }
}