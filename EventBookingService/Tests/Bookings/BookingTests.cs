using Bookings.Service.Implementation;
using DataAccess.Abstract;
using DataAccess.Abstract.Common;
using DataAccess.Abstract.Enums;
using Entities.Bookings;
using Entities.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Exceptions;

namespace Tests.Bookings;

public partial class BookingTests
{
    private static BookingService CreateService(
        out Mock<IBookingRepository> bookingStorageMock,
        out Mock<IEventRepository> eventStorageMock,
        out Mock<IUnitOfWork> unitOfWorkMock,
        out Mock<ILogger<BookingService>> loggerMock)
    {
        bookingStorageMock = new Mock<IBookingRepository>();
        eventStorageMock = new Mock<IEventRepository>();
        unitOfWorkMock = new Mock<IUnitOfWork>();
        loggerMock = new Mock<ILogger<BookingService>>();

        return new BookingService(
            bookingStorageMock.Object,
            eventStorageMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object);
    }

    #region GetBookingByIdAsync

    [Fact]
    public async Task GetBookingByIdAsync_ValidId_ReturnSuccess()
    {
        var service = CreateService(out var bookingStorageMock, out var _, out var _, out var _);
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            BookingStatus.Pending,
            DateTime.UtcNow
        );

        bookingStorageMock.Setup(s => s.GetByIdAsync(bookingToReturn.Id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        var result = await service.GetBookingByIdAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        result.Should().BeEquivalentTo(bookingToReturn);
    }

    [Fact]
    public async Task GetBookingByIdAsync_InvalidId_SuccessWithoutValue()
    {
        var service = CreateService(out var bookingStorageMock, out var _, out var _, out var _);
        var bookId = Guid.NewGuid();
        bookingStorageMock.Setup(s => s.GetByIdAsync(bookId, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking?)null)
            .Verifiable(Times.Once);

        var result = await service.GetBookingByIdAsync(bookId, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        result.Should().BeNull();
    }

    #endregion

    #region CreateBookingAsync

    [Fact]
    public async Task CreateBookingAsync_EventExistsAndHasSeats_ReturnSuccess()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var eventId = Guid.NewGuid();
        var bookingList = new List<Booking>();
        var bookEvent = new Event(eventId, "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount);
        var beforeCount = bookEvent.AvailableSeats;

        // Успешно получили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookEvent)
            .Verifiable(Times.Once);

        // Успешно добавили бронь в хранилище
        bookingStorageMock.Setup(s => s.AddAsync(
                Capture.In(bookingList),
                TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Успешно сохранили изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        result.Should().BeEquivalentTo(bookingList.First());
        bookingList.First().ProcessedAt.Should().BeNull();
        bookingList.First().CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        bookingList.First().EventId.Should().Be(eventId);
        bookEvent.AvailableSeats.Should().Be(beforeCount - 1);
    }

    [Fact]
    public async Task CreateBookingAsync_EventDoesNotExists_NotFoundException()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var eventId = Guid.NewGuid();

        // Попытались получить событие, но его не оказалось
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?)null)
            .Verifiable(Times.Once);

        // Не создали бронь и не пытались добавить ничего в хранилище
        bookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Сохранение не вызывалось
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var act = async () => await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowExactlyAsync<NotFoundException>()
            .WithMessage(BookingServiceErrors.EventNotFound(eventId));

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
    }

    [Fact]
    public async Task CreateBookingAsync_NoSeatsAvailable_ReturnConflictError()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var eventId = Guid.NewGuid();

        // Успешно получили событие без свободных мест
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(new Event(eventId, "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount, 0))
            .Verifiable(Times.Once);

        // Не попытались сохранить бронь, потому что свободных мест нет
        bookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Сохранение не вызывалось
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var act = async () => await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowExactlyAsync<ConflictException>()
            .WithMessage(BookingServiceErrors.NoAvailableSeats);

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
    }

    [Fact]
    public async Task CreateBookingAsync_ParallelBookMoreThanSeats_NoOverbookingOccurs()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var semaphore = new SemaphoreSlim(1, 1);
        var eventId = Guid.NewGuid();
        var bookEvent = new Event(
            eventId,
            "Some title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            5);

        // Успешно получили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookEvent)
            .Verifiable(Times.Exactly(20));

        // Успешно добавили бронь в хранилище (только 5 мест)
        bookingStorageMock.Setup(s => s.AddAsync(
                It.IsAny<Booking>(),
                TestContext.Current.CancellationToken))
            .Verifiable(Times.Exactly(5));

        // Успешно сохранили (только 5 мест)
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Exactly(5));

        var arrayOfRequests = new Task<Booking>[20];

        for (int i = 0; i < 20; i++)
        {
            arrayOfRequests[i] = Task.Run(() => service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken));
        }

        try
        {
            // Игнор исключения здесь, проверяем ниже
            await Task.WhenAll(arrayOfRequests);
        }
        catch
        {

        }

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        bookEvent.AvailableSeats.Should().Be(0);
        arrayOfRequests.Count(t => t.Status == TaskStatus.RanToCompletion && t.Result is not null).Should().Be(5);
        arrayOfRequests.Count(t => t.Status == TaskStatus.Faulted && t.Exception?.InnerException is ConflictException).Should().Be(15);
    }


