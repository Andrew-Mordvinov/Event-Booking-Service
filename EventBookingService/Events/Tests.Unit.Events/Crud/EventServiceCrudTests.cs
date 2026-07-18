using Application.Events.DTO.Requests;
using Application.Events.Implementation;
using Application.Events.Infrastructure;

using Domain.Events;

using FluentAssertions;

using Moq;

using Shared.Exceptions;
using Shared.Infrastructure.Abstract;
using Shared.Infrastructure.Abstract.Enums;

namespace Tests.Unit.Events.Crud;

public partial class EventServiceCrudTests
{
    private class Holder
    {
        public required Mock<IEventRepository> RepositoryMock { get; init; }
        public required Mock<IEventCache> CacheMock { get; init; }
        public required Mock<IUnitOfWork> UnitOfWorkMock { get; init; }
    }

    private static EventService CreateService(out Holder holder)
    {
        holder = new Holder
        {
            RepositoryMock = new Mock<IEventRepository>(),
            CacheMock = new Mock<IEventCache>(),
            UnitOfWorkMock = new Mock<IUnitOfWork>(),
        };

        return new EventService(holder.RepositoryMock.Object, holder.CacheMock.Object, holder.UnitOfWorkMock.Object);
    }

    private static Event CreateTestEvent(Guid id) =>
        new(id, "Some text", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), 10, 5, "description");

    // TODO плюс новый метод

    #region GetEvent

    [Fact]
    public async Task GetEvent_ExistingEventIdCacheHit_ReturnedFromCache()
    {
        // Arrange
        var service = CreateService(out var holder);
        var id = Guid.NewGuid();
        var @event = CreateTestEvent(id);

        // Получение из кэша успешно
        holder.CacheMock.Setup(s => s.GetEventAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync((true, @event))
            .Verifiable(Times.Once);

        // Не трогали базу
        holder.RepositoryMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), GetMode.Readonly, TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не устанавливали значение кэша
        holder.CacheMock.Setup(s => s.SetEventAsync(It.IsAny<Guid>(), It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var result = await service.GetEventByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        result.Should().BeEquivalentTo(@event);
    }

    [Fact]
    public async Task GetEvent_ExistingEventIdCacheMiss_ReturnFromDb()
    {
        // Arrange
        var service = CreateService(out var holder);
        var id = Guid.NewGuid();
        var @event = CreateTestEvent(id);

        // В кэше нет
        holder.CacheMock.Setup(s => s.GetEventAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync((false, null))
            .Verifiable(Times.Once);

        // Нашли в БД
        holder.RepositoryMock.Setup(s => s.GetByIdAsync(id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(@event)
            .Verifiable(Times.Once);

        // Установили в кэш
        holder.CacheMock.Setup(s => s.SetEventAsync(@event.Id, @event, TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        var result = await service.GetEventByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        result.Should().BeEquivalentTo(@event);
    }

    [Fact]
    public async Task GetEvent_BadId_ThrowNotFound()
    {
        // Arrange
        var service = CreateService(out var holder);
        var id = Guid.NewGuid();

        // В кэше нет
        holder.CacheMock.Setup(s => s.GetEventAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync((false, null))
            .Verifiable(Times.Once);

        // Не нашли в БД
        holder.RepositoryMock.Setup(s => s.GetByIdAsync(id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?) null)
            .Verifiable(Times.Once);

        // Не установили в кэш, так как нет ничего
        holder.CacheMock.Setup(s => s.SetEventAsync(It.IsAny<Guid>(), It.IsAny<Event?>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.GetEventByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowExactlyAsync<NotFoundException>();
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
    }

    #endregion

    #region CreateEvent

    [Theory]
    [MemberData(nameof(CreateEvent_ValidModel))]
    public async Task CreateEvent_ValidModel_SuccessfullyReturned(CreateEventRequest request, Event expected)
    {
        // Arrange
        var service = CreateService(out var holder);
        var capturedEvents = new List<Event>();
        var capturedIds = new List<Guid>();

        // Добавили в репо
        holder.RepositoryMock.Setup(s => s.AddAsync(Capture.In(capturedEvents), TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Зафиксировали изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Установили кэш
        holder.CacheMock.Setup(s => s.SetEventAsync(Capture.In(capturedIds), Capture.In(capturedEvents), TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        var result = await service.CreateEventAsync(request, TestContext.Current.CancellationToken);

        // Assert
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        result.Should().NotBeNull();
        // Id выделяется динамически, его не проверяем
        result.Should().BeEquivalentTo(expected, options => options.Excluding(e => e.Id));
        capturedEvents.Should().AllBeEquivalentTo(expected, options => options.Excluding(e => e.Id));
        capturedIds.Should().AllBeEquivalentTo(result.Id);
    }

    [Theory]
    [MemberData(nameof(CreateEvent_InvalidModel))]
    public async Task CreateEvent_InvalidModel_ThrowException(CreateEventRequest request, List<string> errors)
    {
        // Arrange
        var service = CreateService(out var holder);

        // Не добавили в репо
        holder.RepositoryMock.Setup(s => s.AddAsync(It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не зафиксировали изменения
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не установили кэш
        holder.CacheMock.Setup(s => s.SetEventAsync(It.IsAny<Guid>(), It.IsAny<Event?>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.CreateEventAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        assertion.Which.Errors.Should().BeEquivalentTo(errors);
    }

    #endregion

    #region DeleteEvent

    [Fact]
    public async Task DeleteEvent_ExistingEventId_SuccessfullyDeleted()
    {
        // Arrange
        var service = CreateService(out var holder);
        var id = Guid.NewGuid();

        // Успешно удалили
        holder.RepositoryMock.Setup(s => s.RemoveAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(true)
            .Verifiable(Times.Once);

        // Зафиксировали
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Сбросили кэш
        holder.CacheMock.Setup(s => s.SetEventAsync(id, null, TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        await service.DeleteEventByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
    }

    [Fact]
    public async Task DeleteEvent_BadId_ThrowNotFound()
    {
        // Arrange
        var service = CreateService(out var holder);
        var id = Guid.NewGuid();

        // Удаление не удалось
        holder.RepositoryMock.Setup(s => s.RemoveAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        // Не зафиксировали
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не установили в кэш, так как не нашли
        holder.CacheMock.Setup(s => s.SetEventAsync(It.IsAny<Guid>(), It.IsAny<Event?>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.DeleteEventByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowExactlyAsync<NotFoundException>();
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
    }

    #endregion

    #region ModifyEvent

    [Theory]
    [MemberData(nameof(ModifyEvent_ValidDataAndId))]
    public async Task ModifyEvent_ValidDataAndId_SuccessfullyReturned(Guid id, ModifyEventRequest request, Event stored, Event expected)
    {
        // Arrange
        var service = CreateService(out var holder);
        var captured = new List<Event>();

        // Получили для редактирования
        holder.RepositoryMock.Setup(s => s.GetByIdAsync(id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(stored)
            .Verifiable(Times.Once);

        // Зафиксировали результат
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Закэшировали
        holder.CacheMock.Setup(s => s.SetEventAsync(id, Capture.In(captured), TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        // Assert
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        result.Should().BeEquivalentTo(expected);
        captured.Should().AllBeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(ModifyEvent_ValidDataAndBadId))]
    public async Task ModifyEvent_ValidDataAndBadId_ThrowNotFound(Guid id, ModifyEventRequest request)
    {
        // Arrange
        var service = CreateService(out var holder);

        // Не нашли событие
        holder.RepositoryMock.Setup(s => s.GetByIdAsync(id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?) null)
            .Verifiable(Times.Once);

        // Не фиксировали результат
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не кэшировали
        holder.CacheMock.Setup(s => s.SetEventAsync(It.IsAny<Guid>(), It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowExactlyAsync<NotFoundException>();
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
    }

    [Theory]
    [MemberData(nameof(ModifyEvent_InvalidData))]
    public async Task ModifyEvent_InvalidData_ThrowException(Guid id, ModifyEventRequest request, List<string> errors)
    {
        // Arrange
        var service = CreateService(out var holder);

        // Получили событие
        holder.RepositoryMock.Setup(s => s.GetByIdAsync(id, GetMode.Edit, TestContext.Current.CancellationToken))
            // Не важно что вернуть
            .ReturnsAsync(CreateTestEvent(id))
            .Verifiable(Times.Once);

        // Не фиксировали результат
        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не кэшировали
        holder.CacheMock.Setup(s => s.SetEventAsync(It.IsAny<Guid>(), It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        // Assert
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        assertion.Which.Errors.Should().BeEquivalentTo(errors);
    }

    #endregion

    #region GetTopSalesEventsAsync

    [Fact]
    public async Task GetTopSalesEventsAsync_CacheHit_ReturnedFromCache()
    {
        // Arrange
        var service = CreateService(out var holder);
        List<Event> eventList = [CreateTestEvent(Guid.NewGuid())];

        // Получение из кэша успешно
        holder.CacheMock.Setup(s => s.GetTopSalesEventAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync((true, eventList))
            .Verifiable(Times.Once);

        // Не трогали базу
        holder.RepositoryMock.Setup(s => s.GetTopSalesEventsAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не устанавливали значение кэша
        holder.CacheMock.Setup(s => s.SetTopSalesEventAsync(It.IsAny<List<Event>>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var result = await service.GetTopSalesEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        result.Should().BeEquivalentTo(eventList);
    }

    [Fact]
    public async Task GetTopSalesEventsAsync_CacheMiss_ReturnFromDb()
    {
        // Arrange
        var service = CreateService(out var holder);
        List<Event> eventList = [CreateTestEvent(Guid.NewGuid())];

        // В кэше нет
        holder.CacheMock.Setup(s => s.GetTopSalesEventAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync((false, []))
            .Verifiable(Times.Once);

        // Нашли в БД
        holder.RepositoryMock.Setup(s => s.GetTopSalesEventsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(eventList)
            .Verifiable(Times.Once);

        // Установили в кэш
        holder.CacheMock.Setup(s => s.SetTopSalesEventAsync(eventList, TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        var result = await service.GetTopSalesEventsAsync(TestContext.Current.CancellationToken);
        // Assert
        holder.CacheMock.Verify();
        holder.RepositoryMock.Verify();
        result.Should().BeEquivalentTo(eventList);
    }

    #endregion
}
