using EventBookingService.Application.Events;
using EventBookingService.Application.Events.Implementation;
using EventBookingService.Common.Storage;
using EventBookingService.Models.Events;

namespace EventBookingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddKeyedSingleton<IStorage<Event>, ListStorage<Event>>("Mem");

        return services;
    }
}

