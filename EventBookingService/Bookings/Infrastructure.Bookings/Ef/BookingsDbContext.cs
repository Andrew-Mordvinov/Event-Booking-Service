using Domain.Bookings;

using Infrastructure.Bookings.Ef.Models;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Bookings.Ef;

/// <summary>
/// Контекст для работы с БД
/// </summary>
public class BookingsDbContext(DbContextOptions<BookingsDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingConfirmedOutboxItem> BookingConfirmed => Set<BookingConfirmedOutboxItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsDbContext).Assembly);
    }
}
