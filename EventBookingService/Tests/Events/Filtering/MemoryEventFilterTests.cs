using EventBookingService.Application.Events.Implementation;
using EventBookingService.Common.Storage;
using EventBookingService.Models.Events;
using FluentAssertions;
using Moq;

namespace Tests.Events.Filtering;

public partial class MemoryEventFilterTests
{
    [Fact]
    public async Task FilterEvent_EmptyCollectionParamValid_SuccessfulWithNull()
    {
        var mock = new Mock<IStorage<Event>>();

        mock.Setup(s => s.GetAll())
            .Returns([])
            .Verifiable(Times.Never);

        mock.Setup(s => s.Count)
            .Returns(0)
            .Verifiable(Times.Once);

        var service = new MemoryEventService(mock.Object);

        var result = await service.GetEventsAsync(new EventFilters { Title = "неважно" }, 1, 10, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(FilterEvent_ParamValid))]
    public async Task FilterEvent_ParamValid_SuccessfullyReturned(
        IEnumerable<Event> collection,
        EventFilters filters,
        int page,
        int pageSize,
        int expectedCount,
        int expectedPageCount,
        Guid[] expectedIds)
    {
        var mock = new Mock<IStorage<Event>>();

        mock.Setup(s => s.GetAll())
            .Returns(collection)
            .Verifiable(Times.Once);

        mock.Setup(s => s.Count)
            .Returns(collection.Count())
            .Verifiable(Times.AtLeastOnce);

        var service = new MemoryEventService(mock.Object);

        var result = await service.GetEventsAsync(filters, page, pageSize, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.FilteredCount.Should().Be(expectedCount);
        result.Value.TotalPages.Should().Be(expectedPageCount);
        result.Value.Items.Select(t => t.Id).Should().Equal(expectedIds);
    }

    [Theory]
    [MemberData(nameof(FilterEvent_BadPagingParam))]
    public async Task FilterEvent_BadPagingParam_FailWithErrors(
        IEnumerable<Event> collection,
        EventFilters filters,
        int page,
        int pageSize,
        List<string> errors)
    {
        var mock = new Mock<IStorage<Event>>();

        mock.Setup(s => s.GetAll())
            .Returns(collection)
            .Verifiable(Times.Never);

        mock.Setup(s => s.Count)
            .Returns(collection.Count())
            .Verifiable(Times.Never);

        var service = new MemoryEventService(mock.Object);

        var result = await service.GetEventsAsync(filters, page, pageSize, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Theory]
    [MemberData(nameof(FilterEvent_PageGreaterThanMaxPage))]
    public async Task FilterEvent_PageGreaterThanMaxPage_FailWithErrors(
        IEnumerable<Event> collection,
        EventFilters filters,
        int page,
        int pageSize,
        List<string> errors)
    {
        var mock = new Mock<IStorage<Event>>();

        mock.Setup(s => s.GetAll())
            .Returns(collection)
            .Verifiable(Times.AtLeastOnce);

        mock.Setup(s => s.Count)
            .Returns(collection.Count())
            .Verifiable(Times.AtLeastOnce);

        var service = new MemoryEventService(mock.Object);

        var result = await service.GetEventsAsync(filters, page, pageSize, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Theory]
    [MemberData(nameof(FilterEvent_NoElementsAfterFilter))]
    public async Task FilterEvent_NoElementsAfterFilter_SuccessWithNull(
        IEnumerable<Event> collection,
        EventFilters filters,
        int page,
        int pageSize)
    {
        var mock = new Mock<IStorage<Event>>();

        mock.Setup(s => s.GetAll())
            .Returns(collection)
            .Verifiable(Times.AtMostOnce);

        mock.Setup(s => s.Count)
            .Returns(collection.Count())
            .Verifiable(Times.Once);

        var service = new MemoryEventService(mock.Object);

        var result = await service.GetEventsAsync(filters, page, pageSize, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
