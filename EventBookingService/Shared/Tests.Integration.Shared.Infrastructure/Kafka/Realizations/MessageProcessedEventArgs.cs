namespace Tests.Integration.Shared.Infrastructure.Kafka.Realizations;

internal class MessageProcessedEventArgs : EventArgs
{
    public required TestMessage Message { get; init; }
}
