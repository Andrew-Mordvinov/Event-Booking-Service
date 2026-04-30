using Bookings.Service;

using DataAccess.Abstract;

namespace Web.Infrastructure.Bookings;

public class BookingManagerBackgroundService(
    IServiceScopeFactory _scopeFactory,
    ILogger<BookingManagerBackgroundService> _logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Сервис BookingManagerBackgroundService начал работу");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "При обработке бронирований возникло исключение");
            }
        }

        _logger.LogInformation("Сервис BookingManagerBackgroundService остановлен");
    }

    internal async Task ProcessPendingBookingsAsync(CancellationToken stoppingToken)
    {
        List<Guid> pendingBookings = [];
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            pendingBookings = await repo.GetPendingBookingsAsync(stoppingToken);
        }

        if (pendingBookings.Count < 1)
        {
            _logger.LogInformation("Не найдено ожидающих обработки бронирований");
            // Задержка чтобы не спамить вызовами, когда нет ожидающих бронирований
            await Task.Delay(1000, stoppingToken);

            return;
        }

        var tasks = pendingBookings.Select(async (booking) =>
        {
            await using var taskScope = _scopeFactory.CreateAsyncScope();
            
            var service = taskScope.ServiceProvider.GetRequiredService<IBookingService>();

            await service.ProcessBookingAsync(booking, stoppingToken);
        });

        await Task.WhenAll(tasks);
    }
}
