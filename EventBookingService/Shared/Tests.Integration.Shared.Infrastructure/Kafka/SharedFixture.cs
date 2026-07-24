using Confluent.Kafka;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Npgsql;

using Shared.Infrastructure.Kafka.Settings;

using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

using Tests.Integration.Shared.Infrastructure.Kafka.Realizations;

namespace Tests.Integration.Shared.Infrastructure.Kafka;

[CollectionDefinition(KafkaTests)]
public class SharedFixture : IAsyncLifetime, ICollectionFixture<SharedFixture>
{
    public const string KafkaTests = "KafkaTests";
    public string CurrentTestTopicName { get; private set; } = string.Empty;

    public KafkaContainer KafkaContainer { get; private set; }
    public PostgreSqlContainer PgContainer { get; private set; }
    public ServiceProvider ServiceProvider { get; private set; }

    public IOptions<KafkaSettings>? KafkaSettings { get; private set; }
    public IOptions<KafkaConsumerSettings>? KafkaConsumerSettings { get; private set; }

    public SharedFixture()
    {
        KafkaContainer = new KafkaBuilder("confluentinc/cp-kafka:7.6.0")
            .WithKRaft()
            .WithCleanUp(true)
            .WithAutoRemove(true)
            .Build();

        PgContainer = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options
            .UseNpgsql(PgContainer.GetConnectionString()));

        services.AddLogging();

        ServiceProvider = services.BuildServiceProvider();
    }

    public async ValueTask InitializeAsync()
    {
        await KafkaContainer.StartAsync(TestContext.Current.CancellationToken);
        await PgContainer.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await KafkaContainer.DisposeAsync();
        await PgContainer.DisposeAsync();
    }

    public async Task PrepareTopicsAsync(CancellationToken token = default)
    {
        // Не чистим топики, так как попытка работать с одним топиком и группой работает плохо, то одна ошибка, то другая
        KafkaSettings = Options.Create(new KafkaSettings { BootstrapServer = KafkaContainer.GetBootstrapAddress() });
        KafkaConsumerSettings = Options.Create(new KafkaConsumerSettings { GroupId = Guid.NewGuid().ToString(), AutoOffsetReset = AutoOffsetReset.Earliest });
        CurrentTestTopicName = Guid.NewGuid().ToString();

        // Отключили все пулы, если есть
        NpgsqlConnection.ClearAllPools();

        using var scope = ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        // Удалили БД
        await db.Database.EnsureDeletedAsync(token);
        // Создание БД
        await db.Database.EnsureCreatedAsync(token);
    }
}
