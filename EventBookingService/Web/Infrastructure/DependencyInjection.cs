using DataAccess.EF;

using Microsoft.EntityFrameworkCore;

using Web.Infrastructure.Bookings;

namespace Web.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfigurationManager configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContextPool<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .LogTo(message => Serilog.Log.Information(message), LogLevel.Debug)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging(), 100);

        services.AddHostedService<BookingManagerBackgroundService>();

        return services;
    }
}

