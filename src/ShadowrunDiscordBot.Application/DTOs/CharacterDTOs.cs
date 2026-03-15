namespace ShadowrunDiscordBot.Application.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// GPT-5.4 FIX: Data transfer object for character creation with OPTIONAL archetype
/// Priority System: Supports priority-based character creation
/// SR3 COMPLIANCE: User provides BASE attributes, system applies racial modifiers
/// </summary>
public class CreateCharacterDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("metatype")]
    public string Metatype { get; set; } = "Human";

    // GPT-5.4 FIX: Made nullable for optional archetype system
    [JsonPropertyName("archetype")]
    public string? Archetype { get; set; }

    // GPT-5.4 FIX: Archetype ID is OPTIONAL - null means custom build
    [JsonPropertyName("archetypeId")]
    public string? ArchetypeId { get; set; }

    // Priority System: Priority level for priority-based character creation
    [JsonPropertyName("priorityLevel")]
    public string? PriorityLevel { get; set; }

    // SR3 COMPLIANCE: BASE attributes (user input before racial modifiers)
    [JsonPropertyName("baseBody")]
    public int BaseBody { get; set; } = 3;

    [JsonPropertyName("baseQuickness")]
    public int BaseQuickness { get; set; } = 3;

    [JsonPropertyName("baseStrength")]
    public int BaseStrength { get; set; } = 3;

    [JsonPropertyName("baseCharisma")]
    public int BaseCharisma { get; set; } = 3;

    [JsonPropertyName("baseIntelligence")]
    public int BaseIntelligence { get; set; } = 3;

    [JsonPropertyName("baseWillpower")]
    public int BaseWillpower { get; set; } = 3;
    
    // Legacy properties for backward compatibility (mapped to base attributes)
    [JsonPropertyName("body")]
    [Obsolete("Use BaseBody instead. This is for backward compatibility.")]
    public int Body 
    { 
        get => BaseBody; 
        set => BaseBody = value; 
    }

    [JsonPropertyName("quickness")]
    [Obsolete("Use BaseQuickness instead. This is for backward compatibility.")]
    public int Quickness 
    { 
        get => BaseQuickness; 
        set => BaseQuickness = value; 
    }

    [JsonPropertyName("strength")]
    [Obsolete("Use BaseStrength instead. This is for backward compatibility.")]
    public int Strength 
    { 
        get => BaseStrength; 
        set => BaseStrength = value; 
    }

    [JsonPropertyName("charisma")]
    [Obsolete("Use BaseCharisma instead. This is for backward compatibility.")]
    public int Charisma 
    { 
        get => BaseCharisma; 
        set => BaseCharisma = value; 
    }

    [JsonPropertyName("intelligence")]
    [Obsolete("Use BaseIntelligence instead. This is for backward compatibility.")]
    public int Intelligence 
    { 
        get => BaseIntelligence; 
        set => BaseIntelligence = value; 
    }

    [JsonPropertyName("willpower")]
    [Obsolete("Use BaseWillpower instead. This is for backward compatibility.")]
    public int Willpower 
    { 
        get => BaseWillpower; 
        set => BaseWillpower = value; 
    }
}

/// <summary>
/// Data transfer object for character display
/// SR3 COMPLIANCE: Shows both base and final attributes with racial modifier breakdown
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

    // GPT-5.4 FIX: Made nullable for optional archetype system
    [JsonPropertyName("archetype")]
    public string? Archetype { get; set; }

    // GPT-5.4 FIX: Added to distinguish archetype-based vs custom builds
    [JsonPropertyName("isCustomBuild")]
    public bool IsCustomBuild { get; set; }

    // Priority System: Priority level for priority-based characters
    [JsonPropertyName("priorityLevel")]
    public string? PriorityLevel { get; set; }

    // SR3 COMPLIANCE: BASE attributes (user input)
    [JsonPropertyName("baseBody")]
    public int BaseBody { get; set; }

    [JsonPropertyName("baseQuickness")]
    public int BaseQuickness { get; set; }

    [JsonPropertyName("baseStrength")]
    public int BaseStrength { get; set; }

    [JsonPropertyName("baseCharisma")]
    public int BaseCharisma { get; set; }

    [JsonPropertyName("baseIntelligence")]
    public int BaseIntelligence { get; set; }

    [JsonPropertyName("baseWillpower")]
    public int BaseWillpower { get; set; }

    // SR3 COMPLIANCE: FINAL attributes (base + racial modifiers)
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

    // SR3 COMPLIANCE: Applied racial modifiers for display
    [JsonPropertyName("appliedRacialModifiers")]
    public Dictionary<string, int>? AppliedRacialModifiers { get; set; }

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
