
using Application.Events.Implementation;
using Application.Events.Interfaces;

namespace Presentation.Events.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}

