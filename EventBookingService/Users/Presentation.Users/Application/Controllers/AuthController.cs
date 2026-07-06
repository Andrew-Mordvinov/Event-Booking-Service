
using Application.Users.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Presentation.Users.DTO.Request;
using Presentation.Users.DTO.Response;

namespace Presentation.Users.Application.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController(IUserService _userService) : ControllerBase
    {
        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        /// <param name="request">Запрос на регистрацию</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
        /// <response code="204">Пользователь зарегистрирован</response>
        /// <response code="409">Логин уже занят</response>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUserAsync(AspRegisterUserRequest request, CancellationToken cancellationToken = default)
        {
            await _userService.RegisterUserAsync(request.ToRegisterUserRequest(), cancellationToken);

            return NoContent();
        }

        /// <summary>
        /// Аутентификация пользователя и выдача токена
        /// </summary>
        /// <param name="request">Запрос на аутентификацию</param>
        /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
        /// <response code="200">Аутентификация успешна - выдан токен</response>
        /// <response code="404">Неверные данные</response>
        [Produces("application/json")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpPost("login")]
        public async Task<ActionResult<TokenResponse>> LoginAsync(AspAuthUserRequest request, CancellationToken cancellationToken = default)
        {
            var generatedToken = await _userService.AuthUserAsync(request.ToAuthUserRequest(), cancellationToken);

            return Ok(new TokenResponse { Token = generatedToken });
        }
    }
}
