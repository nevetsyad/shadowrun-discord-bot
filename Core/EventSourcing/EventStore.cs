namespace ShadowrunDiscordBot.Core.EventSourcing;

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Domain.Common;

/// <summary>
/// In-memory implementation of event store for development/testing
/// Can be replaced with database-backed implementation later
/// </summary>
public class EventStore : IEventStore
{
    private readonly ILogger<EventStore> _logger;
    private readonly ConcurrentBag<StoredEvent> _events = new();
    
    public EventStore(ILogger<EventStore> logger)
    {
        _logger = logger;
    }
    
    public Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var storedEvents = _events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.OccurredAt)
            .ToList();
        
        var domainEvents = storedEvents.Select(DeserializeEvent).Where(e => e != null)!;
        
        _logger.LogDebug(
            "Retrieved {Count} events for aggregate {AggregateId}",
            storedEvents.Count,
            aggregateId);
        
        return Task.FromResult(domainEvents!);
    }
    
    public Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId, DateTime from, CancellationToken cancellationToken = default)
    {
        var storedEvents = _events
            .Where(e => e.AggregateId == aggregateId && e.OccurredAt >= from)
            .OrderBy(e => e.OccurredAt)
            .ToList();
        
        var domainEvents = storedEvents.Select(DeserializeEvent).Where(e => e != null)!;
        
        return Task.FromResult(domainEvents!);
    }
    
    public Task SaveEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        var storedEvent = new StoredEvent
        {
            Id = @event.EventId,
            AggregateId = @event.AggregateId,
            EventType = @event.EventType,
            EventData = SerializeEvent(@event),
            OccurredAt = @event.OccurredAt,
            StoredAt = DateTime.UtcNow
        };
        
        _events.Add(storedEvent);
        
        _logger.LogInformation(
            "Saved event {EventType} for aggregate {AggregateId}",
            @event.EventType,
            @event.AggregateId);
        
        return Task.CompletedTask;
    }
    
    public Task SaveEventsAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var @event in events)
        {
            var storedEvent = new StoredEvent
            {
                Id = @event.EventId,
                AggregateId = @event.AggregateId,
                EventType = @event.EventType,
                EventData = SerializeEvent(@event),
                OccurredAt = @event.OccurredAt,
                StoredAt = DateTime.UtcNow
            };
            
            _events.Add(storedEvent);
        }
        
        _logger.LogInformation("Saved {Count} events", events.Count());
        
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<DomainEvent>> GetAllEventsAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = _events
            .OrderBy(e => e.OccurredAt)
            .Select(DeserializeEvent)
            .Where(e => e != null)!;
        
        return Task.FromResult(domainEvents!);
    }
    
    private string SerializeEvent(DomainEvent @event)
    {
        return JsonSerializer.Serialize(@event, @event.GetType());
    }
    
    private DomainEvent? DeserializeEvent(StoredEvent storedEvent)
    {
        try
        {
            // Get the event type from the type name
            var type = Type.GetType($"ShadowrunDiscordBot.Domain.Events.{storedEvent.EventType}");
            
            if (type == null)
            {
                _logger.LogWarning("Could not find event type: {EventType}", storedEvent.EventType);
                return null;
            }
            
            return JsonSerializer.Deserialize(storedEvent.EventData, type) as DomainEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize event {EventId}", storedEvent.Id);
            return null;
        }
    }
}
