
using Application.Users.Implementation;
using Application.Users.Interfaces;

namespace Presentation.Users.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}

