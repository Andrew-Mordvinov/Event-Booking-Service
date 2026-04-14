using Bookings.Models;
using Bookings.Service.Implementation;
using DataAccess.Storage;
using Events.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Locking;
using Validation;

namespace Tests.Bookings;

public partial class BookingTests
{
    private static BookingService CreateService(
        out Mock<IStorage<Booking>> bookingStorageMock,
        out Mock<IStorage<Event>> eventStorageMock,
        out Mock<ILogger<BookingService>> loggerMock,
        out Mock<ISemaphoreGetter> createMock,
        out Mock<ISemaphoreGetter> processMock)
    {
        bookingStorageMock = new Mock<IStorage<Booking>>();
        eventStorageMock = new Mock<IStorage<Event>>();
        loggerMock = new Mock<ILogger<BookingService>>();
        createMock = new Mock<ISemaphoreGetter>();
        processMock = new Mock<ISemaphoreGetter>();

        return new BookingService(
            bookingStorageMock.Object,
            eventStorageMock.Object,
            loggerMock.Object,
            createMock.Object,
            processMock.Object);
    }

    #region GetBookingByIdAsync

    [Fact]
    public async Task GetBookingByIdAsync_ValidId_ReturnSuccess()
    {
        var service = CreateService(out var bookingStorageMock, out var _, out var _, out var _, out var _);
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
        var service = CreateService(out var bookingStorageMock, out var _, out var _, out var _, out var _);
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
    public async Task CreateBookingAsync_EventExistsAndHasSeats_ReturnSuccess()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var _, out var semaphoreMock, out var _);
        var eventId = Guid.NewGuid();
        var bookingList = new List<Booking>();
        var bookEvent = new Event(eventId, "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount);
        var beforeCount = bookEvent.AvailableSeats;

        // Вызвали семафор для блокировки, потом для разблокировки
        semaphoreMock.Setup(s => s.SemaphoreSlim)
            .Returns(new SemaphoreSlim(1, 1))
            .Verifiable(Times.Exactly(2));

        // Успешно получили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(bookEvent))
            .Verifiable(Times.Once);

        // Успешно добавили бронь в хранилище
        bookingStorageMock.Setup(s => s.AddAsync(
                Capture.In(bookingList),
                TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success<Booking>(null))
            .Verifiable(Times.Once);

        // Успешно обновили событие
        eventStorageMock.Setup(s => s.UpdateAsync(bookEvent, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(true))
            .Verifiable(Times.Once);

        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        semaphoreMock.Verify();
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(bookingList.First());
        bookingList.First().ProcessedAt.Should().BeNull();
        bookingList.First().CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        bookingList.First().EventId.Should().Be(eventId);
        bookEvent.AvailableSeats.Should().Be(beforeCount - 1);
    }

    [Fact]
    public async Task CreateBookingAsync_EventDoesNotExists_SuccessWithoutValue()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var _, out var semaphoreMock, out var _);
        var eventId = Guid.NewGuid();

        // Вызвали семафор для блокировки, потом для разблокировки
        semaphoreMock.Setup(s => s.SemaphoreSlim)
            .Returns(new SemaphoreSlim(1, 1))
            .Verifiable(Times.Exactly(2));

        // Попытались получить событие, но его не оказалось
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success<Event>(null))
            .Verifiable(Times.Once);

        // Не создали бронь и не пытались добавить ничего в хранилище
        bookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Обновление события не вызывалось
        eventStorageMock.Setup(s => s.UpdateAsync(It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        semaphoreMock.Verify();
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_GetEventFailed_ReturnErrors()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var _, out var semaphoreMock, out var _);
        var eventId = Guid.NewGuid();

        // Вызвали семафор для блокировки, потом для разблокировки
        semaphoreMock.Setup(s => s.SemaphoreSlim)
            .Returns(new SemaphoreSlim(1, 1))
            .Verifiable(Times.Exactly(2));

        // Попытались получить событие, но ошибка
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Fail<Event>(null, "Произошла ошибка"))
            .Verifiable(Times.Once);

