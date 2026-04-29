using SpotIt.API.Middleware;
using SpotIt.API.Services;
using SpotIt.Application.Extensions;
using SpotIt.Application.Interfaces;
using SpotIt.Infrastructure.Data.Seed;
using SpotIt.Infrastructure.Extensions;
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

builder.Services.AddIdentityServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await DatabaseSeeder.SeedAsync(app.Services);
app.Run();

public partial class Program {}