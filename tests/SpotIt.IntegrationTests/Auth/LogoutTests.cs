// ============================================================================
// LogoutTests.cs — Integration tests for logout endpoint
// ============================================================================

using System.Net;
using FluentAssertions;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Auth;

[Collection("Database")]
public class LogoutTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LogoutTests(SpotItWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Logout_ClearsCookies_Returns200()
    {
        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // The server should instruct the client to clear the access and refresh tokens
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        
        var cookieList = cookies!.ToList();
        cookieList.Should().Contain(c => c.Contains("accessToken=") && c.Contains("expires=Thu, 01 Jan 1970"));
        cookieList.Should().Contain(c => c.Contains("refreshToken=") && c.Contains("expires=Thu, 01 Jan 1970"));
    }
}
