using Bookings.Models;
using Bookings.Service.Implementation;
using DataAccess.Storage;
using Events.Models;
using Events.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Validation;

namespace Tests.Bookings;

public partial class BookingTests
{
    private static BookingService CreateService(
        out Mock<IStorage<Booking>> bookingStorageMock,
        out Mock<IEventService> eventServiceMock,
        out Mock<ILogger<BookingService>> loggerMock)
    {
        bookingStorageMock = new Mock<IStorage<Booking>>();
        eventServiceMock = new Mock<IEventService>();
        loggerMock = new Mock<ILogger<BookingService>>();

        return new BookingService(
            bookingStorageMock.Object,
            eventServiceMock.Object,
            loggerMock.Object);
    }

    #region GetBookingByIdAsync

    [Fact]
    public async Task GetBookingByIdAsync_ValidId_ReturnSuccess()
    {
        var service = CreateService(out var bookingStorageMock, out var _, out var _);
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            BookingStatus.Pending,
            DateTime.UtcNow
        );

        bookingStorageMock.Setup(s => s.GetByIdAsync(bookingToReturn.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(bookingToReturn))
            .Verifiable(Times.Once);

        var result = await service.GetBookingByIdAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(bookingToReturn);
    }

    [Fact]
    public async Task GetBookingByIdAsync_InvalidId_SuccessWithoutValue()
    {
        var service = CreateService(out var bookingStorageMock, out var _, out var _);
        var bookId = Guid.NewGuid();
        bookingStorageMock.Setup(s => s.GetByIdAsync(bookId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success<Booking>(null))
            .Verifiable(Times.Once);

        var result = await service.GetBookingByIdAsync(bookId, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    #endregion

    #region CreateBookingAsync

    [Fact]
    public async Task CreateBookingAsync_EventExists_ReturnSuccess()
    {
        var service = CreateService(out var bookingStorageMock, out var eventServiceMock, out var _);
        var eventId = Guid.NewGuid();
        var bookingList = new List<Booking>();

        bookingStorageMock.Setup(s => s.AddAsync(
                Capture.In(bookingList),
                TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success<Booking>(null))
            .Verifiable(Times.Once);

        eventServiceMock.Setup(s => s.GetEventByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(new Event(eventId, "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount)))
            .Verifiable(Times.Once);

        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        eventServiceMock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(bookingList.First());
        bookingList.First().ProcessedAt.Should().BeNull();
        bookingList.First().CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        bookingList.First().EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task CreateBookingAsync_EventDoesNotExists_SuccessWithoutValue()
    {
        var service = CreateService(out var bookingStorageMock, out var eventServiceMock, out var _);
        var eventId = Guid.NewGuid();

        bookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        eventServiceMock.Setup(s => s.GetEventByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success<Event>(null))
            .Verifiable(Times.Once);

        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        eventServiceMock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_GetEventFailed_ReturnErrors()
    {
        var service = CreateService(out var bookingStorageMock, out var eventServiceMock, out var _);
        var eventId = Guid.NewGuid();

        bookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        eventServiceMock.Setup(s => s.GetEventByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Fail<Event>(null, "Произошла ошибка"))
            .Verifiable(Times.Once);

        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        eventServiceMock.Verify();
        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_BookingStorageErrorOccured_ReturnErrors()
    {
        var service = CreateService(out var bookingStorageMock, out var eventServiceMock, out var _);
        var eventId = Guid.NewGuid();

        bookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Fail<Booking>(null, "Ошибка сохранения"))
            .Verifiable(Times.Once);

        eventServiceMock.Setup(s => s.GetEventByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(new Event(eventId, "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount)))
            .Verifiable(Times.Once);

        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        eventServiceMock.Verify(); 
        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
    }

    #endregion

    // Пока нет логики обработки бронирования кроме принудительного перевода в другой статус, поэтому тестов для этого метода нет 
}
