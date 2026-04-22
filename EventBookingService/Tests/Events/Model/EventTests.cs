using Events.Models;
using FluentAssertions;

namespace Tests.Events.Model;

public partial class EventTests
{
    #region TryReserveSeats

    [Theory]
    [MemberData(nameof(TryReserveSeats_SeatsAvailable))]
    public void TryReserveSeats_SeatsAvailable_ReturnTrue(int total, int count)
    {
        var eventObject = new Event(
            Guid.NewGuid(),
            "Some Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            total);

        var result = eventObject.TryReserveSeats(count);

        result.Should().BeTrue();
        eventObject.AvailableSeats.Should().Be(total - count);
    }

    [Theory]
    [MemberData(nameof(TryReserveSeats_NotEnoughSeats))]
    public void TryReserveSeats_NotEnoughSeats_ReturnFalse(int total, int tryToReserve)
    {
        var eventObject = new Event(
            Guid.NewGuid(),
            "Some Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            total);

        var result = eventObject.TryReserveSeats(tryToReserve);

        result.Should().BeFalse();
        eventObject.AvailableSeats.Should().Be(total);
    }

    [Theory]
    [MemberData(nameof(TryReserveSeats_IncorrectCount))]
    public void TryReserveSeats_IncorrectCount_ReturnFalse(int tryToReserve)
    {
        int countOfSeats = 10;
        var eventObject = new Event(
            Guid.NewGuid(),
            "Some Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10);

        var result = eventObject.TryReserveSeats(tryToReserve);

        result.Should().BeFalse();
        eventObject.AvailableSeats.Should().Be(countOfSeats);
    }

    #endregion

    #region TryReleaseSeats

    [Theory]
    [MemberData(nameof(TryReleaseSeats_SeatsAvailable))]
    public void TryReleaseSeats_SeatsAvailable_ReturnTrue(int total, int available, int count)
    {
        var eventObject = new Event(
            Guid.NewGuid(),
            "Some Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            total,
            available);

        var result = eventObject.TryReleaseSeats(count);

        result.Should().BeTrue();
        eventObject.AvailableSeats.Should().Be(available + count);
    }

    [Theory]
    [MemberData(nameof(TryReleaseSeats_NotEnoughSeats))]
    public void TryReleaseSeats_NotEnoughSeats_ReturnFalse(int total, int available, int tryToRelease)
    {
        var eventObject = new Event(
            Guid.NewGuid(),
            "Some Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            total,
            available);

        var result = eventObject.TryReleaseSeats(tryToRelease);

        result.Should().BeFalse();
        eventObject.AvailableSeats.Should().Be(available);
    }

    [Theory]
    [MemberData(nameof(TryReleaseSeats_IncorrectCount))]
    public void TryReleaseSeats_IncorrectCount_ReturnFalse(int tryToRelease)
    {
        int countOfSeats = 10;
        var eventObject = new Event(
            Guid.NewGuid(),
            "Some Title",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            10);

        var result = eventObject.TryReleaseSeats(tryToRelease);

        result.Should().BeFalse();
        eventObject.AvailableSeats.Should().Be(countOfSeats);
    }

    #endregion

    #region TryCreate

    [Theory]
    [MemberData(nameof(TryCreate_ValidParams))]
    public void TryCreate_ValidParams_ReturnEventWithNoError(
        string title,
        DateTime start,
        DateTime end,
        int total,
        int? available,
        string? description)
    {
        var guid = Guid.NewGuid();
        var expected = new Event(
            guid,
            title,
            start,
            end,
            total,
            available,
            description);

        var (eventObject, errors) = Event.TryCreate(
            guid,
            title,
            start,
            end,
            total,
            available,
            description);

        errors.Should().BeNullOrEmpty();
        eventObject.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(TryCreate_InvalidParams))]
    public void TryCreate_InvalidParams_ReturnAllErrors(
        string? title,
        DateTime? start,
        DateTime? end,
        int? total,
        int? available,
        string? description,
        string[] expectedErrors)
    {
        var (eventObject, errors) = Event.TryCreate(
            Guid.NewGuid(),
            title,
            start,
            end,
            total,
            available,
            description);

        eventObject.Should().BeNull();
        errors.Should().BeEquivalentTo(expectedErrors);
    }

    #endregion
}
