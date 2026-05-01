using DataAccess.EF;
using Microsoft.EntityFrameworkCore;

namespace Tests.TemplateRepository.EfRepository;

/// <summary>
/// Тестовый контекст для работы с БД. Не конструирует модели доменных сущностей, только тестовую
/// </summary>
internal class TestDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestItem>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}
