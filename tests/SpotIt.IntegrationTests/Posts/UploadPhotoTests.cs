using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.DTOs;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Posts;

[Collection("Database")]
public class UploadPhotoTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public UploadPhotoTests(SpotItWebApplicationFactory factory) => _factory = factory;
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static MultipartFormDataContent MinimalJpeg()
    {
        // Validator requires: length >= 12 bytes, bytes[0]=0xFF, bytes[1]=0xD8 (JPEG magic)
        var bytes = new byte[12];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes), "Photo", "test.jpg");
        return content;
    }

    private async Task<Guid> CreatePostAsync(HttpClient client, int categoryId)
    {
        var response = await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "Photo upload test post",
            Description = "A post used to test photo uploads.",
            CategoryId = categoryId,
            IsAnonymous = false
        });
        var post = await response.Content.ReadFromJsonAsync<PostDto>();
        return post!.Id;
    }

    [Fact]
    public async Task UploadPhoto_AsAuthor_Returns200WithUrl()
    {
        var userId = await _factory.CreateTestUserAsync("author@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "author@test.com", "Citizen");

        var categories = await client.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;
        var postId = await CreatePostAsync(client, categoryId);

        var response = await client.PostAsync($"/api/posts/{postId}/photo", MinimalJpeg());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UrlResponse>();
        body.Should().NotBeNull();
        body!.Url.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UploadPhoto_AsNonAuthor_Returns403()
    {
        var authorId = await _factory.CreateTestUserAsync("author2@test.com", "Test123!", "Citizen");
        var authorClient = _factory.CreateClient().AsRole(authorId, "author2@test.com", "Citizen");

        var categories = await authorClient.GetFromJsonAsync<IEnumerable<CategoryDto>>("/api/categories");
        var categoryId = categories!.First().Id;
        var postId = await CreatePostAsync(authorClient, categoryId);

        var otherId = await _factory.CreateTestUserAsync("other@test.com", "Test123!", "Citizen");
        var otherClient = _factory.CreateClient().AsRole(otherId, "other@test.com", "Citizen");

        var response = await otherClient.PostAsync($"/api/posts/{postId}/photo", MinimalJpeg());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadPhoto_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/posts/{Guid.NewGuid()}/photo", MinimalJpeg());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadPhoto_PostNotFound_Returns404()
    {
        var userId = await _factory.CreateTestUserAsync("uploader@test.com", "Test123!", "Citizen");
        var client = _factory.CreateClient().AsRole(userId, "uploader@test.com", "Citizen");

        var response = await client.PostAsync($"/api/posts/{Guid.NewGuid()}/photo", MinimalJpeg());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

file record UrlResponse(string Url);
