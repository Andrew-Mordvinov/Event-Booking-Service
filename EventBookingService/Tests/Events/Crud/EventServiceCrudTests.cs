using DataAccess.Abstract;
using DataAccess.Abstract.Common;
using DataAccess.Abstract.Enums;
using DTO.Presentation.Events.Requests;
using Events.Models;
using Events.Service.Implementation;
using FluentAssertions;
using Moq;
using Shared.Exceptions;

namespace Tests.Events.Crud;

public partial class EventServiceCrudTests
{
    private static EventService GetMemoryEventService(
        IEnumerable<Event> collection,
        out Mock<IEventRepository> repoMock,
        out Mock<IUnitOfWork> unitOfWorkMock,
        out List<Event> scopedCollection)
    {
        repoMock = new Mock<IEventRepository>();
        unitOfWorkMock = new Mock<IUnitOfWork>();
        scopedCollection = collection.ToList() ?? [];

        return new EventService(repoMock.Object, unitOfWorkMock.Object);
    }

    #region GetEvent

    [Theory]
    [MemberData(nameof(GetEvent_ExistingEventId))]
    public async Task GetEvent_ExistingEventId_SuccessfullyReturned(IEnumerable<Event> baseCollection, Guid id, Event expected)
    {
        var service = GetMemoryEventService(baseCollection, out var repoMock, out var _, out var scopedCollection);

        repoMock.Setup(s => s.GetByIdAsync(id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync(scopedCollection.FirstOrDefault(e => e.Id == id))
            .Verifiable(Times.Once);

        var result = await service.GetEventByIdAsync(id, TestContext.Current.CancellationToken);

        repoMock.Verify();
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(GetEvent_BadId))]
    public async Task GetEvent_BadId_ReturnNull(IEnumerable<Event> baseCollection, Guid id)
    {
        var service = GetMemoryEventService(baseCollection, out var repoMock, out var _, out var scopedCollection);

        repoMock.Setup(s => s.GetByIdAsync(id, GetMode.Readonly, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?)null)
            .Verifiable(Times.Once);

        var result = await service.GetEventByIdAsync(id, TestContext.Current.CancellationToken);

        repoMock.Verify();
        result.Should().BeNull();
    }

    #endregion

    #region CreateEvent

    [Theory]
    [MemberData(nameof(CreateEvent_ValidModel))]
    public async Task CreateEvent_ValidModel_SuccessfullyReturned(IEnumerable<Event> baseCollection, CreateEventRequest request, Event expected)
    {
        var service = GetMemoryEventService(baseCollection, out var repoMock, out var unitOfWorkMock, out var _);

        repoMock.Setup(s => s.AddAsync(It.Is<Event>(e => e.Equivalent(expected)), TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        var result = await service.CreateEventAsync(request, TestContext.Current.CancellationToken);

        repoMock.Verify();
        unitOfWorkMock.Verify();
        result.Should().NotBeNull();
        result.Equivalent(expected);
        // Id выделяется динамически, его не проверяем
        result.Should().BeEquivalentTo(expected, options => options.Excluding(e => e.Id));
    }

    [Theory]
    [MemberData(nameof(CreateEvent_InvalidModel))]
    public async Task CreateEvent_InvalidModel_ThrowException(IEnumerable<Event> baseCollection, CreateEventRequest request, List<string> errors)
    {
        var service = GetMemoryEventService(baseCollection, out var repoMock, out var unitOfWorkMock, out var _);

        repoMock.Setup(s => s.AddAsync(It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var act = async () => await service.CreateEventAsync(request, TestContext.Current.CancellationToken);

        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        repoMock.Verify();
        unitOfWorkMock.Verify();
        assertion.Which.Errors.Should().BeEquivalentTo(errors);
    }

    #endregion

    #region DeleteEvent

    [Theory]
    [MemberData(nameof(DeleteEvent_ExistingEventId))]
    public async Task DeleteEvent_ExistingEventId_SuccessfullyDeleted(IEnumerable<Event> baseCollection, Guid id)
    {
        var service = GetMemoryEventService(baseCollection, out var repoMock, out var unitOfWorkMock, out var scopedCollection);

        repoMock.Setup(s => s.RemoveAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(true)
            .Verifiable(Times.Once);

        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        var result = await service.DeleteEventByIdAsync(id, TestContext.Current.CancellationToken);

        repoMock.Verify();
        unitOfWorkMock.Verify();
        result.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(DeleteEvent_BadId))]
    public async Task DeleteEvent_BadId_ReturnFalse(IEnumerable<Event> baseCollection, Guid id)
    {
        var service = GetMemoryEventService(baseCollection, out var repoMock, out var unitOfWorkMock, out var _);

        repoMock.Setup(s => s.RemoveAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(false)
            .Verifiable(Times.Once);

        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var result = await service.DeleteEventByIdAsync(id, TestContext.Current.CancellationToken);

        repoMock.Verify();
        unitOfWorkMock.Verify();
        result.Should().BeFalse();
    }

    #endregion

    #region ModifyEvent

    [Theory]
    [MemberData(nameof(ModifyEvent_ValidDataAndId))]
    public async Task ModifyEvent_ValidDataAndId_SuccessfullyReturned(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request, Event stored, Event expected)
    {
        var service = GetMemoryEventService(baseCollection, out var repoMock, out var unitOfWorkMock, out var _);

        repoMock.Setup(s => s.GetByIdAsync(id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync(stored)
            .Verifiable(Times.Once);

        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        repoMock.Verify();
        unitOfWorkMock.Verify();
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(ModifyEvent_ValidDataAndBadId))]
    public async Task ModifyEvent_ValidDataAndBadId_ReturnNull(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request)
    {
        var service = GetMemoryEventService(baseCollection, out var repoMock, out var unitOfWorkMock, out var _);

        repoMock.Setup(s => s.GetByIdAsync(id, GetMode.Edit, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event?)null)
            .Verifiable(Times.Once);

        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        repoMock.Verify();
        unitOfWorkMock.Verify();
        result.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(ModifyEvent_InvalidData))]
    public async Task ModifyEvent_InvalidData_ThrowException(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request, List<string> errors)
    {
        var service = GetMemoryEventService(baseCollection, out var repoMock, out var unitOfWorkMock, out var _);

        repoMock.Setup(s => s.GetByIdAsync(id, GetMode.Edit, TestContext.Current.CancellationToken))
            // Не важно что вернуть
            .ReturnsAsync(baseCollection.First())
            .Verifiable(Times.Once);

        unitOfWorkMock.Setup(s => s.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var act = async () => await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        repoMock.Verify();
        unitOfWorkMock.Verify();
        assertion.Which.Errors.Should().BeEquivalentTo(errors);
    }

    #endregion
}
