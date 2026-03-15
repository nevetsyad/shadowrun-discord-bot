using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;

namespace ShadowrunDiscordBot.Domain.Interfaces;

/// <summary>
/// Repository interface for combat session operations
/// </summary>
public interface ICombatSessionRepository : IRepository<CombatSession>
{
    /// <summary>
    /// Get active combat session for a channel
    /// </summary>
    Task<CombatSession?> GetActiveByChannelIdAsync(ulong channelId);

    /// <summary>
    /// Get combat session with participants and their characters
    /// </summary>
    Task<CombatSession?> GetWithParticipantsAsync(int sessionId);

    /// <summary>
    /// Get recent combat sessions
    /// </summary>
    Task<IEnumerable<CombatSession>> GetRecentAsync(int limit = 10);

    /// <summary>
    /// Check if there's an active combat session in a channel
    /// </summary>
    Task<bool> HasActiveSessionAsync(ulong channelId);
}
