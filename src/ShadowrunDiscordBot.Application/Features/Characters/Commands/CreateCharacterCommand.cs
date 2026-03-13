namespace ShadowrunDiscordBot.Application.Features.Characters.Commands;

using MediatR;
using DTOs;

/// <summary>
/// Command to create a new character
/// </summary>
public class CreateCharacterCommand : IRequest<CharacterDto>
{
    public ulong DiscordUserId { get; set; }
    public CreateCharacterDto Character { get; set; } = new();
}

/// <summary>
/// Command to update character attributes
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
