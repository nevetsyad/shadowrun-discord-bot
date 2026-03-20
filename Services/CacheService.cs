using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Redis-based distributed cache service implementation
/// Falls back to in-memory caching when Redis is unavailable
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var data = await _cache.GetStringAsync(key).ConfigureAwait(false);
            if (string.IsNullOrEmpty(data))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(data, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cache value for key: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var data = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
            };

            await _cache.SetStringAsync(key, data, options).ConfigureAwait(false);
            _logger.LogDebug("Set cache key: {Key}, expires in: {Expiration}", key, expiration ?? TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache value for key: {Key}", key);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key).ConfigureAwait(false);
            _logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache key: {Key}", key);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var data = await _cache.GetStringAsync(key).ConfigureAwait(false);
            return !string.IsNullOrEmpty(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check cache existence for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task RefreshAsync(string key, TimeSpan? expiration = null)
    {
        try
        {
            var data = await _cache.GetStringAsync(key).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(data))
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
                };
                await _cache.SetStringAsync(key, data, options).ConfigureAwait(false);
                _logger.LogDebug("Refreshed cache key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh cache key: {Key}", key);
        }
    }
}

/// <summary>
/// Cache key constants for consistent key naming
/// </summary>
public static class CacheKeys
{
    private const string _prefix = "shadowrun:";
    
    public static string Character(int characterId) => $"{_prefix}character:{characterId}";
    public static string CharacterByUser(ulong userId, string name) => $"{_prefix}character:user:{userId}:name:{name.ToLowerInvariant()}";
    public static string UserCharacters(ulong userId) => $"{_prefix}characters:user:{userId}";
    
    public static string CombatSession(int sessionId) => $"{_prefix}combat:session:{sessionId}";
    public static string ActiveCombatSession(ulong channelId) => $"{_prefix}combat:active:channel:{channelId}";
    
    public static string GameSession(int sessionId) => $"{_prefix}game:session:{sessionId}";
    public static string ActiveGameSession(ulong channelId) => $"{_prefix}game:active:channel:{channelId}";
    
    public static string MatrixRun(int runId) => $"{_prefix}matrix:run:{runId}";
    public static string ActiveMatrixRun(int characterId) => $"{_prefix}matrix:active:character:{characterId}";
}
