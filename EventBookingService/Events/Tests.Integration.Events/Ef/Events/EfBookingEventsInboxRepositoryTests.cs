using Application.Events.DTO.Requests;
using Application.Events.Infrastructure;
using FluentAssertions;
using Infrastructure.Events.Ef;
using Infrastructure.Events.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.Events.Ef.Events;

[Collection("PostgresTests")]
public class EfBookingEventsInboxRepositoryTests(SharedFixture sharedFixture) : IAsyncLifetime
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

    private async Task AddBookingConfirmedListAsync(params BookingConfirmedRequest[] requests)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        var eventId = Guid.NewGuid();

        var userId = Guid.NewGuid();

        db.BookingConfirmed.AddRange(requests.Select(t => new BookingConfirmedInboxItem
        (
            t.BookingId,
            t.EventId,
            t.UserId,
            1,
            SharedFixture.TrimToMicroseconds(t.Approved)
        )));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Метод генерирует элементы для inbox, которые не совпадают с переданным аргументом
    /// по основному ключу
    /// </summary>
    /// <param name="targetRequest">Целевой запрос, ключа которого в исходящей коллекции быть не должно</param>
    /// <returns>Коллекция элементов</returns>
    private List<BookingConfirmedRequest> GenerateNoiseItems(BookingConfirmedRequest targetRequest)
    {
        var eventCount = 7;
        var eventIds = new Guid[eventCount];
        for (int i = 0; i < eventCount - 1; i++)
        {
            eventIds[i] = Guid.NewGuid();
        }
        // Для события могут быть другие бронирования
        eventIds[^1] = targetRequest.EventId;

        var userCount = 6;
        var usersIds = new Guid[userCount];
        for (int i = 0; i < userCount - 1; i++)
        {
            usersIds[i] = Guid.NewGuid();
        }

        return 
        [
            new(Guid.NewGuid(), eventIds[0], usersIds[5], 1, DateTimeOffset.UtcNow.AddMinutes(-5)),
            new(Guid.NewGuid(), eventIds[1], usersIds[4], 1, DateTimeOffset.UtcNow.AddMinutes(-1)),
            new(Guid.NewGuid(), eventIds[2], usersIds[3], 1, DateTimeOffset.UtcNow.AddMinutes(-50)),
            new(Guid.NewGuid(), eventIds[3], usersIds[2], 1, DateTimeOffset.UtcNow.AddMinutes(-34)),
            new(Guid.NewGuid(), eventIds[4], usersIds[1], 1, DateTimeOffset.UtcNow.AddMinutes(-20)),
            new(Guid.NewGuid(), eventIds[5], usersIds[0], 1, DateTimeOffset.UtcNow.AddMinutes(-11)),
            new(Guid.NewGuid(), eventIds[6], usersIds[0], 1, DateTimeOffset.UtcNow.AddMinutes(-1)),
            new(Guid.NewGuid(), eventIds[1], usersIds[4], 1, DateTimeOffset.UtcNow.AddMinutes(-7)),
            new(Guid.NewGuid(), eventIds[1], usersIds[5], 1, DateTimeOffset.UtcNow.AddMinutes(-44)),
            new(Guid.NewGuid(), eventIds[3], usersIds[3], 1, DateTimeOffset.UtcNow.AddMinutes(-56))
        ];
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_CorrectEvent_SavedSuccessfully()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var booking = new BookingConfirmedRequest
        (
            bookingId,
            eventId,
            userId,
            1,
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddMinutes(-1))
        );

        var expected = new BookingConfirmedInboxItem(bookingId, eventId, userId, 1, booking.Approved);

        // Act
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var inboxRepo = scope.ServiceProvider.GetRequiredService<IBookingEventsInboxRepository>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            await inboxRepo.AddAsync(booking, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
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
    public async Task AddAsync_TryAddDuplicateEvent_ThrowDbUpdate()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var booking = new BookingConfirmedRequest
        (
            bookingId,
            eventId,
            userId,
            1,
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddMinutes(-1))
        );

        await AddBookingConfirmedListAsync(booking);

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var inboxRepo = scope.ServiceProvider.GetRequiredService<IBookingEventsInboxRepository>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            await inboxRepo.AddAsync(booking, TestContext.Current.CancellationToken);
            var act = async () => await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            await act.Should().ThrowAsync<DbUpdateException>();
        }


        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
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

    #region CheckIfProcessedAsync

    [Fact]
    public async Task CheckIfProcessedAsync_HasItem_ReturnTrue()
    {
        // Arrange
        var target = new BookingConfirmedRequest
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddMinutes(-1))
        );

        var testData = GenerateNoiseItems(target);
        testData.Add(target);

        await AddBookingConfirmedListAsync([.. testData]);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();
        var inboxRepo = scope.ServiceProvider.GetRequiredService<IBookingEventsInboxRepository>();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        var result = await inboxRepo.CheckIfProcessedAsync(target, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckIfProcessedAsync_NoItem_ReturnFalse()
    {
        // Arrange
        var target = new BookingConfirmedRequest
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddMinutes(-1))
        );

        var testData = GenerateNoiseItems(target);

        await AddBookingConfirmedListAsync([.. testData]);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();
        var inboxRepo = scope.ServiceProvider.GetRequiredService<IBookingEventsInboxRepository>();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        var result = await inboxRepo.CheckIfProcessedAsync(target, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
