using Application.Bookings.Implementation;
using Application.Bookings.Infrastructure;
using Application.Bookings.Settings;
using Domain.Bookings;
using Domain.Bookings.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Exceptions;
using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Abstract.Enums;

namespace Tests.Unit.Bookings;

public partial class BookingTests
{
    private const int MaxBookingCount = 5;

    private class MockHolder
    {
        public required Mock<IBookingRepository> BookingStorageMock { get; init; }
        public required Mock<IUnitOfWork> UnitOfWorkMock { get; init; }
        public required Mock<IUserContext> UserContextMock { get; init; }
    }

    private static BookingService CreateService(out MockHolder holder)
    {
        holder = new MockHolder
        {
            BookingStorageMock = new Mock<IBookingRepository>(),
            UnitOfWorkMock = new Mock<IUnitOfWork>(),
            UserContextMock = new Mock<IUserContext>(),
        };

        var optionsMock = new Mock<IOptions<BookingSettings>>();

        var settings = new BookingSettings { MaxBookingPerUser = MaxBookingCount };

        optionsMock.Setup(t => t.Value).Returns(settings);

        return new BookingService(
            holder.BookingStorageMock.Object,
            holder.UnitOfWorkMock.Object,
            holder.UserContextMock.Object,
            optionsMock.Object);
    }

    #region GetBookingByIdAsync

    [Fact]
    public async Task GetBookingByIdAsync_ValidId_ReturnSuccessfully()
    {
        // Arrange
        var service = CreateService(out var holder);
        var userId = Guid.NewGuid();
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            userId,
            BookingStatus.Pending,
            DateTime.UtcNow
        );

        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(bookingToReturn.Id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        holder.UserContextMock.Setup(s => s.UserId)
            .Returns(userId)
            .Verifiable(Times.Once);

        // Не вызывали, т.к. тот же пользователь
        holder.UserContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        var result = await service.GetBookingByIdAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingStorageMock.Verify();
        holder.UserContextMock.Verify();
        result.Should().BeEquivalentTo(bookingToReturn);
    }

    [Fact]
    public async Task GetBookingByIdAsync_InvalidId_ThrowNotFound()
    {
        // Arrange
        var service = CreateService(out var holder);
        var userId = Guid.NewGuid();

        var bookId = Guid.NewGuid();
        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(bookId, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking?)null)
            .Verifiable(Times.Once);

        // Не вызывали, т.к. выбросили исключение раньше
        holder.UserContextMock.Setup(s => s.UserId)
            .Verifiable(Times.Never);

        // Не вызывали, т.к. выбросили исключение раньше
        holder.UserContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.GetBookingByIdAsync(bookId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowExactlyAsync<NotFoundException>();
        holder.BookingStorageMock.Verify();
        holder.UserContextMock.Verify();
    }

