namespace ShadowrunDiscordBot.Core.Metrics;

/// <summary>
/// Interface for tracking application metrics
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Increment a counter metric
    /// </summary>
    void IncrementCounter(string metricName, string? tags = null);
    
    /// <summary>
    /// Record a gauge metric (current value)
    /// </summary>
    void RecordGauge(string metricName, double value, string? tags = null);
    
    /// <summary>
    /// Record a histogram metric (distribution of values)
    /// </summary>
    void RecordHistogram(string metricName, double value, string? tags = null);
    
    /// <summary>
    /// Record a timer metric (duration)
    /// </summary>
    void RecordTimer(string metricName, TimeSpan duration, string? tags = null);
    
    /// <summary>
    /// Get all metrics
    /// </summary>
    Task<Dictionary<string, List<MetricValue>>> GetAllMetricsAsync();
    
    /// <summary>
    /// Get metrics by name
    /// </summary>
    Task<List<MetricValue>?> GetMetricsAsync(string metricName);
    
    /// <summary>
    /// Export metrics to JSON
    /// </summary>
    Task<string> ExportToJsonAsync();
    
    /// <summary>
    /// Export metrics to CSV
    /// </summary>
    Task<string> ExportToCsvAsync();
}

/// <summary>
/// Represents a single metric value
/// </summary>
public class MetricValue
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Tags { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Metric names constants
/// </summary>
public static class MetricNames
{
    // User metrics
    public const string CharactersCreated = "users.characters_created";
    public const string SessionsStarted = "users.sessions_started";
    public const string CommandsUsed = "users.commands_used";
    public const string MessagesProcessed = "users.messages_processed";
    
    // Performance metrics
    public const string CommandLatency = "performance.command_latency_ms";
    public const string DatabaseQueryTime = "performance.database_query_ms";
    public const string DiceRollTime = "performance.dice_roll_ms";
    public const string ApiResponseTime = "performance.api_response_ms";
    
    // System metrics
    public const string CpuUsage = "system.cpu_usage_percent";
    public const string MemoryUsage = "system.memory_usage_mb";
    public const string ActiveSessions = "system.active_sessions";
    public const string BotUptime = "system.uptime_seconds";
    
    // Combat metrics
    public const string CombatSessionsActive = "combat.sessions_active";
    public const string CombatParticipants = "combat.participants_total";
    public const string CombatDuration = "combat.duration_seconds";
    
    // Dice metrics
    public const string DiceRollsTotal = "dice.rolls_total";
    public const string DiceSuccesses = "dice.successes_total";
    public const string DiceGlitches = "dice.glitches_total";
}
