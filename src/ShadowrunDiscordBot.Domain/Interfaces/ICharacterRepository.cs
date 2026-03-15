using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;

namespace ShadowrunDiscordBot.Domain.Interfaces;

/// <summary>
/// Repository interface for character-specific operations
/// </summary>
public interface ICharacterRepository : IRepository<Character>
{
    /// <summary>
    /// Get character by Discord user ID and character name
    /// </summary>
    Task<Character?> GetByUserIdAndNameAsync(ulong userId, string name);

    /// <summary>
    /// Get all characters for a Discord user
    /// </summary>
    Task<IEnumerable<Character>> GetByUserIdAsync(ulong userId);

    /// <summary>
    /// Get character with all related data (skills, cyberware, spells, etc.)
    /// </summary>
    Task<Character?> GetWithDetailsAsync(int characterId);

    /// <summary>
    /// Check if a character name exists for a user
    /// </summary>
    Task<bool> NameExistsForUserAsync(ulong userId, string name);
}
