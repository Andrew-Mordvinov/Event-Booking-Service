using DataAccess.Abstract;
using DataAccess.EF.EfRepository;
using Entities.Events;

namespace DataAccess.EF;

public class EfEventRepository(AppDbContext dbContext, EfUnitOfWork efUnitOfWork) 
    : EfRepository<Event>(dbContext, efUnitOfWork), IEventRepository
{

}
