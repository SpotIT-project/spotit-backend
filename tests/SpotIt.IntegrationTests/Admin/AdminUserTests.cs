using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.Authorization;
using SpotIt.Application.DTOs;
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
        // Arrange: register a real user so we can assert it appears in the list
        const string testEmail = "listed@test.com";
        var anonClient = _factory.CreateClient();
        var registerResponse = await anonClient.PostAsJsonAsync("/api/auth/register", new
        {
            FullName = "Listed User",
            Email = testEmail,
            Password = "Test123!",
            City = "Oradea"
        });
        registerResponse.IsSuccessStatusCode.Should().BeTrue("registration must succeed before querying users");

        // Act
        var admin = AdminClient();
        var response = await admin.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserSummaryDto>>();
        users.Should().NotBeNull();
        users!.Should().Contain(u => u.Email == testEmail, "the registered user must appear in the user list");
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

        var assignResponse = await admin.PostAsJsonAsync(
            $"/api/admin/users/{userId}/role",
            new { RoleName = "CityHallEmployee" });

        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the role change is reflected in the user list
        var usersResponse = await admin.GetAsync("/api/admin/users");
        usersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserSummaryDto>>();
        users.Should().NotBeNull();

        var updatedUser = users!.FirstOrDefault(u => u.Id == userId);
        updatedUser.Should().NotBeNull("the assigned user must appear in the user list");
        updatedUser!.Role.Should().Be("CityHallEmployee");
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
