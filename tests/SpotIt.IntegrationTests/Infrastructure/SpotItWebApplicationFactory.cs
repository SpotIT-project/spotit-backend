using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Enums;
using SpotIt.Infrastructure.Data;
using Xunit;

namespace SpotIt.IntegrationTests.Infrastructure;

public class SpotItWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=spotit_test;Username=postgres;Password=postgres";

    private Respawner _respawner = default!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:SecretKey"] = "test-secret-key-must-be-at-least-32-chars-long",
                ["Jwt:Issuer"] = "SpotIt.API",
                ["Jwt:Audience"] = "SpotIt.Client",
                ["Jwt:ExpiryMinutes"] = "60",
                ["RateLimiting:Auth:PermitLimit"] = int.MaxValue.ToString()
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the NpgsqlDataSource singleton so all EF Core + Identity operations
            // target the test database regardless of what appsettings.Development.json says.
            var existing = services.SingleOrDefault(d => d.ServiceType == typeof(NpgsqlDataSource));
            if (existing != null) services.Remove(existing);
            services.AddSingleton(_ => new NpgsqlDataSourceBuilder(ConnectionString).Build());
        });
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            WithReseed = true,
            TablesToIgnore =
            [
                new Respawn.Graph.Table("public", "__EFMigrationsHistory"),
                new Respawn.Graph.Table("public", "asp_net_roles"),
                new Respawn.Graph.Table("public", "asp_net_role_claims"),
                new Respawn.Graph.Table("public", "categories")
            ]
        });
    }

    public async Task<string> CreateTestUserAsync(string email, string password, string role)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Test User",
            City = "Oradea"
        };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
        return user.Id;
    }

    public async Task ResetDatabaseAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);

        // Re-seed the database so lookup tables (categories, roles) are present for the next test
        using var scope = Services.CreateScope();
        await SpotIt.Infrastructure.Data.Seed.DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }

    public new async Task DisposeAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
    }
}
