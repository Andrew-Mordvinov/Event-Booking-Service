using Application.Events.Infrastructure;
using Infrastructure.Events.Ef;
using Infrastructure.Events.Ef.ExceptionPatterns;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Ef;
using Shared.Infrastructure.Ef.ExceptionPatterns;
using Shared.Interfaces.Infrastructure;

namespace Presentation.Events.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfigurationManager configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContextPool<EventsDbContext>(options => options
            .UseNpgsql(connectionString)
            .LogTo(message => Serilog.Log.Information(message), LogLevel.Error)
            .EnableDetailedErrors(), 100);

        //services.AddOptionsWithValidateOnStart<JwtSettings>()
        //    .Bind(configuration.GetSection("JwtSettings"))
        //    .ValidateDataAnnotations();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            //var settings = configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new Exception("Couldn't load settings for jwt token");

            //options.TokenValidationParameters = new TokenValidationParameters
            //{
            //    ValidateIssuer = true,
            //    ValidIssuer = settings.Issuer,

            //    ValidateAudience = true,
            //    ValidAudience = settings.Audience,

            //    ValidateLifetime = true,

            //    ValidateIssuerSigningKey = true,
            //    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
            //};

            // Опция, для того чтобы система сама не делала маппинг sub на какое-то длинное поле http-бла-бла-бла
            options.MapInboundClaims = false;
        });

        services.AddAuthorization();

        services.AddScoped<IUnitOfWork, EfUnitOfWork<EventsDbContext>>();
        services.AddScoped<IEventRepository, EfEventRepository>();
        services.AddSingleton<IExceptionPatternsProvider, EventsExceptionPatternsProvider>();

        return services;
    }
}

