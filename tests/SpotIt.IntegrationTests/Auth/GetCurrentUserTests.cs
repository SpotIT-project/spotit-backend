using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Auth;

[Collection("Database")]
public class GetCurrentUserTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public GetCurrentUserTests(SpotItWebApplicationFactory factory) => _factory = factory;
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMe_Authenticated_ReturnsCurrentUserProfile()
    {
        var userId = await _factory.CreateTestUserAsync("me@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "me@test.com", "Citizen");

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("email").GetString().Should().Be("me@test.com");
        body.GetProperty("fullName").GetString().Should().Be("Test User");
        body.GetProperty("role").GetString().Should().Be("Citizen");
    }

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
