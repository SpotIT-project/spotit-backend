// ============================================================================
// GetPostByIdTests.cs — Integration tests for getting a single post
// ============================================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.DTOs;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Posts;

[Collection("Database")]
public class GetPostByIdTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public GetPostByIdTests(SpotItWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPostById_ExistingPost_Returns200()
    {
        // Arrange
        var userId = await _factory.CreateTestUserAsync("getter@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "getter@test.com", "Citizen");

        // Get a valid category ID
        var categoriesResponse = await client.GetFromJsonAsync<IEnumerable<SpotIt.Application.DTOs.CategoryDto>>("/api/categories");
        var categoryId = categoriesResponse!.First().Id;

        // Create a post
        var createResponse = await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "Specific Post",
            Description = "Fetch this directly",
            CategoryId = categoryId,
            IsAnonymous = false
        });
        
        if (!createResponse.IsSuccessStatusCode)
        {
            var createBody = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create post. Status: {createResponse.StatusCode}, Body: {createBody}");
        }
        
        // Find the post ID by getting all posts
        var allPosts = await client.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts");
        var postId = allPosts!.Items.First().Id;

        // Act
        var getResponse = await client.GetAsync($"/api/posts/{postId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var post = await getResponse.Content.ReadFromJsonAsync<PostDto>();
        post.Should().NotBeNull();
        post!.Title.Should().Be("Specific Post");
    }

    [Fact]
    public async Task GetPostById_NonExistent_Returns404()
    {
        // Arrange
        var userId = await _factory.CreateTestUserAsync("getter@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "getter@test.com", "Citizen");
        
        var fakeId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/posts/{fakeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

