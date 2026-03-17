using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;
using ShadowrunDiscordBot.Infrastructure.Data;

namespace ShadowrunDiscordBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for combat session operations
/// </summary>
public class CombatSessionRepository : Repository<CombatSession>, ICombatSessionRepository
{
    public CombatSessionRepository(ShadowrunDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<CombatSession?> GetActiveByChannelIdAsync(ulong channelId)
    {
        return await _dbSet
            .Include(s => s.Participants)
                .ThenInclude(p => p.CharacterData)
            .FirstOrDefaultAsync(s => s.DiscordChannelId == channelId && s.IsActive)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<CombatSession?> GetWithParticipantsAsync(int sessionId)
    {
        return await _dbSet
            .Include(s => s.Participants)
                .ThenInclude(p => p.CharacterData)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CombatSession>> GetRecentAsync(int limit = 10)
    {
        return await _dbSet
            .Include(s => s.Participants)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> HasActiveSessionAsync(ulong channelId)
    {
        return await _dbSet
            .AnyAsync(s => s.DiscordChannelId == channelId && s.IsActive)
            .ConfigureAwait(false);
    }
}
