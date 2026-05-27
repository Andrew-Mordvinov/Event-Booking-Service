using Application.Infrastructure;
using Application.Infrastructure.Enums;
using Application.Interfaces;
using Domain.Bookings;
using Domain.Events;
using Domain.Exceptions;
using FluentAssertions;
using Infrastructure.Ef;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.Ef.Bookings;

/// <summary>
/// Тесты сервиса бронирований
/// </summary>
[Collection("PostgresTests")]
public class BookingServiceTests(SharedFixture sharedFixture) : IAsyncLifetime
{
    private readonly SharedFixture _sharedFixture = sharedFixture;

    public async ValueTask InitializeAsync()
    {
        await _sharedFixture.PrepareTestDbAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private async Task<Event?> GetEventAsync(Guid id, CancellationToken token = default)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        return await repo.GetByIdAsync(id, GetMode.Readonly, token);
    }

    [Fact]
    public async Task CreateBookingAsync_ParallelBookMoreThanSeats_NoOverbookingOccurs()
    {
        // Arrange
        var (@event, errors) = Event.TryCreate(Guid.NewGuid(), "Test title", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), 5);

        if (@event is null)
        {
            Assert.Fail("Не удалось создать событие: " + string.Join(Environment.NewLine, errors));
        }

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Добавление тестового события
            db.Events.Add(@event);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var arrayOfRequests = new Task<Booking>[20];

        for (int i = 0; i < 20; i++)
        {
            arrayOfRequests[i] = Task.Run(async () =>
            {
                using var scope = _sharedFixture.ServiceProvider.CreateScope();

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
        var storedEvent = await GetEventAsync(@event.Id, TestContext.Current.CancellationToken);

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
