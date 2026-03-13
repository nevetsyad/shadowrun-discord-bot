namespace ShadowrunDiscordBot.Core.EventSourcing;

using ShadowrunDiscordBot.Domain.Common;
using System.Text.Json;

/// <summary>
/// Stored event representation for persistence
/// </summary>
public class StoredEvent
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime StoredAt { get; set; }
    public string? Metadata { get; set; }
}

/// <summary>
/// Interface for event store service
/// </summary>
public interface IEventStoreService
{
    /// <summary>
    /// Apply and persist a domain event
    /// </summary>
    Task ApplyEventAsync(DomainEvent @event, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Apply and persist multiple domain events
    /// </summary>
    Task ApplyEventsAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Replay all events to rebuild state
    /// </summary>
    Task ReplayAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get event history for an aggregate
    /// </summary>
    Task<IEnumerable<StoredEvent>> GetHistoryAsync(Guid aggregateId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all events (for replay)
    /// </summary>
    Task<IEnumerable<StoredEvent>> GetAllEventsAsync(int? limit = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for event handlers
/// </summary>
/// <typeparam name="T">The event type</typeparam>
public interface IEventHandler<in T> where T : DomainEvent
{
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}
