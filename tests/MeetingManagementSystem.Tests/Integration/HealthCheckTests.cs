using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MeetingManagementSystem.Tests.Integration
{
    public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public HealthCheckTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task HealthEndpoint_ShouldReturnHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("Healthy", content);
        }

        [Fact]
        public async Task HealthEndpoint_ShouldReturn200StatusCode()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task HealthEndpoint_ShouldReturnTextPlain()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task HealthEndpoint_ShouldRespondQuickly()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync("/health");
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Health check took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        }

        [Fact]
        public async Task Application_ShouldHandleMultipleHealthChecks()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Send 10 concurrent health check requests
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_client.GetAsync("/health"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.All(responses, response =>
            {
                Assert.True(response.IsSuccessStatusCode);
            });
        }
    }
}
