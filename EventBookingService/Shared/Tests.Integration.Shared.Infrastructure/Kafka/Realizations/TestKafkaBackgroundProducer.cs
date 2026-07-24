using System.Text.Json;

using Confluent.Kafka;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Infrastructure.Kafka;
using Shared.Infrastructure.Kafka.Settings;

namespace Tests.Integration.Shared.Infrastructure.Kafka.Realizations;

internal class TestKafkaBackgroundProducer : KafkaBackgroundProducer<TestMessage, TestDbContext>
{
    /// <summary>
    /// ВАЖНО. Запускать тестовый метод с токеном из этого источника, чтобы выйти
    /// </summary>
    public CancellationTokenSource TokenSource { get; init; }

    public TestKafkaBackgroundProducer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> kafkaSettings,
        ILoggerFactory loggerFactory,
        string topic,
        CancellationTokenSource tokenSource)
        : base(scopeFactory, kafkaSettings, loggerFactory, topic)
    {
        TokenSource = tokenSource;
    }

    public override Message<string, string> CreateTopicMessage(TestMessage message)
    {
        return new()
        {
            Key = message.Id.ToString(),
            Value = JsonSerializer.Serialize(message)
        };
    }

    public override async Task<List<TestMessage>> GetMessagesAsync(TestDbContext dbContext, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var messages = await dbContext.Messages.ToListAsync(stoppingToken);
        if (messages.Count < 1)
        {
            TokenSource.Cancel();
            stoppingToken.ThrowIfCancellationRequested();
        }

        return messages;
    }
}
