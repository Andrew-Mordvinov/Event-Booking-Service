using FluentAssertions;
using Infrastructure.Users.Security;

namespace Tests.Unit.Security;

public class DefautPasswordManagerTests
{
    // Для менеджера паролей есть по сути только четыре условия:
    // 1. Захэшировать пароль - верифицировать захэшированный пароль
    // 2. Захэшировать пароль - не пройти верификацию при другом пароле
    // 3. Один и тот же пароль не должен выдавать одинаковый хэш
    // 4. Разные пароли не должны выдавать одинаковый хэш
    private static DefautPasswordManager CreateHasher() => new();

    [Fact]
    public void HashPassword_OnePassword_DifferentHashReturns()
    {
        // Arrange 
        var hasher = CreateHasher();
        var testPass = "SomePassword123!";

        // Act
        var hash1 = hasher.HashPassword(testPass);
        var hash2 = hasher.HashPassword(testPass);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashPassword_DifferentPassword_DifferentHashReturns()
    {
        // Arrange 
        var hasher = CreateHasher();
        var testPass = "SomePassword123!";
        var anotherTestPass = "44SecretPass#";

        // Act
        var hash1 = hasher.HashPassword(testPass);
        var hash2 = hasher.HashPassword(anotherTestPass);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_VerifyAfterHashing_SuccessfullyVerified()
    {
        // Arrange
        var hasher = CreateHasher();
        var testPass = "SomePassword123!";
        var hash = hasher.HashPassword(testPass);
        
        // Act
        var verifyResult = hasher.VerifyPassword(testPass, hash);

        // Assert
        verifyResult.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_VerifyWithWrongPass_VerificationFailed()
    {
        // Arrange
        var hasher = CreateHasher();
        var testPass = "SomePassword123!";
        var hash = hasher.HashPassword(testPass);
        var anotherPass = "diffpw0912";

        // Act
        var verifyResult = hasher.VerifyPassword(anotherPass, hash);

        // Assert
        verifyResult.Should().BeFalse();
    }
}
