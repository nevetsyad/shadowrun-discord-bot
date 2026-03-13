namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Service interface for distributed caching operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a cached value by key
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Set a value in cache with optional expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Remove a value from cache
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Check if a key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Refresh the expiration time for a key
    /// </summary>
    Task RefreshAsync(string key, TimeSpan? expiration = null);
}
