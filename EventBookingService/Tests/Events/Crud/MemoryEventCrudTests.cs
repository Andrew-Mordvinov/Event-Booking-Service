using EventBookingService.Application.Events.Implementation;
using EventBookingService.Common.Storage;
using EventBookingService.Models.Events;
using EventBookingService.Models.Events.Requests;
using FluentAssertions;
using Moq;

namespace Tests.Events.Crud;

public partial class MemoryEventCrudTests
{
    private static (MemoryEventService service, Mock<IStorage<Event>> mock, List<Event> scopedCollection) GetMemoryEventService(IEnumerable<Event> collection)
    {
        var mock = new Mock<IStorage<Event>>();
        var events = collection.ToList() ?? [];

        return (new MemoryEventService(mock.Object), mock, events);
    }

    #region GetEvent

    [Theory]
    [MemberData(nameof(GetEvent_ExistingEventId))]
    public async Task GetEvent_ExistingEventId_SuccessfullyReturned(IEnumerable<Event> baseCollection, Guid id, Event expected)
    {
        var (service, mock, scopedCollection) = GetMemoryEventService(baseCollection);

        mock.Setup(s => s.GetById(id))
            .Returns<Guid>(id => scopedCollection.FirstOrDefault(e => e.Id == id));

        var result = await service.GetEventByIdAsync(id, TestContext.Current.CancellationToken);

        mock.Verify(s => s.GetById(id), Times.Once);
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(GetEvent_BadId))]
    public async Task GetEvent_BadId_SuccessWithNull(IEnumerable<Event> baseCollection, Guid id)
    {
        var (service, mock, scopedCollection) = GetMemoryEventService(baseCollection);

        mock.Setup(s => s.GetById(It.IsAny<Guid>()))
            .Returns<Guid>(id => scopedCollection.FirstOrDefault(e => e.Id == id))
            .Verifiable();

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
        var (service, mock, _) = GetMemoryEventService(baseCollection);

        mock.Setup(s => s.Add(It.Is<Event>(e => e.Equivalent(expected))))
            .Verifiable();

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
        var (service, _, _) = GetMemoryEventService(baseCollection);

        var result = await service.CreateEventAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(errors);
    }

    #endregion

    #region DeleteEvent

    [Theory]
    [MemberData(nameof(DeleteEvent_ExistingEventId))]
    public async Task DeleteEvent_ExistingEventId_SuccessfullyDeleted(IEnumerable<Event> baseCollection, Guid id)
    {
        var (service, mock, scopedCollection) = GetMemoryEventService(baseCollection);

        mock.Setup(s => s.Remove(id))
            .Returns(scopedCollection.Count(t => t.Id == id))
            .Verifiable();

        var result = await service.DeleteEventByIdAsync(id, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(DeleteEvent_BadId))]
    public async Task DeleteEvent_BadId_SuccessWithFalse(IEnumerable<Event> baseCollection, Guid id)
    {
        var (service, mock, _) = GetMemoryEventService(baseCollection);

        mock.Setup(s => s.Remove(id))
            .Returns(0)
            .Verifiable();

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
        var (service, mock, scopedCollection) = GetMemoryEventService(baseCollection);

        mock.Setup(s => s.GetById(id))
            .Returns(scopedCollection.FirstOrDefault(t => t.Id == id))
            .Verifiable();

        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(ModifyEvent_ValidDataAndBadId))]
    public async Task ModifyEvent_ValidDataAndBadId_SuccessWithNull(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request)
    {
        var (service, mock, _) = GetMemoryEventService(baseCollection);

        mock.Setup(s => s.GetById(id))
            .Returns(() => null)
            .Verifiable();

        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(ModifyEvent_InvalidDataAndId))]
    public async Task ModifyEvent_InvalidDataAndId_SuccessWithNull(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request)
    {
        var (service, mock, _) = GetMemoryEventService(baseCollection);

        mock.Setup(s => s.GetById(id))
            .Returns(() => null)
            .Verifiable();

        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(ModifyEvent_InvalidDataAndCorrectId))]
    public async Task ModifyEvent_InvalidDataAndCorrectId_FailWithError(IEnumerable<Event> baseCollection, Guid id, ModifyEventRequest request, List<string> errors)
    {
        var (service, mock, scopedCollection) = GetMemoryEventService(baseCollection);

        mock.Setup(s => s.GetById(id))
            .Returns(scopedCollection.FirstOrDefault(t => t.Id == id))
            .Verifiable();

        var result = await service.ModifyEventAsync(id, request, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(errors);
    }

    #endregion
}
