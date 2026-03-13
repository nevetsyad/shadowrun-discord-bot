namespace ShadowrunDiscordBot.Core.Metrics;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of metrics service using in-memory storage
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<MetricValue>> _metrics = new();
    private readonly DateTime _startTime = DateTime.UtcNow;
    
    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
    }
    
    public void IncrementCounter(string metricName, string? tags = null)
    {
        var metric = new MetricValue
        {
            Name = metricName,
            Type = "counter",
            Value = 1,
            Tags = tags,
            Timestamp = DateTime.UtcNow
        };
        
        AddMetric(metricName, metric);
        _logger.LogDebug("Counter incremented: {MetricName}", metricName);
    }
    
    public void RecordGauge(string metricName, double value, string? tags = null)
    {
        var metric = new MetricValue
        {
            Name = metricName,
            Type = "gauge",
            Value = value,
            Tags = tags,
            Timestamp = DateTime.UtcNow
        };
        
        AddMetric(metricName, metric);
        _logger.LogDebug("Gauge recorded: {MetricName} = {Value}", metricName, value);
    }
    
    public void RecordHistogram(string metricName, double value, string? tags = null)
    {
        var metric = new MetricValue
        {
            Name = metricName,
            Type = "histogram",
            Value = value,
            Tags = tags,
            Timestamp = DateTime.UtcNow
        };
        
        AddMetric(metricName, metric);
        _logger.LogDebug("Histogram recorded: {MetricName} = {Value}", metricName, value);
    }
    
    public void RecordTimer(string metricName, TimeSpan duration, string? tags = null)
    {
        var metric = new MetricValue
        {
            Name = metricName,
            Type = "timer",
            Value = duration.TotalMilliseconds,
            Tags = tags,
            Timestamp = DateTime.UtcNow
        };
        
        AddMetric(metricName, metric);
        _logger.LogDebug("Timer recorded: {MetricName} = {Duration}ms", metricName, duration.TotalMilliseconds);
    }
    
    public Task<Dictionary<string, List<MetricValue>>> GetAllMetricsAsync()
    {
        var result = new Dictionary<string, List<MetricValue>>();
        
        foreach (var kvp in _metrics)
        {
            result[kvp.Key] = kvp.Value.ToList();
        }
        
        return Task.FromResult(result);
    }
    
    public Task<List<MetricValue>?> GetMetricsAsync(string metricName)
    {
        if (_metrics.TryGetValue(metricName, out var values))
        {
            return Task.FromResult<List<MetricValue>?>(values.ToList());
        }
        
        return Task.FromResult<List<MetricValue>?>(null);
    }
    
    public async Task<string> ExportToJsonAsync()
    {
        var allMetrics = await GetAllMetricsAsync();
        return JsonSerializer.Serialize(allMetrics, new JsonSerializerOptions { WriteIndented = true });
    }
    
    public async Task<string> ExportToCsvAsync()
    {
        var allMetrics = await GetAllMetricsAsync();
        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("MetricName,Type,Value,Tags,Timestamp");
        
        // Data
        foreach (var kvp in allMetrics)
        {
            foreach (var metric in kvp.Value)
            {
                csv.AppendLine($"\"{metric.Name}\",{metric.Type},{metric.Value},\"{metric.Tags ?? ""}\",{metric.Timestamp:O}");
            }
        }
        
        return csv.ToString();
    }
    
    private void AddMetric(string metricName, MetricValue metric)
    {
        var bag = _metrics.GetOrAdd(metricName, _ => new ConcurrentBag<MetricValue>());
        bag.Add(metric);
    }
    
    /// <summary>
    /// Get a snapshot of system metrics
    /// </summary>
    public void RecordSystemMetrics()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            
            // CPU usage (approximate)
            RecordGauge(MetricNames.CpuUsage, process.TotalProcessorTime.TotalMilliseconds);
            
            // Memory usage in MB
            RecordGauge(MetricNames.MemoryUsage, process.WorkingSet64 / 1024.0 / 1024.0);
            
            // Bot uptime in seconds
            RecordGauge(MetricNames.BotUptime, (DateTime.UtcNow - _startTime).TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record system metrics");
        }
    }
}

/// <summary>
/// Helper class for timing operations
/// </summary>
public class MetricsTimer : IDisposable
{
    private readonly IMetricsService _metricsService;
    private readonly string _metricName;
    private readonly string? _tags;
    private readonly Stopwatch _stopwatch;
    
    public MetricsTimer(IMetricsService metricsService, string metricName, string? tags = null)
    {
        _metricsService = metricsService;
        _metricName = metricName;
        _tags = tags;
        _stopwatch = Stopwatch.StartNew();
    }
    
    public void Dispose()
    {
        _stopwatch.Stop();
        _metricsService.RecordTimer(_metricName, _stopwatch.Elapsed, _tags);
    }
}