        // Не создали бронь и не пытались добавить ничего в хранилище
        bookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Обновление события не вызывалось
        eventStorageMock.Setup(s => s.UpdateAsync(It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        semaphoreMock.Verify();
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_BookingStorageErrorOccured_ReturnErrors()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var _, out var semaphoreMock, out var _);
        var eventId = Guid.NewGuid();

        // Вызвали семафор для блокировки, потом для разблокировки
        semaphoreMock.Setup(s => s.SemaphoreSlim)
            .Returns(new SemaphoreSlim(1, 1))
            .Verifiable(Times.Exactly(2));

        // Успешно получили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(new Event(eventId, "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount)))
            .Verifiable(Times.Once);

        // Попытались сохранить бронь - ошибка
        bookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Fail<Booking>(null, "Ошибка сохранения"))
            .Verifiable(Times.Once);

        // Обновление события не вызывалось
        eventStorageMock.Setup(s => s.UpdateAsync(It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        semaphoreMock.Verify();
        bookingStorageMock.Verify();
        eventStorageMock.Verify(); 
        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_NoSeatsAvailable_ReturnConflictError()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var _, out var semaphoreMock, out var _);
        var eventId = Guid.NewGuid();

        // Вызвали семафор для блокировки, потом для разблокировки
        semaphoreMock.Setup(s => s.SemaphoreSlim)
            .Returns(new SemaphoreSlim(1, 1))
            .Verifiable(Times.Exactly(2));

        // Успешно получили событие без свободных мест
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(new Event(eventId, "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount, 0)))
            .Verifiable(Times.Once);

        // Не попытались сохранить бронь, потому что свободных мест нет
        bookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Обновление события не вызывалось
        eventStorageMock.Setup(s => s.UpdateAsync(It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        semaphoreMock.Verify();
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        result.IsSuccessful.Should().BeFalse();
        result.Errors.Should().BeEquivalentTo([new ValidationItem(BookingServiceErrors.NoAvailableSeats, ItemCategory.ConflictError)]);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_ParallelBookMoreThanSeats_NoOverbookingOccurs()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var _, out var semaphoreMock, out var _);
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
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(bookEvent))
            .Verifiable(Times.Exactly(20));

        // Успешно добавили бронь в хранилище (только 5 мест)
        bookingStorageMock.Setup(s => s.AddAsync(
                It.IsAny<Booking>(),
                TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success<Booking>(null))
            .Verifiable(Times.Exactly(5));

        // Успешно обновили событие (только 5 мест)
        eventStorageMock.Setup(s => s.UpdateAsync(bookEvent, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(true))
            .Verifiable(Times.Exactly(5));

        var arrayOfRequests = new Task<ValidationResult<Booking?>>[20];

        for (int i = 0; i < 20; i++)
        {
            arrayOfRequests[i] = Task.Run(() => service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(arrayOfRequests);

        semaphoreMock.Verify();
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        bookEvent.AvailableSeats.Should().Be(0);
        arrayOfRequests.Where(t => t.Result.Value is not null && t.Result.IsSuccessful is true).Count().Should().Be(5);
        arrayOfRequests.Where(t => t.Result.Value is null && t.Result.HasCategory(ItemCategory.ConflictError)).Count().Should().Be(15);
    }


    [Fact]
    public async Task CreateBookingAsync_ParallelBook_UniqueIdOfBooks()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var _, out var semaphoreMock, out var _);
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
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(bookEvent))
            .Verifiable(Times.Exactly(10));

        // Успешно добавили бронь в хранилище
        bookingStorageMock.Setup(s => s.AddAsync(
                It.IsAny<Booking>(),
                TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success<Booking>(null))
            .Verifiable(Times.Exactly(10));

        // Успешно обновили событие
        eventStorageMock.Setup(s => s.UpdateAsync(bookEvent, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(true))
            .Verifiable(Times.Exactly(10));

        var arrayOfRequests = new Task<ValidationResult<Booking?>>[10];

        for (int i = 0; i < 10; i++)
        {
            arrayOfRequests[i] = Task.Run(() => service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(arrayOfRequests);

        semaphoreMock.Verify();
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        bookEvent.AvailableSeats.Should().Be(0);
        arrayOfRequests.Count(t => t.Result.Value is not null && t.Result.IsSuccessful is true).Should().Be(10);
        arrayOfRequests.Select(t => t.Result.Value!.Id).Distinct().Count().Should().Be(10);
    }

    //TODO тест на обработку одной брони
    //Тесты на общий запуск обработки броней

    #endregion
}
