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
        public required Mock<IUnitOfWork> UnitOfWorkMock { get; init; }
        public required List<Event> ScopedCollection { get; init; }
    }

    private static EventService CreateService(
        IEnumerable<Event> collection,
        out Holder holder)
    {
        holder = new Holder
        {
            RepositoryMock = new Mock<IEventRepository>(),
            UnitOfWorkMock = new Mock<IUnitOfWork>(),
            ScopedCollection = collection.ToList() ?? [],
        };

        return new EventService(holder.RepositoryMock.Object, holder.UnitOfWorkMock.Object);
    }

    #region GetEvent

    [Theory]
    [MemberData(nameof(GetEvent_ExistingEventId))]
    public async Task GetEvent_ExistingEventId_SuccessfullyReturned(IEnumerable<Event> baseCollection, Guid id, Event expected)
    {
        // Arrange
        var service = CreateService(baseCollection, out var holder);

        holder.RepositoryMock.Setup(s => s.GetByIdAsync(id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(holder.ScopedCollection.FirstOrDefault(e => e.Id == id))
            .Verifiable(Times.Once);

        // Act
        var result = await service.GetEventByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        holder.RepositoryMock.Verify();
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(GetEvent_BadId))]
    public async Task GetEvent_BadId_ThrowNotFound(IEnumerable<Event> baseCollection, Guid id)
    {
        // Arrange
        var service = CreateService(baseCollection, out var holder);

        holder.RepositoryMock.Setup(s => s.GetByIdAsync(id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?) null)
            .Verifiable(Times.Once);

        // Act
        var act = async () => await service.GetEventByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowExactlyAsync<NotFoundException>();
        holder.RepositoryMock.Verify();
    }

    #endregion

    #region CreateEvent

    [Theory]
    [MemberData(nameof(CreateEvent_ValidModel))]
    public async Task CreateEvent_ValidModel_SuccessfullyReturned(IEnumerable<Event> baseCollection, CreateEventRequest request, Event expected)
    {
        // Arrange
        var service = CreateService(baseCollection, out var holder);

        holder.RepositoryMock.Setup(s => s.AddAsync(It.Is<Event>(e => e.Equivalent(expected)), TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        var result = await service.CreateEventAsync(request, TestContext.Current.CancellationToken);

        // Assert
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        result.Should().NotBeNull();
        result.Equivalent(expected);
        // Id выделяется динамически, его не проверяем
        result.Should().BeEquivalentTo(expected, options => options.Excluding(e => e.Id));
    }

    [Theory]
    [MemberData(nameof(CreateEvent_InvalidModel))]
    public async Task CreateEvent_InvalidModel_ThrowException(IEnumerable<Event> baseCollection, CreateEventRequest request, List<string> errors)
    {
        // Arrange
        var service = CreateService(baseCollection, out var holder);

        holder.RepositoryMock.Setup(s => s.AddAsync(It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.CreateEventAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        assertion.Which.Errors.Should().BeEquivalentTo(errors);
    }

    #endregion

    #region DeleteEvent

    [Theory]
    [MemberData(nameof(DeleteEvent_ExistingEventId))]
    public async Task DeleteEvent_ExistingEventId_SuccessfullyDeleted(IEnumerable<Event> baseCollection, Guid id)
    {
        // Arrange
        var service = CreateService(baseCollection, out var holder);

        holder.RepositoryMock.Setup(s => s.RemoveAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(true)
            .Verifiable(Times.Once);

        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        await service.DeleteEventByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
    }

    [Theory]
    [MemberData(nameof(DeleteEvent_BadId))]
    public async Task DeleteEvent_BadId_ThrowNotFound(IEnumerable<Event> baseCollection, Guid id)
    {
        // Arrange
        var service = CreateService(baseCollection, out var holder);

        holder.RepositoryMock.Setup(s => s.RemoveAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.DeleteEventByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowExactlyAsync<NotFoundException>();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
    }

    #endregion

    #region ModifyEvent

    [Theory]
    [MemberData(nameof(ModifyEvent_ValidDataAndId))]
    public async Task ModifyEvent_ValidDataAndId_SuccessfullyReturned(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request, Event stored, Event expected)
    {
        // Arrange
        var service = CreateService(baseCollection, out var holder);

        holder.RepositoryMock.Setup(s => s.GetByIdAsync(id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(stored)
            .Verifiable(Times.Once);

        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        // Assert
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(ModifyEvent_ValidDataAndBadId))]
    public async Task ModifyEvent_ValidDataAndBadId_ThrowNotFound(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request)
    {
        // Arrange
        var service = CreateService(baseCollection, out var holder);

        holder.RepositoryMock.Setup(s => s.GetByIdAsync(id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?) null)
            .Verifiable(Times.Once);

        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowExactlyAsync<NotFoundException>();
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
    }

    [Theory]
    [MemberData(nameof(ModifyEvent_InvalidData))]
    public async Task ModifyEvent_InvalidData_ThrowException(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request, List<string> errors)
    {
        // Arrange
        var service = CreateService(baseCollection, out var holder);

        holder.RepositoryMock.Setup(s => s.GetByIdAsync(id, GetMode.Edit, TestContext.Current.CancellationToken))
            // Не важно что вернуть
            .ReturnsAsync(baseCollection.First())
            .Verifiable(Times.Once);

        holder.UnitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        // Assert
        holder.RepositoryMock.Verify();
        holder.UnitOfWorkMock.Verify();
        assertion.Which.Errors.Should().BeEquivalentTo(errors);
    }

    #endregion
}
