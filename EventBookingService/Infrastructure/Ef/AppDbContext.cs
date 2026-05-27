using Domain.Bookings;
using Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Ef;

/// <summary>
/// Контекст для работы с БД
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
