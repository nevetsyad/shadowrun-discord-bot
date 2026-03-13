namespace ShadowrunDiscordBot.Core.EventSourcing;

using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Domain.Common;

/// <summary>
/// Service for managing event sourcing operations
/// </summary>
public class EventStoreService : IEventStoreService
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<EventStoreService> _logger;
    private readonly Dictionary<Type, List<Func<DomainEvent, CancellationToken, Task>>> _handlers = new();
    
    public EventStoreService(IEventStore eventStore, ILogger<EventStoreService> logger)
    {
        _eventStore = eventStore;
        _logger = logger;
    }
    
    /// <summary>
    /// Register a handler for a specific event type
    /// </summary>
    public void RegisterHandler<T>(IEventHandler<T> handler) where T : DomainEvent
    {
        var eventType = typeof(T);
        
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<Func<DomainEvent, CancellationToken, Task>>();
        }
        
        _handlers[eventType].Add(async (domainEvent, ct) =>
        {
            if (domainEvent is T typedEvent)
            {
                await handler.HandleAsync(typedEvent, ct);
            }
        });
        
        _logger.LogDebug("Registered handler for event type {EventType}", eventType.Name);
    }
    
    public async Task ApplyEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        // Save the event
        await _eventStore.SaveEventAsync(@event, cancellationToken);
        
        // Call registered handlers
        await CallHandlersAsync(@event, cancellationToken);
        
        _logger.LogInformation(
            "Applied event {EventType} for aggregate {AggregateId}",
            @event.EventType,
            @event.AggregateId);
    }
    
    public async Task ApplyEventsAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        // Save all events
        await _eventStore.SaveEventsAsync(events, cancellationToken);
        
        // Call handlers for each event
        foreach (var @event in events)
        {
            await CallHandlersAsync(@event, cancellationToken);
        }
        
        _logger.LogInformation("Applied {Count} events", events.Count());
    }
    
    public async Task ReplayAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting event replay...");
        
        var events = await _eventStore.GetAllEventsAsync(cancellationToken);
        var eventList = events.ToList();
        
        _logger.LogInformation("Replaying {Count} events", eventList.Count);
        
        foreach (var @event in eventList)
        {
            await CallHandlersAsync(@event, cancellationToken);
        }
        
        _logger.LogInformation("Event replay complete");
    }
    
    public async Task<IEnumerable<StoredEvent>> GetHistoryAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
        
        // Convert to stored events (in a real implementation, this would query the database)
        return events.Select(e => new StoredEvent
        {
            Id = e.EventId,
            AggregateId = e.AggregateId,
            EventType = e.EventType,
            EventData = "{}", // Would serialize the actual event
            OccurredAt = e.OccurredAt,
            StoredAt = DateTime.UtcNow
        });
    }
    
    public async Task<IEnumerable<StoredEvent>> GetAllEventsAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        var events = await _eventStore.GetAllEventsAsync(cancellationToken);
        
        if (limit.HasValue)
        {
            events = events.Take(limit.Value);
        }
        
        return events.Select(e => new StoredEvent
        {
            Id = e.EventId,
            AggregateId = e.AggregateId,
            EventType = e.EventType,
            EventData = "{}",
            OccurredAt = e.OccurredAt,
            StoredAt = DateTime.UtcNow
        });
    }
    
    private async Task CallHandlersAsync(DomainEvent @event, CancellationToken cancellationToken)
    {
        var eventType = @event.GetType();
        
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                try
                {
                    await handler(@event, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Handler failed for event {EventType}",
                        eventType.Name);
                }
            }
        }
    }
}
