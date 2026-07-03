using Application.Bookings.Infrastructure;
using Domain.Bookings;
using FluentAssertions;
using Infrastructure.Bookings.Ef;
using Infrastructure.Bookings.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.Bookings.Ef.Bookings;

[Collection("PostgresTests")]
public class EfBookingEventsProducerTests(SharedFixture sharedFixture) : IAsyncLifetime
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

    private async Task AddBookingConfirmedAsync(Booking booking)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

        var eventId = Guid.NewGuid();

        var userId = Guid.NewGuid();

        db.BookingConfirmed.Add(new BookingConfirmedOutboxItem
        (
            booking.Id,
            booking.EventId,
            booking.UserId,
            1,
            SharedFixture.TrimToMicroseconds(booking.ProcessedAt ?? SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddMinutes(-1)))
        ));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion

    #region BookingConfirmedAsync

    [Fact]
    public async Task BookingConfirmedAsync_CorrectEvent_SavedSuccessfully()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var booking = new Booking
        (
            bookingId,
            eventId,
            userId,
            BookingStatus.Confirmed,
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddMinutes(-1)),
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow)
        );

        var expected = new BookingConfirmedOutboxItem(bookingId, eventId, userId, 1, booking.ProcessedAt!.Value);

        // Act
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var producer = scope.ServiceProvider.GetRequiredService<IBookingEventsProducer>();
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

            await producer.BookingConfirmedAsync(booking, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var result = await db.BookingConfirmed.FirstOrDefaultAsync
            (
                b => 
                    b.BookingId == bookingId && b.EventId == eventId,
                TestContext.Current.CancellationToken
            );

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expected);
        }
    }

    [Fact]
    public async Task BookingConfirmedAsync_TryAddDuplicateEvent_ThrowDbUpdate()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var booking = new Booking
        (
            bookingId,
            eventId,
            userId,
            BookingStatus.Confirmed,
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddMinutes(-1)),
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow)
        );

        await AddBookingConfirmedAsync(booking);
  
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var producer = scope.ServiceProvider.GetRequiredService<IBookingEventsProducer>();
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

            // Act
            await producer.BookingConfirmedAsync(booking, TestContext.Current.CancellationToken);
            var act = async () => await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            await act.Should().ThrowAsync<DbUpdateException>();
        }


        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            var result = await db.BookingConfirmed.CountAsync
            (
                b =>
                    b.BookingId == bookingId && b.EventId == eventId,
                TestContext.Current.CancellationToken
            );
            // Проверка, что в базе одна запись, дубликат не добавился
            result.Should().Be(1);
        }
    }

    #endregion
}
