using Domain.Bookings;
using Domain.Events;
using Domain.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Interfaces.Infrastructure.Enums;

namespace Tests.Integration.Ef.Bookings;

[Collection("PostgresTests")]
public partial class EfBookingRepositoryTests(SharedFixture sharedFixture) : IAsyncLifetime
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

    private async Task AddPendingBookingAsync(Guid id)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventId = Guid.NewGuid();
        db.Events.Add(new Event
        (
            eventId,
            "Don't Care Title",
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
            10
        ));
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        (
            userId,
            "user",
            "somehash",
            Roles.User
        ));
        db.Bookings.Add(new Booking(id, eventId, userId, BookingStatus.Pending, DateTimeOffset.UtcNow));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task AddEventAsync(Guid eventId)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Events.Add(new Event(eventId, "Don't Care Title", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), 10));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task AddUserAsync(Guid userId)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Users.Add(new User(userId, "user", "somehash", Roles.User));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task AddBookingsUsersAndEventsAsync(IEnumerable<Event> events, IEnumerable<User> users, IEnumerable<Booking> bookings)
    {
        if (!events.Any() && !bookings.Any() && !users.Any())
        {
            return;
        }

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Последовательно применяем, а то ef сам связи может выставить и при передаче одной и той же коллекции событий
        // она добавляется вместе с бронями
        db.Events.AddRange(events);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();

        db.Users.AddRange(users);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();

        db.Bookings.AddRange(bookings);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_EditMode_SuccessfullyModifiedAndSaved()
    {
        // Arrange
        var targetGuid = Guid.NewGuid();

        await AddPendingBookingAsync(targetGuid);

        // Act
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var booking = await repository.GetByIdAsync(targetGuid, GetMode.Edit, TestContext.Current.CancellationToken);
            if (booking is null)
            {
                Assert.Fail("Бронирование не получено из репозитория");
            }

            booking.Confirm();
            if (db.Database.CurrentTransaction is null)
            {
                Assert.Fail("В контексте получения бронирования должна быть открыта транзакция");
            }
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            await db.Database.CommitTransactionAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var result = await db.Bookings.FirstOrDefaultAsync(t => t.Id == targetGuid, TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result.Status.Should().Be(BookingStatus.Confirmed);
        }
    }

    /// <summary>
    /// Тест проверяет корректный захват блокировки на конкретную запись. Чтобы убедиться в этом, первая транзакция
    /// захватывает запись и держит ее с эмуляционной задержкой, после чего сохраняет с изменением поля ProcessedAt.
    /// Вторая транзакция параллельно пытается получить доступ к записи с небольшой задержкой относительно первой и
    /// в итоге должна получить уже измененную запись, что и будет являться положительным результатом теста
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ParallelEditAccess_CorrectSequentalAccess()
    {
        // Arrange
        var targetGuid = Guid.NewGuid();

        // .Net более точное время, которое округляется при вставке, поэтому округлим сразу здесь
        var processed = SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow);
        await AddPendingBookingAsync(targetGuid);

        // Act
        var firstTask = Task.Run<Booking?>(async () =>
        {
            using var scope = _sharedFixture.ServiceProvider.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var booking = await repository.GetByIdAsync(targetGuid, GetMode.Edit, TestContext.Current.CancellationToken);
            if (booking is null)
            {
                Assert.Fail("Бронирование в первой транзакции не получено из репозитория");
            }

            booking.ProcessedAt = processed;
            // Ждем, чтобы убедиться, что второй поток действительно будет ожидать результата отсюда
            await Task.Delay(100);

            if (db.Database.CurrentTransaction is null)
            {
                Assert.Fail("В контексте получения бронирования должна быть открыта транзакция");
            }
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            await db.Database.CommitTransactionAsync(TestContext.Current.CancellationToken);

            return booking;
        }, TestContext.Current.CancellationToken);

        var secondTask = Task.Run(async () =>
        {
            using var scope = _sharedFixture.ServiceProvider.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Ждем, чтобы второй поток точно не мог первым взять транзакцию
            await Task.Delay(15);

            var booking = await repository.GetByIdAsync(targetGuid, GetMode.Edit, TestContext.Current.CancellationToken);
            await db.Database.RollbackTransactionAsync(TestContext.Current.CancellationToken);

            return booking;
        }, TestContext.Current.CancellationToken);

        try
        {
            await Task.WhenAll(firstTask, secondTask);
        }
        catch
        {

        }

        // Assert
        firstTask.IsCompletedSuccessfully.Should().BeTrue();
        secondTask.IsCompletedSuccessfully.Should().BeTrue();
        firstTask.Result?.Should().NotBeNull();
        secondTask.Result?.Should().NotBeNull();
        firstTask.Result.Should().BeEquivalentTo(secondTask.Result);
    }

    [Fact]
    public async Task GetByIdAsync_ReadonlyMode_ShouldReturnDetachedEntity()
    {
        // Arrange
        var targetGuid = Guid.NewGuid();

        await AddPendingBookingAsync(targetGuid);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        var result = await repository.GetByIdAsync(targetGuid, GetMode.Readonly, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        db.Entry(result).State.Should().Be(EntityState.Detached);
    }

    [Theory]
    [InlineData(GetMode.Readonly)]
    [InlineData(GetMode.Edit)]
    public async Task GetByIdAsync_NotFound_ShouldReturnNull(GetMode mode)
    {
        // Arrange
        await AddPendingBookingAsync(Guid.NewGuid());

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid(), mode, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_BookingWithCorrectEvent_SavedSuccessfully()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        await AddEventAsync(eventId);
        await AddUserAsync(userId);

        var booking = new Booking(bookingId, eventId, userId, BookingStatus.Pending, SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow));

        // Act
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await repository.AddAsync(booking, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var result = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(booking);
        }
    }

    [Fact]
    public async Task AddAsync_BookingWithNoEventInDb_ExceptionThrown()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        await AddUserAsync(userId);

        var booking = new Booking(bookingId, Guid.NewGuid(), userId, BookingStatus.Pending, SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow));

        // Act
        var act = async () =>
        {
            using var scope = _sharedFixture.ServiceProvider.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await repository.AddAsync(booking, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        };

        // Assert
        await act.Should()
            .ThrowExactlyAsync<DbUpdateException>();
    }

    // TODO добавить тесты на добавление бука без пользователя

    #endregion

    #region RemoveAsync

    [Fact]
    public async Task RemoveAsync_ExistingEntity_ShouldMarkAsDeletedInTracker()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        await AddPendingBookingAsync(bookingId);

        // Act
        var removed = false;
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            removed = await repository.RemoveAsync(bookingId, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var result = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }
    }

    [Fact]
    public async Task RemoveAsync_NonExistingEntity_ShouldReturnFalse()
    {
        // Arrange
        await AddPendingBookingAsync(Guid.NewGuid());

        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        var removed = await repository.RemoveAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        removed.Should().BeFalse();
    }

    #endregion

    #region GetPendingBookingsAsync

    [Theory]
    [MemberData(nameof(GetPendingBookingsAsync_Common))]
    public async Task GetPendingBookingsAsync_Common_ReturnValidGuids(List<Event> events, List<User> users, List<Booking> bookings, List<Guid> expectedBookings)
    {
        // Arrange
        await AddBookingsUsersAndEventsAsync(events, users, bookings);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        // Act
        var result = await repository.GetPendingBookingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expectedBookings);
    }

    #endregion

    #region GetPendingBookingsAsync

    [Theory]
    [MemberData(nameof(GetCountActiveBookingForPersonAsync_Common))]
    public async Task GetCountActiveBookingForPersonAsync_Common_ReturnValidCount(List<Event> events, List<User> users, List<Booking> bookings, Guid userId, int expectedCount)
    {
        // Arrange
        await AddBookingsUsersAndEventsAsync(events, users, bookings);

        using var scope = _sharedFixture.ServiceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        // Act
        var result = await repository.GetCountActiveBookingForPersonAsync(userId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedCount);
    }

    #endregion
}
