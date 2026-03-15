using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;

namespace ShadowrunDiscordBot.Domain.Interfaces;

/// <summary>
/// Repository implementation for combat participant operations
/// </summary>
public class CombatParticipantRepository : Repository<CombatParticipant>, ICombatParticipantRepository
{
    public CombatParticipantRepository(ShadowrunDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CombatParticipant>> GetBySessionIdAsync(int sessionId)
    {
        return await _dbSet
            .Include(p => p.Character)
            .Where(p => p.CombatSessionId == sessionId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<CombatParticipant?> GetWithCharacterAsync(int participantId)
    {
        return await _dbSet
            .Include(p => p.Character)
            .FirstOrDefaultAsync(p => p.Id == participantId)
            .ConfigureAwait(false);
    }
}
