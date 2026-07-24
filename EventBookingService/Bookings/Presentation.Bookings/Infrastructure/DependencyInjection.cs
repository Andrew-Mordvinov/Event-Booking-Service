using System.Text;

using Application.Bookings.Infrastructure;
using Application.Bookings.Settings;

using Contracts.Settings;

using Infrastructure.Bookings.Background;
using Infrastructure.Bookings.Ef;
using Infrastructure.Bookings.Ef.ExceptionPatterns;
using Infrastructure.Bookings.Http;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Abstract.ExceptionPatterns;
using Shared.Infrastructure.Ef;
using Shared.Infrastructure.Kafka.Settings;

namespace Presentation.Bookings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfigurationManager configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContextPool<BookingsDbContext>(options => options
            .UseNpgsql(connectionString)
            .EnableDetailedErrors(), 100);

        services.AddOptionsWithValidateOnStart<BookingSettings>()
            .Bind(configuration.GetSection("BookingSettings"))
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<KafkaSettings>()
            .Bind(configuration.GetSection("KafkaSettings"))
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

        services.AddScoped<IUnitOfWork, EfUnitOfWork<BookingsDbContext>>();
        services.AddScoped<IBookingRepository, EfBookingRepository>();
        services.AddScoped<IBookingEventsProducer, EfBookingEventsProducer>();
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddSingleton<IExceptionPatternsProvider, BookingsExceptionPatternsProvider>();

        services.AddHttpContextAccessor();

        services.AddHostedService<BookingConfirmationBackgroundService>();
        services.AddHostedService<BookingEventSenderBackgroundService>();

        return services;
    }
}

