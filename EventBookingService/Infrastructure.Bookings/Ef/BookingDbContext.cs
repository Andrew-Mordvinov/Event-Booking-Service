using Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Bookings.Ef;

/// <summary>
/// Контекст для работы с БД
/// </summary>
public class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
    }
}
