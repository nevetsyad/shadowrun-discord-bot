namespace ShadowrunDiscordBot.Core.Metrics;

using System.Diagnostics;
using Microsoft.Extensions.Logging;

/// <summary>
/// Extension methods for metrics service
/// </summary>
public static class MetricsServiceExtensions
{
    /// <summary>
    /// Time an operation and record the duration
    /// </summary>
    public static async Task<T> TimeAsync<T>(
        this IMetricsService metricsService,
        string metricName,
        Func<Task<T>> operation,
        string? tags = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            stopwatch.Stop();
            metricsService.RecordTimer(metricName, stopwatch.Elapsed, tags);
            return result;
        }
        catch
        {
            stopwatch.Stop();
            metricsService.RecordTimer(metricName, stopwatch.Elapsed, $"{tags};error=true");
            throw;
        }
    }
    
    /// <summary>
    /// Time an operation and record the duration (non-async)
    /// </summary>
    public static T Time<T>(
        this IMetricsService metricsService,
        string metricName,
        Func<T> operation,
        string? tags = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = operation();
            stopwatch.Stop();
            metricsService.RecordTimer(metricName, stopwatch.Elapsed, tags);
            return result;
        }
        catch
        {
            stopwatch.Stop();
            metricsService.RecordTimer(metricName, stopwatch.Elapsed, $"{tags};error=true");
            throw;
        }
    }
    
    /// <summary>
    /// Start a timer that will record when disposed
    /// </summary>
    public static MetricsTimer StartTimer(
        this IMetricsService metricsService,
        string metricName,
        string? tags = null)
    {
        return new MetricsTimer(metricsService, metricName, tags);
    }
}
