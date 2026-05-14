using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.Common;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Enums;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Posts;

[Collection("Database")]
public class FilterPostsTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public FilterPostsTests(SpotItWebApplicationFactory factory) => _factory = factory;
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPosts_FilterByStatus_ReturnsOnlyMatchingPosts()
    {
        // Seeded data includes exactly 1 Resolved post after ResetDatabaseAsync
        var userId = await _factory.CreateTestUserAsync("filter@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "filter@test.com", "Citizen");

        var response = await client.GetAsync("/api/posts?status=Resolved");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PostDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(p => p.Status.Should().Be(PostStatus.Resolved));
    }

    [Fact]
    public async Task GetPosts_FilterByCategoryId_ReturnsOnlyMatchingPosts()
    {
        var userId = await _factory.CreateTestUserAsync("filter@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "filter@test.com", "Citizen");

        var categories = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var roadsId = categories!.First(c => c.Name == "Roads").Id;

        var response = await client.GetAsync($"/api/posts?categoryId={roadsId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PostDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(p => p.CategoryId.Should().Be(roadsId));
    }

    [Fact]
    public async Task GetPosts_FilterByStatusAndSearch_ReturnsCombinedFilter()
    {
        // Seeded post "Missing road signs at roundabout" is Pending
        var userId = await _factory.CreateTestUserAsync("filter@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "filter@test.com", "Citizen");

        var response = await client.GetAsync("/api/posts?status=Pending&search=Missing");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<PostDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(p =>
        {
            p.Status.Should().Be(PostStatus.Pending);
            (p.Title.Contains("Missing", StringComparison.OrdinalIgnoreCase) ||
             p.Description.Contains("Missing", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
        });
    }
}
