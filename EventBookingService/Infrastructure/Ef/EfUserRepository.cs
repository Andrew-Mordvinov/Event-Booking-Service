using Application.Infrastructure;
using Application.Infrastructure.Common;
using Domain.Users;
using Infrastructure.Ef.EfRepository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Ef;

public class EfUserRepository(AppDbContext dbContext, IUnitOfWork efUnitOfWork)
    : EfRepository<User>(dbContext, efUnitOfWork, TableNames.Users), IUserRepository
{
    public Task<User?> GetByLoginAsync(string login, CancellationToken token = default)
    {
        return Items
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Login == login, token);
    }
}
