using Domain.Events;
using FluentAssertions;
using Infrastructure.Events.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Abstract;

namespace Tests.Integration.Shared.Infrastructure;

// TODO тесты лучше сделать с другой сущностью, но пока для экономии времени будут тесты с ивентами
[Collection("PostgresTests")]
public class EfUnitOfWorkTests(SharedFixture sharedFixture) : IAsyncLifetime
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

    private async Task AddEventAsync(Guid id)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        db.Events.Add(new Event
        (
            id,
            "Don't Care Title",
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
            10
        ));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #region EnsureTransactionAsync

    [Fact]
    public async Task EnsureTransactionAsync_OneCall_TransactionOpened()
    {
        // Arrange
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        // Act
        await unit.EnsureTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        db.Database.CurrentTransaction.Should().NotBeNull();
    }

    [Fact]
    public async Task EnsureTransactionAsync_TwoCall_NoNewTransaction()
    {
        // Arrange
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        await unit.EnsureTransactionAsync(TestContext.Current.CancellationToken);

        var transactionId = db.Database.CurrentTransaction!.TransactionId;

        // Act
        await unit.EnsureTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        db.Database.CurrentTransaction.Should().NotBeNull();
        db.Database.CurrentTransaction.TransactionId.Should().Be(transactionId);
    }

    #endregion

    #region RollbackChangesAsync

    [Fact]
    public async Task RollbackChangesAsync_NoTransaction_NoTrackedApplied()
    {
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            // Arrange
            var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            db.Events.Add(new Event
            (
                Guid.NewGuid(),
                "Some title",
                SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
                SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
                10
            ));

            // Act
            await unit.RollbackChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            db.ChangeTracker.HasChanges().Should().BeFalse();
        }

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            (await db.Events.AnyAsync(TestContext.Current.CancellationToken)).Should().BeFalse();
        }
    }

    [Fact]
    public async Task RollbackChangesAsync_HasTransaction_NoChangesApplied()
    {
        // Arrange
        var addedEventId = Guid.NewGuid();
        await AddEventAsync(addedEventId);

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            await db.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
            // Добавление события через трекер
            db.Events.Add(new Event
            (
                Guid.NewGuid(),
                "Some title",
                SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
                SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
                10
            ));
            // Выполнение сырого sql в транзакции
            await db.Database.ExecuteSqlAsync($"DELETE FROM events WHERE \"Id\" = {addedEventId}", TestContext.Current.CancellationToken);

            // Act
            await unit.RollbackChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            db.ChangeTracker.HasChanges().Should().BeFalse();
            db.Database.CurrentTransaction.Should().BeNull();
        }

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            // Осталось одно событие, которое было добавлено в начале (всего одно + с таким id)
            (await db.Events.CountAsync(TestContext.Current.CancellationToken))
                .Should()
                .Be(1);
            (await db.Events.FirstOrDefaultAsync(t => t.Id == addedEventId, TestContext.Current.CancellationToken))?.Id
                .Should()
                .Be(addedEventId);
        }
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_NoTransaction_TrackedApplied()
    {
        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            // Arrange
            var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            db.Events.Add(new Event
            (
                Guid.NewGuid(),
                "Some title",
                SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
                SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
                10
            ));

            // Act
            await unit.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            db.ChangeTracker.HasChanges().Should().BeFalse();
        }

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            (await db.Events.AnyAsync(TestContext.Current.CancellationToken)).Should().BeTrue();
        }
    }

    [Fact]
    public async Task SaveChangesAsync_HasTransaction_AllChangesApplied()
    {
        // Arrange
        var addedEventId = Guid.NewGuid();
        await AddEventAsync(addedEventId);

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            await db.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
            // Добавление события через трекер
            db.Events.Add(new Event
            (
                Guid.NewGuid(),
                "Some title",
                SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
                SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
                10
            ));
            // Выполнение сырого sql в транзакции
            await db.Database.ExecuteSqlAsync($"DELETE FROM events WHERE \"Id\" = {addedEventId}", TestContext.Current.CancellationToken);

            // Act
            await unit.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            db.ChangeTracker.HasChanges().Should().BeFalse();
            db.Database.CurrentTransaction.Should().BeNull();
        }

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            // Событие, которое было добавлено вначале, удалено, а другое добавилось
            (await db.Events.CountAsync(TestContext.Current.CancellationToken))
                .Should()
                .Be(1);
            (await db.Events.FirstOrDefaultAsync(t => t.Id != addedEventId, TestContext.Current.CancellationToken))?.Id
                .Should()
                .NotBe(addedEventId);
        }
    }

    // TODO этот и следующий тест лучше переделать, когда будет левый дб контекст, чтобы транзакция падала из-за ключей или уникальности
    [Fact]
    public async Task SaveChangesAsync_ErrorOccuredInTransaction_ChangesRollback()
    {
        // Arrange
        var addedEventId = Guid.NewGuid();
        var duplicateId = Guid.NewGuid();
        var titleForAnotherDuplicate = "Another title text";
        await AddEventAsync(addedEventId);
        await AddEventAsync(duplicateId);

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            await db.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

            // Выполнение сырого sql в транзакции
            await db.Database.ExecuteSqlAsync($"DELETE FROM events WHERE \"Id\" = {addedEventId}", TestContext.Current.CancellationToken);

            // Добавление дубликата события, чтобы вызвать ошибку
            db.Events.AddRange(
            [
                new Event
                (
                    duplicateId,
                    titleForAnotherDuplicate,
                    SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
                    SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
                    10
                )
            ]);

            // Act
            var act = async () => await unit.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            await act.Should().ThrowAsync<DbUpdateException>();
            db.Database.CurrentTransaction.Should().BeNull();
            db.ChangeTracker.HasChanges().Should().BeFalse();
        }

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            // Событие, которое было добавлено вначале, осталось
            (await db.Events.FirstOrDefaultAsync(t => t.Id == addedEventId, TestContext.Current.CancellationToken))?.Id
                .Should()
                .Be(addedEventId);
            // Дубликат c новым тайтлом не добавился, остался только старый
            var duplicates = await db.Events.Where(t => t.Id == duplicateId).ToListAsync(TestContext.Current.CancellationToken);
            duplicates.Count.Should().Be(1);
            duplicates.First().Title.Should().NotBe(titleForAnotherDuplicate);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ErrorOccuredWithoutTransaction_ChangesRollback()
    {
        // Arrange
        var addedEventId = Guid.NewGuid();
        var duplicateId = Guid.NewGuid();
        var titleForAnotherDuplicate = "Another title text";
        await AddEventAsync(addedEventId);
        await AddEventAsync(duplicateId);

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            // Добавление дубликатов события, чтобы вызвать ошибку
            db.Events.AddRange(
            [
                new Event
                (
                    duplicateId,
                    titleForAnotherDuplicate,
                    SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
                    SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
                    10
                )
            ]);
            // Удаление события ранее добавленного
            var toDelete = await db.Events.FirstOrDefaultAsync(t => t.Id == addedEventId, TestContext.Current.CancellationToken);
            db.Events.Remove(toDelete!);

            // Act
            var act = async () => await unit.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Assert
            await act.Should().ThrowAsync<DbUpdateException>();
        }

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            // Событие, которое было добавлено вначале, осталось
            (await db.Events.FirstOrDefaultAsync(t => t.Id == addedEventId, TestContext.Current.CancellationToken))?.Id
                .Should()
                .Be(addedEventId);
            // Дубликат c новым тайтлом не добавился, остался только старый
            var duplicates = await db.Events.Where(t => t.Id == duplicateId).ToListAsync(TestContext.Current.CancellationToken);
            duplicates.Count.Should().Be(1);
            duplicates.First().Title.Should().NotBe(titleForAnotherDuplicate);
        }
    }

    #endregion
}
