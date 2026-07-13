using System.Text.Json;

using Confluent.Kafka;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Tests.Integration.Shared.Infrastructure.Kafka.Realizations;

namespace Tests.Integration.Shared.Infrastructure.Kafka;

[Collection(SharedFixture.KafkaTests)]
public class KafkaBackgroundConsumerTests(SharedFixture sharedFixture) : IAsyncLifetime
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

    private TestKafkaBackgroundConsumer CreateKafkaConsumer()
    {
        return new TestKafkaBackgroundConsumer
        (
            _sharedFixture.KafkaSettings!,
            _sharedFixture.KafkaConsumerSettings!,
            _sharedFixture.ServiceProvider.GetRequiredService<ILoggerFactory>(),
            _sharedFixture.CurrentTestTopicName
        );
    }

    // object чтобы можно было подсунуть кривое сообщение
    private async Task SendMessages(object[] messages)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _sharedFixture.KafkaSettings?.Value.BootstrapServer
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        // Отправляем сообщение
        foreach (var message in messages)
        {
            var result = await producer.ProduceAsync(_sharedFixture.CurrentTestTopicName, new Message<string, string>
            {
                Key = "key",
                Value = JsonSerializer.Serialize(message)
            }, TestContext.Current.CancellationToken);

            if (result.Status is not PersistenceStatus.Persisted)
            {
                throw new Exception("Ошибка при попытке отправить сообщение в Kafka");
            }
        }
    }

    #endregion

    #region PrepareAndStartAsync

    [Fact]
    public async Task PrepareAndStartAsync_CorrectEventsList_SavedSuccessfully()
    {
        // Arrange
        var background = CreateKafkaConsumer();

        List<TestMessage> sendedMessages =
        [
            new TestMessage { Id = Guid.NewGuid(), Payload = "SomeText", TestInt = 5 },
            new TestMessage { Id = Guid.NewGuid(), Payload = "Another text", TestInt = -5 },
            new TestMessage { Id = Guid.NewGuid(), Payload = "JNJJ", TestInt = 90035 },
            new TestMessage { Id = Guid.NewGuid(), Payload = "Vs ls cs 1234", TestInt = 0 },
        ];
        var resultMessages = new List<TestMessage>();

        var tokenSource = new CancellationTokenSource();

        EventHandler<MessageProcessedEventArgs> handler = (sender, e) =>
        {
            resultMessages.Add(e.Message);
            if (resultMessages.Count == sendedMessages.Count)
            {
                tokenSource.Cancel();
            }
        };
        background.MessageProcessed += handler;

        await SendMessages([.. sendedMessages]);
        // Теперь обратный отсчет на прочтение, на случай если что-то пойдет не так и метод зависнет
        tokenSource.CancelAfter(TimeSpan.FromSeconds(5));

        // Act
        await background.PrepareAndStartAsync(tokenSource.Token);

        // Assert
        resultMessages.Should().BeEquivalentTo(sendedMessages);
    }

    [Fact]
    public async Task PrepareAndStartAsync_SomeEventIncorrect_SuccessfullySentAnother()
    {
        // Arrange
        var background = CreateKafkaConsumer();

        List<object> sendedMessages =
        [
            new TestMessage { Id = Guid.NewGuid(), Payload = "SomeText", TestInt = 5, TestDate = DateTime.UtcNow },
            new object(),
            new SomeWrongMessage(Guid.NewGuid(), "Message", 0, DateTime.UtcNow),
            new AnotherWrongMessage(Guid.NewGuid(), 6, DateTime.UtcNow),
            new TestMessage { Id = Guid.NewGuid(), Payload = "JNJJ", TestInt = 90035 },
            new TestMessage { Id = Guid.NewGuid(), Payload = "Vs ls cs 1234", TestInt = 0 },
        ];
        var resultMessages = new List<TestMessage>();

        var tokenSource = new CancellationTokenSource();

        EventHandler<MessageProcessedEventArgs> handler = (sender, e) =>
        {
            resultMessages.Add(e.Message);
            if (resultMessages.Count == sendedMessages.Count - 3)
            {
                tokenSource.Cancel();
            }
        };
        background.MessageProcessed += handler;

        await SendMessages([.. sendedMessages]);
        // Теперь обратный отсчет на прочтение, на случай если что-то пойдет не так и метод зависнет
        tokenSource.CancelAfter(TimeSpan.FromSeconds(10));

        // Act
        await background.PrepareAndStartAsync(tokenSource.Token);

        // Assert
        resultMessages.Should().BeEquivalentTo(sendedMessages.OfType<TestMessage>());
    }

    // TODO подумать еще над тестами

    #endregion
}
