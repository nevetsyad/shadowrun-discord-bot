using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Services;
using ShadowrunDiscordBot.Infrastructure.Data;

namespace ShadowrunDiscordBot.Repositories;

/// <summary>
/// Repository implementation for game session operations
/// </summary>
public class GameSessionRepository : Repository<GameSession>, IGameSessionRepository
{
    public GameSessionRepository(ShadowrunDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<GameSession?> GetActiveByChannelIdAsync(ulong channelId)
    {
        return await _dbSet
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.DiscordChannelId == channelId && s.Status == SessionStatus.Active)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<GameSession?> GetWithDetailsAsync(int sessionId)
    {
        return await _dbSet
            .Include(s => s.Participants)
            .Include(s => s.NarrativeEvents)
            .Include(s => s.PlayerChoices)
            .Include(s => s.NPCRelationships)
            .Include(s => s.ActiveMissions)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<GameSession>> GetByGuildIdAsync(ulong guildId)
    {
        return await _dbSet
            .Where(s => s.DiscordGuildId == guildId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
