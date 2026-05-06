using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.Authorization;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Admin;

[Collection("Database")]
public class AdminUserTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public AdminUserTests(SpotItWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient AdminClient()
    {
        var id = Guid.NewGuid().ToString();
        return _factory.CreateClient().AsRole(
            id, "admin@test.com", "Admin",
            Permissions.Roles.Manage,
            Permissions.Users.Manage,
            Permissions.Posts.UpdateStatus,
            Permissions.Analytics.View);
    }

    private HttpClient CitizenClient()
    {
        var id = Guid.NewGuid().ToString();
        return _factory.CreateClient().AsRole(id, "citizen@test.com", "Citizen");
    }

    [Fact]
    public async Task GetUsers_ReturnsUserList()
    {
        var admin = AdminClient();

        var response = await admin.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserSummaryDto>>();
        users.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUsers_Returns403_ForNonAdmin()
    {
        var citizen = CitizenClient();

        var response = await citizen.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_ChangesUserRole()
    {
        var userId = await _factory.CreateTestUserAsync("newuser@test.com", "Test123!", "Citizen");
        var admin = AdminClient();

        var response = await admin.PostAsJsonAsync(
            $"/api/admin/users/{userId}/role",
            new { RoleName = "CityHallEmployee" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignRole_ReturnsNotFound_ForMissingUser()
    {
        var admin = AdminClient();
        var randomId = Guid.NewGuid().ToString();

        var response = await admin.PostAsJsonAsync(
            $"/api/admin/users/{randomId}/role",
            new { RoleName = "Citizen" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignRole_ReturnsBadRequest_ForInvalidRole()
    {
        var userId = await _factory.CreateTestUserAsync("user2@test.com", "Test123!", "Citizen");
        var admin = AdminClient();

        var response = await admin.PostAsJsonAsync(
            $"/api/admin/users/{userId}/role",
            new { RoleName = "SuperAdmin" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

file record UserSummaryDto(string Id, string Email, string FullName, string City, string Role);
