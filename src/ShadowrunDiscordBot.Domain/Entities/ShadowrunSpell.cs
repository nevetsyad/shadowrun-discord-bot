namespace ShadowrunDiscordBot.Domain.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Enhanced Shadowrun Spell with detailed tracking
/// </summary>
public class ShadowrunSpell
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int CharacterId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Spell category: Combat, Detection, Health, Illusion, Manipulation
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "Combat";
    
    /// <summary>
    /// Spell subcategory (e.g., Direct, Indirect for Combat)
    /// </summary>
    [MaxLength(50)]
    public string? Subcategory { get; set; }
    
    /// <summary>
    /// Target type: Physical, Mana
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string TargetType { get; set; } = "Physical";
    
    /// <summary>
    /// Range: Touch, LOS, LOS(A), etc.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Range { get; set; } = "LOS";
    
    /// <summary>
    /// Damage type for combat spells: Physical, Stun
    /// </summary>
    [MaxLength(20)]
    public string? DamageType { get; set; }
    
    /// <summary>
    /// Duration: Instant, Sustained, Permanent
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Duration { get; set; } = "Instant";
    
    /// <summary>
    /// Drain value base (e.g., (F/2) + 3)
    /// </summary>
    [Required]
    public int DrainBase { get; set; } = 0;
    
    /// <summary>
    /// Drain modifier added to base
    /// </summary>
    [Required]
    public int DrainModifier { get; set; } = 0;
    
    /// <summary>
    /// Full drain formula as string
    /// </summary>
    [MaxLength(50)]
    public string? DrainFormula { get; set; }
    
    /// <summary>
    /// Whether this is an exclusive spell (limited to one tradition)
    /// </summary>
    public bool IsExclusive { get; set; } = false;
    
    /// <summary>
    /// Required tradition for exclusive spells
    /// </summary>
    [MaxLength(50)]
    public string? RequiredTradition { get; set; }
    
    /// <summary>
    /// Spell description and effects
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Any special rules or notes
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Source book and page (e.g., "SR3 p.152")
    /// </summary>
    [MaxLength(50)]
    public string? Source { get; set; }
    
    /// <summary>
    /// Whether spell causes drain damage
    /// </summary>
    public bool CausesDrain { get; set; } = true;
    
    /// <summary>
    /// Whether spell requires LOS to maintain
    /// </summary>
    public bool RequiresLOSMaintenance { get; set; } = false;
    
    /// <summary>
    /// Minimum force level
    /// </summary>
    public int MinForce { get; set; } = 1;
    
    /// <summary>
    /// Whether character has learned this spell at a specific force
    /// </summary>
    public int? LearnedAtForce { get; set; }
    
    // Navigation property
    public virtual Character Character { get; set; } = null!;
    
    /// <summary>
    /// Calculate drain for a given force
    /// </summary>
    public int CalculateDrain(int force)
    {
        // Most spells use (Force / 2) + modifier formula
        var baseDrain = DrainBase > 0 ? DrainBase : force / 2;
        return baseDrain + DrainModifier;
    }
    
    /// <summary>
    /// Get drain resistance target number
    /// </summary>
    public int GetDrainTargetNumber(int force)
    {
        var drain = CalculateDrain(force);
        return Math.Max(2, drain); // Minimum target number is 2
    }
}

/// <summary>
/// Spell categories and their descriptions
/// </summary>
public static class SpellCategories
{
    public const string Combat = "Combat";
    public const string Detection = "Detection";
    public const string Health = "Health";
    public const string Illusion = "Illusion";
    public const string Manipulation = "Manipulation";
    
    public static readonly Dictionary<string, string> Descriptions = new()
    {
        [Combat] = "Spells that cause direct damage or physical effects",
        [Detection] = "Spells that enhance or reveal information",
        [Health] = "Spells that heal, cure, or modify biological functions",
        [Illusion] = "Spells that create false sensory impressions",
        [Manipulation] = "Spells that alter reality or control minds"
    };
}

/// <summary>
/// Spell target types
/// </summary>
public static class SpellTargetTypes
{
    public const string Physical = "Physical";
    public const string Mana = "Mana";
}

/// <summary>
/// Spell ranges
/// </summary>
public static class SpellRanges
{
    public const string Touch = "Touch";
    public const string LOS = "LOS"; // Line of Sight
    public const string LOSArea = "LOS(A)"; // Line of Sight Area
    public const string Self = "Self";
}

/// <summary>
/// Spell durations
/// </summary>
public static class SpellDurations
{
    public const string Instant = "Instant";
    public const string Sustained = "Sustained";
    public const string Permanent = "Permanent";
}
