using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.Common;
using SpotIt.Application.DTOs;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Posts;

[Collection("Database")]
public class SearchPostsTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public SearchPostsTests(SpotItWebApplicationFactory factory) => _factory = factory;
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPosts_WithSearchTerm_ReturnsOnlyMatchingPosts()
    {
        var userId = await _factory.CreateTestUserAsync("searcher@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "searcher@test.com", "Citizen");

        var categories = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;

        await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "UniqueZebra2024 title",
            Description = "There is a big crack near the market.",
            CategoryId = categoryId,
            IsAnonymous = false
        });

        await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "Broken street light",
            Description = "The lamp on Oak Avenue is not working.",
            CategoryId = categoryId,
            IsAnonymous = false
        });

        var response = await client.GetAsync("/api/posts?page=1&pageSize=10&search=UniqueZebra2024");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PostDto>>();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Title.Should().Be("UniqueZebra2024 title");
    }

    [Fact]
    public async Task GetPosts_SearchMatchesDescription_ReturnsPost()
    {
        var userId = await _factory.CreateTestUserAsync("searcher2@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "searcher2@test.com", "Citizen");

        var categories = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;

        await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "Road issue",
            Description = "UniqueAlpha9876 crack near the schoolyard.",
            CategoryId = categoryId,
            IsAnonymous = false
        });

        var response = await client.GetAsync("/api/posts?page=1&pageSize=10&search=UniqueAlpha9876");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PostDto>>();
        result!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPosts_SearchNoMatch_ReturnsEmpty()
    {
        var userId = await _factory.CreateTestUserAsync("searcher3@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "searcher3@test.com", "Citizen");

        var response = await client.GetAsync("/api/posts?page=1&pageSize=10&search=xyznotreal");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PostDto>>();
        result!.Items.Should().BeEmpty();
    }
}
