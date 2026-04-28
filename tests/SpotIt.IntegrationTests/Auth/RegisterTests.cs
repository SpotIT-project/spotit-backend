// ============================================================================
// RegisterTests.cs — Integration tests for the registration endpoint
// ============================================================================
// WHAT WE'RE TESTING:
//   We are testing the full flow from HTTP Request -> API Controller -> Database.
//   We want to ensure that providing valid data creates a user, while invalid
//   data (like a duplicate email or bad password) returns a 400 Bad Request.
//
// HOW WE TEST IT:
//   We use the HttpClient provided by SpotItWebApplicationFactory to send real
//   HTTP requests to the in-memory TestServer. Data is saved in the real test DB.
// ============================================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SpotIt.IntegrationTests.Infrastructure;
using Xunit;

namespace SpotIt.IntegrationTests.Auth;

[Collection("Database")]
public class RegisterTests : IAsyncLifetime
{
    private readonly SpotItWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RegisterTests(SpotItWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ValidData_Returns200()
    {
        // Act: Send a POST request to the register endpoint
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "newcitizen@test.com",
            Password = "TestPassword123!",
            FullName = "New Citizen",
            City = "Cluj"
        });

        // Assert: The endpoint should return 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        // Arrange: Register a user first
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "duplicate@test.com",
            Password = "TestPassword123!",
            FullName = "First User",
            City = "Cluj"
        });

        // Act: Try to register AGAIN with the same email
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "duplicate@test.com",
            Password = "AnotherPassword123!",
            FullName = "Second User",
            City = "Bucuresti"
        });

        // Assert: Registration must fail with 400 Bad Request
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidPassword_Returns400()
    {
        // Act: Try to register with a password that doesn't meet Identity requirements
        // (Identity requires uppercase, digit, and length >= 8)
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "badpass@test.com",
            Password = "weak", // Fails minimum length and complexity
            FullName = "Weak Pass",
            City = "Cluj"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
