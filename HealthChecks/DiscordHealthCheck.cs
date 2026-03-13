using Discord.WebSocket;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ShadowrunDiscordBot.HealthChecks;

/// <summary>
/// Health check for Discord connection status
/// </summary>
public class DiscordHealthCheck : IHealthCheck
{
    private readonly DiscordSocketClient _discordClient;
    private readonly ILogger<DiscordHealthCheck> _logger;

    public DiscordHealthCheck(
        DiscordSocketClient discordClient,
        ILogger<DiscordHealthCheck> logger)
    {
        _discordClient = discordClient;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionState = _discordClient.ConnectionState;
            var latency = _discordClient.Latency;
            var guildCount = _discordClient.Guilds.Count;

            var data = new Dictionary<string, object>
            {
                ["connectionState"] = connectionState.ToString(),
                ["latencyMs"] = latency,
                ["guildCount"] = guildCount,
                ["isConnected"] = _discordClient.IsConnected
            };

            if (connectionState == ConnectionState.Connected)
            {
                if (latency < 500)
                {
                    return Task.FromResult(HealthCheckResult.Healthy(
                        $"Discord connected with {latency}ms latency across {guildCount} guilds",
                        data));
                }
                else if (latency < 1000)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Discord connected but latency is high: {latency}ms",
                        data));
                }
                else
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Discord connected but latency is very high: {latency}ms",
                        data));
                }
            }
            else if (connectionState == ConnectionState.Connecting)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Discord is connecting...",
                    data));
            }
            else if (connectionState == ConnectionState.Disconnecting)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Discord is disconnecting",
                    data));
            }
            else
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Discord is disconnected (state: {connectionState})",
                    data));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Discord health");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Error checking Discord health",
                ex));
        }
    }
}
