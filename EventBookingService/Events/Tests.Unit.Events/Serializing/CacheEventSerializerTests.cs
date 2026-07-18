using Domain.Events;

using FluentAssertions;

using Infrastructure.Events.Redis.Serializer;

using Shared.Exceptions;

namespace Tests.Unit.Events.Serializing;

public class CacheEventSerializerTests
{
    #region Helping

    private CacheEventSerializer CreateService() => new CacheEventSerializer();

    private static Event CreateTestEvent() => new
    (
        Guid.NewGuid(),
        "Title",
        DateTimeOffset.UtcNow.AddHours(1),
        DateTimeOffset.UtcNow.AddHours(2),
        15,
        10,
        "Desc"
    );

    private static List<Event> CreateTestList() =>
    [
        CreateTestEvent(),
        CreateTestEvent(),
        CreateTestEvent(),
        CreateTestEvent()
    ];

    #endregion

    // TODO расширить тесты, вынести ошибки в константы и их тоже проверять

    #region GetEvent

    [Fact]
    public void GetEvent_FromGetJsonInput_SuccessfulyParsed()
    {
        // Arrange
        var service = CreateService();
        var @event = CreateTestEvent();

        var json = service.GetJsonEvent(@event);

        // Act
        var result = service.GetEvent(json);

        // Assert
        result.Should().BeEquivalentTo(@event);
    }

    [Fact]
    public void GetEvent_BrokenModel_ThrowValidation()
    {
        // Arrange
        var service = CreateService();
        var brokenJson = "{}";

        // Act
        var act = () => service.GetEvent(brokenJson);

        // Assert
        act.Should().ThrowExactly<ValidationException>();
    }

    #endregion

    #region GetEventList

    [Fact]
    public void GetEventList_FromGetJsonInput_SuccessfulyParsed()
    {
        // Arrange
        var service = CreateService();
        var events = CreateTestList();

        var json = service.GetJsonEventList(events);

        // Act
        var result = service.GetEventList(json);

        // Assert
        result.Should().BeEquivalentTo(events, op => op.WithStrictOrdering());
    }

    [Fact]
    public void GetEventList_BrokenModel_ThrowValidation()
    {
        // Arrange
        var service = CreateService();
        var brokenJson = "[{}]";

        // Act
        var act = () => service.GetEventList(brokenJson);

        // Assert
        act.Should().ThrowExactly<ValidationException>();
    }

    #endregion
}
