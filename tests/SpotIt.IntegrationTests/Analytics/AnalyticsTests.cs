using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.Authorization;
using SpotIt.Application.DTOs;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Analytics;

[Collection("Database")]
public class AnalyticsTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public AnalyticsTests(SpotItWebApplicationFactory factory) => _factory = factory;
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient AdminClient()
    {
        var id = Guid.NewGuid().ToString();
        return _factory.CreateClient().AsRole(id, "admin@test.com", "Admin", Permissions.Analytics.View);
    }

    private HttpClient CitizenClient()
    {
        var id = Guid.NewGuid().ToString();
        return _factory.CreateClient().AsRole(id, "citizen@test.com", "Citizen");
    }

    [Fact]
    public async Task GetByStatus_AsAdmin_Returns200()
    {
        var response = await AdminClient().GetAsync("/api/analytics/by-status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<PostsByStatusDto>>();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTopCategories_AsAdmin_Returns200()
    {
        var response = await AdminClient().GetAsync("/api/analytics/top-categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<TopCategoryDto>>();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByStatus_AsCitizen_Returns403()
    {
        var response = await CitizenClient().GetAsync("/api/analytics/by-status");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTopCategories_AsCitizen_Returns403()
    {
        var response = await CitizenClient().GetAsync("/api/analytics/top-categories");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetByStatus_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/analytics/by-status");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

// DTOs returned by the analytics endpoints — file-scoped so they don't leak.
file record PostsByStatusDto(string Status, int Count);
file record TopCategoryDto(string CategoryName, int PostCount);
