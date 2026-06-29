using Domain.Bookings;

namespace Tests.Integration.Bookings.Ef.Bookings;

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

    private static class UserIds
    {
        public static readonly Guid User = Guid.Parse("258aa9c7-80f6-4dea-9ccb-5976bd2839c2");
        public static readonly Guid AnotherUser = Guid.Parse("1866d9a2-6e3e-405d-bdd5-40fdc3315964");
        public static readonly Guid Admin = Guid.Parse("1de8303c-7478-4e82-8f5d-40afade82499");
    }

    public static IEnumerable<object?[]> GetPendingBookingsAsync_Common =>
    [
        [
            new List<Booking>
            {
                new(BookingIds.First, EventIds.First, UserIds.User, BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-10)),
                new(BookingIds.Second, EventIds.First, UserIds.Admin, BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-11)),
                new(BookingIds.Third, EventIds.Second, UserIds.Admin, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-3)),
                new(BookingIds.Fourth, EventIds.Second, UserIds.User, BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-20)),
                new(BookingIds.Fifth, EventIds.Third, UserIds.User, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-1)),
            },
            new List<Guid>
            {
                BookingIds.First,
                BookingIds.Second,
                BookingIds.Fourth
            }
        ],
        [
            new List<Booking>(),
            new List<Guid>()
        ],
        [
            new List<Booking>
            {
                new(BookingIds.First, EventIds.First, UserIds.User, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-10)),
                new(BookingIds.Second, EventIds.First,UserIds.Admin, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-11)),
                new(BookingIds.Third, EventIds.Second, UserIds.Admin, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-3)),
                new(BookingIds.Fourth, EventIds.Second, UserIds.User, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-20)),
                new(BookingIds.Fifth, EventIds.Third, UserIds.User, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-1)),
            },
            new List<Guid>()
        ],
    ];

    public static IEnumerable<object?[]> GetCountActiveBookingForPersonAsync_Common =>
    [
        [
            new List<Booking>
            {
                new(BookingIds.First, EventIds.First, UserIds.User, BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-10)),
                new(BookingIds.Second, EventIds.First, UserIds.Admin, BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-11)),
                new(BookingIds.Third, EventIds.Second, UserIds.User, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-3)),
                new(BookingIds.Fourth, EventIds.Second, UserIds.User, BookingStatus.Pending, DateTimeOffset.UtcNow.AddHours(-20)),
                new(BookingIds.Fifth, EventIds.Third, UserIds.User, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-1)),
            },
            UserIds.User,
            3
        ],
        [
            new List<Booking>(),
            UserIds.User,
            0
        ],
        [
            new List<Booking>
            {
                new(BookingIds.First, EventIds.First, UserIds.User, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-10)),
                new(BookingIds.Second, EventIds.First,UserIds.AnotherUser, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-11)),
                new(BookingIds.Third, EventIds.Second, UserIds.AnotherUser, BookingStatus.Cancelled, DateTimeOffset.UtcNow.AddHours(-3)),
                new(BookingIds.Fourth, EventIds.Second, UserIds.Admin, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-20)),
                new(BookingIds.Fifth, EventIds.Third, UserIds.User, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-1))
            },
            UserIds.AnotherUser,
            1
        ],
        [
            new List<Booking>
            {
                new(BookingIds.First, EventIds.First, UserIds.User, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-10)),
                new(BookingIds.Second, EventIds.First,UserIds.AnotherUser, BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddHours(-11)),
                new(BookingIds.Third, EventIds.Second, UserIds.AnotherUser, BookingStatus.Cancelled, DateTimeOffset.UtcNow.AddHours(-3)),
                new(BookingIds.Fourth, EventIds.Second, UserIds.Admin, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-20)),
                new(BookingIds.Fifth, EventIds.Third, UserIds.User, BookingStatus.Rejected, DateTimeOffset.UtcNow.AddHours(-1))
            },
            UserIds.Admin,
            0
        ],
    ];
}
