using Shared.Infrastructure.Abstract.ExceptionPatterns;

namespace Infrastructure.Events.Ef.ExceptionPatterns;

/// <summary>
/// <inheritdoc cref="IExceptionPatternsProvider"/>
/// </summary>
public class EventsExceptionPatternsProvider : IExceptionPatternsProvider
{
    public IReadOnlyCollection<ExceptionPattern> GetExceptionPatterns() => [];
}
