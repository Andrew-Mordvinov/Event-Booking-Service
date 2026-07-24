using Microsoft.EntityFrameworkCore;

namespace Tests.Integration.Shared.Infrastructure.Kafka.Realizations;

/// <summary>
/// Тестовый dbcontext для тестов продюсера
/// </summary>
internal class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestMessage> Messages => Set<TestMessage>();
}
