using Application.Users.Infrastructure;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Ef;
using Shared.Interfaces.Infrastructure;

namespace Infrastructure.Users.Ef;

public class EfUserRepository(UsersDbContext dbContext, IUnitOfWork efUnitOfWork)
    : EfRepository<User, UsersDbContext>(dbContext, efUnitOfWork, TableNames.Users), IUserRepository
{
    public Task<User?> GetByLoginAsync(string login, CancellationToken token = default)
    {
        return Items
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Login == login, token);
    }
}
