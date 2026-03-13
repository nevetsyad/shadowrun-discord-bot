namespace ShadowrunDiscordBot.Domain.Interfaces;

/// <summary>
/// Interface for the event store used in event sourcing
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Get all events for an aggregate
    /// </summary>
    Task<IEnumerable<Common.DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get events for an aggregate from a specific date
    /// </summary>
    Task<IEnumerable<Common.DomainEvent>> GetEventsAsync(Guid aggregateId, DateTime from, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Save a single event
    /// </summary>
    Task SaveEventAsync(Common.DomainEvent @event, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Save multiple events
    /// </summary>
    Task SaveEventsAsync(IEnumerable<Common.DomainEvent> events, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all events (for replay)
    /// </summary>
    Task<IEnumerable<Common.DomainEvent>> GetAllEventsAsync(CancellationToken cancellationToken = default);
}
