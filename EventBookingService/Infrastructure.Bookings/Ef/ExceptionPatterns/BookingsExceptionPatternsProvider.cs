using Infrastructure.Users.Ef.ExceptionPatterns;
using Shared.Infrastructure.Ef.ExceptionPatterns;

namespace Infrastructure.Bookings.Ef.ExceptionPatterns;

/// <summary>
/// <inheritdoc cref="IExceptionPatternsProvider"/>
/// </summary>
public class BookingsExceptionPatternsProvider : IExceptionPatternsProvider
{
    public IReadOnlyCollection<ExceptionPattern> GetExceptionPatterns() => [];
}
