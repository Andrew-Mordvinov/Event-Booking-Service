using Shared.Infrastructure.Ef.ExceptionPatterns;

namespace Infrastructure.Users.Ef.ExceptionPatterns;

public class UsersExceptionPatternsProvider : IExceptionPatternsProvider
{
    private static readonly List<ExceptionPattern> _exceptionPatterns =
    [
        new LoginNotUniquePattern()
    ];

    public IReadOnlyCollection<ExceptionPattern> GetExceptionPatterns()
    {
        return _exceptionPatterns;
    }
}
