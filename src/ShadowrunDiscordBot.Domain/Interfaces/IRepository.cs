using ShadowrunDiscordBot.Domain.Entities;

namespace ShadowrunDiscordBot.Domain.Interfaces;

/// <summary>
/// Generic repository interface for data access operations
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Get all entities
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Get entity by ID
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Add a new entity
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Update an existing entity
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Delete an entity
    /// </summary>
    Task DeleteAsync(T entity);

    /// <summary>
    /// Check if entity exists by ID
    /// </summary>
    Task<bool> ExistsAsync(int id);
}
