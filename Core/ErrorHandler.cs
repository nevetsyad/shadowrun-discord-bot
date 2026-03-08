using System.Diagnostics;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace ShadowrunDiscordBot.Core;

/// <summary>
/// Centralized error handling with structured logging and metrics
/// </summary>
public sealed class ErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;
    private readonly Dictionary<string, int> _errorCounts = new();
    private readonly object _errorLock = new();
    private readonly Stopwatch _uptime = Stopwatch.StartNew();

    public ErrorHandler(ILogger<ErrorHandler> logger)
    {
        _logger = logger;
    }

    public void HandleError(Exception exception, string context, SocketUser? user = null, string? command = null)
    {
        var errorKey = $"{context}:{exception.GetType().Name}";
        
        lock (_errorLock)
        {
            _errorCounts.TryGetValue(errorKey, out var count);
            _errorCounts[errorKey] = count + 1;
        }

        var userId = user?.Id.ToString() ?? "Unknown";
        var username = user?.Username ?? "Unknown";

        _logger.LogError(exception, 
            "Error in {Context} | User: {Username} ({UserId}) | Command: {Command} | Error: {ErrorType} | Message: {Message}",
            context, username, userId, command ?? "N/A", exception.GetType().Name, exception.Message);

        // Alert on repeated errors
        if (_errorCounts.TryGetValue(errorKey, out var errorCount) && errorCount >= 5)
        {
            _logger.LogWarning("Repeated error detected: {ErrorKey} has occurred {Count} times", errorKey, errorCount);
        }
    }

    public void HandleWarning(string message, string context, SocketUser? user = null)
    {
        var userId = user?.Id.ToString() ?? "Unknown";
        var username = user?.Username ?? "Unknown";

        _logger.LogWarning("Warning in {Context} | User: {Username} ({UserId}) | Message: {Message}",
            context, username, userId, message);
    }

    public Dictionary<string, object> GetErrorMetrics()
    {
        lock (_errorLock)
        {
            return new Dictionary<string, object>
            {
                ["uptime_seconds"] = _uptime.Elapsed.TotalSeconds,
                ["total_error_types"] = _errorCounts.Count,
                ["error_breakdown"] = new Dictionary<string, int>(_errorCounts),
                ["total_errors"] = _errorCounts.Values.Sum()
            };
        }
    }

    public void ResetErrorCounts()
    {
        lock (_errorLock)
        {
            _errorCounts.Clear();
            _logger.LogInformation("Error counts reset");
        }
    }

    public string GetUserFriendlyErrorMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "Invalid input provided. Please check your command and try again.",
            TimeoutException => "The operation timed out. Please try again.",
            UnauthorizedAccessException => "You don't have permission to perform this action.",
            InvalidOperationException => "An invalid operation was attempted. Please check the command syntax.",
            _ => "An unexpected error occurred. Please try again or contact an administrator."
        };
    }
}
