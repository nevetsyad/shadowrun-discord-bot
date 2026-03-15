using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Services;
using ShadowrunDiscordBot.Infrastructure.Data;

namespace ShadowrunDiscordBot.Repositories;

/// <summary>
/// Repository implementation for character-specific operations
/// </summary>
public class CharacterRepository : Repository<ShadowrunCharacter>, ICharacterRepository
{
    public CharacterRepository(ShadowrunDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<ShadowrunCharacter?> GetByUserIdAndNameAsync(ulong userId, string name)
    {
        // FIX: Removed eager loading to avoid N+1 queries.
        // Call GetByUserIdAndNameWithDetailsAsync() if you need full data.
        return await _dbSet
            .FirstOrDefaultAsync(c => c.DiscordUserId == userId && c.Name == name)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ShadowrunCharacter>> GetByUserIdAsync(ulong userId)
    {
        // FIX: Removed eager loading to avoid N+1 queries
        return await _dbSet
            .Where(c => c.DiscordUserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ShadowrunCharacter?> GetWithDetailsAsync(int characterId)
    {
        // FIX: Removed eager loading to avoid N+1 queries
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Id == characterId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a character by ID with all related data (Skills, Cyberware, Spells, etc.)
    /// WARNING: This triggers multiple database queries (N+1 problem).
    /// For performance, use GetByUserIdAndNameAsync() instead.
    /// </summary>
    public async Task<ShadowrunCharacter> GetByUserIdAndNameWithDetailsAsync(ulong userId, string name, CancellationToken cancellationToken = default)
    {
        var character = await _dbSet
            .FirstOrDefaultAsync(c => c.DiscordUserId == userId && c.Name == name, cancellationToken)
            .ConfigureAwait(false);

        if (character == null)
        {
            throw new KeyNotFoundException($"Character '{name}' not found for user {userId}");
        }

        // FIX: Use Query() for explicit loading to avoid unnecessary queries
        await _dbSet.Entry(character)
            .Collection(c => c.Skills)
            .Query()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await _dbSet.Entry(character)
            .Collection(c => c.Cyberware)
            .Query()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await _dbSet.Entry(character)
            .Collection(c => c.Spells)
            .Query()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await _dbSet.Entry(character)
            .Collection(c => c.Spirits)
            .Query()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await _dbSet.Entry(character)
            .Collection(c => c.Gear)
            .Query()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return character;
    }

    /// <inheritdoc/>
    public async Task<bool> NameExistsForUserAsync(ulong userId, string name)
    {
        return await _dbSet
            .AnyAsync(c => c.DiscordUserId == userId && c.Name == name)
            .ConfigureAwait(false);
    }
}
