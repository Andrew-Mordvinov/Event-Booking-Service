using Bookings.Service;

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
                using var scope = _scopeFactory.CreateScope();
                var _service = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                await _service.ProcessPendingBookingsAsync(100, stoppingToken);
                await Task.Delay(5000, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "При обработке бронирований возникло исключение");
            }
        }

        _logger.LogInformation("Сервис BookingManagerBackgroundService остановлен");
    }
}
