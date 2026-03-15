using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Services;
using ShadowrunDiscordBot.Infrastructure.Data;

namespace ShadowrunDiscordBot.Repositories;

/// <summary>
/// Repository implementation for matrix session/run operations
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
            .Include(r => r.ICEncounters)
            .FirstOrDefaultAsync(r => r.CharacterId == characterId && r.EndedAt == null)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<MatrixRun?> GetWithEncountersAsync(int runId)
    {
        return await _dbSet
            .Include(r => r.ICEncounters)
            .FirstOrDefaultAsync(r => r.Id == runId)
            .ConfigureAwait(false);
    }
}
