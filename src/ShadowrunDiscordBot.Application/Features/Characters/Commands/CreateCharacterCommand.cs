namespace ShadowrunDiscordBot.Application.Features.Characters.Commands;

using MediatR;
using DTOs;

/// <summary>
/// GPT-5.4 FIX: Command to create a new character with required archetype
/// Priority System: Supports both archetype-based and priority-based character creation
/// </summary>
public class CreateCharacterCommand : IRequest<CharacterDto>
{
    public ulong DiscordUserId { get; set; }
    public CreateCharacterDto Character { get; set; } = new();
}

/// <summary>
/// Priority-based character creation command
/// </summary>
public class CreatePriorityCharacterCommand : IRequest<CharacterDto>
{
    public ulong DiscordUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Metatype { get; set; } = "Human";
    public string PriorityLevel { get; set; } = "C"; // Default to C
    public int Body { get; set; } = 1;
    public int Quickness { get; set; } = 1;
    public int Strength { get; set; } = 1;
    public int Charisma { get; set; } = 1;
    public int Intelligence { get; set; } = 1;
    public int Willpower { get; set; } = 1;
}

/// <summary>
/// GPT-5.4 FIX: Extended command for archetype-based character creation
/// </summary>
public class CreateArchetypeCharacterCommand : IRequest<CharacterDto>
{
    public ulong DiscordUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Metatype { get; set; } = "Human";
    public string ArchetypeId { get; set; } = string.Empty; // GPT-5.4 FIX: Required archetype ID
    public int Body { get; set; } = 3;
    public int Quickness { get; set; } = 3;
    public int Strength { get; set; } = 3;
    public int Charisma { get; set; } = 3;
    public int Intelligence { get; set; } = 3;
    public int Willpower { get; set; } = 3;
}

/// <summary>
/// Command to update character attributes
/// Priority System: Supports priority-level-based attribute updates
/// </summary>
public class UpdateCharacterAttributesCommand : IRequest<CharacterDto>
{
    public int CharacterId { get; set; }
    public int? Body { get; set; }
    public int? Quickness { get; set; }
    public int? Strength { get; set; }
    public int? Charisma { get; set; }
    public int? Intelligence { get; set; }
    public int? Willpower { get; set; }
    public string? PriorityLevel { get; set; } // For validation purposes
}

/// <summary>
/// Command to add karma to a character
/// </summary>
public class AddKarmaCommand : IRequest<CharacterDto>
{
    public int CharacterId { get; set; }
    public int KarmaAmount { get; set; }
}

/// <summary>
/// Command to spend karma
/// </summary>
public class SpendKarmaCommand : IRequest<CharacterDto>
{
    public int CharacterId { get; set; }
    public int KarmaAmount { get; set; }
}

/// <summary>
/// Command to add nuyen to a character
/// </summary>
public class AddNuyenCommand : IRequest<CharacterDto>
{
    public int CharacterId { get; set; }
    public long NuyenAmount { get; set; }
}

/// <summary>
/// Command to spend nuyen
/// </summary>
public class SpendNuyenCommand : IRequest<CharacterDto>
{
    public int CharacterId { get; set; }
    public long NuyenAmount { get; set; }
}

/// <summary>
/// Command to take damage
/// </summary>
public class TakeDamageCommand : IRequest<CharacterDto>
{
    public int CharacterId { get; set; }
    public int DamageAmount { get; set; }
    public bool IsStun { get; set; }
}

/// <summary>
/// Command to heal damage
/// </summary>
public class HealDamageCommand : IRequest<CharacterDto>
{
    public int CharacterId { get; set; }
    public int HealingAmount { get; set; }
    public bool IsStun { get; set; }
}

/// <summary>
/// Command to install cyberware
/// </summary>
public class InstallCyberwareCommand : IRequest<CharacterDto>
{
    public int CharacterId { get; set; }
    public string CyberwareName { get; set; } = string.Empty;
    public string Category { get; set; } = "Cyberware";
    public decimal EssenceCost { get; set; }
    public long NuyenCost { get; set; }
    public int Rating { get; set; }
}

/// <summary>
/// Command to learn a spell
/// </summary>
public class LearnSpellCommand : IRequest<CharacterDto>
{
    public int CharacterId { get; set; }
    public string SpellName { get; set; } = string.Empty;
    public string Category { get; set; } = "Combat";
    public int DrainModifier { get; set; }
}
