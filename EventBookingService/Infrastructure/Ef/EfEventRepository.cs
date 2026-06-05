using Application.Infrastructure;
using Application.Infrastructure.Common;
using Domain.Events;
using Infrastructure.Ef.EfRepository;

namespace Infrastructure.Ef;

public class EfEventRepository(AppDbContext dbContext, IUnitOfWork efUnitOfWork)
    : EfRepository<Event>(dbContext, efUnitOfWork, TableNames.Events), IEventRepository
{

}
