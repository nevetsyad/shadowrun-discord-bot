namespace ShadowrunDiscordBot.Domain.Common;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid AggregateId { get; protected set; }
    public string EventType { get; } = string.Empty;
}
