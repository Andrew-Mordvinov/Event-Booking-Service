using DataAccess.EF;
using Entities.Bookings;
using Entities.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Web.Infrastructure.Bookings;

namespace Tests.Integration.Ef.Bookings;

[Collection("PostgresTests")]
public class BookingManagerBackgroundServiceTests(SharedFixture sharedFixture) : IAsyncLifetime
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

    #region Helping

    private BookingManagerBackgroundService CreateBackground()
    {
        return new BookingManagerBackgroundService
        (
            _sharedFixture.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            _sharedFixture.ServiceProvider.GetRequiredService<ILogger<BookingManagerBackgroundService>>()
        );
    }

    private static Guid[] GenerateGuids(int count)
    {
        var guids = new Guid[count];

        for (int i = 0; i < count; i++)
        {
            guids[i] = Guid.NewGuid();
        }

        return guids;
    }

    private async Task SetTestDataToDbAsync()
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Создаем события и бронирования для теста. Пока особо нечего тестировать, логики по сути нет, поэтому набор не очень большой

        Guid[] eventIds = GenerateGuids(3);
        Guid[] bookingIds = GenerateGuids(8);

        db.Events.AddRange
        ([
            new Event(eventIds[0], "Some title 1", DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1), 4, 1),
            new Event(eventIds[1], "Some title 2", DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow.AddDays(-4), 10, 6),
            new Event(eventIds[2], "Some title 3", DateTimeOffset.UtcNow.AddDays(4), DateTimeOffset.UtcNow.AddDays(5), 5, 5),
        ]);

        db.Bookings.AddRange
        ([
            new Booking(bookingIds[0], eventIds[0], BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-20), DateTimeOffset.UtcNow.AddHours(-19.8)),
            new Booking(bookingIds[1], eventIds[1], BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-0.5)),
            new Booking(bookingIds[2], eventIds[1], BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-10), DateTimeOffset.UtcNow.AddHours(-9.8)),
            new Booking(bookingIds[3], eventIds[2], BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-0.5), DateTimeOffset.UtcNow.AddHours(-0.4)),
            new Booking(bookingIds[4], eventIds[0], BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-0.1)),
            new Booking(bookingIds[5], eventIds[1], BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-10), DateTimeOffset.UtcNow.AddHours(-9.9)),
            new Booking(bookingIds[6], eventIds[0], BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-0.2)),
            new Booking(bookingIds[7], eventIds[1], BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-0.1)),
        ]);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion

    [Fact]
    public async Task ProcessPendingBookingsAsync_Common_SuccessfullyProcessed()
    {
        // Arrange
        await SetTestDataToDbAsync();
        var background = CreateBackground();

        // Act
        await background.ProcessPendingBookingsAsync(TestContext.Current.CancellationToken);

        // Assert
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var unprocessed = await db.Bookings.Where(t => !t.ProcessedAt.HasValue).CountAsync();
        var pending = await db.Bookings.Where(t => t.Status == BookingStatus.Pending).CountAsync();

        unprocessed.Should().Be(0, "Обнаружены бронирования, у которых не установлено время обработки");
        pending.Should().Be(0, "Обнаружены бронирования в статусе Pending");
    }
}
