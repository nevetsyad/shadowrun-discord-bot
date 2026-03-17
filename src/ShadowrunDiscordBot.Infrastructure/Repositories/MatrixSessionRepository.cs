using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;
using ShadowrunDiscordBot.Infrastructure.Data;
using MatrixRun = ShadowrunDiscordBot.Infrastructure.Data.MatrixRun;

namespace ShadowrunDiscordBot.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for matrix session/run operations
/// </summary>
public class MatrixSessionRepository : Repository<MatrixRun>, IMatrixSessionRepository
{
    public MatrixSessionRepository(ShadowrunDbContext context) : base(context)
    {
    }

    public Task AddAsync(Domain.Entities.MatrixRun entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Domain.Entities.MatrixRun entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(Func<Domain.Entities.MatrixRun, bool> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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

    public Task UpdateAsync(Domain.Entities.MatrixRun entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    Task<Domain.Entities.MatrixRun?> IMatrixSessionRepository.GetActiveByCharacterIdAsync(int characterId)
    {
        throw new NotImplementedException();
    }

    Task<IEnumerable<Domain.Entities.MatrixRun>> IRepository<Domain.Entities.MatrixRun>.GetAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    Task<Domain.Entities.MatrixRun?> IRepository<Domain.Entities.MatrixRun>.GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    Task<Domain.Entities.MatrixRun?> IMatrixSessionRepository.GetWithEncountersAsync(int runId)
    {
        throw new NotImplementedException();
    }
}
