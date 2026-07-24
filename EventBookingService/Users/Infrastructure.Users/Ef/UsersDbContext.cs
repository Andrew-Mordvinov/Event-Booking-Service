using Domain.Users;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Users.Ef;

/// <summary>
/// Контекст для работы с БД
/// </summary>
public class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}
