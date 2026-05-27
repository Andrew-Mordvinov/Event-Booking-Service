using Application.Implementation;
using Application.Infrastructure;
using Application.Infrastructure.Common;
using Application.Interfaces;
using Infrastructure.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Tests.Integration.Ef;

[CollectionDefinition("PostgresTests")]
public class SharedFixture : IAsyncLifetime, ICollectionFixture<SharedFixture>
{
    public PostgreSqlContainer Container { get; private set; }
    public ServiceProvider ServiceProvider { get; private set; }

    public SharedFixture()
    {
        Container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(Container.GetConnectionString()));

        services.AddScoped<IEventRepository, EfEventRepository>();
        services.AddScoped<IBookingRepository, EfBookingRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IBookingService, BookingService>();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

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

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
