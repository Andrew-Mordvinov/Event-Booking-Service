using Application.DTO.Users;
using Application.Infrastructure;
using Application.Infrastructure.Common;
using Application.Interfaces;
using Application.Validations;
using Domain.Exceptions;
using Domain.Exceptions.Users;
using Domain.Users;

namespace Application.Implementation;

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
