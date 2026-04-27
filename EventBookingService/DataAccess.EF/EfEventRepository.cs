using DataAccess.Abstract;
using DataAccess.EF.EfRepository;
using Events.Models;

namespace DataAccess.EF;

public class EfEventRepository(AppDbContext dbContext) : EfRepository<Event>(dbContext), IEventRepository
{

}
