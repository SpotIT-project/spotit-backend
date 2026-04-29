using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Auth;

[Collection("Database")]
public class LoginTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LoginTests(SpotItWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithAccessTokenCookie()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "citizen@test.com",
            Password = "Test123!",
            FullName = "Test Citizen",
            City = "Oradea"
        });
        var response = await _client.PostAsJsonAsync("/api/auth/login", new 
        {
            Email = "citizen@test.com",
            Password = "Test123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Any(c=>c.StartsWith("accessToken")).Should().BeTrue();
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "citizen@test.com",
            Password = "Test123Password",
            FullName = "Test Citizen",
            City = "Oradea"
        });
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "citizen@test.com",
            Password = "Test123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
