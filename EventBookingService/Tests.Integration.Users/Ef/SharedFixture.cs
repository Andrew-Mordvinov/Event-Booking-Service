using Application.Users.Infrastructure;
using Infrastructure.Users.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Tests.Integration.Users.Ef;

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

        services.AddDbContext<UsersDbContext>(options => options
            .UseNpgsql(Container.GetConnectionString()));

        services.AddScoped<IUserRepository, EfUserRepository>();
        //services.AddScoped<IUnitOfWork, EfUnitOfWork>();

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

        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
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
