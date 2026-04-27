using Bookings.Models;
using Bookings.Service.Implementation;
using DataAccess.Abstract;
using DataAccess.Abstract.Common;
using DataAccess.Abstract.Enums;
using Events.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Exceptions;
using Shared.Locking;
using Shared.Paging;
using System.Linq.Expressions;

namespace Tests.Bookings;

public partial class BookingTests
{
    private static BookingService CreateService(
        out Mock<IBookingRepository> bookingStorageMock,
        out Mock<IEventRepository> eventStorageMock,
        out Mock<IUnitOfWork> unitOfWorkMock,
        out Mock<ILogger<BookingService>> loggerMock,
        out Mock<ISemaphoreGetter> createMock,
        out Mock<ISemaphoreGetter> processMock)
    {
        bookingStorageMock = new Mock<IBookingRepository>();
        eventStorageMock = new Mock<IEventRepository>();
        unitOfWorkMock = new Mock<IUnitOfWork>();
        loggerMock = new Mock<ILogger<BookingService>>();
        createMock = new Mock<ISemaphoreGetter>();
        processMock = new Mock<ISemaphoreGetter>();

        return new BookingService(
            bookingStorageMock.Object,
            eventStorageMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object,
            createMock.Object,
            processMock.Object);
    }

    #region GetBookingByIdAsync

    [Fact]
    public async Task GetBookingByIdAsync_ValidId_ReturnSuccess()
    {
        var service = CreateService(out var bookingStorageMock, out var _, out var _, out var _, out var _, out var _);
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
        var service = CreateService(out var bookingStorageMock, out var _, out var _, out var _, out var _, out var _);
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
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _, out var semaphoreMock, out var _);
        var eventId = Guid.NewGuid();
        var bookingList = new List<Booking>();
        var bookEvent = new Event(eventId, "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount);
        var beforeCount = bookEvent.AvailableSeats;

        // Вызвали семафор для блокировки, потом для разблокировки
        semaphoreMock.Setup(s => s.SemaphoreSlim)
            .Returns(new SemaphoreSlim(1, 1))
            .Verifiable(Times.Exactly(2));

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

        semaphoreMock.Verify();
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
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _, out var semaphoreMock, out var _);
        var eventId = Guid.NewGuid();

        // Вызвали семафор для блокировки, потом для разблокировки
        semaphoreMock.Setup(s => s.SemaphoreSlim)
            .Returns(new SemaphoreSlim(1, 1))
            .Verifiable(Times.Exactly(2));

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

