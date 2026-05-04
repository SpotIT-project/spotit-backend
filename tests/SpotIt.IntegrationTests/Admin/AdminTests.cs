using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.Authorization;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Admin;

[Collection("Database")]
public class AdminTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public AdminTests(SpotItWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient AdminClient()
    {
        var id = Guid.NewGuid().ToString();
        return _factory.CreateClient().AsRole(id, "admin@test.com", "Admin", Permissions.Roles.Manage);
    }

    private HttpClient CitizenClient()
    {
        var id = Guid.NewGuid().ToString();
        return _factory.CreateClient().AsRole(id, "citizen@test.com", "Citizen");
    }

    [Fact]
    public async Task CreateRole_AsAdmin_Returns200AndRoleAppearsInList()
    {
        var admin = AdminClient();

        var createResponse = await admin.PostAsJsonAsync("/api/admin/roles", new { Name = "Moderator" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await admin.GetFromJsonAsync<IEnumerable<RoleDto>>("/api/admin/roles");
        listResponse!.Should().Contain(r => r.Name == "Moderator");
    }

    [Fact]
    public async Task DeleteBuiltInRole_Returns400()
    {
        var admin = AdminClient();

        var response = await admin.DeleteAsync("/api/admin/roles/Citizen");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddClaimToRole_AsAdmin_Returns200()
    {
        var admin = AdminClient();

        await admin.PostAsJsonAsync("/api/admin/roles", new { Name = "Moderator" });

        var response = await admin.PostAsJsonAsync(
            "/api/admin/roles/Moderator/claims",
            new { Permission = Permissions.Posts.UpdateStatus });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveRolesManageFromAdmin_Returns400()
    {
        var admin = AdminClient();
        var perm  = Uri.EscapeDataString(Permissions.Roles.Manage);

        var response = await admin.DeleteAsync($"/api/admin/roles/Admin/claims/{perm}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdminEndpoint_AsCitizen_Returns403()
    {
        var citizen  = CitizenClient();
        var response = await citizen.GetAsync("/api/admin/roles");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LoginAsCityHallEmployee_JwtEmbedsClaim_StatusEndpointReturns204()
    {
        var employeeId    = await _factory.CreateTestUserAsync("employee@test.com", "Test123!", "CityHallEmployee");
        var citizenId     = await _factory.CreateTestUserAsync("citizen@test.com",  "Test123!", "Citizen");
        var citizenClient = _factory.CreateClient().AsRole(citizenId, "citizen@test.com", "Citizen");

        var categoriesResponse = await citizenClient.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categoriesResponse!.First().Id;

        await citizenClient.PostAsJsonAsync("/api/posts", new
        {
            Title       = "Test post",
            Description = "For status update test",
            CategoryId  = categoryId,
            IsAnonymous = false
        });

        var allPosts = await citizenClient.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts");
        var postId   = allPosts!.Items.First().Id;

        var loginClient = _factory.CreateClient();
        var loginResp   = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            Email    = "employee@test.com",
            Password = "Test123!"
        });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var setCookieHeader = loginResp.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("accessToken"));
        var tokenValue      = setCookieHeader.Split(';')[0].Replace("accessToken=", "");
        var employeeClient  = _factory.CreateClient();
        employeeClient.DefaultRequestHeaders.Add("Cookie", $"accessToken={tokenValue}");

        var statusResp = await employeeClient.PatchAsJsonAsync($"/api/posts/{postId}/status", new
        {
            NewStatus = SpotIt.Domain.Enums.PostStatus.InProgress,
            Note      = "Working on it"
        });
        statusResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

file record RoleDto(string Name, List<string> Claims);
file record CategoryDto(int Id, string Name);
file record PostDto(Guid Id, SpotIt.Domain.Enums.PostStatus Status);
