using EventBookingService.Application.Events.Implementation;
using EventBookingService.Common.Paging;
using EventBookingService.Common.Storage;
using EventBookingService.Common.Validations.Results;
using EventBookingService.Models.Events;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;

namespace Tests.Events.Filtering;

public partial class MemoryEventFilterTests
{
    private static EventService CreateService(out Mock<IStorage<Event>> storageMock)
    {
        storageMock = new Mock<IStorage<Event>>();
        return new EventService(storageMock.Object);
    }

    [Fact]
    public async Task FilterEvent_ParamValidStorageReturnNoElements_SuccessfulWithNull()
    {
        var service = CreateService(out var mock);

        mock.Setup(s => s.GetPageAsync(It.IsAny<Expression<Func<Event, bool>>>(), 1, 10, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success<PaginatedResult<Event>>(null))
            .Verifiable(Times.Once);

        var result = await service.GetEventsAsync(
            new EventFilters { Title = "неважно" },
            1,
            10,
            TestContext.Current.CancellationToken);

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
        var service = CreateService(out var mock);

        // Arrange
        var capturedFilter = new List<Expression<Func<Event, bool>>>();
        // Не важно, фильтрует репозиторий, сервис валидирует параметры и формирует выражение фильтра
        var noMatterResult = new PaginatedResult<Event>
        {
            CurrentPage = page,
            FilteredCount = expectedCount,
            TotalPages = expectedPageCount,
            Items = []
        };

        mock.Setup(s => s.GetPageAsync(Capture.In(capturedFilter), page, pageSize, TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Success(noMatterResult))
            .Verifiable(Times.Once);

        // Act
        var result = await service.GetEventsAsync(filters, page, pageSize, TestContext.Current.CancellationToken);

        if (capturedFilter.Count != 1)
        {
            Assert.Fail($"{nameof(IStorage<>.GetPageAsync)} захватилось несколько выражений фильтров, ожидалось одно значение");
        }

        var filtered = capturedFilter.First() is not null
            ? collection.Where(capturedFilter.First().Compile())
            : collection;

        // Assert
        mock.Verify();
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.FilteredCount.Should().Be(expectedCount);
        result.Value.TotalPages.Should().Be(expectedPageCount);
        // В результат заложена пустая коллекция, сам сервис просто передает то, что вернул репозиторий
        result.Value.Items.Should().BeEmpty();

        filtered.Select(t => t.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Theory]
    [MemberData(nameof(FilterEvent_BadPagingParam))]
    public async Task FilterEvent_BadPagingParam_FailWithErrors(
        EventFilters filters,
        int page,
        int pageSize,
        List<string> errors)
    {
        var service = CreateService(out var mock);

        mock.Setup(s => s.GetPageAsync(It.IsAny<Expression<Func<Event, bool>>>(), page, pageSize, TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var result = await service.GetEventsAsync(filters, page, pageSize, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Theory]
    [MemberData(nameof(FilterEvent_PageGreaterThanMaxPage))]
    public async Task FilterEvent_PageGreaterThanMaxPage_FailWithErrors(
        EventFilters filters,
        int page,
        int pageSize,
        int totalPages,
        List<string> errors)
    {
        var service = CreateService(out var mock);

        mock.Setup(s => s.GetPageAsync(
                It.IsAny<Expression<Func<Event, bool>>>(),
                page,
                pageSize,
                TestContext.Current.CancellationToken))
            .ReturnsAsync(ResultCreator.Fail<PaginatedResult<Event>?>(
                null,
                StorageErrors.PageNotFound(page, totalPages)))
            .Verifiable(Times.Once);

        var result = await service.GetEventsAsync(filters, page, pageSize, TestContext.Current.CancellationToken);

        mock.Verify();
        result.IsSuccessful.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Errors.Should().BeEquivalentTo(errors);
    }
}
