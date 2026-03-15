namespace ShadowrunDiscordBot.Domain.Interfaces;

using ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// Generic repository interface for aggregate roots
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Character repository interface with specialized queries
/// </summary>
public interface ICharacterRepository : IRepository<Character>
{
    Task<Character?> GetByNameAsync(ulong discordUserId, string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Character>> GetByDiscordUserIdAsync(ulong discordUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(ulong discordUserId, string name, CancellationToken cancellationToken = default);

    // GPT-5.4 FIX: Archetype-related methods for backward compatibility
    /// <summary>
    /// Gets all characters created before the archetype system was implemented (for backward compatibility)
    /// </summary>
    Task<IEnumerable<Character>> GetLegacyCharactersAsync(ulong discordUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets characters by archetype
    /// </summary>
    Task<IEnumerable<Character>> GetByArchetypeAsync(string archetypeId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Combat session repository interface
/// </summary>
public interface ICombatSessionRepository : IRepository<CombatSession>
{
    Task<CombatSession?> GetActiveByChannelIdAsync(ulong channelId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CombatSession>> GetRecentSessionsAsync(int count = 10, CancellationToken cancellationToken = default);
}

/// <summary>
/// Combat session entity (placeholder for now)
/// </summary>
public class CombatSession
{
    public int Id { get; set; }
    public ulong DiscordChannelId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
