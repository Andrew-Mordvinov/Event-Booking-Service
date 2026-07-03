using Application.Users.Infrastructure;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Users.Security;

/// <summary>
/// Дефолтный менеджер паролей
/// </summary>
public class DefautPasswordManager : IPasswordManager
{
    public const int DegreeOfParallelism = 4;
    public const int Iterations = 4;
    public const int MemorySize = 16 * 1024;
    public const int HashSize = 32;
    public const int SaltSize = 16;

    public const string Algorithm = "Argon2id";

    public const string Separator = ":";

    public string HashPassword(string pass)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(pass))
        {
            Salt = GenerateSalt(),
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize
        };

        byte[] hash = argon2.GetBytes(HashSize);
        return $"{Convert.ToBase64String(hash)}{Separator}{Convert.ToBase64String(argon2.Salt)}";
    }

    public bool VerifyPassword(string pass, string storedHash)
    {
        string[] parts = storedHash.Split(Separator);
        if (parts.Length != 2)
        {
            throw new FormatException("Неверный формат хэша");
        }

        string saltBase64 = parts[1];
        string hashBase64 = parts[0];

        byte[] salt = Convert.FromBase64String(saltBase64);
        byte[] storedHashBytes = Convert.FromBase64String(hashBase64);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(pass))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize
        };

        byte[] computedHash = argon2.GetBytes(storedHashBytes.Length);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
    }

    private static byte[] GenerateSalt()
    {
        byte[] salt = new byte[SaltSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }
}
