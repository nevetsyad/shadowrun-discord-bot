using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShadowrunDiscordBot.Models;

/// <summary>
/// Magic system tracking for awakened characters
/// </summary>
public class MagicSystem
{
    [JsonPropertyName("magic")]
    public int Magic { get; set; } = 0;
    
    [JsonPropertyName("magician")]
    public bool Magician { get; set; } = false;
    
    [JsonPropertyName("awakened")]
    public bool Awakened { get; set; } = false;
    
    [JsonPropertyName("sorcerer")]
    public bool Sorcerer { get; set; } = false;
    
    [JsonPropertyName("adept")]
    public bool Adept { get; set; } = false;
    
    [JsonPropertyName("criticality")]
    public int Criticality { get; set; } = 0;
    
    [JsonPropertyName("foci")]
    public List<Focus> Foci { get; set; } = new List<Focus>();
    
    [JsonPropertyName("instinct")]
    public int Instinct { get; set; } = 0;
    
    [JsonPropertyName("initiative")]
    public int Initiative { get; set; } = 0;
    
    [JsonPropertyName("wounds")]
    public int Wounds { get; set; } = 0;
    
    [JsonPropertyName("woundMod")]
    public int WoundMod { get; set; } = 0;
    
    [JsonPropertyName("recovery")]
    public int Recovery { get; set; } = 0;
    
    [JsonPropertyName("magicalResistance")]
    public int MagicalResistance { get; set; } = 0;
    
    [JsonPropertyName("initiativePool")]
    public int InitiativePool { get; set; } = 0;
    
    [JsonPropertyName("complexFormPool")]
    public int ComplexFormPool { get; set; } = 0;
}

/// <summary>
/// Magical focus item
/// </summary>
public class Focus
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;
    
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 1;
    
    [JsonPropertyName("essenceCost")]
    public double EssenceCost { get; set; } = 0;
    
    [JsonPropertyName("skillBonus")]
    public int SkillBonus { get; set; } = 0;
}

/// <summary>
/// Spell definition
/// </summary>
public class Spell
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("defenseTarget")]
    public int DefenseTarget { get; set; } = 0;
    
    [JsonPropertyName("defenseType")]
    public string DefenseType { get; set; } = string.Empty;
    
    [JsonPropertyName("damage")]
    public int Damage { get; set; } = 0;
    
    [JsonPropertyName("damageType")]
    public string DamageType { get; set; } = string.Empty;
    
    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;
    
    [JsonPropertyName("complexForm")]
    public bool ComplexForm { get; set; } = false;
    
    [JsonPropertyName("service")]
    public int Service { get; set; } = 0;
    
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 1;
    
    [JsonPropertyName("force")]
    public int Force { get; set; } = 0;
}

/// <summary>
/// Spirit entity
/// </summary>
public class Spirit
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("service")]
    public int Service { get; set; } = 0;
    
    [JsonPropertyName("force")]
    public int Force { get; set; } = 0;
    
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 0;
}

/// <summary>
/// Astral form tracking
/// </summary>
public class AstralForm
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("attributes")]
    public AstralAttributes Attributes { get; set; } = new AstralAttributes();
}

/// <summary>
/// Astral attributes
/// </summary>
public class AstralAttributes
{
    [JsonPropertyName("quick")]
    public int Quick { get; set; } = 0;
    
    [JsonPropertyName("heavy")]
    public int Heavy { get; set; } = 0;
    
    [JsonPropertyName("mov")]
    public int Mov { get; set; } = 0;
}

/// <summary>
/// Database of available spells
/// </summary>
public static class SpellDatabase
{
    public static readonly List<Spell> Spells = new List<Spell>
    {
        new Spell { Name = "Manafort", Category = "Combat", DefenseTarget = 4, DefenseType = "Physical", Damage = 6, DamageType = "Physical", Duration = "Instant", ComplexForm = false, Service = 1, Limit = 1, Force = 1 },
        new Spell { Name = "Stun Baton", Category = "Combat", DefenseTarget = 4, DefenseType = "Physical", Damage = 4, DamageType = "Stun", Duration = "Instant", ComplexForm = false, Service = 1, Limit = 1, Force = 1 },
        new Spell { Name = "Flame Bell", Category = "Combat", DefenseTarget = 4, DefenseType = "Physical", Damage = 6, DamageType = "Physical", Duration = "Instant", ComplexForm = false, Service = 1, Limit = 1, Force = 1 },
        new Spell { Name = "Chilly Armor", Category = "Combat", DefenseTarget = 4, DefenseType = "Physical", Damage = 4, DamageType = "Cold", Duration = "Continuous", ComplexForm = false, Service = 1, Limit = 1, Force = 1 },
        new Spell { Name = "Pain Resistance", Category = "Protection", DefenseTarget = 4, DefenseType = "Physical", Damage = 4, DamageType = "Stun", Duration = "Continuous", ComplexForm = false, Service = 1, Limit = 1, Force = 1 },
        new Spell { Name = "Coughing", Category = "Manipulation", DefenseTarget = 4, DefenseType = "Physical", Damage = 1, DamageType = "Stun", Duration = "Continuous", ComplexForm = false, Service = 1, Limit = 1, Force = 1 },
        new Spell { Name = "Electric Illusion", Category = "Manipulation", DefenseTarget = 4, DefenseType = "Physical", Damage = 4, DamageType = "Stun", Duration = "Instant", ComplexForm = false, Service = 1, Limit = 1, Force = 1 },
        new Spell { Name = "Cooling", Category = "Manipulation", DefenseTarget = 4, DefenseType = "Physical", Damage = 2, DamageType = "Cold", Duration = "Continuous", ComplexForm = false, Service = 1, Limit = 1, Force = 1 },
        new Spell { Name = "Obscure", Category = "Illusion", DefenseTarget = 4, DefenseType = "Mental", Damage = 0, DamageType = "None", Duration = "Instant", ComplexForm = false, Service = 1, Limit = 1, Force = 1 },
        new Spell { Name = "Shock", Category = "Illusion", DefenseTarget = 4, DefenseType = "Mental", Damage = 2, DamageType = "Stun", Duration = "Instant", ComplexForm = false, Service = 1, Limit = 1, Force = 1 }
    };
}

/// <summary>
/// Astral projection and perception tracking
/// </summary>
public class AstralState
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    public bool IsProjecting { get; set; } = false;

    public bool HasPerception { get; set; } = false;

    public DateTime? ProjectionStartedAt { get; set; }

    public int HoursProjected { get; set; } = 0;

    // Astral combat stats
    public int AstralCombatPool { get; set; } = 0;

    public int AstralDamage { get; set; } = 0;

    [MaxLength(200)]
    public string? CurrentLocation { get; set; }

    [Required]
    public virtual ShadowrunCharacter Character { get; set; } = null!;
}

/// <summary>
/// Astral signature tracking
/// </summary>
public class AstralSignature
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string SignatureType { get; set; } = "Spell"; // Spell, Spirit, Emotional

    public int Force { get; set; } = 0;

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public int? CharacterId { get; set; }

    public virtual ShadowrunCharacter? Character { get; set; }
}
