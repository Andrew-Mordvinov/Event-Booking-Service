using DataAccess.Abstract.Common;
using DataAccess.EF;
using Entities.Bookings;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests.Unit.BookingRepository;

public partial class EfBookingRepositoryTests
{
    private static (AppDbContext Context, EfBookingRepository Repository) CreateRepository()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var repository = new EfBookingRepository(context, unitOfWorkMock.Object);

        return (context, repository);
    }

    #region GetPendingBookingsAsync

    [Theory]
    [MemberData(nameof(GetPendingBookingsAsync_HasPending))]
    public async Task GetPendingBookingsAsync_Common_ReturnValidGuids(List<Booking> initial, List<Guid> expected)
    {
        var (context, repository) = CreateRepository();

        context.AddRange(initial);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var result = await repository.GetPendingBookingsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expected, result);
    }

    #endregion
}
