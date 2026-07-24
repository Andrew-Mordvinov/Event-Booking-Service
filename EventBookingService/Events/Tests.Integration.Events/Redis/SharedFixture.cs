using Application.Events.Infrastructure;

using Infrastructure.Events.Redis;
using Infrastructure.Events.Redis.Serializer;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

using Testcontainers.Redis;

namespace Tests.Integration.Events.Redis
{
    [CollectionDefinition(RedisTests)]
    public class SharedFixture : IAsyncLifetime, ICollectionFixture<SharedFixture>
    {
        public const string RedisTests = "RedisTests";
        public RedisContainer Container { get; private set; }
        public ServiceProvider ServiceProvider { get; private set; }

        public SharedFixture()
        {
            Container = new RedisBuilder("redis:8.8.0-alpine")
                .Build();
        }

        public async ValueTask InitializeAsync()
        {
            await Container.StartAsync(TestContext.Current.CancellationToken);

            var services = new ServiceCollection();

            services.AddScoped<IEventCache, RedisEventCache>();
            services.AddScoped<ICacheEventSerializer, CacheEventSerializer>();

            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(Container.GetConnectionString(), t => t.AllowAdmin = true)
            );

            services.AddSingleton(sp =>
            {
                return Options.Create(new TTLSettings { SingleEventMsec = 100, TopSalesSec = 1 });
            });

            ServiceProvider = services.BuildServiceProvider();
        }

        public async ValueTask DisposeAsync()
        {
            await Container.DisposeAsync();
        }

        public async Task PrepareRedisAsync()
        {
            var multiplexer = ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
            var endpoint = multiplexer.GetEndPoints().FirstOrDefault() ?? throw new Exception("Не получен эндпоинт в Redis для сброса между тестами");

            await multiplexer.GetServer(endpoint).FlushAllDatabasesAsync();
        }
    }

}
