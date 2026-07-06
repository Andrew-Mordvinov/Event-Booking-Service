using System.Text;

using Application.Events.Infrastructure;

using Contracts.Settings;

using Infrastructure.Events.Background;
using Infrastructure.Events.Ef;
using Infrastructure.Events.Ef.ExceptionPatterns;
using Infrastructure.Events.Settings;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Abstract.ExceptionPatterns;
using Shared.Infrastructure.Ef;

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

        services.AddOptionsWithValidateOnStart<KafkaSettings>()
            .Bind(configuration.GetSection("KafkaSettings"))
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<KafkaConsumerSettings>()
            .Bind(configuration.GetSection("KafkaConsumerSettings"))
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<JwtSettings>()
            .Bind(configuration.GetSection("JwtSettings"))
            .ValidateDataAnnotations();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var settings = configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new Exception("Couldn't load settings for jwt token");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,

                ValidateAudience = true,
                ValidAudience = settings.Audience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
            };

            // Опция, для того чтобы система сама не делала маппинг sub на какое-то длинное поле http-бла-бла-бла
            options.MapInboundClaims = false;
        });

        services.AddAuthorization();

        services.AddScoped<IUnitOfWork, EfUnitOfWork<EventsDbContext>>();
        services.AddScoped<IEventRepository, EfEventRepository>();
        services.AddScoped<IBookingEventsInboxRepository, EfBookingEventsInboxRepository>();
        services.AddSingleton<IExceptionPatternsProvider, EventsExceptionPatternsProvider>();

        services.AddHostedService<BookEventBackgroundService>();

        return services;
    }
}

