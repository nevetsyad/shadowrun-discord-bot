using ShadowrunDiscordBot.Domain.Entities;

namespace ShadowrunDiscordBot.Application.Services;

/// <summary>
/// GPT-5.4 FIX: Interface for archetype service
/// </summary>
public interface IArchetypeService
{
    /// <summary>
    /// Get archetype by ID
    /// </summary>
    Task<ArchetypeTemplate?> GetArchetypeByIdAsync(string archetypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if metatype is compatible with archetype
    /// </summary>
    Task<bool> IsMetatypeCompatibleAsync(string archetypeId, string metatype, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate attributes against archetype constraints
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateAttributesAsync(
        string archetypeId,
        int body,
        int quickness,
        int strength,
        int charisma,
        int intelligence,
        int willpower,
        CancellationToken cancellationToken = default);
}
