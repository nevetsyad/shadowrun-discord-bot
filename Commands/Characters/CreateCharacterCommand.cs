using MediatR;
using ShadowrunDiscordBot.Domain.Entities;

namespace ShadowrunDiscordBot.Commands.Characters;

/// <summary>
/// SR3 COMPLIANT: Command to create a new Shadowrun character
/// REQUIRES: Either PriorityAllocation OR ArchetypeId (not both)
/// REMOVES: Custom builds - all characters must use priority system or archetype templates
/// </summary>
public class CreateCharacterCommand : IRequest<CreateCharacterResponse>
{
    public ulong DiscordUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Metatype { get; set; } = "Human";

    // SR3 COMPLIANCE: Optional archetype ID for archetype-based creation
    // If provided, uses FIXED archetype attributes (no customization)
    public string? ArchetypeId { get; set; }

    // SR3 COMPLIANCE: Required priority allocation for priority-based creation
    // Must have all 5 priorities (A-E) assigned to categories
    public PriorityAllocation? PriorityAllocation { get; set; }

    // SR3 COMPLIANCE: Attributes are derived from priority allocation or archetype
    // These are OUTPUT values, not inputs (set by handler based on priority/archetype)
    public int Body { get; set; } = 1;
    public int Quickness { get; set; } = 1;
    public int Strength { get; set; } = 1;
    public int Charisma { get; set; } = 1;
    public int Intelligence { get; set; } = 1;
    public int Willpower { get; set; } = 1;

    // SR3 COMPLIANCE: Resources are derived from priority allocation or archetype
    // These are OUTPUT values, not inputs
    public int Karma { get; set; } = 0;
    public long Nuyen { get; set; } = 0;

    // SR3 COMPLIANCE: Skills are derived from priority allocation or archetype
    public List<CharacterSkillRequest>? Skills { get; set; }
}

/// <summary>
/// Request for character skill allocation
/// </summary>
public class CharacterSkillRequest
{
    public string SkillName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Specialization { get; set; }
    public bool IsKnowledgeSkill { get; set; }
}

/// <summary>
/// Response from character creation
/// </summary>
public class CreateCharacterResponse
{
    public bool Success { get; set; }
    public int CharacterId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
    
    // SR3 COMPLIANCE: Include allocation details in response
    public string? BuildType { get; set; } // "Priority" or "Archetype"
    public int? AttributePointsUsed { get; set; }
    public int? SkillPointsUsed { get; set; }
}
