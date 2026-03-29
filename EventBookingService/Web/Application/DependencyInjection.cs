using EventBookingService.Application.Bookings;
using EventBookingService.Application.Bookings.Implementation;
using EventBookingService.Application.Events;
using EventBookingService.Application.Events.Implementation;
using EventBookingService.Common.Storage;
using EventBookingService.Models.Bookings;
using EventBookingService.Models.Events;

namespace EventBookingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddKeyedSingleton<IStorage<Event>, ListStorage<Event>>("Mem");
        services.AddKeyedSingleton<IStorage<Booking>, ListStorage<Booking>>("Mem");

        return services;
    }
}

