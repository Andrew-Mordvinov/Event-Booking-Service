
using Application.Bookings.Implementation;
using Application.Bookings.Interfaces;

namespace Presentation.Bookings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}

