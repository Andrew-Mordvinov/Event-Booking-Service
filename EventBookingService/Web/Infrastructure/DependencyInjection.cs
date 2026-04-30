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

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddHostedService<BookingManagerBackgroundService>();

        return services;
    }
}

