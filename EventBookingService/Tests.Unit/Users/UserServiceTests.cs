using Domain.Users;
using FluentAssertions;
using Moq;

namespace Tests.Unit.Users;

public class UserServiceTests
{
    private class MockHolder
    {
        public required Mock<IUserRepository> UserRepositoryMock { get; init; }
        public required Mock<IPasswordManager> PasswordManagerMock { get; init; }
        public required Mock<ITokenGenerator> TokenGeneratorMock { get; init; }
        public required Mock<IUnitOfWork> UnitOfWorkMock { get; init; }
        public required Mock<IValidator<RegisterUserRequest>> RegistrationValidatorMock { get; init; }
    }

    private static UserService CreateService(out MockHolder mockHolder)
    {
        mockHolder = new MockHolder
        {
            UserRepositoryMock = new Mock<IUserRepository>(),
            PasswordManagerMock = new Mock<IPasswordManager>(),
            UnitOfWorkMock = new Mock<IUnitOfWork>(),
            TokenGeneratorMock = new Mock<ITokenGenerator>(),
            RegistrationValidatorMock = new Mock<IValidator<RegisterUserRequest>>(),
        };

        return new UserService(
            mockHolder.UserRepositoryMock.Object,
            mockHolder.PasswordManagerMock.Object,
            mockHolder.TokenGeneratorMock.Object,
            mockHolder.UnitOfWorkMock.Object,
            mockHolder.RegistrationValidatorMock.Object);
    }

    #region AuthUserAsync

    [Fact]
    public async Task AuthUserAsync_ValidCreds_ReturnToken()
    {
        // Arrange
        var service = CreateService(out var holder);
        var request = new AuthUserRequest { Login = "admin", Password = "password" };
        var user = new User(Guid.NewGuid(), request.Login, "passwordhash", Roles.Admin);
        var token = "some.generated.token";

        // Получили пользователя по логину
        holder.UserRepositoryMock.Setup(t => t.GetByLoginAsync(request.Login, TestContext.Current.CancellationToken))
            .ReturnsAsync(user)
            .Verifiable(Times.Once);

        // Проверили пароль
        holder.PasswordManagerMock.Setup(t => t.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true)
            .Verifiable(Times.Once);

        // Сгенерировали токен
        holder.TokenGeneratorMock.Setup(t => t.GenerateToken(user))
            .Returns(token)
            .Verifiable(Times.Once);

        // Act
        var result = await service.AuthUserAsync(request, TestContext.Current.CancellationToken);

        // Assert
        holder.UserRepositoryMock.Verify();
        holder.PasswordManagerMock.Verify();
        holder.TokenGeneratorMock.Verify();
        result.Should().Be(token);
    }

    [Fact]
    public async Task AuthUserAsync_NoUserWithLogin_ThrowAuthFailed()
    {
        // Arrange
        var service = CreateService(out var holder);
        var request = new AuthUserRequest { Login = "admin", Password = "password" };

        // Не получили пользователя по логину
        holder.UserRepositoryMock.Setup(t => t.GetByLoginAsync(request.Login, TestContext.Current.CancellationToken))
            .ReturnsAsync((User?)null)
            .Verifiable(Times.Once);

        // Не проверяли пароль
        holder.PasswordManagerMock.Setup(t => t.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable(Times.Never);

        // Не генерировали токен
        holder.TokenGeneratorMock.Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.AuthUserAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<AuthFailedException>()
            .WithMessage(UserServiceErrors.InvalidCredentials);

        holder.UserRepositoryMock.Verify();
        holder.PasswordManagerMock.Verify();
        holder.TokenGeneratorMock.Verify();
    }

    [Fact]
    public async Task AuthUserAsync_WrongPassword_ThrowAuthFailed()
    {
        // Arrange
        var service = CreateService(out var holder);
        var request = new AuthUserRequest { Login = "admin", Password = "password" };
        var user = new User(Guid.NewGuid(), request.Login, "passwordhash", Roles.Admin);

        // Получили пользователя по логину
        holder.UserRepositoryMock.Setup(t => t.GetByLoginAsync(request.Login, TestContext.Current.CancellationToken))
            .ReturnsAsync(user)
            .Verifiable(Times.Once);

        // Проверили пароль, не совпал
        holder.PasswordManagerMock.Setup(t => t.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false)
            .Verifiable(Times.Once);

        // Не генерировали токен
        holder.TokenGeneratorMock.Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.AuthUserAsync(request, TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowExactlyAsync<AuthFailedException>()
            .WithMessage(UserServiceErrors.InvalidCredentials);

        holder.UserRepositoryMock.Verify();
        holder.PasswordManagerMock.Verify();
        holder.TokenGeneratorMock.Verify();
    }

    #endregion

    #region RegisterUserAsync

    [Fact]
    public async Task RegisterUserAsync_ValidRequest_SuccessfullyRegistered()
    {
        // Arrange
        var service = CreateService(out var holder);
        var request = new RegisterUserRequest { Login = "user", Password = "password", Role = Roles.User };
        var hash = "passwordhash";

        // Валидация без ошибок
        holder.RegistrationValidatorMock.Setup(t => t.Validate(request))
            .Returns([])
            .Verifiable(Times.Once);

        // Хэшируем пароль
        holder.PasswordManagerMock.Setup(t => t.HashPassword(request.Password))
            .Returns(hash)
            .Verifiable(Times.Once);

        // Добавили пользователя
        holder.UserRepositoryMock.Setup(t => t.AddAsync(
                It.Is<User>(t => t.Role == request.Role && t.Login == request.Login && t.PasswordHash == hash), 
                TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Сохранили
        holder.UnitOfWorkMock.Setup(t => t.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Once);

        // Act
        await service.RegisterUserAsync(request, TestContext.Current.CancellationToken);

        // Assert
        holder.UserRepositoryMock.Verify();
        holder.PasswordManagerMock.Verify();
        holder.RegistrationValidatorMock.Verify();
        holder.UnitOfWorkMock.Verify();
    }

    [Fact]
    public async Task RegisterUserAsync_InvalidRequest_ThrowValidation()
    {
        // Arrange
        var service = CreateService(out var holder);
        var request = new RegisterUserRequest { Login = string.Empty, Password = "password", Role = Roles.User };
        var errorList = new List<string> { "Ошибка" };

        // Валидация c ошибками
        holder.RegistrationValidatorMock.Setup(t => t.Validate(request))
            .Returns(errorList)
            .Verifiable(Times.Once);

        // Не хэшируем пароль
        holder.PasswordManagerMock.Setup(t => t.HashPassword(request.Password))
            .Verifiable(Times.Never);

        // Не добавили пользователя
        holder.UserRepositoryMock.Setup(t => t.AddAsync(It.IsAny<User>(), TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Не сохранили
        holder.UnitOfWorkMock.Setup(t => t.SaveChangesAsync(TestContext.Current.CancellationToken))
            .Verifiable(Times.Never);

        // Act
        var act = async () => await service.RegisterUserAsync(request, TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowExactlyAsync<ValidationException>())
            .Which.Errors.Should().BeEquivalentTo(errorList);

        holder.UserRepositoryMock.Verify();
        holder.PasswordManagerMock.Verify();
        holder.RegistrationValidatorMock.Verify();
        holder.UnitOfWorkMock.Verify();
    }

    // TODO написать тесты на парсинг исключений по уникальности в UoW и эти заполнить + тесты репозитория и юзер контекста, дохуя тестов короч

    #endregion
}
