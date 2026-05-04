using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpotIt.Application.Authorization;
using SpotIt.Domain.Entities;

namespace SpotIt.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedRoleClaimsAsync(roleManager);
        await SeedAdminAsync(userManager);
        await SeedCategoriesAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["Admin", "CityHallEmployee", "Citizen"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedRoleClaimsAsync(RoleManager<IdentityRole> roleManager)
    {
        var rolePermissions = new Dictionary<string, IEnumerable<string>>
        {
            ["Admin"] = Permissions.GetAll(),
            ["CityHallEmployee"] = [Permissions.Posts.UpdateStatus, Permissions.Analytics.View],
            ["Citizen"] = []
        };

        foreach (var (roleName, permissions) in rolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue;

            var existingClaims = await roleManager.GetClaimsAsync(role);

            foreach (var permission in permissions)
            {
                var alreadyExists = existingClaims.Any(c =>
                    c.Type == "permission" && c.Value == permission);

                if (!alreadyExists)
                    await roleManager.AddClaimAsync(role, new Claim("permission", permission));
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@spotit.ro";

        if (await userManager.FindByEmailAsync(adminEmail) != null)
            return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Admin",
            City = "Oradea",
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(admin, "Admin@1234");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    private static async Task SeedCategoriesAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var categories = new List<Category>
          {
              new() { Name = "Roads", Description = "Potholes, damaged roads, missing signs", IconUrl = "🛣️" },
              new() { Name = "Lighting", Description = "Street lights, public area lighting", IconUrl = "💡" },
              new() { Name = "Parks", Description = "Park maintenance, green areas", IconUrl = "🌳" },
              new() { Name = "Waste", Description = "Garbage collection, illegal dumping", IconUrl = "🗑️" },
              new() { Name = "Water", Description = "Water supply, sewage issues", IconUrl = "💧" },
              new() { Name = "Safety", Description = "Public safety concerns", IconUrl = "🚨" }
          };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }
}
