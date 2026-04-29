using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.DTOs;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Posts;

[Collection("Database")]
public class DeletePostTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public DeletePostTests(SpotItWebApplicationFactory factory) => _factory = factory;
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(HttpClient client, Guid postId)> CreatePostAsUser(string email)
    {
        var userId = await _factory.CreateTestUserAsync(email, "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, email, "Citizen");

        var categories = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;

        await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "Post to delete",
            Description = "Some description",
            CategoryId = categoryId,
            IsAnonymous = false
        });

        var posts = await client.GetFromJsonAsync<SpotIt.Application.Common.PagedResult<PostDto>>("/api/posts?page=1&pageSize=10");
        var postId = posts!.Items.First().Id;

        return (client, postId);
    }

    [Fact]
    public async Task DeletePost_ByAuthor_Returns204()
    {
        var (client, postId) = await CreatePostAsUser("author@test.com");

        var response = await client.DeleteAsync($"/api/posts/{postId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletePost_ByAuthor_PostNoLongerExistsInDb()
    {
        var (client, postId) = await CreatePostAsUser("author2@test.com");

        await client.DeleteAsync($"/api/posts/{postId}");

        var getResponse = await client.GetAsync($"/api/posts/{postId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_ByOtherUser_Returns403()
    {
        var (_, postId) = await CreatePostAsUser("owner@test.com");

        var otherUserId = await _factory.CreateTestUserAsync("intruder@test.com", "Test123!", "Citizen");
        var otherClient = _factory.CreateClient().AsRole(otherUserId, "intruder@test.com", "Citizen");

        var response = await otherClient.DeleteAsync($"/api/posts/{postId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeletePost_Unauthenticated_Returns401()
    {
        var (_, postId) = await CreatePostAsUser("author3@test.com");

        var response = await _factory.CreateClient().DeleteAsync($"/api/posts/{postId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePost_NotFound_Returns404()
    {
        var userId = await _factory.CreateTestUserAsync("nobody@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "nobody@test.com", "Citizen");

        var response = await client.DeleteAsync($"/api/posts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
