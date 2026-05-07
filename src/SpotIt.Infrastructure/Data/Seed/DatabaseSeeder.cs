using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpotIt.Application.Authorization;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Enums;

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
        await SeedEmployeeAsync(userManager);
        await SeedCitizenAsync(userManager);
        await SeedCategoriesAsync(context);
        await SeedMockPostsAsync(context, userManager);
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
            if (role is null) continue;

            var existingClaims = await roleManager.GetClaimsAsync(role);

            foreach (var permission in permissions)
            {
                var alreadyExists = existingClaims.Any(c => c.Type == "permission" && c.Value == permission);
                if (!alreadyExists)
                    await roleManager.AddClaimAsync(role, new Claim("permission", permission));
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string email = "admin@spotit.ro";
        if (await userManager.FindByEmailAsync(email) != null) return;

        var user = new ApplicationUser
        {
            UserName = email, Email = email,
            FullName = "System Admin", City = "Oradea",
            CreatedAt = DateTime.UtcNow, EmailConfirmed = true
        };
        await userManager.CreateAsync(user, "Admin@1234");
        await userManager.AddToRoleAsync(user, "Admin");
    }

    private static async Task SeedEmployeeAsync(UserManager<ApplicationUser> userManager)
    {
        const string email = "employee@spotit.ro";
        if (await userManager.FindByEmailAsync(email) != null) return;

        var user = new ApplicationUser
        {
            UserName = email, Email = email,
            FullName = "Ion Popescu", City = "Oradea",
            CreatedAt = DateTime.UtcNow, EmailConfirmed = true
        };
        await userManager.CreateAsync(user, "Employee@1234");
        await userManager.AddToRoleAsync(user, "CityHallEmployee");
    }

    private static async Task SeedCitizenAsync(UserManager<ApplicationUser> userManager)
    {
        const string email = "citizen@spotit.ro";
        if (await userManager.FindByEmailAsync(email) != null) return;

        var user = new ApplicationUser
        {
            UserName = email, Email = email,
            FullName = "Maria Ionescu", City = "Oradea",
            CreatedAt = DateTime.UtcNow, EmailConfirmed = true
        };
        await userManager.CreateAsync(user, "Citizen@1234");
        await userManager.AddToRoleAsync(user, "Citizen");
    }

    private static async Task SeedCategoriesAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        var categories = new List<Category>
        {
            new() { Name = "Roads",    Description = "Potholes, damaged roads, missing signs", IconUrl = "🛣️" },
            new() { Name = "Lighting", Description = "Street lights, public area lighting",    IconUrl = "💡" },
            new() { Name = "Parks",    Description = "Park maintenance, green areas",           IconUrl = "🌳" },
            new() { Name = "Waste",    Description = "Garbage collection, illegal dumping",     IconUrl = "🗑️" },
            new() { Name = "Water",    Description = "Water supply, sewage issues",             IconUrl = "💧" },
            new() { Name = "Safety",   Description = "Public safety concerns",                  IconUrl = "🚨" }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedMockPostsAsync(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.Posts.AnyAsync()) return;

        var citizen  = await userManager.FindByEmailAsync("citizen@spotit.ro");
        var employee = await userManager.FindByEmailAsync("employee@spotit.ro");
        if (citizen is null || employee is null) return;

        var categories = await context.Categories.ToListAsync();
        int Roads    = categories.First(c => c.Name == "Roads").Id;
        int Lighting = categories.First(c => c.Name == "Lighting").Id;
        int Parks    = categories.First(c => c.Name == "Parks").Id;
        int Waste    = categories.First(c => c.Name == "Waste").Id;
        int Water    = categories.First(c => c.Name == "Water").Id;

        var now = DateTime.UtcNow;

        // ── Posts ────────────────────────────────────────────────────────────
        var post1 = new Post
        {
            Id = Guid.NewGuid(), Title = "Large pothole on Republicii Street",
            Description = "There is a dangerous pothole near the intersection with Magheru Blvd. Cars swerve to avoid it daily.",
            CategoryId = Roads, Status = PostStatus.InProgress,
            AuthorId = citizen.Id, IsAnonymous = false,
            CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-3)
        };

        var post2 = new Post
        {
            Id = Guid.NewGuid(), Title = "Broken street light near Central Park",
            Description = "Three consecutive street lights are out on Calea Aradului. The area is completely dark after 9 PM.",
            CategoryId = Lighting, Status = PostStatus.Resolved,
            AuthorId = citizen.Id, IsAnonymous = false,
            CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-1)
        };

        var post3 = new Post
        {
            Id = Guid.NewGuid(), Title = "Illegal trash dumping in Brătianu Park",
            Description = "Someone left several bags of construction waste near the east entrance of the park.",
            CategoryId = Waste, Status = PostStatus.Pending,
            AuthorId = citizen.Id, IsAnonymous = true,
            CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-2)
        };

        var post4 = new Post
        {
            Id = Guid.NewGuid(), Title = "Water pipe burst on Independenței Ave",
            Description = "Water is flooding the sidewalk. The leak has been visible for 2 days and is getting worse.",
            CategoryId = Water, Status = PostStatus.UnderReview,
            AuthorId = citizen.Id, IsAnonymous = false,
            CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-5)
        };

        var post5 = new Post
        {
            Id = Guid.NewGuid(), Title = "Park benches completely destroyed",
            Description = "All benches in the Libertății Square park have been vandalized. They need immediate replacement.",
            CategoryId = Parks, Status = PostStatus.Rejected,
            AuthorId = citizen.Id, IsAnonymous = false,
            CreatedAt = now.AddDays(-30), UpdatedAt = now.AddDays(-25)
        };

        var post6 = new Post
        {
            Id = Guid.NewGuid(), Title = "Missing road signs at roundabout",
            Description = "The yield signs at the Decebal roundabout are missing. There have been two near-misses already.",
            CategoryId = Roads, Status = PostStatus.Pending,
            AuthorId = citizen.Id, IsAnonymous = false,
            CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1)
        };

        await context.Posts.AddRangeAsync(post1, post2, post3, post4, post5, post6);
        await context.SaveChangesAsync();

        // ── Status History ───────────────────────────────────────────────────
        var history = new List<StatusHistory>
        {
            // post1: Pending → UnderReview → InProgress
            new() { PostId = post1.Id, ChangedByUserId = employee.Id, OldStatus = PostStatus.Pending,      NewStatus = PostStatus.UnderReview, Note = "Report received, inspecting site.",          ChangedAt = now.AddDays(-8) },
            new() { PostId = post1.Id, ChangedByUserId = employee.Id, OldStatus = PostStatus.UnderReview,  NewStatus = PostStatus.InProgress,  Note = "Repair crew scheduled for next week.",       ChangedAt = now.AddDays(-3) },
            // post2: Pending → UnderReview → InProgress → Resolved
            new() { PostId = post2.Id, ChangedByUserId = employee.Id, OldStatus = PostStatus.Pending,      NewStatus = PostStatus.UnderReview, Note = "Forwarded to electricity department.",        ChangedAt = now.AddDays(-18) },
            new() { PostId = post2.Id, ChangedByUserId = employee.Id, OldStatus = PostStatus.UnderReview,  NewStatus = PostStatus.InProgress,  Note = "Technician dispatched.",                     ChangedAt = now.AddDays(-10) },
            new() { PostId = post2.Id, ChangedByUserId = employee.Id, OldStatus = PostStatus.InProgress,   NewStatus = PostStatus.Resolved,    Note = "All three lights replaced and tested.",      ChangedAt = now.AddDays(-1) },
            // post4: Pending → UnderReview
            new() { PostId = post4.Id, ChangedByUserId = employee.Id, OldStatus = PostStatus.Pending,      NewStatus = PostStatus.UnderReview, Note = "Water company notified, awaiting response.",  ChangedAt = now.AddDays(-4) },
            // post5: Pending → Rejected
            new() { PostId = post5.Id, ChangedByUserId = employee.Id, OldStatus = PostStatus.Pending,      NewStatus = PostStatus.Rejected,    Note = "Benches are scheduled for replacement in the Q3 urban renewal budget. Closing duplicate report.", ChangedAt = now.AddDays(-25) },
        };

        await context.StatusHistories.AddRangeAsync(history);

        // ── Comments ─────────────────────────────────────────────────────────
        var comments = new List<Comment>
        {
            new() { PostId = post1.Id, AuthorId = employee.Id,  Content = "We have dispatched an inspection team. The repair is scheduled for next Tuesday.",                           IsOfficialResponse = true,  CreatedAt = now.AddDays(-3) },
            new() { PostId = post1.Id, AuthorId = citizen.Id,   Content = "Thank you! The pothole caused a flat tyre on my car last week.",                                             IsOfficialResponse = false, CreatedAt = now.AddDays(-2) },
            new() { PostId = post2.Id, AuthorId = employee.Id,  Content = "The lights have been repaired. Please let us know if any issues persist.",                                   IsOfficialResponse = true,  CreatedAt = now.AddDays(-1) },
            new() { PostId = post2.Id, AuthorId = citizen.Id,   Content = "Confirmed, everything is working now. Fast response, thank you!",                                            IsOfficialResponse = false, CreatedAt = now.AddDays(-1).AddHours(2) },
            new() { PostId = post4.Id, AuthorId = employee.Id,  Content = "We have contacted the water utility company. They will assess the situation within 48 hours.",               IsOfficialResponse = true,  CreatedAt = now.AddDays(-4) },
            new() { PostId = post5.Id, AuthorId = employee.Id,  Content = "This area is included in the Q3 urban renewal project. New benches will be installed by September.",         IsOfficialResponse = true,  CreatedAt = now.AddDays(-25) },
            new() { PostId = post3.Id, AuthorId = citizen.Id,   Content = "The situation is getting worse. More bags appeared overnight.",                                               IsOfficialResponse = false, CreatedAt = now.AddDays(-1) },
        };

        await context.Comments.AddRangeAsync(comments);

        // ── Likes ────────────────────────────────────────────────────────────
        var likes = new List<Like>
        {
            new() { PostId = post1.Id, UserId = citizen.Id,  CreatedAt = now.AddDays(-9) },
            new() { PostId = post2.Id, UserId = citizen.Id,  CreatedAt = now.AddDays(-19) },
            new() { PostId = post3.Id, UserId = citizen.Id,  CreatedAt = now.AddDays(-2) },
            new() { PostId = post6.Id, UserId = citizen.Id,  CreatedAt = now.AddDays(-1) },
        };

        await context.Likes.AddRangeAsync(likes);
        await context.SaveChangesAsync();
    }
}
