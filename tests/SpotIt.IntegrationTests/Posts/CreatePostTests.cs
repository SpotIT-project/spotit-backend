using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpotIt.Infrastructure.Data;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Posts;

[Collection("Database")]
public class CreatePostTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public CreatePostTests(SpotItWebApplicationFactory factory) => _factory = factory;
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreatePost_AuthenticatedCitizen_Returns201AndPostExistsInDb()
    {
        var userId = await _factory.CreateTestUserAsync("citizen@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "citizen@test.com", "Citizen");

        var categoriesResponse = await client.GetFromJsonAsync<IEnumerable<SpotIt.Application.DTOs.CategoryDto>>("/api/categories");
        var categoryId = categoriesResponse!.First().Id;

        var response = await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "Title Test",
            Description = "Large pothole on Main Street",
            CategoryId = categoryId,
            IsAnonymous = false
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Title == "Title Test");
        post.Should().NotBeNull();
        post!.AuthorId.Should().Be(userId);
    }

    [Fact]
    public async Task CreatePost_Unauthenticated_Returns401()
    {
        var client=_factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/posts", new 
        {
            Title = "Title Test",
            Description = "Large pothole on Main Street",
            CategoryId = 1, // This is fine for 401 because auth is checked first, but good practice to keep it dynamic if possible. Actually, for 401 it doesn't matter, but I'll keep it as 1 to avoid an extra call if not needed, or just change it for consistency.
            IsAnonymous = false
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
    }
}
