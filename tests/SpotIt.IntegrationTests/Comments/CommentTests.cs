// ============================================================================
// CommentTests.cs — Integration tests for Comments endpoints
// ============================================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.DTOs;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Comments;

[Collection("Database")]
public class CommentTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public CommentTests(SpotItWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddComment_Authenticated_Returns201()
    {
        // Arrange
        var userId = await _factory.CreateTestUserAsync("commenter@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "commenter@test.com", "Citizen");

        // Get valid category
        var categories = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;

        // Seed a post to comment on
        await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "Post for commenting",
            Description = "Empty",
            CategoryId = categoryId,
            IsAnonymous = false
        });
        
        var allPosts = await client.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts");
        var postId = allPosts!.Items.First().Id;

        // Act
        var response = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", new
        {
            Content = "This is a great comment"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetComments_ReturnsCommentsForPost()
    {
        // Arrange
        var userId = await _factory.CreateTestUserAsync("commenter@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "commenter@test.com", "Citizen");

        var categories = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;

        await client.PostAsJsonAsync("/api/posts", new { Title = "Post", Description = "Desc", CategoryId = categoryId, IsAnonymous = false });
        var allPosts = await client.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts");
        var postId = allPosts!.Items.First().Id;

        await client.PostAsJsonAsync($"/api/posts/{postId}/comments", new { Content = "Comment 1" });
        await client.PostAsJsonAsync($"/api/posts/{postId}/comments", new { Content = "Comment 2" });

        // Act
        var response = await client.GetAsync($"/api/posts/{postId}/comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var pagedResult = await response.Content.ReadFromJsonAsync<SpotIt.Application.Common.PagedResult<CommentDto>>();
        pagedResult!.Items.Should().HaveCount(2);
        pagedResult.Items.Should().Contain(c => c.Content == "Comment 1");
        pagedResult.Items.Should().Contain(c => c.Content == "Comment 2");
    }

    [Fact]
    public async Task AddComment_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync($"/api/posts/{Guid.NewGuid()}/comments", new { Content = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetComments_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/posts/{Guid.NewGuid()}/comments");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

