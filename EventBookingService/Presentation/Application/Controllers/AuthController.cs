using Application.Interfaces;

using Microsoft.AspNetCore.Mvc;

using Presentation.DTO.Users.Request;
using Presentation.DTO.Users.Response;

namespace Presentation.Application.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController(IUserService _userService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUserAsync(AspRegisterUserRequest request, CancellationToken token = default)
        {
            await _userService.RegisterUserAsync(request.ToRegisterUserRequest(), token);

            return NoContent();
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponse>> LoginAsync(AspAuthUserRequest request, CancellationToken token = default)
        {
            var generatedToken = await _userService.AuthUserAsync(request.ToAuthUserRequest(), token);

            return Ok(new TokenResponse { Token = generatedToken });
        }
    }
}
