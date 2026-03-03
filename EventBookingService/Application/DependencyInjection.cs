using EventBookingService.Application.Events;
using EventBookingService.Application.Events.Implementation;

namespace EventBookingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventRepository, MemoryEventRepository>();

        return services;
    }
}

