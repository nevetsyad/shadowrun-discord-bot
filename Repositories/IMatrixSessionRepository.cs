using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Repositories;

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
