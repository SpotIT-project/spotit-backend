// ============================================================================
// PostStatusHistoryTests.cs — Integration tests for GET /api/posts/{id}/history
// ============================================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.Authorization;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Enums;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Posts;

[Collection("Database")]
public class PostStatusHistoryTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public PostStatusHistoryTests(SpotItWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetHistory_ReturnsHistoryForPost()
    {
        // Arrange — create a post as a Citizen
        var citizenId = await _factory.CreateTestUserAsync("citizen@test.com", "Test123!", "Citizen");
        var citizenClient = _factory.CreateClient().AsRole(citizenId, "citizen@test.com", "Citizen");

        var categoriesResponse = await citizenClient.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categoriesResponse!.First().Id;

        await citizenClient.PostAsJsonAsync("/api/posts", new
        {
            Title = "History Test Post",
            Description = "This post will have status updated",
            CategoryId = categoryId,
            IsAnonymous = false
        });

        var allPosts = await citizenClient.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts");
        var postId = allPosts!.Items.First().Id;

        // Update status as an Employee (creates a StatusHistory record)
        var employeeId = await _factory.CreateTestUserAsync("employee@test.com", "Test123!", "CityHallEmployee");
        var employeeClient = _factory.CreateClient().AsRole(employeeId, "employee@test.com", "CityHallEmployee", Permissions.Posts.UpdateStatus);

        var updateResponse = await employeeClient.PatchAsJsonAsync($"/api/posts/{postId}/status", new
        {
            NewStatus = PostStatus.InProgress,
            Note = "Working on it"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var historyResponse = await citizenClient.GetAsync($"/api/posts/{postId}/history");

        // Assert
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await historyResponse.Content.ReadFromJsonAsync<IEnumerable<StatusHistoryDto>>();
        history.Should().NotBeNull();
        history.Should().NotBeEmpty();
        history!.Last().NewStatus.Should().Be(PostStatus.InProgress.ToString());
    }

    [Fact]
    public async Task GetHistory_ReturnsNotFound_ForMissingPost()
    {
        // Arrange
        var citizenId = await _factory.CreateTestUserAsync("citizen@test.com", "Test123!", "Citizen");
        var citizenClient = _factory.CreateClient().AsRole(citizenId, "citizen@test.com", "Citizen");

        var fakeId = Guid.NewGuid();

        // Act
        var response = await citizenClient.GetAsync($"/api/posts/{fakeId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHistory_Returns401_WhenNotAuthenticated()
    {
        // Arrange — unauthenticated client
        var client = _factory.CreateClient();
        var fakeId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/posts/{fakeId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
