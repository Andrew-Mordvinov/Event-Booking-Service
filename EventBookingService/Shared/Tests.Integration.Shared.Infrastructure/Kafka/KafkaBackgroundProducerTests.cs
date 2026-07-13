using System.Text.Json;

using Confluent.Kafka;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Tests.Integration.Shared.Infrastructure.Kafka.Realizations;

namespace Tests.Integration.Shared.Infrastructure.Kafka;

[Collection(SharedFixture.KafkaTests)]
public class KafkaBackgroundProducerTests(SharedFixture sharedFixture) : IAsyncLifetime
{
    private readonly SharedFixture _sharedFixture = sharedFixture;

    public async ValueTask InitializeAsync()
    {
        await _sharedFixture.PrepareTopicsAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    #region Helping

    private TestKafkaBackgroundProducer CreateKafkaProducer()
    {
        return new TestKafkaBackgroundProducer
        (
            _sharedFixture.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            _sharedFixture.KafkaSettings!,
            _sharedFixture.ServiceProvider.GetRequiredService<ILoggerFactory>(),
            _sharedFixture.CurrentTestTopicName,
            new CancellationTokenSource()
        );
    }

    private List<TestMessage?> GetMessages(int count)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _sharedFixture.KafkaSettings!.Value.BootstrapServer,
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_sharedFixture.CurrentTestTopicName);

        var tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(TimeSpan.FromSeconds(10));

        var result = new List<TestMessage?>();
        while (!tokenSource.Token.IsCancellationRequested && count > 0)
        {
            try
            {
                var consumeResult = consumer.Consume(tokenSource.Token);

                var message = JsonSerializer.Deserialize<TestMessage>(consumeResult.Message.Value);
                result.Add(message);
                count--;
            }
            catch
            {

            }
        }

        return result;
    }

    private async Task SetMessagesToStorage(List<TestMessage> messages)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        db.Messages.AddRange(messages);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion

    #region PrepareAndStartAsync

    [Fact]
    public async Task PrepareAndStartAsync_CorrectEventsList_SuccessfullySent()
    {
        // Arrange
        var producer = CreateKafkaProducer();
        var messagesToSend = new List<TestMessage>
        {
            new TestMessage { Id = Guid.NewGuid(), Payload = "Payload", TestInt = 5 },
            new TestMessage { Id = Guid.NewGuid(), Payload = "Another payload", TestInt = -5 },
            new TestMessage { Id = Guid.NewGuid(), Payload = "JNJJ", TestInt = 90035 },
            new TestMessage { Id = Guid.NewGuid(), Payload = "Vs ls cs 1234", TestInt = -97 },
        };

        await SetMessagesToStorage(messagesToSend);

        // Act
        await producer.PrepareAndStartAsync(producer.TokenSource.Token);

        // Assert
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var hasMessages = await db.Messages.AnyAsync(TestContext.Current.CancellationToken);

        hasMessages.Should().BeFalse();

        GetMessages(messagesToSend.Count).Should().BeEquivalentTo(messagesToSend);
    }

    // TODO подумать еще над тестами

    #endregion
}
