// ============================================================================
// GetPostsTests.cs — Integration tests for listing posts
// ============================================================================
// WHAT WE'RE TESTING:
//   That an authenticated user can retrieve a paginated list of posts, and
//   an unauthenticated user is rejected (401 Unauthorized).
// ============================================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.Common;
using SpotIt.Application.DTOs;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Posts;

[Collection("Database")]
public class GetPostsTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public GetPostsTests(SpotItWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPosts_Authenticated_Returns200WithPagedResult()
    {
        // Arrange
        var userId = await _factory.CreateTestUserAsync("getter@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "getter@test.com", "Citizen");

        // Seed a post
        var categoriesResponse = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categoriesResponse!.First().Id;

        await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "First Post",
            Description = "A seeded post",
            CategoryId = categoryId,
            IsAnonymous = false
        });

        // Act
        var response = await client.GetAsync("/api/posts?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Read the PagedResult
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PostDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.First().Title.Should().Be("First Post");
    }

    [Fact]
    public async Task GetPosts_Unauthenticated_Returns401()
    {
        // Act: Use a client WITHOUT an auth token
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/posts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

