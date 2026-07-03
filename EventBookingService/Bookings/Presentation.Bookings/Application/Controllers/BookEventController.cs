using Application.Bookings.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Presentation.Bookings.DTO.Response;

namespace Presentation.Bookings.Application.Controllers
{
    /// <remarks>
    /// При размещении в одном контроллере не получается совместить пути events и bookings, сваггер падает.
    /// Поскольку не было команды менять эндпоинты, то для сохранения старого порядка пока так
    /// </remarks>
    /// <param name="_bookingService"></param>
    [Route("events")]
    [ApiController]
    [Authorize]
    public class BookEventController(IBookingService _bookingService) : ControllerBase
    {
        /// <summary>
        /// Бронирование места на событие. Создает ожидающее обработки бронирование и возвращает ссылку на него для отслеживания статуса
        /// </summary>
        /// <param name="eventId">Идентификатор события, на которое бронируется место</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
        /// <response code="202">Бронирование создано и ожидает обработки</response>
        /// <response code="401">Пользователь не определен</response>
        [Produces("application/json")]
        [ProducesResponseType(typeof(BookingAcceptedResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [HttpPost("{eventId}/book")]
        public async Task<ActionResult<BookingAcceptedResponse>> BookEventAsync(Guid eventId, CancellationToken cancellationToken)
        {
            var result = await _bookingService.CreateBookingAsync(eventId, cancellationToken);

            return AcceptedAtRoute(
                nameof(BookingController.GetBookingByIdAsync),
                new { id = result.Id },
                BookingAcceptedResponse.FromBooking(result));
        }
    }
}
