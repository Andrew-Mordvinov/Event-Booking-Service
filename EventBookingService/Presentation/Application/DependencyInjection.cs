using Application.Implementation;
using Application.Infrastructure;
using Application.Infrastructure.Common;
using Application.Interfaces;

using Infrastructure.Ef;

namespace Presentation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IEventRepository, EfEventRepository>();
        services.AddScoped<IBookingRepository, EfBookingRepository>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}

