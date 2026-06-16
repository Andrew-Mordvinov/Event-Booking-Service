using Application.Implementation;
using Application.Infrastructure;
using Application.Infrastructure.Common;
using Application.Infrastructure.Enums;
using Application.Settings;
using Domain.Bookings;
using Domain.Events;
using Domain.Exceptions;
using Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Unit.Bookings;

public partial class BookingTests
{
    private const int MaxBookingCount = 5;

    private static BookingService CreateService(
        out Mock<IBookingRepository> bookingStorageMock,
        out Mock<IEventRepository> eventStorageMock,
        out Mock<IUnitOfWork> unitOfWorkMock)
    {
        bookingStorageMock = new Mock<IBookingRepository>();
        eventStorageMock = new Mock<IEventRepository>();
        unitOfWorkMock = new Mock<IUnitOfWork>();     
        
        var loggerMock = new Mock<ILogger<BookingService>>();
        var optionsMock = new Mock<IOptions<BookingSettings>>();

        var settings = new BookingSettings { MaxBookingPerUser = MaxBookingCount };

        optionsMock.Setup(t => t.Value).Returns(settings);

        return new BookingService(
            bookingStorageMock.Object,
            eventStorageMock.Object,
            unitOfWorkMock.Object,
            optionsMock.Object,
            loggerMock.Object);
    }

    private static User CreateUser() => new(Guid.NewGuid(), "user", "somehash", Roles.User);
    private static User CreateAdmin() => new(Guid.NewGuid(), "admin", "somehash", Roles.Admin);

    #region GetBookingByIdAsync

    // TODO Надо внедрить юзеров. Возможно хэлпер типа createuser/createadmin для начала, тут вроде нет массовой выборки
    // Добавить тесты на новые кейсы, что пользак не имеет прав

    [Fact]
    public async Task GetBookingByIdAsync_ValidId_ReturnSuccessfully()
    {
        var service = CreateService(out var bookingStorageMock, out var _, out var _);
        var user = CreateUser();
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            user.Id,
            BookingStatus.Pending,
            DateTime.UtcNow,
            user: user
        );

        bookingStorageMock.Setup(s => s.GetByIdAsync(bookingToReturn.Id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        var result = await service.GetBookingByIdAsync(bookingToReturn.Id, user.Id, TestContext.Current.CancellationToken);

        bookingStorageMock.Verify();
        result.Should().BeEquivalentTo(bookingToReturn);
    }

    [Fact]
    public async Task GetBookingByIdAsync_InvalidId_ThrowNotFound()
    {
        var service = CreateService(out var bookingStorageMock, out var _, out var _);
        var user = CreateUser();

        var bookId = Guid.NewGuid();
        bookingStorageMock.Setup(s => s.GetByIdAsync(bookId, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking?)null)
            .Verifiable(Times.Once);

        var act = async () => await service.GetBookingByIdAsync(bookId, user.Id, TestContext.Current.CancellationToken);

        await act.Should().ThrowExactlyAsync<NotFoundException>();
        bookingStorageMock.Verify();
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingForAnotherUser_ThrowBookingOwnershipError()
    {

    }

    [Fact]
    public async Task GetBookingByIdAsync_AdminGetBookingForAnotherUser_ReturnSuccessfully()
    {

    }

    #endregion

    #region CreateBookingAsync

    [Fact]
    public async Task CreateBookingAsync_EventExistsAndHasSeats_ReturnSuccess()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock);
        var eventId = Guid.NewGuid();
        var bookingList = new List<Booking>();
        var user = CreateUser();
        var bookEvent = new Event(eventId, "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount);
        var beforeCount = bookEvent.AvailableSeats;

        // Проверили количество броней пользователя
        bookingStorageMock.Setup(s => s.GetCountActiveBookingForPersonAsync(
                user.Id,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(MaxBookingCount - 1)
            .Verifiable(Times.Once);

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

        var result = await service.CreateBookingAsync(eventId, user.Id, TestContext.Current.CancellationToken);

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
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock);
        var user = CreateUser();
        var eventId = Guid.NewGuid();

        // Проверили количество броней пользователя
        bookingStorageMock.Setup(s => s.GetCountActiveBookingForPersonAsync(
                user.Id,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(MaxBookingCount - 1)
            .Verifiable(Times.Once);

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

        var act = async () => await service.CreateBookingAsync(eventId, user.Id, TestContext.Current.CancellationToken);

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
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock);
        var user = CreateUser();
        var eventId = Guid.NewGuid();

        // Проверили количество броней пользователя
        bookingStorageMock.Setup(s => s.GetCountActiveBookingForPersonAsync(
                user.Id,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(MaxBookingCount - 1)
            .Verifiable(Times.Once);

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

        var act = async () => await service.CreateBookingAsync(eventId, user.Id, TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowExactlyAsync<ConflictException>()
            .WithMessage(BookingServiceErrors.NoAvailableSeats);

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
    }

    [Fact]
    public async Task CreateBookingAsync_MaxLimitExceed_ReturnBookingLimitExceededError()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock);
        var user = CreateUser();
        var eventId = Guid.NewGuid();

        // Проверили количество броней пользователя - максимум
        bookingStorageMock.Setup(s => s.GetCountActiveBookingForPersonAsync(
                user.Id,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(MaxBookingCount)
            .Verifiable(Times.Once);

        // Не запрашивали событие
        eventStorageMock.Setup(s => s.GetByIdAsync(eventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не попытались сохранить бронь, потому что свободных мест нет
        bookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Сохранение не вызывалось
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var act = async () => await service.CreateBookingAsync(eventId, user.Id, TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowExactlyAsync<BookingLimitExceededException>()
            .WithMessage(BookingServiceErrors.ExceedBookingLimit(MaxBookingCount));

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
    }

    #endregion

    #region ProcessBookingAsync

    [Fact]
    public async Task ProcessBookingAsync_EventExits_ConfirmBook()
    {
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock);
        var eventId = Guid.NewGuid();
        var user = CreateUser();
        var book = new Booking(Guid.NewGuid(), eventId, user.Id, BookingStatus.Pending, DateTime.UtcNow);

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
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock);
        var eventId = Guid.NewGuid();
        var user = CreateUser();
        var book = new Booking(Guid.NewGuid(), eventId, user.Id, BookingStatus.Pending, DateTime.UtcNow);

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
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock);
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
