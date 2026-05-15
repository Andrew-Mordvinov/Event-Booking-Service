using Entities.Bookings;
using Entities.Events;
using Microsoft.Extensions.Logging;

namespace Tests.Integration.Ef.Bookings;

public partial class EfBookingRepositoryTests
{
    private static class BookingIds
    {
        public static readonly Guid First = Guid.Parse("d7a9f3e1-6b4c-4c8a-9f21-3d7e5a2b8c91");
        public static readonly Guid Second = Guid.Parse("b9d3c5a7-2e6f-4b1a-8c9d-5e7f2a4b6c31");
        public static readonly Guid Third = Guid.Parse("9e2c1a7b-4d6f-4a8c-b3e1-9d5a2f7c6b84");
        public static readonly Guid Fourth = Guid.Parse("5b8d3f1e-9c2a-4a7f-b6d4-1e2c9a5f8b73");
        public static readonly Guid Fifth = Guid.Parse("f4a7c9d1-5b3e-4a8c-9d6f-2c1b7e4a5d89");
    }

    private static class EventIds
    {
        public static readonly Guid First = Guid.Parse("e9b2a1d7-5c3f-4a8e-b6d4-2f1c7a9b5d83");
        public static readonly Guid Second = Guid.Parse("c2a7e1d9-4b5f-4a8c-9d3e-7f1a2c6b4d58");
        public static readonly Guid Third = Guid.Parse("b1d9a7c3-5e2f-4a8b-9c6d-3f7a2e1b4c85");
    }

    private static List<Event> BaseEventCollection =
    [
        new Event
        (
            EventIds.First,
            "First",
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
            10
        ),
        new Event
        (
            EventIds.Second,
            "Second",
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
            10
        ),
        new Event
        (
            EventIds.Third,
            "Third",
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow),
            SharedFixture.TrimToMicroseconds(DateTimeOffset.UtcNow.AddDays(1)),
            10
        )
    ];

    public static IEnumerable<object?[]> GetPendingBookingsAsync_Common =>
    [
        [
            BaseEventCollection,
            new List<Booking>
            {
                new(BookingIds.First, EventIds.First, BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-10)),
                new(BookingIds.Second, EventIds.First, BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-11)),
                new(BookingIds.Third, EventIds.Second, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-3)),
                new(BookingIds.Fourth, EventIds.Second, BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-20)),
                new(BookingIds.Fifth, EventIds.Third, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-1)),
            },
            new List<Guid>
            {
                BookingIds.First,
                BookingIds.Second,
                BookingIds.Fourth
            }
        ],
        [
            BaseEventCollection,
            new List<Booking>(),
            new List<Guid>()
        ],
        [
            BaseEventCollection,
            new List<Booking>
            {
                new(BookingIds.First, EventIds.First, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-10)),
                new(BookingIds.Second, EventIds.First, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-11)),
                new(BookingIds.Third, EventIds.Second, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-3)),
                new(BookingIds.Fourth, EventIds.Second, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-20)),
                new(BookingIds.Fifth, EventIds.Third, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-1)),
            },
            new List<Guid>()
        ],
    ];
}
