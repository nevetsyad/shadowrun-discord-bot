using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShadowrunDiscordBot.Models;

/// <summary>
/// Shadowrun 3rd Edition Character with all attributes and systems
/// SR3 COMPLIANCE: Supports base attributes + racial modifiers = final attributes
/// </summary>
public class ShadowrunCharacter
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public ulong DiscordUserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Metatype { get; set; } = "Human";

    [Required]
    [MaxLength(50)]
    public string Archetype { get; set; } = "Street Samurai";

    // SR3 COMPLIANCE: BASE Attributes (user input before racial modifiers)
    public int BaseBody { get; set; } = 3;
    public int BaseQuickness { get; set; } = 3;
    public int BaseStrength { get; set; } = 3;
    public int BaseCharisma { get; set; } = 3;
    public int BaseIntelligence { get; set; } = 3;
    public int BaseWillpower { get; set; } = 3;
    
    // SR3 COMPLIANCE: FINAL Attributes (base + racial modifiers applied)
    public int Body { get; set; } = 3;
    public int Quickness { get; set; } = 3;
    public int Strength { get; set; } = 3;
    public int Charisma { get; set; } = 3;
    public int Intelligence { get; set; } = 3;
    public int Willpower { get; set; } = 3;
    
    // SR3 COMPLIANCE: Racial modifiers that were applied (for display)
    public Dictionary<string, int> AppliedRacialModifiers { get; set; } = new();

    // Derived Attributes
    public int Reaction => (Quickness + Intelligence) / 2;
    public int Essence { get; set; } = 600; // Stored as 100x (6.00 = 600)
    public decimal EssenceDecimal => Essence / 100m;
    public int BioIndex { get; set; } = 0;
    public int Magic { get; set; } = 0;
    public int InitiationGrade { get; set; } = 0;

    // Resources
    public int Karma { get; set; } = 0;
    public long Nuyen { get; set; } = 0;

    // Damage tracks
    public int PhysicalDamage { get; set; } = 0;
    public int StunDamage { get; set; } = 0;

    // Calculated values
    public int PhysicalConditionMonitor => (Body + 8) / 2;
    public int StunConditionMonitor => (Willpower + 8) / 2;

    // Navigation properties
    public virtual ICollection<CharacterSkill> Skills { get; set; } = new List<CharacterSkill>();
    public virtual ICollection<CharacterCyberware> Cyberware { get; set; } = new List<CharacterCyberware>();
    public virtual ICollection<CharacterSpell> Spells { get; set; } = new List<CharacterSpell>();
    public virtual ICollection<CharacterSpirit> Spirits { get; set; } = new List<CharacterSpirit>();
    public virtual ICollection<CharacterGear> Gear { get; set; } = new List<CharacterGear>();

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Priority System (for character creation)
    public string? Priorities { get; set; } // JSON-serialized priority allocation

    /// <summary>
    /// Priority level used for character creation (A, B, C, D, or E)
    /// </summary>
    public string? PriorityLevel { get; set; }

    /// <summary>
    /// Metatype-specific attribute modifiers from priority allocation
    /// </summary>
    public Dictionary<string, int> AttributeModifiers { get; set; } = new();

    /// <summary>
    /// List of skills allocated with priority system
    /// </summary>
    public List<CharacterSkill> PrioritySkills { get; set; } = new()

    // GPT-5.4 FIX: Archetype system tracking for backward compatibility
    /// <summary>
    /// Indicates if this character was created using the custom build system (pre-archetype)
    /// Characters created after the archetype system will have this as false
    /// </summary>
    public bool IsCustomBuild { get; set; } = true; // Default to true for backward compatibility

    /// <summary>
    /// The archetype ID used to create this character (null for legacy characters)
    /// </summary>
    [MaxLength(50)]
    public string? ArchetypeId { get; set; }

    /// <summary>
    /// Calculate total essence loss from cyberware and bioware
    /// </summary>
    public decimal CalculateEssenceLoss()
    {
        var cyberwareLoss = Cyberware?.Sum(c => c.EssenceCost) ?? 0m;
        return cyberwareLoss;
    }

    /// <summary>
    /// Get current essence after all modifications
    /// </summary>
    public decimal GetCurrentEssence()
    {
        return Math.Max(0, 6.0m - CalculateEssenceLoss());
    }

    /// <summary>
    /// Check if character is awakened (has magic attribute)
    /// </summary>
    public bool IsAwakened()
    {
        return Magic > 0 || Archetype.Contains("Mage") || Archetype.Contains("Shaman") || Archetype.Contains("Adept");
    }

    /// <summary>
    /// Check if character is a decker
    /// </summary>
    public bool IsDecker()
    {
        return Archetype.Contains("Decker");
    }

    /// <summary>
    /// Check if character is a rigger
    /// </summary>
    public bool IsRigger()
    {
        return Archetype.Contains("Rigger");
    }

    /// <summary>
    /// Get wounds modifier
    /// </summary>
    public int GetWoundModifier()
    {
        var physicalWounds = PhysicalDamage / 3;
        var stunWounds = StunDamage / 3;
        return -(physicalWounds + stunWounds);
    }
}

/// <summary>
/// Character skills (flexible skill system)
/// </summary>
public class CharacterSkill
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SkillName { get; set; } = string.Empty;

    [Required]
    public int Rating { get; set; } = 0;

    [MaxLength(100)]
    public string? Specialization { get; set; }

    public bool IsKnowledgeSkill { get; set; } = false;

    [Required]
    public virtual ShadowrunCharacter Character { get; set; } = null!;
}

/// <summary>
/// Cyberware and Bioware implants
/// </summary>
public class CharacterCyberware
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "Cyberware"; // Cyberware or Bioware

    [Required]
    public decimal EssenceCost { get; set; } = 0m;

    public long NuyenCost { get; set; } = 0;

    public int Rating { get; set; } = 0;

    [MaxLength(200)]
    public string? Bonuses { get; set; } // JSON-serialized bonus effects

    public bool IsInstalled { get; set; } = true;

    [Required]
    public virtual ShadowrunCharacter Character { get; set; } = null!;
}

/// <summary>
/// Spells known by awakened characters
/// </summary>
public class CharacterSpell
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "Combat"; // Combat, Detection, Health, Illusion, Manipulation

    [Required]
    public int DrainModifier { get; set; } = 0;

    public bool IsExclusive { get; set; } = false;

    [MaxLength(200)]
    public string? Description { get; set; }

    [Required]
    public virtual ShadowrunCharacter Character { get; set; } = null!;
}

/// <summary>
/// Spirits bound by magicians
/// </summary>
public class CharacterSpirit
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    [MaxLength(50)]
    public string SpiritType { get; set; } = string.Empty;

    [Required]
    public int Force { get; set; } = 1;

    [Required]
    [MaxLength(50)]
    public string Tradition { get; set; } = "Hermetic"; // Hermetic or Shamanic

    public int ServicesOwed { get; set; } = 0;

    public bool IsBound { get; set; } = false;

    public DateTime? SummonedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [Required]
    public virtual ShadowrunCharacter Character { get; set; } = null!;
}

/// <summary>
/// Gear and equipment
/// </summary>
public class CharacterGear
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "General";

    public long Value { get; set; } = 0;

    public int Quantity { get; set; } = 1;

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsEquipped { get; set; } = false;

    [Required]
    public virtual ShadowrunCharacter Character { get; set; } = null!;
}
