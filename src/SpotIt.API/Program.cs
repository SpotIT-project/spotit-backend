using SpotIt.API.Middleware;
using SpotIt.API.Services;
using SpotIt.Application.Extensions;
using SpotIt.Application.Interfaces;
using SpotIt.Infrastructure.Data.Seed;
using SpotIt.Infrastructure.Extensions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
var builder = WebApplication.CreateBuilder(args);

if (Directory.Exists("/run/secrets"))
{
    if (File.Exists("/run/secrets/connection_string"))
        builder.Configuration["ConnectionStrings:DefaultConnection"] = File.ReadAllText("/run/secrets/connection_string").Trim();
    if (File.Exists("/run/secrets/jwt_key"))
        builder.Configuration["Jwt:SecretKey"] = File.ReadAllText("/run/secrets/jwt_key").Trim();
}



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader()));

builder.Services.AddIdentityServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddRateLimiter(options =>
{
    var permitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Auth:PermitLimit") ?? 10;
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = permitLimit;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var healthConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var healthChecksBuilder = builder.Services.AddHealthChecks();
if (!string.IsNullOrEmpty(healthConnectionString))
{
    healthChecksBuilder.AddNpgSql(
        healthConnectionString,
        name: "postgres",
        tags: ["db", "ready"]);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

await DatabaseSeeder.SeedAsync(app.Services);

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

public partial class Program {}