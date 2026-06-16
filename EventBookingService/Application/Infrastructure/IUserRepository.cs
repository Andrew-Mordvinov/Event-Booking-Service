using Application.Infrastructure.Common;
using Domain.Bookings;

namespace Application.Infrastructure;

/// <summary>
/// Репозиторий пользователей
/// </summary>
public interface IUserRepository : IBaseStorage<Booking>
{
}
