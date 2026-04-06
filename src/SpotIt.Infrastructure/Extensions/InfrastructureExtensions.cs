using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpotIt.Domain.Interfaces;
using SpotIt.Infrastructure.Data;
using SpotIt.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotIt.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure (this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        options
        .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
