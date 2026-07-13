using Application.Events.DTO.Requests;
using Application.Events.Implementation;
using Application.Events.Infrastructure;

using Domain.Events;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Abstract.Enums;

namespace Tests.Unit.Events.Processing;

public class EventProcessingServiceTests
{
    private class Holder
    {
        public required Mock<IEventRepository> RepositoryMock { get; init; }
        public required Mock<IUnitOfWork> UnitOfWorkMock { get; init; }
        public required Mock<IBookingEventsInboxRepository> BookingEventsInboxRepositoryMock { get; init; }
    }

    private static EventProcessingService CreateService(out Holder holder)
    {
        holder = new Holder
        {
            RepositoryMock = new Mock<IEventRepository>(),
            UnitOfWorkMock = new Mock<IUnitOfWork>(),
            BookingEventsInboxRepositoryMock = new Mock<IBookingEventsInboxRepository>(),
        };

        return new EventProcessingService
        (
            holder.RepositoryMock.Object,
            holder.BookingEventsInboxRepositoryMock.Object,
            holder.UnitOfWorkMock.Object,
            new Mock<ILogger<EventProcessingService>>().Object
        );
    }

    [Fact]
    public async Task ProcessConfirmationAsync_CommonCase_SavedAndDecreaseSeats()
    {
        // Arrange
        var service = CreateService(out var holder);
        var @event = new Event(Guid.NewGuid(), "Title", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1.1), 10);
        var beforeSeats = @event.AvailableSeats;
        var bookRequest = new BookingConfirmedRequest(Guid.NewGuid(), @event.Id, Guid.NewGuid(), 1, DateTime.UtcNow.AddMinutes(-1));

        // Такое событие еще не обработано
        holder.BookingEventsInboxRepositoryMock.Setup(s => s.CheckIfProcessedAsync(bookRequest, TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        // Запросили событие
        holder.RepositoryMock.Setup(s => s.GetByIdAsync(bookRequest.EventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(@event)
            .Verifiable(Times.Once);

        // Добавили в inbox
        holder.BookingEventsInboxRepositoryMock.Setup(s => s.AddAsync(bookRequest, TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Сохранили изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        await service.ProcessConfirmationAsync(bookRequest, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingEventsInboxRepositoryMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        @event.AvailableSeats.Should().Be(beforeSeats - 1);
    }

    [Fact]
    public async Task ProcessConfirmationAsync_EventWasStarted_SavedWithoutDecreaseSeats()
    {
        // Arrange
        var service = CreateService(out var holder);
        var @event = new Event(Guid.NewGuid(), "Title", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-0.8), 10);
        var beforeSeats = @event.AvailableSeats;
        var bookRequest = new BookingConfirmedRequest(Guid.NewGuid(), @event.Id, Guid.NewGuid(), 1, DateTime.UtcNow.AddMinutes(-1));

        // Такое событие еще не обработано
        holder.BookingEventsInboxRepositoryMock.Setup(s => s.CheckIfProcessedAsync(bookRequest, TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        // Запросили событие
        holder.RepositoryMock.Setup(s => s.GetByIdAsync(bookRequest.EventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(@event)
            .Verifiable(Times.Once);

        // Добавили в inbox
        holder.BookingEventsInboxRepositoryMock.Setup(s => s.AddAsync(bookRequest, TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Сохранили изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        await service.ProcessConfirmationAsync(bookRequest, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingEventsInboxRepositoryMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        @event.AvailableSeats.Should().Be(beforeSeats);
    }

    [Fact]
    public async Task ProcessConfirmationAsync_EventHasNoSeats_SavedWithoutDecreaseSeats()
    {
        // Arrange
        var service = CreateService(out var holder);
        var @event = new Event(Guid.NewGuid(), "Title", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1.2), 10, 0);
        var beforeSeats = @event.AvailableSeats;
        var bookRequest = new BookingConfirmedRequest(Guid.NewGuid(), @event.Id, Guid.NewGuid(), 1, DateTime.UtcNow.AddMinutes(-1));

        // Такое событие еще не обработано
        holder.BookingEventsInboxRepositoryMock.Setup(s => s.CheckIfProcessedAsync(bookRequest, TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        // Запросили событие
        holder.RepositoryMock.Setup(s => s.GetByIdAsync(bookRequest.EventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(@event)
            .Verifiable(Times.Once);

        // Добавили в inbox
        holder.BookingEventsInboxRepositoryMock.Setup(s => s.AddAsync(bookRequest, TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Сохранили изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        await service.ProcessConfirmationAsync(bookRequest, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingEventsInboxRepositoryMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        @event.AvailableSeats.Should().Be(beforeSeats);
    }

    [Fact]
    public async Task ProcessConfirmationAsync_EventNotFound_SavedWithoutDecreaseSeats()
    {
        // Arrange
        var service = CreateService(out var holder);
        var bookRequest = new BookingConfirmedRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow.AddMinutes(-1));

        // Такое событие еще не обработано
        holder.BookingEventsInboxRepositoryMock.Setup(s => s.CheckIfProcessedAsync(bookRequest, TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        // Запросили событие
        holder.RepositoryMock.Setup(s => s.GetByIdAsync(bookRequest.EventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?) null)
            .Verifiable(Times.Once);

        // Добавили в inbox
        holder.BookingEventsInboxRepositoryMock.Setup(s => s.AddAsync(bookRequest, TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Сохранили изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        await service.ProcessConfirmationAsync(bookRequest, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingEventsInboxRepositoryMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
    }
}
