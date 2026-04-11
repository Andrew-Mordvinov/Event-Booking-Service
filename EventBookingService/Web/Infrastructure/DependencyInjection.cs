using Web.Infrastructure.Bookings;

namespace Web.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfigurationManager configuration) =>
        services.AddHostedService<BookingManagerBackgroundService>();
}

