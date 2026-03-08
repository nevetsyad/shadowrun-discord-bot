using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Background service that automatically detects idle sessions and pauses them.
/// Runs every 5 minutes to check for sessions with no activity for 30+ minutes.
/// </summary>
public class SessionIdleDetectionService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SessionIdleDetectionService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public SessionIdleDetectionService(
        IServiceProvider services,
        ILogger<SessionIdleDetectionService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Idle Detection Service started");

        // Wait a bit for the bot to fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var sessionManagement = scope.ServiceProvider
                    .GetRequiredService<SessionManagementService>();

                _logger.LogDebug("Checking for idle sessions...");

                var autoBreaks = await sessionManagement.CheckForIdleSessionsAsync();

                if (autoBreaks.Count > 0)
                {
                    _logger.LogInformation("Auto-paused {Count} idle sessions", autoBreaks.Count);

                    // Optionally send Discord notifications here
                    // You would need to inject DiscordSocketClient and send messages to channels
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for idle sessions");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
        }

        _logger.LogInformation("Session Idle Detection Service stopped");
    }
}

/// <summary>
/// Extension methods for registering the idle detection service
/// </summary>
public static class SessionIdleDetectionServiceExtensions
{
    /// <summary>
    /// Adds the session idle detection background service
    /// </summary>
    public static IServiceCollection AddSessionIdleDetection(this IServiceCollection services)
    {
        services.AddHostedService<SessionIdleDetectionService>();
        return services;
    }
}
