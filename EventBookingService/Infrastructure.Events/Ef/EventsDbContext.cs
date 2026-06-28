using Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Events.Ef;

/// <summary>
/// Контекст для работы с БД
/// </summary>
public class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
    }
}