    [Fact]
    public async Task CreateBookingAsync_ParallelBook_UniqueIdOfBooks()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var semaphore = new SemaphoreSlim(1, 1);
        var eventId = Guid.NewGuid();
        var bookEvent = new Event(
            eventId,
            "Some title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10);

        // Успешно получили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookEvent)
            .Verifiable(Times.Exactly(10));

        // Успешно добавили бронь в хранилище
        bookingStorageMock.Setup(s => s.AddAsync(
                It.IsAny<Booking>(),
                TestContext.Current.CancellationToken))
            .Verifiable(Times.Exactly(10));

        // Успешно сохранили
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Exactly(10));

        var arrayOfRequests = new Task<Booking>[10];

        for (int i = 0; i < 10; i++)
        {
            arrayOfRequests[i] = Task.Run(() => service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(arrayOfRequests);

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        bookEvent.AvailableSeats.Should().Be(0);
        arrayOfRequests.Count(t => t.Result is not null).Should().Be(10);
        arrayOfRequests.Select(t => t.Result!.Id).Distinct().Count().Should().Be(10);
    }

    #endregion

    #region ProcessBookingAsync

    [Fact]
    public async Task ProcessBookingAsync_EventExits_ConfirmBook()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var eventId = Guid.NewGuid();
        var book = new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);

        // Получили бронь
        bookingStorageMock.Setup(s => s.GetByIdAsync(book.Id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(book)
            .Verifiable(Times.Once);

        // Получили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(new Event(eventId, "Some text", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1))
            .Verifiable(Times.Once);

        // Сохранили изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        await service.ProcessBookingAsync(book.Id, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        book.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        book.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task ProcessBookingAsync_EventNotExits_RejectBook()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var eventId = Guid.NewGuid();
        var book = new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);

        // Получили бронь
        bookingStorageMock.Setup(s => s.GetByIdAsync(book.Id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(book)
            .Verifiable(Times.Once);

        // Событие не получено 
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?)null)
            .Verifiable(Times.Once);

        // Сохранили изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        await service.ProcessBookingAsync(book.Id, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        unitOfWorkMock.Verify();
        eventStorageMock.Verify();
        book.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        book.Status.Should().Be(BookingStatus.Rejected);
    }

    [Fact]
    public async Task ProcessBookingAsync_BookNotExits_RejectBook()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var bookId = Guid.NewGuid();
        // Бронь не найдена
        bookingStorageMock.Setup(s => s.GetByIdAsync(bookId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking?)null)
            .Verifiable(Times.Once);

        // Не получали событие 
        eventStorageMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), GetMode.Edit, TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не сохраняли (нечего сохранять)
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        await service.ProcessBookingAsync(bookId, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        unitOfWorkMock.Verify();
        eventStorageMock.Verify();
    }

    #endregion
}
