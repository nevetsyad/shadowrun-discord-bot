using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;

namespace ShadowrunDiscordBot.Infrastructure.Repositories;

/// <summary>
/// Implementation of character repository using Entity Framework Core
/// FIX: Added XML documentation to all public methods for better code documentation
/// </summary>
public class CharacterRepository : ICharacterRepository
{
    private readonly Data.ShadowrunDbContext _context;

    /// <summary>
    /// Initializes a new instance of the CharacterRepository
    /// </summary>
    /// <param name="context">Database context for character operations</param>
    public CharacterRepository(Data.ShadowrunDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a character by ID with all related entities (skills, cyberware, spells, spirits, gear)
    /// </summary>
    /// <param name="id">The unique character identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The character if found, null otherwise</returns>
    public async Task<Character?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Characters
            .Include(c => c.Skills)
            .Include(c => c.Cyberware)
            .Include(c => c.Spells)
            .Include(c => c.Spirits)
            .Include(c => c.Gear)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retrieves all characters with their skills loaded
    /// FIX: Note - Consider adding pagination for large datasets
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of all characters</returns>
    public async Task<IEnumerable<Character>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Characters
            .Include(c => c.Skills)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a new character to the database
    /// </summary>
    /// <param name="entity">The character entity to add</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The added character with generated ID</returns>
    public async Task<Character> AddAsync(Character entity, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Characters.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    /// <summary>
    /// Updates an existing character in the database
    /// FIX: Removed dead code (await Task.CompletedTask)
    /// </summary>
    /// <param name="entity">The character entity to update</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Completed task</returns>
    public Task UpdateAsync(Character entity, CancellationToken cancellationToken = default)
    {
        _context.Characters.Update(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes a character from the database
    /// FIX: Removed dead code (await Task.CompletedTask)
    /// </summary>
    /// <param name="entity">The character entity to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Completed task</returns>
    public Task DeleteAsync(Character entity, CancellationToken cancellationToken = default)
    {
        _context.Characters.Remove(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of state entries written to the database</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<Character?> GetByNameAsync(ulong discordUserId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Characters
            .Include(c => c.Skills)
            .Include(c => c.Cyberware)
            .Include(c => c.Spells)
            .Include(c => c.Spirits)
            .Include(c => c.Gear)
            .FirstOrDefaultAsync(c => c.DiscordUserId == discordUserId && c.Name == name, cancellationToken);
    }
    
    public async Task<IEnumerable<Character>> GetByDiscordUserIdAsync(ulong discordUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Characters
            .Where(c => c.DiscordUserId == discordUserId)
            .Include(c => c.Skills)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<bool> ExistsAsync(ulong discordUserId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Characters
            .AnyAsync(c => c.DiscordUserId == discordUserId && c.Name == name, cancellationToken);
    }

    /// <summary>
    /// Gets all characters created before the archetype system was implemented (for backward compatibility)
    /// </summary>
    public async Task<IEnumerable<Character>> GetLegacyCharactersAsync(ulong discordUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Characters
            .Where(c => c.DiscordUserId == discordUserId && string.IsNullOrEmpty(c.Archetype))
            .Include(c => c.Skills)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets characters by archetype
    /// </summary>
    public async Task<IEnumerable<Character>> GetByArchetypeAsync(string archetypeId, CancellationToken cancellationToken = default)
    {
        return await _context.Characters
            .Where(c => c.Archetype == archetypeId)
            .Include(c => c.Skills)
            .ToListAsync(cancellationToken);
    }
}