    [Fact]
    public async Task GetBookingByIdAsync_BookingForAnotherUser_ThrowBookingOwnership()
    {
        // Arrange
        var service = CreateService(out var holder);
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

        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(booking.Id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(booking)
            .Verifiable(Times.Once);

        // Другой пользователь
        holder.UserContextMock.Setup(s => s.UserId)
            .Returns(userId)
            .Verifiable(Times.Once);

        // Не админ
        holder.UserContextMock.Setup(s => s.IsAdmin(TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        // Act
        var act = async () => await service.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowExactlyAsync<BookingOwnershipException>();
        holder.BookingStorageMock.Verify();
        holder.UserContextMock.Verify();
    }

    [Fact]
    public async Task GetBookingByIdAsync_AdminGetBookingForAnotherUser_ReturnSuccessfully()
    {
        // Arrange
        var service = CreateService(out var holder);
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

        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(bookingToReturn.Id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Другой пользователь
        holder.UserContextMock.Setup(s => s.UserId)
            .Returns(adminId)
            .Verifiable(Times.Once);

        // Действительно админ
        holder.UserContextMock.Setup(s => s.IsAdmin(TestContext.Current.CancellationToken))
            .ReturnsAsync(true)
            .Verifiable(Times.Once);

        // Act
        var result = await service.GetBookingByIdAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingStorageMock.Verify();
        holder.UserContextMock.Verify();
        result.Should().BeEquivalentTo(bookingToReturn);
    }

    #endregion

    #region CreateBookingAsync

    [Fact]
    public async Task CreateBookingAsync_NoLimitExceeded_ReturnSuccess()
    {
        // Arrange
        var service = CreateService(out var holder);
        var eventId = Guid.NewGuid();
        var bookingList = new List<Booking>();
        var userId = Guid.NewGuid();

        // Проверили пользователя
        holder.UserContextMock.Setup(s => s.UserId)
            .Returns(userId)
            .Verifiable(Times.AtLeastOnce);

        // Проверили количество броней пользователя
        holder.BookingStorageMock.Setup(s => s.GetCountActiveBookingForPersonAsync(
                userId,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(MaxBookingCount - 1)
            .Verifiable(Times.Once);

        // Успешно добавили бронь в хранилище
        holder.BookingStorageMock.Setup(s => s.AddAsync(
                Capture.In(bookingList),
                TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Успешно сохранили изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        var result = await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingStorageMock.Verify();
        holder.UnitOfWorkMock.Verify();
        result.Should().BeEquivalentTo(bookingList.First());
        bookingList.First().ProcessedAt.Should().BeNull();
        bookingList.First().CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        bookingList.First().EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task CreateBookingAsync_MaxLimitExceed_ThrowBookingLimitExceeded()
    {
        // Arrange
        var service = CreateService(out var holder);
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Проверили пользователя
        holder.UserContextMock.Setup(s => s.UserId)
            .Returns(userId)
            .Verifiable(Times.Once);

        // Проверили количество броней пользователя - максимум
        holder.BookingStorageMock.Setup(s => s.GetCountActiveBookingForPersonAsync(
                userId,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(MaxBookingCount)
            .Verifiable(Times.Once);

        // Не попытались сохранить бронь, потому что свободных мест нет
        holder.BookingStorageMock.Setup(s => s.AddAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Сохранение не вызывалось
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.CreateBookingAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<BookingLimitExceededException>()
            .WithMessage(BookingServiceErrors.ExceedBookingLimit(MaxBookingCount));

        holder.BookingStorageMock.Verify();
        holder.UnitOfWorkMock.Verify();
    }

    #endregion

    #region CancelBookingAsync

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    public async Task CancelBookingAsync_BookingAndEventExists_CancelSuccessfully(BookingStatus status)
    {
        // Arrange
        var service = CreateService(out var holder);
        var userId = Guid.NewGuid();
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            userId,
            status,
            DateTime.UtcNow
        );

        // Запросили бронь для редактирования
        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingToReturn.Id,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Сохранили изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Событие на того же пользователя
        holder.UserContextMock.Setup(s => s.UserId)
            .Returns(userId)
            .Verifiable(Times.Once);

        // Не вызывали, т.к. событие на того же пользователя
        holder.UserContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        await service.CancelBookingAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingStorageMock.Verify();
        holder.UnitOfWorkMock.Verify();
        holder.UserContextMock.Verify();
        bookingToReturn.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    public async Task CancelBookingAsync_AdminCancelOtherPersonBooking_CancelSuccessfully(BookingStatus status)
    {
        // Arrange
        var service = CreateService(out var holder);
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            userId,
            status,
            DateTime.UtcNow
        );

        // Запросили бронь для редактирования
        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingToReturn.Id,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Сохранили изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Событие на другого пользователя
        holder.UserContextMock.Setup(s => s.UserId)
            .Returns(adminId)
            .Verifiable(Times.Once);

        // Проверили, что запросил админ
        holder.UserContextMock.Setup(s => s.IsAdmin(TestContext.Current.CancellationToken))
            .ReturnsAsync(true)
            .Verifiable(Times.Once);

        // Act
        await service.CancelBookingAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        holder.BookingStorageMock.Verify();
        holder.UnitOfWorkMock.Verify();
        holder.UserContextMock.Verify();
        bookingToReturn.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelBookingAsync_BookingNotFound_ThrowNotFound()
    {
        // Arrange
        var service = CreateService(out var holder);
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        // Запросили бронь для редактирования, но ее нет
        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingId,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking?)null)
            .Verifiable(Times.Once);

        // Не сохраняли изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не вызывали, т.к. событие не найдено
        holder.UserContextMock.Setup(s => s.UserId)
            .Verifiable(Times.Never);

        // Не вызывали, т.к. событие не найдено
        holder.UserContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.CancelBookingAsync(bookingId, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<NotFoundException>()
            .WithMessage(BookingServiceErrors.BookingNotFound(bookingId));

        holder.BookingStorageMock.Verify();
        holder.UnitOfWorkMock.Verify();
        holder.UserContextMock.Verify();
    }

    [Fact]
    public async Task CancelBookingAsync_BookingAlreadyCancel_ThrowBookingCancelled()
    {
        // Arrange
        var service = CreateService(out var holder);
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
        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingToReturn.Id,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Не сохраняли изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // На всякий случай все изменения откачены и транзакция закрыта
        holder.UnitOfWorkMock.Setup(s => s.RollbackChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Не вызывали, т.к. событие уже отменено
        holder.UserContextMock.Setup(s => s.UserId)
            .Verifiable(Times.Never);

        // Не вызывали, т.к. событие уже отменено
        holder.UserContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.CancelBookingAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<InvalidBookingOperationException>()
            .WithMessage(BookingServiceErrors.BookingAlreadyCancelled(bookingToReturn.Id));

        holder.BookingStorageMock.Verify();
        holder.UnitOfWorkMock.Verify();
        holder.UserContextMock.Verify();
    }

    [Fact]
    public async Task CancelBookingAsync_BookingRejected_ThrowBookingCancelled()
    {
        // Arrange
        var service = CreateService(out var holder);
        var userId = Guid.NewGuid();
        var bookingToReturn = new Booking
        (
            Guid.NewGuid(),
            Guid.NewGuid(),
            userId,
            BookingStatus.Rejected,
            DateTime.UtcNow
        );

        // Запросили бронь для редактирования, но она была отклонена
        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingToReturn.Id,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Не сохраняли изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // На всякий случай все изменения откачены и транзакция закрыта
        holder.UnitOfWorkMock.Setup(s => s.RollbackChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Не вызывали, т.к. уже отклонено
        holder.UserContextMock.Setup(s => s.UserId)
            .Verifiable(Times.Never);

        // Не вызывали, т.к. уже отклонено
        holder.UserContextMock.Setup(s => s.IsAdmin(It.IsAny<CancellationToken>()))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.CancelBookingAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<InvalidBookingOperationException>()
            .WithMessage(BookingServiceErrors.BookingRejected(bookingToReturn.Id));

        holder.BookingStorageMock.Verify();
        holder.UnitOfWorkMock.Verify();
        holder.UserContextMock.Verify();
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    public async Task CancelBookingAsync_UserCancelOtherPersonBooking_ThrowBookingOwnership(BookingStatus status)
    {
        // Arrange
        var service = CreateService(out var holder);
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
        holder.BookingStorageMock.Setup(s => s.GetByIdAsync(
                bookingToReturn.Id,
                GetMode.Edit,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(bookingToReturn)
            .Verifiable(Times.Once);

        // Не сохраняли изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // На всякий случай все изменения откачены и транзакция закрыта
        holder.UnitOfWorkMock.Setup(s => s.RollbackChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Событие на другого пользователя
        holder.UserContextMock.Setup(s => s.UserId)
            .Returns(userThatTryingToCancel)
            .Verifiable(Times.Once);

        // Простой пользователь без админки
        holder.UserContextMock.Setup(s => s.IsAdmin(TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        // Act
        var act = async () => await service.CancelBookingAsync(bookingToReturn.Id, TestContext.Current.CancellationToken);

        // Assert 
        await act.Should()
            .ThrowExactlyAsync<BookingOwnershipException>();

        holder.BookingStorageMock.Verify();
        holder.UnitOfWorkMock.Verify();
        holder.UserContextMock.Verify();
    }

    #endregion
}
