namespace ShadowrunDiscordBot.Application.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// Data transfer object for character creation
/// </summary>
public class CreateCharacterDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("metatype")]
    public string Metatype { get; set; } = "Human";
    
    [JsonPropertyName("archetype")]
    public string Archetype { get; set; } = "Street Samurai";
    
    [JsonPropertyName("body")]
    public int Body { get; set; } = 3;
    
    [JsonPropertyName("quickness")]
    public int Quickness { get; set; } = 3;
    
    [JsonPropertyName("strength")]
    public int Strength { get; set; } = 3;
    
    [JsonPropertyName("charisma")]
    public int Charisma { get; set; } = 3;
    
    [JsonPropertyName("intelligence")]
    public int Intelligence { get; set; } = 3;
    
    [JsonPropertyName("willpower")]
    public int Willpower { get; set; } = 3;
}

/// <summary>
/// Data transfer object for character display
/// </summary>
public class CharacterDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("discordUserId")]
    public ulong DiscordUserId { get; set; }
    
    [JsonPropertyName("metatype")]
    public string Metatype { get; set; } = string.Empty;
    
    [JsonPropertyName("archetype")]
    public string Archetype { get; set; } = string.Empty;
    
    [JsonPropertyName("body")]
    public int Body { get; set; }
    
    [JsonPropertyName("quickness")]
    public int Quickness { get; set; }
    
    [JsonPropertyName("strength")]
    public int Strength { get; set; }
    
    [JsonPropertyName("charisma")]
    public int Charisma { get; set; }
    
    [JsonPropertyName("intelligence")]
    public int Intelligence { get; set; }
    
    [JsonPropertyName("willpower")]
    public int Willpower { get; set; }
    
    [JsonPropertyName("reaction")]
    public int Reaction { get; set; }
    
    [JsonPropertyName("essence")]
    public decimal Essence { get; set; }
    
    [JsonPropertyName("magic")]
    public int Magic { get; set; }
    
    [JsonPropertyName("karma")]
    public int Karma { get; set; }
    
    [JsonPropertyName("nuyen")]
    public long Nuyen { get; set; }
    
    [JsonPropertyName("physicalDamage")]
    public int PhysicalDamage { get; set; }
    
    [JsonPropertyName("stunDamage")]
    public int StunDamage { get; set; }
    
    [JsonPropertyName("physicalConditionMonitor")]
    public int PhysicalConditionMonitor { get; set; }
    
    [JsonPropertyName("stunConditionMonitor")]
    public int StunConditionMonitor { get; set; }
    
    [JsonPropertyName("woundModifier")]
    public int WoundModifier { get; set; }
    
    [JsonPropertyName("isAwakened")]
    public bool IsAwakened { get; set; }
    
    [JsonPropertyName("isDecker")]
    public bool IsDecker { get; set; }
    
    [JsonPropertyName("isRigger")]
    public bool IsRigger { get; set; }
}

/// <summary>
/// Data transfer object for dice roll results
/// </summary>
public class DiceRollResultDto
{
    [JsonPropertyName("successes")]
    public int Successes { get; set; }
    
    [JsonPropertyName("glitches")]
    public int Glitches { get; set; }
    
    [JsonPropertyName("isCriticalGlitch")]
    public bool IsCriticalGlitch { get; set; }
    
    [JsonPropertyName("diceRolled")]
    public int DiceRolled { get; set; }
    
    [JsonPropertyName("targetNumber")]
    public int TargetNumber { get; set; }
    
    [JsonPropertyName("threshold")]
    public int Threshold { get; set; }
    
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }
    
    [JsonPropertyName("rolls")]
    public List<int> Rolls { get; set; } = new();
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for combat participant
/// </summary>
public class CombatParticipantDto
{
    [JsonPropertyName("characterId")]
    public int CharacterId { get; set; }
    
    [JsonPropertyName("characterName")]
    public string CharacterName { get; set; } = string.Empty;
    
    [JsonPropertyName("initiative")]
    public int Initiative { get; set; }
    
    [JsonPropertyName("initiativeDice")]
    public int InitiativeDice { get; set; }
    
    [JsonPropertyName("hasActed")]
    public bool HasActed { get; set; }
    
    [JsonPropertyName("passes")]
    public int Passes { get; set; }
}

/// <summary>
/// Data transfer object for combat session
/// </summary>
public class CombatSessionDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("channelId")]
    public ulong ChannelId { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("round")]
    public int Round { get; set; }
    
    [JsonPropertyName("pass")]
    public int Pass { get; set; }
    
    [JsonPropertyName("participants")]
    public List<CombatParticipantDto> Participants { get; set; } = new();
}
