using Shared.Infrastructure.Abstract.ExceptionPatterns;

namespace Infrastructure.Bookings.Ef.ExceptionPatterns;

/// <summary>
/// <inheritdoc cref="IExceptionPatternsProvider"/>
/// </summary>
public class BookingsExceptionPatternsProvider : IExceptionPatternsProvider
{
    public IReadOnlyCollection<ExceptionPattern> GetExceptionPatterns() => [];
}
