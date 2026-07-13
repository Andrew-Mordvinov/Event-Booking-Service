using Confluent.Kafka;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Infrastructure.Kafka.Settings;

namespace Shared.Infrastructure.Kafka;

/// <summary>
/// Абстрактный продюсер для кафки, достающий сообщения из аутбокса и отправляющий их.
/// Требуется определить механизм получения сообщений (соединение с базой уже открыто),
/// которые будут автоматически удалены из аутбокса при отправке, а также способ создания самого сообщения
/// </summary>
/// <typeparam name="TMessage">Тип сообщения, которое достается из аутбокса</typeparam>
/// <typeparam name="TDbContext">Контекст для работы с БД</typeparam>
public abstract class KafkaBackgroundProducer<TMessage, TDbContext> : BackgroundService where TDbContext : DbContext
{
    protected readonly IServiceScopeFactory _scopeFactory;
    protected readonly IOptions<KafkaSettings> _kafkaSettings;
    protected readonly ILogger _logger;
    protected readonly string _topic;

    public KafkaBackgroundProducer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaSettings> kafkaSettings,
        ILoggerFactory loggerFactory,
        string topic)
    {
        _scopeFactory = scopeFactory;
        _kafkaSettings = kafkaSettings;
        _logger = loggerFactory.CreateLogger(GetType());
        _topic = topic;
    }

    public abstract Task<List<TMessage>> GetMessagesAsync(TDbContext dbContext, CancellationToken stoppingToken);

    public abstract Message<string, string> CreateTopicMessage(TMessage message);

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Сервис {GetType().Name} начал работу");

        await PrepareAndStartAsync(stoppingToken);

        _logger.LogInformation($"Сервис {GetType().Name} остановлен");
    }

    internal async Task PrepareAndStartAsync(CancellationToken stoppingToken)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaSettings.Value.BootstrapServer,
            Acks = Acks.All
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProduceMessages(producer, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "При обработке сообщений возникло исключение");
            }
        }
    }

    internal async Task ProduceMessages(IProducer<string, string> producer, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var messages = await GetMessagesAsync(dbContext, stoppingToken);

        _logger.LogInformation("Сообщений для отправки: {Count}", messages.Count);

        if (messages.Count < 1)
        {
            await Task.Delay(1000, stoppingToken);
            return;
        }
        // TODO можно организовать группировку отправлять группы параллельно
        foreach (var message in messages)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (message is null)
            {
                continue;
            }

            try
            {
                var result = await producer.ProduceAsync(_topic, CreateTopicMessage(message), stoppingToken);

                if (result.Status is not PersistenceStatus.Persisted)
                {
                    _logger.LogError("Сообщение не доставлено брокеру {@Message}", result.Value);
                    continue;
                }

                dbContext.Remove(message);
            }
            catch (Exception ex)
            {
                // TODO обработчик для ошибки, возможно инкремент числа попыток через переопределяемый метод
                _logger.LogError(ex, "Возникло исключение при отправке сообщения {@Message}", message);
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
