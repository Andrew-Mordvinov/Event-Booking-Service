using Application.Implementation;
using Application.Infrastructure;
using Application.Infrastructure.Common;
using Application.Infrastructure.Enums;
using Application.Settings;
using Domain.Bookings;
using Domain.Events;
using Domain.Exceptions;
using Domain.Exceptions.Bookings;
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
        out Mock<IUnitOfWork> unitOfWorkMock,
        out Mock<IUserContext> userContextMock)
    {
        bookingStorageMock = new Mock<IBookingRepository>();
        eventStorageMock = new Mock<IEventRepository>();
        unitOfWorkMock = new Mock<IUnitOfWork>();
        userContextMock = new Mock<IUserContext>();     
        
        var loggerMock = new Mock<ILogger<BookingService>>();
        var optionsMock = new Mock<IOptions<BookingSettings>>();

        var settings = new BookingSettings { MaxBookingPerUser = MaxBookingCount };

        optionsMock.Setup(t => t.Value).Returns(settings);

        return new BookingService(
            bookingStorageMock.Object,
            eventStorageMock.Object,
            unitOfWorkMock.Object,
            userContextMock.Object,
            optionsMock.Object,
            loggerMock.Object);
    }

    #region GetBookingByIdAsync

    [Fact]
    public async Task GetBookingByIdAsync_ValidId_ReturnSuccessfully()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var _, out var _, out var userContextMock);
        var userId = Guid.NewGuid();
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            userId,
            BookingStatus.Pending,
            DateTime.UtcNow
        );

        bookingStorageMock.Setup(s => s.GetByIdAsync(bookingToReturn.Id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        userContextMock.Setup(s => s.UserId)
            .Returns(userId)
            .Verifiable(Times.Once);

        // Не вызывали, т.к. тот же пользователь
        userContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        var result = await service.GetBookingByIdAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        bookingStorageMock.Verify();
        userContextMock.Verify();
        result.Should().BeEquivalentTo(bookingToReturn);
    }

    [Fact]
    public async Task GetBookingByIdAsync_InvalidId_ThrowNotFound()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var _, out var _, out var userContextMock);
        var userId = Guid.NewGuid();

        var bookId = Guid.NewGuid();
        bookingStorageMock.Setup(s => s.GetByIdAsync(bookId, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking?)null)
            .Verifiable(Times.Once);

        // Не вызывали, т.к. выбросили исключение раньше
        userContextMock.Setup(s => s.UserId)
            .Verifiable(Times.Never);

        // Не вызывали, т.к. выбросили исключение раньше
        userContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.GetBookingByIdAsync(bookId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowExactlyAsync<NotFoundException>();
        bookingStorageMock.Verify();
        userContextMock.Verify();
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingForAnotherUser_ThrowBookingOwnership()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var _, out var _, out var userContextMock);
        var userId = Guid.NewGuid();
        var booking = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            // Бронирование на другого
            Guid.NewGuid(),
            BookingStatus.Pending,
            DateTime.UtcNow
        );

        bookingStorageMock.Setup(s => s.GetByIdAsync(booking.Id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(booking)
            .Verifiable(Times.Once);

        // Другой пользователь
        userContextMock.Setup(s => s.UserId)
            .Returns(userId)
            .Verifiable(Times.Once);

        // Не админ
        userContextMock.Setup(s => s.IsAdmin(TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        // Act
        var act = async () => await service.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowExactlyAsync<BookingOwnershipException>();
        bookingStorageMock.Verify();
        userContextMock.Verify();
    }

    [Fact]
    public async Task GetBookingByIdAsync_AdminGetBookingForAnotherUser_ReturnSuccessfully()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var _, out var _, out var userContextMock);
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            // Бронирование на другого
            userId,
            BookingStatus.Pending,
            DateTime.UtcNow
        );

        bookingStorageMock.Setup(s => s.GetByIdAsync(bookingToReturn.Id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Другой пользователь
        userContextMock.Setup(s => s.UserId)
            .Returns(adminId)
            .Verifiable(Times.Once);

        // Действительно админ
        userContextMock.Setup(s => s.IsAdmin(TestContext.Current.CancellationToken))
            .ReturnsAsync(true)
            .Verifiable(Times.Once);

        // Act
        var result = await service.GetBookingByIdAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        bookingStorageMock.Verify();
        userContextMock.Verify();
        result.Should().BeEquivalentTo(bookingToReturn);
    }

    #endregion

    #region CreateBookingAsync

    [Fact]
    public async Task CreateBookingAsync_EventExistsAndHasSeats_ReturnSuccess()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var eventId = Guid.NewGuid();
        var bookingList = new List<Booking>();
        var userId = Guid.NewGuid();
        var bookEvent = new Event(eventId, "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount);
        var beforeCount = bookEvent.AvailableSeats;

        // Проверили количество броней пользователя
        bookingStorageMock.Setup(s => s.GetCountActiveBookingForPersonAsync(
                userId,
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

        // Act
        var result = await service.CreateBookingAsync(eventId, userId, TestContext.Current.CancellationToken);

        // Assert
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
    public async Task CreateBookingAsync_EventDoesNotExists_ThrowNotFound()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Проверили количество броней пользователя
        bookingStorageMock.Setup(s => s.GetCountActiveBookingForPersonAsync(
                userId,
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

        // Act
        var act = async () => await service.CreateBookingAsync(eventId, userId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<NotFoundException>()
            .WithMessage(BookingServiceErrors.EventNotFound(eventId));

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
    }

    [Fact]
    public async Task CreateBookingAsync_NoSeatsAvailable_ThrowConflict()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Проверили количество броней пользователя
        bookingStorageMock.Setup(s => s.GetCountActiveBookingForPersonAsync(
                userId,
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

        // Act
        var act = async () => await service.CreateBookingAsync(eventId, userId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<ConflictException>()
            .WithMessage(BookingServiceErrors.NoAvailableSeats);

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
    }

    [Fact]
    public async Task CreateBookingAsync_MaxLimitExceed_ThrowBookingLimitExceeded()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Проверили количество броней пользователя - максимум
        bookingStorageMock.Setup(s => s.GetCountActiveBookingForPersonAsync(
                userId,
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

        // Act
        var act = async () => await service.CreateBookingAsync(eventId, userId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<BookingLimitExceededException>()
            .WithMessage(BookingServiceErrors.ExceedBookingLimit(MaxBookingCount));

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
    }

    #endregion

    #region CancelBookingAsync

    [Fact]
    public async Task CancelBookingAsync_BookingAndEventExists_CancelSuccessfully()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var userContextMock);
        var userId = Guid.NewGuid();
        var bookEvent = new Event(Guid.NewGuid(), "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount, CorrectSeatsCount - 1);
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            bookEvent.Id,
            userId,
            BookingStatus.Pending,
            DateTime.UtcNow
        );

        // Запросили бронь для редактирования
        bookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingToReturn.Id,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Запросили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(bookEvent.Id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookEvent)
            .Verifiable(Times.Once);

        // Сохранили изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Событие на того же пользователя
        userContextMock.Setup(s => s.UserId)
            .Returns(userId)
            .Verifiable(Times.Once);

        // Не вызывали, т.к. событие на того же пользователя
        userContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        await service.CancelBookingAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        userContextMock.Verify();
        bookingToReturn.Status.Should().Be(BookingStatus.Cancelled);
        bookEvent.AvailableSeats.Should().Be(CorrectSeatsCount);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Confirmed)]
    public async Task CancelBookingAsync_AdminCancelOtherPersonBooking_CancelSuccessfully(BookingStatus status)
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var userContextMock);
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var bookEvent = new Event(Guid.NewGuid(), "SomeTitle", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), CorrectSeatsCount, CorrectSeatsCount - 1);
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            bookEvent.Id,
            userId,
            status,
            DateTime.UtcNow
        );

        // Запросили бронь для редактирования
        bookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingToReturn.Id,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Запросили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(bookEvent.Id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookEvent)
            .Verifiable(Times.Once);

        // Сохранили изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Событие на другого пользователя
        userContextMock.Setup(s => s.UserId)
            .Returns(adminId)
            .Verifiable(Times.Once);

        // Проверили, что запросил админ
        userContextMock.Setup(s => s.IsAdmin(TestContext.Current.CancellationToken))
            .ReturnsAsync(true)
            .Verifiable(Times.Once);

        // Act
        await service.CancelBookingAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        userContextMock.Verify();
        bookingToReturn.Status.Should().Be(BookingStatus.Cancelled);
        bookEvent.AvailableSeats.Should().Be(CorrectSeatsCount);
    }

    [Fact]
    public async Task CancelBookingAsync_BookingNotFound_ThrowNotFound()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var userContextMock);
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        // Запросили бронь для редактирования, но ее нет
        bookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingId,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking?)null)
            .Verifiable(Times.Once);

        // Не запросили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), GetMode.Edit, TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не сохраняли изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не вызывали, т.к. событие не найдено
        userContextMock.Setup(s => s.UserId)
            .Verifiable(Times.Never);

        // Не вызывали, т.к. событие не найдено
        userContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.CancelBookingAsync(bookingId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<NotFoundException>()
            .WithMessage(BookingServiceErrors.BookingNotFound(bookingId));

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        userContextMock.Verify();
    }

    [Fact]
    public async Task CancelBookingAsync_BookingAlreadyCancel_ThrowBookingCancelled()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var userContextMock);
        var userId = Guid.NewGuid();
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            userId,
            BookingStatus.Cancelled,
            DateTime.UtcNow
        );

        // Запросили бронь для редактирования, но она уже отменена
        bookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingToReturn.Id,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Не запросили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), GetMode.Edit, TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не сохраняли изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // На всякий случай все изменения откачены и транзакция закрыта
        unitOfWorkMock.Setup(s => s.RollbackChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Не вызывали, т.к. событие уже отменено
        userContextMock.Setup(s => s.UserId)
            .Verifiable(Times.Never);

        // Не вызывали, т.к. событие уже отменено
        userContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.CancelBookingAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<BookingCancelledException>();

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        userContextMock.Verify();
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Confirmed)]
    public async Task CancelBookingAsync_UserCancelOtherPersonBooking_ThrowBookingOwnership(BookingStatus status)
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var userContextMock);
        var userThatTryingToCancel = Guid.NewGuid();
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            DateTime.UtcNow
        );

        // Запросили бронь для редактирования
        bookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingToReturn.Id,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Не запросили событие
        eventStorageMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), GetMode.Edit, TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не сохраняли изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // На всякий случай все изменения откачены и транзакция закрыта
        unitOfWorkMock.Setup(s => s.RollbackChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Событие на другого пользователя
        userContextMock.Setup(s => s.UserId)
            .Returns(userThatTryingToCancel)
            .Verifiable(Times.Once);

        // Простой пользователь без админки
        userContextMock.Setup(s => s.IsAdmin(TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        // Act
        var act = async () => await service.CancelBookingAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert 
        await act.Should()
            .ThrowExactlyAsync<BookingOwnershipException>();

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        userContextMock.Verify();
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Confirmed)]
    public async Task CancelBookingAsync_EventNotFound_ThrowNotFound(BookingStatus status)
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var userContextMock);
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            DateTime.UtcNow
        );

        // Запросили бронь для редактирования, но она уже отменена
        bookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingToReturn.Id,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Запросили событие, но его нет
        eventStorageMock.Setup(s => s.GetByIdAsync(bookingToReturn.EventId, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?)null)
            .Verifiable(Times.Once);

        // Не сохраняли изменения
        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // На всякий случай все изменения откачены и транзакция закрыта
        unitOfWorkMock.Setup(s => s.RollbackChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Событие на того же пользователя
        userContextMock.Setup(s => s.UserId)
            .Returns(bookingToReturn.UserId)
            .Verifiable(Times.Once);

        // Не вызывали, т.к. событие на того же пользователя
        userContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.CancelBookingAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert 
        await act.Should()
            .ThrowExactlyAsync<NotFoundException>()
            .WithMessage(BookingServiceErrors.EventNotFound(bookingToReturn.EventId));

        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        userContextMock.Verify();
    }

    #endregion

    #region ProcessBookingAsync

    [Fact]
    public async Task ProcessBookingAsync_EventExits_ConfirmBook()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var eventId = Guid.NewGuid();
        var book = new Booking(Guid.NewGuid(), eventId, Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);

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

        // Act
        await service.ProcessBookingAsync(book.Id, TestContext.Current.CancellationToken);

        // Assert
        bookingStorageMock.Verify();
        eventStorageMock.Verify();
        unitOfWorkMock.Verify();
        book.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        book.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task ProcessBookingAsync_EventNotExits_RejectBook()
    {
        // Arrange
        var service = CreateService(out var bookingStorageMock, out var eventStorageMock, out var unitOfWorkMock, out var _);
        var eventId = Guid.NewGuid();
        var book = new Booking(Guid.NewGuid(), eventId, Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);

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

        // Act
        await service.ProcessBookingAsync(book.Id, TestContext.Current.CancellationToken);

        // Assert
        bookingStorageMock.Verify();
        unitOfWorkMock.Verify();
        eventStorageMock.Verify();
        book.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        book.Status.Should().Be(BookingStatus.Rejected);
    }

    [Fact]
    public async Task ProcessBookingAsync_BookNotExits_RejectBook()
    {
        // Arrange
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

        // Act
        await service.ProcessBookingAsync(bookId, TestContext.Current.CancellationToken);

        // Assert
        bookingStorageMock.Verify();
        unitOfWorkMock.Verify();
        eventStorageMock.Verify();
    }

    #endregion
}
