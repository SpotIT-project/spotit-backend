// ============================================================================
// RefreshTokenTests.cs — Integration tests for token refreshing
// ============================================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Auth;

[Collection("Database")]
public class RefreshTokenTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RefreshTokenTests(SpotItWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Refresh_ValidTokens_Returns200AndSetsNewCookies()
    {
        // Arrange: Register and login to get the access & refresh token cookies
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "refresh@test.com",
            Password = "Test123!",
            FullName = "Test",
            City = "City"
        });
        
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "refresh@test.com",
            Password = "Test123!"
        });
        
        // Extract cookies from the login response and send them with the refresh request
        var loginCookies = loginResponse.Headers.GetValues("Set-Cookie");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", loginCookies);

        // Act
        var refreshResponse = await _client.SendAsync(request);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify new cookies were set
        refreshResponse.Headers.TryGetValues("Set-Cookie", out var refreshCookies).Should().BeTrue();
        refreshCookies!.Any(c => c.StartsWith("accessToken")).Should().BeTrue();
        refreshCookies!.Any(c => c.StartsWith("refreshToken")).Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_MissingTokens_Returns401()
    {
        // Act: Request refresh without sending any cookies
        var response = await _client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
