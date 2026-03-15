using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;

namespace ShadowrunDiscordBot.Domain.Interfaces;

/// <summary>
/// Repository interface for combat participant operations
/// </summary>
public interface ICombatParticipantRepository : IRepository<CombatParticipant>
{
    /// <summary>
    /// Get all participants for a combat session
    /// </summary>
    Task<IEnumerable<CombatParticipant>> GetBySessionIdAsync(int sessionId);

    /// <summary>
    /// Get participant with character details
    /// </summary>
    Task<CombatParticipant?> GetWithCharacterAsync(int participantId);
}
