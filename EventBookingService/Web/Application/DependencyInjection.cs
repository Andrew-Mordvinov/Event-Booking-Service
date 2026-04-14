using Bookings.Models;
using Bookings.Service;
using Bookings.Service.Implementation;

using DataAccess.Storage;

using Events.Models;
using Events.Service;
using Events.Service.Implementation;

using Shared.Locking;

namespace Web.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddKeyedSingleton<IStorage<Event>, DictionaryStorage<Event>>("Mem");
        services.AddKeyedSingleton<IStorage<Booking>, DictionaryStorage<Booking>>("Mem");
        services.AddKeyedSingleton<ISemaphoreGetter, SemaphoreGetter>("CreateBooking");
        services.AddKeyedSingleton<ISemaphoreGetter, SemaphoreGetter>("ProcessBooking");

        return services;
    }
}

