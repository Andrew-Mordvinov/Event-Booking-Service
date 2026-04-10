using EventBookingService.Infrastructure.Bookings;

namespace EventBookingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfigurationManager configuration) =>
        services.AddHostedService<BookingManagerBackgroundService>();
}

