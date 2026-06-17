namespace Application.DTO.Users;

public class AuthUserRequest
{
    public required string Login { get; init; }

    public required string Password { get; init; }
}
