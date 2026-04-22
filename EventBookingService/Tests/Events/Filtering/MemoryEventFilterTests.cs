using DataAccess.Storage;
using Events.Models;
using Events.Service.Implementation;
using FluentAssertions;
using Moq;
using Shared.Exceptions;
using Shared.Paging;
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
    public async Task FilterEvent_ParamValidStorageReturnNoElements_ReturnNull()
    {
        var service = CreateService(out var mock);

        mock.Setup(s => s.GetPageAsync(It.IsAny<Expression<Func<Event, bool>>>(), 1, 10, TestContext.Current.CancellationToken))
            .ReturnsAsync((PaginatedResult<Event>?)null)
            .Verifiable(Times.Once);

        var result = await service.GetEventsAsync(
            new EventFilters { Title = "неважно" },
            1,
            10,
            TestContext.Current.CancellationToken);

        mock.Verify();
        result.Should().BeNull();
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
            .ReturnsAsync(noMatterResult)
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
        result.Should().NotBeNull();
        result.FilteredCount.Should().Be(expectedCount);
        result.TotalPages.Should().Be(expectedPageCount);
        // В результат заложена пустая коллекция, сам сервис просто передает то, что вернул репозиторий
        result.Items.Should().BeEmpty();

        filtered.Select(t => t.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Theory]
    [MemberData(nameof(FilterEvent_BadPagingParam))]
    public async Task FilterEvent_BadPagingParam_ThrowException(
        EventFilters filters,
        int page,
        int pageSize,
        List<string> errors)
    {
        var service = CreateService(out var mock);

        mock.Setup(s => s.GetPageAsync(It.IsAny<Expression<Func<Event, bool>>>(), page, pageSize, TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        var act = async () => await service.GetEventsAsync(filters, page, pageSize, TestContext.Current.CancellationToken);

        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        assertion.Which.Errors.Should().BeEquivalentTo(errors);

        mock.Verify();
    }

    [Theory]
    [MemberData(nameof(FilterEvent_PageGreaterThanMaxPage))]
    public async Task FilterEvent_PageGreaterThanMaxPage_ThrowException(
        EventFilters filters,
        int page,
        int pageSize,
        List<string> errors)
    {
        var service = CreateService(out var mock);

        mock.Setup(s => s.GetPageAsync(
                It.IsAny<Expression<Func<Event, bool>>>(),
                page,
                pageSize,
                TestContext.Current.CancellationToken))
            .ThrowsAsync(new ValidationException(errors))
            .Verifiable(Times.Once);

        var act = async () => await service.GetEventsAsync(filters, page, pageSize, TestContext.Current.CancellationToken);

        var assertion = await act.Should()
            .ThrowExactlyAsync<ValidationException>();

        assertion.Which.Errors.Should().BeEquivalentTo(errors);
        mock.Verify();
    }
}
