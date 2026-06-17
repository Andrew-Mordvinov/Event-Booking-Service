
using System.IdentityModel.Tokens.Jwt;
using System.Text;

using Application.DTO.Users;
using Application.Infrastructure;
using Application.Infrastructure.Common;
using Application.Settings;
using Application.Validations;

using Infrastructure.Ef;
using Infrastructure.Http;
using Infrastructure.Security;
using Infrastructure.Settings;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Presentation.Infrastructure.Bookings;

namespace Presentation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfigurationManager configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");

        services.AddDbContextPool<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .LogTo(message => Serilog.Log.Information(message), LogLevel.Error)
            .EnableDetailedErrors(), 100);

        services.AddOptionsWithValidateOnStart<BookingSettings>()
            .Bind(configuration.GetSection("BookingSettings"))
            .ValidateDataAnnotations();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("JwtSettings"));

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

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IEventRepository, EfEventRepository>();
        services.AddScoped<IBookingRepository, EfBookingRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordManager, DefautPasswordManager>();
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddScoped<IValidator<RegisterUserRequest>, RegistrationRequestValidator>();

        services.AddHttpContextAccessor();

        services.AddHostedService<BookingManagerBackgroundService>();

        return services;
    }
}

