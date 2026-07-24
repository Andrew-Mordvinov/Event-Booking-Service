using Application.Bookings.Implementation;
using Application.Bookings.Infrastructure;

using Domain.Bookings;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Abstract.Enums;

namespace Tests.Unit.Bookings;

public class BookingProcessingServiceTests
{
    private class MockHolder
    {
        public required Mock<IBookingRepository> BookingStorageMock { get; init; }
        public required Mock<IUnitOfWork> UnitOfWorkMock { get; init; }
        public required Mock<IBookingEventsProducer> BookingEventProducer { get; init; }
    }

    private static BookingProcessingService CreateService(out MockHolder holder)
    {
        holder = new MockHolder
        {
            BookingStorageMock = new Mock<IBookingRepository>(),
            UnitOfWorkMock = new Mock<IUnitOfWork>(),
            BookingEventProducer = new Mock<IBookingEventsProducer>(),
        };

        return new BookingProcessingService(
            holder.BookingStorageMock.Object,
            holder.BookingEventProducer.Object,
            holder.UnitOfWorkMock.Object,
            new Mock<ILogger<BookingProcessingService>>().Object);
    }

    [Fact]
    public async Task ProcessBookingAsync_CommonCase_ConfirmBook()
    {
        // Arrange
        var service = CreateService(out var holder);
        var eventId = Guid.NewGuid();
        var book = new Booking(Guid.NewGuid(), eventId, Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);

        // Получили бронь
        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(book.Id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(book)
            .Verifiable(Times.Once);

        // Опубликовали событие
        holder.BookingEventProducer.Setup(s => s.BookingConfirmedAsync(book, TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Сохранили изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        await service.ProcessBookingAsync(book.Id, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingStorageMock.Verify();
        holder.BookingEventProducer.Verify();
        holder.UnitOfWorkMock.Verify();
        book.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        book.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task ProcessBookingAsync_BookNotExits_NoAffection()
    {
        // Arrange
        var service = CreateService(out var holder);
        var bookId = Guid.NewGuid();
        // Бронь не найдена
        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(bookId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking?) null)
            .Verifiable(Times.Once);

        // Не публиковали событие 
        holder.BookingEventProducer.Setup(s => s.BookingConfirmedAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не сохраняли (нечего сохранять)
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        await service.ProcessBookingAsync(bookId, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingStorageMock.Verify();
        holder.UnitOfWorkMock.Verify();
        holder.BookingEventProducer.Verify();
    }
}
