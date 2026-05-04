// ============================================================================
// UpdatePostStatusTests.cs — Integration tests for status updates
// ============================================================================
// WHAT WE'RE TESTING:
//   Role-based authorization (CityHallEmployee vs Citizen) and status 
//   modification logic over HTTP.
// ============================================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Enums;
using SpotIt.Application.Authorization;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Posts;

[Collection("Database")]
public class UpdatePostStatusTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public UpdatePostStatusTests(SpotItWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpdateStatus_CityHallEmployee_Returns204()
    {
        // Arrange — Create a post as a Citizen
        var citizenId = await _factory.CreateTestUserAsync("citizen@test.com", "Test123!", "Citizen");
        var citizenClient = _factory.CreateClient().AsRole(citizenId, "citizen@test.com", "Citizen");

        var categoriesResponse = await citizenClient.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categoriesResponse!.First().Id;

        await citizenClient.PostAsJsonAsync("/api/posts", new
        {
            Title = "Status Update Post",
            Description = "Waiting for employee",
            CategoryId = categoryId,
            IsAnonymous = false
        });
        
        var allPosts = await citizenClient.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts");
        var postId = allPosts!.Items.First().Id;

        // Act — Update the status as an Employee
        var employeeId = await _factory.CreateTestUserAsync("employee@test.com", "Test123!", "CityHallEmployee");
        var employeeClient = _factory.CreateClient().AsRole(employeeId, "employee@test.com", "CityHallEmployee", Permissions.Posts.UpdateStatus);

        var response = await employeeClient.PatchAsJsonAsync($"/api/posts/{postId}/status", new
        {
            NewStatus = PostStatus.InProgress,
            Note = "Working on it"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify status was changed
        var updatedPostResponse = await citizenClient.GetAsync($"/api/posts/{postId}");
        var updatedPost = await updatedPostResponse.Content.ReadFromJsonAsync<PostDto>();
        updatedPost!.Status.Should().Be(PostStatus.InProgress);
    }

    [Fact]
    public async Task UpdateStatus_Citizen_Returns403()
    {
        // Arrange
        var citizenId = await _factory.CreateTestUserAsync("citizen@test.com", "Test123!", "Citizen");
        var citizenClient = _factory.CreateClient().AsRole(citizenId, "citizen@test.com", "Citizen");

        var categoriesResponse = await citizenClient.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categoriesResponse!.First().Id;

        await citizenClient.PostAsJsonAsync("/api/posts", new
        {
            Title = "Forbidden Post",
            Description = "Citizen tries to update status",
            CategoryId = categoryId,
            IsAnonymous = false
        });
        
        var allPosts = await citizenClient.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts");
        var postId = allPosts!.Items.First().Id;

        // Act — Try to update status as a Citizen (should fail permission check)
        var response = await citizenClient.PatchAsJsonAsync($"/api/posts/{postId}/status", new
        {
            NewStatus = PostStatus.InProgress,
            Note = "I fix it myself"
        });

        // Assert — 403 Forbidden because Citizen has no posts:update_status permission
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

