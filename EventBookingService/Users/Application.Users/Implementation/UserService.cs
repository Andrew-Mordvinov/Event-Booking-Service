using Application.Users.DTO;
using Application.Users.Infrastructure;
using Application.Users.Interfaces;
using Application.Users.Validations;
using Domain.Users;
using Domain.Users.Exceptions;
using Shared.Exceptions;
using Shared.Infrastructure.Abstract;

namespace Application.Users.Implementation;

public class UserService(
    IUserRepository _userRepository,
    IPasswordManager _passwordManager,
    ITokenGenerator _tokenGenerator,
    IUnitOfWork _unitOfWork,
    IValidator<RegisterUserRequest> _registrationValidator) : IUserService
{
    public async Task<string> AuthUserAsync(AuthUserRequest request, CancellationToken token = default)
    {
        var user = await _userRepository.GetByLoginAsync(request.Login, token) ?? throw new AuthFailedException(UserServiceErrors.InvalidCredentials);

        if (!_passwordManager.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new AuthFailedException(UserServiceErrors.InvalidCredentials);
        }

        return _tokenGenerator.GenerateToken(user);
    }

    public async Task RegisterUserAsync(RegisterUserRequest request, CancellationToken token = default)
    {
        var errors = _registrationValidator.Validate(request);
        if (errors.Any())
        {
            throw new ValidationException(errors);
        }

        var hash = _passwordManager.HashPassword(request.Password);

        await _userRepository.AddAsync(new User(Guid.NewGuid(), request.Login, hash, request.Role), token);
        await _unitOfWork.SaveChangesAsync(token);
    }
}
