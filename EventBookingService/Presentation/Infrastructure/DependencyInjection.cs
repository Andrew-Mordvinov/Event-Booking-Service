
using Application.Infrastructure;
using Application.Infrastructure.Common;
using Application.Settings;

using Infrastructure.Ef;

using Microsoft.EntityFrameworkCore;

using Presentation.Infrastructure.Bookings;

namespace Presentation.Infrastructure;

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
            .LogTo(message => Serilog.Log.Information(message), LogLevel.Error)
            .EnableDetailedErrors(), 100);

        services.AddOptions<BookingSettings>()
            .Bind(configuration.GetSection("BookingSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart(); 

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IEventRepository, EfEventRepository>();
        services.AddScoped<IBookingRepository, EfBookingRepository>();

        services.AddHostedService<BookingManagerBackgroundService>();

        return services;
    }
}

