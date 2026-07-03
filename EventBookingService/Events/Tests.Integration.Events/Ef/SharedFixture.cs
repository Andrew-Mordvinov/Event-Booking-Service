using Application.Events.Infrastructure;
using Infrastructure.Events.Ef;
using Infrastructure.Events.Ef.ExceptionPatterns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Abstract.ExceptionPatterns;
using Shared.Infrastructure.Ef;
using Testcontainers.PostgreSql;

namespace Tests.Integration.Events.Ef;

[CollectionDefinition("PostgresTests")]
public class SharedFixture : IAsyncLifetime, ICollectionFixture<SharedFixture>
{
    public PostgreSqlContainer Container { get; private set; }
    public ServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; private set; }

    public SharedFixture()
    {
        Container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.test.json", optional: true, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection(); ;

        services.AddDbContext<EventsDbContext>(options => options
            .UseNpgsql(Container.GetConnectionString()));

        services.AddScoped<IEventRepository, EfEventRepository>();
        services.AddScoped<IBookingEventsInboxRepository, EfBookingEventsInboxRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork<EventsDbContext>>();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IExceptionPatternsProvider, EventsExceptionPatternsProvider>();

        ServiceProvider = services.BuildServiceProvider();
    }
    public async ValueTask InitializeAsync()
    {
        await Container.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync();
    }

    public async Task PrepareTestDbAsync(CancellationToken token = default)
    {
        // Отключили все пулы, если есть
        NpgsqlConnection.ClearAllPools();

        using var scope = ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        // Удалили БД
        await db.Database.EnsureDeletedAsync(token);
        // Создание БД
        await db.Database.MigrateAsync(token);
    }

    public static DateTimeOffset TrimToMicroseconds(DateTimeOffset dto)
    {
        var ticksToRemove = dto.Ticks % TimeSpan.TicksPerMicrosecond;
        return dto.AddTicks(-ticksToRemove);
    }
}
