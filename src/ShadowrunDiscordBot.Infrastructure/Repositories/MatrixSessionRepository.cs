using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;
using ShadowrunDiscordBot.Infrastructure.Data;

namespace ShadowrunDiscordBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for matrix session/run operations
/// Note: Simplified during migration period
/// </summary>
public class MatrixSessionRepository : Repository<MatrixRun>, IMatrixSessionRepository
{
    public MatrixSessionRepository(ShadowrunDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<MatrixRun?> GetActiveByCharacterIdAsync(int characterId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.CharacterId == characterId && r.EndedAt == null)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<MatrixRun?> GetWithEncountersAsync(int runId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.Id == runId)
            .ConfigureAwait(false);
    }
}