        semaphoreMock.Verify();
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
    }

    [Fact]
    public async Task CreateBookingAsync_NoSeatsAvailable_ReturnConflictError()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _, out var semaphoreMock, out var _);
        var eventId = Guid.NewGuid();

        // Вызвали семафор для блокировки, потом для разблокировки
        semaphoreMock.Setup(s => s.SemaphoreSlim)
            .Returns(new SemaphoreSlim(1, 1))
            .Verifiable(Times.Exactly(2));

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

        semaphoreMock.Verify();
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
    }

    [Fact]
    public async Task CreateBookingAsync_ParallelBookMoreThanSeats_NoOverbookingOccurs()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _, out var semaphoreMock, out var _);
        var semaphore = new SemaphoreSlim(1, 1);
        var eventId = Guid.NewGuid();
        var bookEvent = new Event(
            eventId,
            "Some title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            5);

        // Вызвали семафор для блокировки, потом для разблокировки
        semaphoreMock.Setup(s => s.SemaphoreSlim)
            .Returns(semaphore)
            .Verifiable(Times.Exactly(40));

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

        semaphoreMock.Verify();
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
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _, out var semaphoreMock, out var _);
        var semaphore = new SemaphoreSlim(1, 1);
        var eventId = Guid.NewGuid();
        var bookEvent = new Event(
            eventId,
            "Some title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10);

        // Вызвали семафор для блокировки, потом для разблокировки
        semaphoreMock.Setup(s => s.SemaphoreSlim)
            .Returns(semaphore)
            .Verifiable(Times.Exactly(20));

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

        semaphoreMock.Verify();
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
    public async Task ProcessBookingAsync_NotPending_DeclineWithoutProcess()
    {
        var service = CreateService(out var _, out var eventStorageMock, out var unitOfWorkMock, out var _, out var _, out var processMock);
        var book = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Confirmed, DateTime.UtcNow);

        // Не вызвали семафор для блокировки, потом для разблокировки
        processMock.Setup(s => s.SemaphoreSlim)
            .Verifiable(Times.Never);

        // Не запрашивали событие
        eventStorageMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), GetMode.Edit, TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не сохраняли изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);
   
        await service.ProcessBookingAsync(book, TestContext.Current.CancellationToken);
        // Меняем на Rejected и вызываем повторно
        book.Status = BookingStatus.Rejected;
        await service.ProcessBookingAsync(book, TestContext.Current.CancellationToken);

        processMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        book.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task ProcessBookingAsync_EventExits_ConfirmBook()
    {
        var service = CreateService(out var _, out var eventStorageMock, out var unitOfWorkMock, out var _, out var _, out var processMock);
        var eventId = Guid.NewGuid();
        var book = new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);

        // Вызвали семафор для блокировки, потом для разблокировки
        processMock.Setup(s => s.SemaphoreSlim)
            .Returns(new SemaphoreSlim(1, 1))
            .Verifiable(Times.Exactly(2));

        // Получили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(new Event(eventId, "Some text", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 1))
            .Verifiable(Times.Once);

        // Сохранили изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        await service.ProcessBookingAsync(book, TestContext.Current.CancellationToken);

        processMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        book.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        book.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task ProcessBookingAsync_EventNotExits_RejectBook()
    {
        var service = CreateService(out var _, out var eventStorageMock, out var unitOfWorkMock, out var _, out var _, out var processMock);
        var eventId = Guid.NewGuid();
        var book = new Booking(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);

        // Вызвали семафор для блокировки, потом для разблокировки
        processMock.Setup(s => s.SemaphoreSlim)
            .Returns(new SemaphoreSlim(1, 1))
            .Verifiable(Times.Exactly(2));

        // Событие не получено 
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?)null)
            .Verifiable(Times.Once);

        // Сохранили изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        await service.ProcessBookingAsync(book, TestContext.Current.CancellationToken);

        processMock.Verify();
        unitOfWorkMock.Verify();
        eventStorageMock.Verify();
        book.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        book.Status.Should().Be(BookingStatus.Rejected);
    }

    /*    [Fact]
        public async Task ProcessPendingBookingsAsync_InvalidCount_ThrowException()
        {
            var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var _, out var _, out var processMock);

            // Не пытались получить страницу
            bookingStorageMock.Setup(s => s.GetPageAsync(
                    It.IsAny<Expression<Func<Booking, bool>>?>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    TestContext.Current.CancellationToken))
                .Verifiable(Times.Never);

            var act = async () => await service.ProcessPendingBookingsAsync(0, TestContext.Current.CancellationToken);

            await act.Should()
                .ThrowExactlyAsync<ArgumentOutOfRangeException>();

            bookingStorageMock.Verify();
        }

        [Fact]
        public async Task ProcessPendingBookingsAsync_StorageDropError_ReturnError()
        {
            var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var _, out var _, out var processMock);
            var count = 100;

            // Вернулась ошибка
            bookingStorageMock.Setup(s => s.GetPageAsync(
                    It.IsAny<Expression<Func<Booking, bool>>?>(),
                    1,
                    count,
                    TestContext.Current.CancellationToken))
                .ThrowsAsync(new Exception())
                .Verifiable(Times.Once);

            var act = async () => await service.ProcessPendingBookingsAsync(count, TestContext.Current.CancellationToken);

            await act.Should()
                .ThrowExactlyAsync<Exception>();

            bookingStorageMock.Verify();
        }

        // Положительный сценарий особо не протестируешь, возможено разнести методы по разным классам и замокать
        [Fact]
        public async Task ProcessPendingBookingsAsync_SomePendingBookings_ReturnSuccess()
        {
            var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var _, out var _, out var processMock);
            var count = 100;
            // Вернулось несколько элементов
            bookingStorageMock.Setup(s => s.GetPageAsync(
                    It.IsAny<Expression<Func<Booking, bool>>?>(),
                    1,
                    count,
                    TestContext.Current.CancellationToken))
                .ReturnsAsync(new PaginatedResult<Booking> 
                { 
                    CurrentPage = 1,
                    FilteredCount = 3,
                    Items = 
                    [
                        new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow),
                        new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow),
                        new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow),
                    ],
                    TotalPages = 1
                })
                .Verifiable(Times.Once);

            var act = async () => await service.ProcessPendingBookingsAsync(count, TestContext.Current.CancellationToken);

            await act.Should().NotThrowAsync();
            bookingStorageMock.Verify();
        }*/
    #endregion
}
