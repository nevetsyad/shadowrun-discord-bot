using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;

namespace ShadowrunDiscordBot.Domain.Interfaces;

/// <summary>
/// Repository interface for matrix session/run operations
/// </summary>
public interface IMatrixSessionRepository : IRepository<MatrixRun>
{
    /// <summary>
    /// Get active matrix run for a character
    /// </summary>
    Task<MatrixRun?> GetActiveByCharacterIdAsync(int characterId);

    /// <summary>
    /// Get matrix run with IC encounters
    /// </summary>
    Task<MatrixRun?> GetWithEncountersAsync(int runId);
}
