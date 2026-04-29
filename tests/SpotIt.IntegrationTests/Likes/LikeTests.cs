// ============================================================================
// LikeTests.cs — Integration tests for Likes
// ============================================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.DTOs;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Likes;

[Collection("Database")]
public class LikeTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public LikeTests(SpotItWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task LikePost_Authenticated_Returns204()
    {
        // Arrange
        var userId = await _factory.CreateTestUserAsync("liker@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "liker@test.com", "Citizen");

        var categories = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;

        await client.PostAsJsonAsync("/api/posts", new { Title = "Post", Description = "Desc", CategoryId = categoryId, IsAnonymous = false });
        var allPosts = await client.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts");
        var postId = allPosts!.Items.First().Id;

        // Act
        var response = await client.PostAsync($"/api/posts/{postId}/likes", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LikePost_AlreadyLiked_Returns409()
    {
        // Arrange -> Like it once
        var userId = await _factory.CreateTestUserAsync("liker@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "liker@test.com", "Citizen");

        var categories = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;

        await client.PostAsJsonAsync("/api/posts", new { Title = "Post", Description = "Desc", CategoryId = categoryId, IsAnonymous = false });
        var allPosts = await client.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts");
        var postId = allPosts!.Items.First().Id;

        await client.PostAsync($"/api/posts/{postId}/likes", null);

        // Act -> Try to like the SAME post AGAIN
        var exceptionResponse = await client.PostAsync($"/api/posts/{postId}/likes", null);

        // Assert -> The ExceptionMiddleware catches InvalidOperationException and returns 409 Conflict
        exceptionResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UnlikePost_Returns204()
    {
        // Arrange -> Like it
        var userId = await _factory.CreateTestUserAsync("liker@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "liker@test.com", "Citizen");

        var categories = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;

        await client.PostAsJsonAsync("/api/posts", new { Title = "Post", Description = "Desc", CategoryId = categoryId, IsAnonymous = false });
        var allPosts = await client.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts");
        var postId = allPosts!.Items.First().Id;

        await client.PostAsync($"/api/posts/{postId}/likes", null);

        // Act -> Unlike it
        var response = await client.DeleteAsync($"/api/posts/{postId}/likes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

