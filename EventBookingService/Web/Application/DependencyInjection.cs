using Bookings.Models;
using Bookings.Service;
using Bookings.Service.Implementation;

using DataAccess.Storage;

using Events.Models;
using Events.Service;
using Events.Service.Implementation;

namespace Web.Application;

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

