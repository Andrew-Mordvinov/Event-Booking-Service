using DataAccess.Storage;
using DTO.Events.Requests;
using Events.Models;
using Events.Service.Implementation;
using FluentAssertions;
using Moq;
using Validation;

namespace Tests.Events.Crud;

public partial class MemoryEventCrudTests
{
    private static EventService GetMemoryEventService(
        IEnumerable<Event> collection,
        out Mock<IStorage<Event>> mock,
        out List<Event> scopedCollection)
    {
        mock = new Mock<IStorage<Event>>();
        scopedCollection = collection.ToList() ?? [];

        return new EventService(mock.Object);
    }

    #region GetEvent

    [Theory]
    [MemberData(nameof(GetEvent_ExistingEventId))]
    public async Task GetEvent_ExistingEventId_SuccessfullyReturned(IEnumerable<Event> baseCollection, Guid id, Event expected)
    {
        var service = GetMemoryEventService(baseCollection, out var mock, out var scopedCollection);

        mock.Setup(s => s.GetByIdAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(scopedCollection.FirstOrDefault(e => e.Id == id)))
            .Verifiable(Times.Once);

        var result = await service.GetEventByIdAsync(id, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(GetEvent_BadId))]
    public async Task GetEvent_BadId_SuccessWithNull(IEnumerable<Event> baseCollection, Guid id)
    {
        var service = GetMemoryEventService(baseCollection, out var mock, out var scopedCollection);

        mock.Setup(s => s.GetByIdAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success<Event>(null))
            .Verifiable(Times.Once);

        var result = await service.GetEventByIdAsync(id, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    #endregion

    #region CreateEvent

    [Theory]
    [MemberData(nameof(CreateEvent_ValidModel))]
    public async Task CreateEvent_ValidModel_SuccessfullyReturned(IEnumerable<Event> baseCollection, CreateEventRequest request, Event expected)
    {
        var service = GetMemoryEventService(baseCollection, out var mock, out var _);

        mock.Setup(s => s.AddAsync(It.Is<Event>(e => e.Equivalent(expected)), TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success())
            .Verifiable(Times.Once);

        var result = await service.CreateEventAsync(request, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Equivalent(expected);
        // Id выделяется динамически, его не проверяем
        result.Value.Should().BeEquivalentTo(expected, options => options.Excluding(e => e.Id));
    }

    [Theory]
    [MemberData(nameof(CreateEvent_InvalidModel))]
    public async Task CreateEvent_InvalidModel_FailWithError(IEnumerable<Event> baseCollection, CreateEventRequest request, List<string> errors)
    {
        var service = GetMemoryEventService(baseCollection, out var _, out var _);

        var result = await service.CreateEventAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(errors.Select(t => new ValidationItem(t)));
    }

    #endregion

    #region DeleteEvent

    [Theory]
    [MemberData(nameof(DeleteEvent_ExistingEventId))]
    public async Task DeleteEvent_ExistingEventId_SuccessfullyDeleted(IEnumerable<Event> baseCollection, Guid id)
    {
        var service = GetMemoryEventService(baseCollection, out var mock, out var scopedCollection);

        mock.Setup(s => s.RemoveAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(true))
            .Verifiable(Times.Once);

        var result = await service.DeleteEventByIdAsync(id, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(DeleteEvent_BadId))]
    public async Task DeleteEvent_BadId_SuccessWithFalse(IEnumerable<Event> baseCollection, Guid id)
    {
        var service = GetMemoryEventService(baseCollection, out var mock, out var _);

        mock.Setup(s => s.RemoveAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(false))
            .Verifiable(Times.Once);

        var result = await service.DeleteEventByIdAsync(id, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    #endregion

    #region ModifyEvent

    [Theory]
    [MemberData(nameof(ModifyEvent_ValidDataAndId))]
    public async Task ModifyEvent_ValidDataAndId_SuccessfullyReturned(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request, Event expected)
    {
        var service = GetMemoryEventService(baseCollection, out var mock, out var scopedCollection);

        mock.Setup(s => s.UpdateAsync(It.Is<Event>(e => e.Equivalent(expected)), TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(true))
            .Verifiable(Times.Once);

        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(ModifyEvent_ValidDataAndBadId))]
    public async Task ModifyEvent_ValidDataAndBadId_SuccessWithNull(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request)
    {
        var service = GetMemoryEventService(baseCollection, out var mock, out var _);

        mock.Setup(s => s.UpdateAsync(It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(false))
            .Verifiable(Times.Once);

        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeNull();
    }


    [Theory]
    [MemberData(nameof(ModifyEvent_InvalidData))]
    public async Task ModifyEvent_InvalidData_FailWithError(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request, List<string> errors)
    {
        var service = GetMemoryEventService(baseCollection, out var mock, out var scopedCollection);

        mock.Setup(s => s.UpdateAsync(It.IsAny<Event>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(errors.Select(t => new ValidationItem(t)));
    }

    #endregion
}
