using Bookings.Service;
using Bookings.Service.Implementation;
using DataAccess.Abstract;
using DataAccess.Abstract.Common;
using DataAccess.Abstract.Enums;
using DataAccess.EF;
using Entities.Bookings;
using Entities.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Exceptions;
using Testcontainers.PostgreSql;

namespace Tests.Integration.Bookings;

/// <summary>
/// Тесты сервиса бронирований, которые нет смысла проводить в памяти с моками
/// </summary>
public class BookingServiceTests
{
    #region Helping

    private sealed class PostgresFixture : IAsyncLifetime
    {
        public PostgreSqlContainer Container { get; }

        public string ConnectionString => Container.GetConnectionString();

        public PostgresFixture()
        {
            Container = new PostgreSqlBuilder("postgres:18")
                .WithDatabase("testdb")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
        }

        public async ValueTask InitializeAsync()
        {
            await Container.StartAsync(TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await Container.DisposeAsync();
        }
    }

    private sealed class ServiceContainerFixture : IAsyncLifetime
    {
        public ServiceProvider ServiceProvider { get; private set; }

        public ServiceContainerFixture(string connectionString)
        {
            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options => options
                .UseNpgsql(connectionString));

            services.AddScoped<IEventRepository, EfEventRepository>();
            services.AddScoped<IBookingRepository, EfBookingRepository>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
            services.AddScoped<IBookingService, BookingService>();

            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            ServiceProvider = services.BuildServiceProvider();
        }

        public ValueTask InitializeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await ServiceProvider.DisposeAsync();
        }
    }

    private async Task PrepareTestDbAsync(ServiceContainerFixture containerFixture, Event @event, CancellationToken token = default)
    {
        using var scope = containerFixture.ServiceProvider.CreateScope();
        // Создание БД
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync(token);
        // Добавление тестового события
        db.Events.Add(@event);
        await db.SaveChangesAsync(token);
    }

    private async Task<Event?> GetEventAsync(ServiceContainerFixture containerFixture, Guid id, CancellationToken token = default)
    {
        using var scope = containerFixture.ServiceProvider.CreateScope();

        var repo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        return await repo.GetByIdAsync(id, GetMode.Readonly, token);
    }

    #endregion

    [Fact]
    public async Task CreateBookingAsync_ParallelBookMoreThanSeats_NoOverbookingOccurs()
    {
        // Arrange
        await using var pgsql = new PostgresFixture();
        await pgsql.InitializeAsync();

        await using var container = new ServiceContainerFixture(pgsql.ConnectionString);
        await container.InitializeAsync();

        var (@event, errors) = Event.TryCreate(Guid.NewGuid(), "Test title", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), 5);

        if (@event is null)
        {
            Assert.Fail("Не удалось создать событие: " + string.Join(Environment.NewLine, errors));
        }

        await PrepareTestDbAsync(container, @event, TestContext.Current.CancellationToken);

        // Act
        var arrayOfRequests = new Task<Booking>[20];

        for (int i = 0; i < 20; i++)
        {
            arrayOfRequests[i] = Task.Run(async () =>
            {
                using var scope = container.ServiceProvider.CreateScope();

                var service = scope.ServiceProvider.GetRequiredService<IBookingService>();

                return await service.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);
            });
        }

        try
        {
            // Игнор исключения здесь, проверяем ниже
            await Task.WhenAll(arrayOfRequests);
        }
        catch
        {

        }

        // Assert
        var storedEvent = await GetEventAsync(container, @event.Id, TestContext.Current.CancellationToken);

        storedEvent.Should().NotBeNull();
        storedEvent.AvailableSeats.Should().Be(0);
        arrayOfRequests.Count(t => t.Status == TaskStatus.RanToCompletion && t.Result is not null).Should().Be(5);
        arrayOfRequests
            .Where(t => t.Status == TaskStatus.RanToCompletion && t.Result is not null)
            .Select(t => t.Result.Id)
            .Distinct().Count()
            .Should().Be(5);
        arrayOfRequests.Count(t => t.Status == TaskStatus.Faulted && t.Exception?.InnerException is ConflictException).Should().Be(15);
    }
}
