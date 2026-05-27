using DataAccess.Abstract;
using DataAccess.Abstract.Common;
using DataAccess.EF.EfRepository;
using Entities.Events;

namespace DataAccess.EF;

public class EfEventRepository(AppDbContext dbContext, IUnitOfWork efUnitOfWork) 
    : EfRepository<Event>(dbContext, efUnitOfWork, TableNames.Events), IEventRepository
{

}
