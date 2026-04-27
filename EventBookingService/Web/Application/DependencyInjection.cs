using Bookings.Service;
using Bookings.Service.Implementation;

using DataAccess.Abstract;
using DataAccess.Abstract.Common;
using DataAccess.EF;

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
        services.AddScoped<IEventRepository, EfEventRepository>();
        services.AddScoped<IBookingRepository, EfBookingRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddDbContext<AppDbContext>();
        services.AddKeyedSingleton<ISemaphoreGetter, SemaphoreGetter>("CreateBooking");
        services.AddKeyedSingleton<ISemaphoreGetter, SemaphoreGetter>("ProcessBooking");

        return services;
    }
}

