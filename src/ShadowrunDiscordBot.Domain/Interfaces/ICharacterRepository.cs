using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Repositories;

/// <summary>
/// Repository interface for character-specific operations
/// </summary>
public interface ICharacterRepository : IRepository<ShadowrunCharacter>
{
    /// <summary>
    /// Get character by Discord user ID and character name
    /// </summary>
    Task<ShadowrunCharacter?> GetByUserIdAndNameAsync(ulong userId, string name);

    /// <summary>
    /// Get all characters for a Discord user
    /// </summary>
    Task<IEnumerable<ShadowrunCharacter>> GetByUserIdAsync(ulong userId);

    /// <summary>
    /// Get character with all related data (skills, cyberware, spells, etc.)
    /// </summary>
    Task<ShadowrunCharacter?> GetWithDetailsAsync(int characterId);

    /// <summary>
    /// Check if a character name exists for a user
    /// </summary>
    Task<bool> NameExistsForUserAsync(ulong userId, string name);
}
