using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Core;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Centralized error handling service for managing and reporting errors
/// </summary>
public class ErrorHandlingService
{
    private readonly ILogger<ErrorHandlingService> _logger;
    private readonly ErrorHandler _errorHandler;

    public ErrorHandlingService(ILogger<ErrorHandlingService> logger, ErrorHandler errorHandler)
    {
        _logger = logger;
        _errorHandler = errorHandler;
    }

    /// <summary>
    /// Handle an error with context information
    /// </summary>
    public async Task HandleErrorAsync(Exception ex, string context)
    {
        _logger.LogError(ex, "Error in {Context}", context);
        
        // Log to error handler for metrics
        _errorHandler.HandleError(ex, context);
        
        // Could send notification to GM or admin here
        await Task.CompletedTask;
    }

    /// <summary>
    /// Determine if an error is critical and requires immediate attention
    /// </summary>
    public bool IsCriticalError(Exception ex)
    {
        return ex is UnauthorizedAccessException || 
               ex is TimeoutException ||
               ex is TaskCanceledException ||
               ex is OutOfMemoryException ||
               ex is StackOverflowException;
    }

    /// <summary>
    /// Get a user-friendly error message for display to users
    /// </summary>
    public string GetUserFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            InvalidOperationException => "Invalid operation. Please check your inputs.",
            ArgumentException => "Invalid argument. Please check your command syntax.",
            ArgumentNullException => "A required value was not provided.",
            ArgumentOutOfRangeException => "A value was out of the acceptable range.",
            TimeoutException => "The operation timed out. Please try again.",
            UnauthorizedAccessException => "You don't have permission to perform this action.",
            NotSupportedException => "This operation is not supported.",
            FormatException => "The input format was invalid. Please check your syntax.",
            OverflowException => "A numeric value was too large or too small.",
            _ => "An unexpected error occurred. Please try again."
        };
    }

    /// <summary>
    /// Handle an error with context and return a user-friendly message
    /// </summary>
    public async Task<string> HandleAndGetMessageAsync(Exception ex, string context)
    {
        await HandleErrorAsync(ex, context);
        return GetUserFriendlyMessage(ex);
    }

    /// <summary>
    /// Log a warning with context
    /// </summary>
    public void LogWarning(string message, string context)
    {
        _logger.LogWarning("Warning in {Context}: {Message}", context, message);
        _errorHandler.HandleWarning(message, context);
    }

    /// <summary>
    /// Get error metrics for monitoring
    /// </summary>
    public Dictionary<string, object> GetErrorMetrics()
    {
        return _errorHandler.GetErrorMetrics();
    }

    /// <summary>
    /// Reset error counts (useful for periodic cleanup)
    /// </summary>
    public void ResetErrorCounts()
    {
        _errorHandler.ResetErrorCounts();
    }
}
