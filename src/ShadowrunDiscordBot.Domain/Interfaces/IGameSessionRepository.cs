using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;

namespace ShadowrunDiscordBot.Domain.Interfaces;

/// <summary>
/// Repository interface for game session operations
/// </summary>
public interface IGameSessionRepository : IRepository<GameSession>
{
    /// <summary>
    /// Get active game session for a channel
    /// </summary>
    Task<GameSession?> GetActiveByChannelIdAsync(ulong channelId);

    /// <summary>
    /// Get game session with all related data
    /// </summary>
    Task<GameSession?> GetWithDetailsAsync(int sessionId);

    /// <summary>
    /// Get sessions by guild ID
    /// </summary>
    Task<IEnumerable<GameSession>> GetByGuildIdAsync(ulong guildId);
}
