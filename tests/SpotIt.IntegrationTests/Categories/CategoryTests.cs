// ============================================================================
// CategoryTests.cs — Integration tests for reading seeded categories
// ============================================================================
// WHAT WE'RE TESTING:
//   Categories are seeded by the DatabaseSeeder on API startup. We want to
//   make sure they are globally accessible via the Categories endpoint.
//   Categories endpoint does not have [Authorize] on it in the main branch
//   so we expect a 200 OK.
// ============================================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.Application.DTOs;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Categories;

[Collection("Database")]
public class CategoryTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;

    public CategoryTests(SpotItWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetCategories_ReturnsSeededCategories()
    {
        // Act — Using an unauthenticated client
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<IEnumerable<CategoryDto>>();
        categories.Should().NotBeNullOrEmpty();
        
        // Ensure the initial seeded categories like "Roads" and "Lighting" are present
        categories.Should().Contain(c => c.Name == "Roads");
    }
}
