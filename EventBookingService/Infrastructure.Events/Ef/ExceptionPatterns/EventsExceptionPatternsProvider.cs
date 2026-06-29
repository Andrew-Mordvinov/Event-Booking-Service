using Infrastructure.Users.Ef.ExceptionPatterns;
using Shared.Infrastructure.Ef.ExceptionPatterns;

namespace Infrastructure.Events.Ef.ExceptionPatterns;

/// <summary>
/// <inheritdoc cref="IExceptionPatternsProvider"/>
/// </summary>
public class EventsExceptionPatternsProvider : IExceptionPatternsProvider
{
    public IReadOnlyCollection<ExceptionPattern> GetExceptionPatterns() => [];
}
