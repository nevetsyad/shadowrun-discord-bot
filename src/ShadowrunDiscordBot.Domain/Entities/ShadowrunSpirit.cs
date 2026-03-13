namespace ShadowrunDiscordBot.Domain.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Enhanced Shadowrun Spirit with full tracking
/// </summary>
public class ShadowrunSpirit
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int CharacterId { get; set; }
    
    /// <summary>
    /// Spirit type (e.g., Fire Elemental, City Spirit, Beast Spirit)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SpiritType { get; set; } = string.Empty;
    
    /// <summary>
    /// Force level (determines power and duration)
    /// </summary>
    [Required]
    public int Force { get; set; } = 1;
    
    /// <summary>
    /// Magical tradition: Hermetic, Shamanic, etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Tradition { get; set; } = "Hermetic";
    
    /// <summary>
    /// Services owed to the summoner
    /// </summary>
    public int ServicesOwed { get; set; } = 0;
    
    /// <summary>
    /// Whether spirit is bound (formal conjuration ritual)
    /// </summary>
    public bool IsBound { get; set; } = false;
    
    /// <summary>
    /// When spirit was summoned
    /// </summary>
    public DateTime? SummonedAt { get; set; }
    
    /// <summary>
    /// When spirit expires (typically Force in hours)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Spirit's current location or task
    /// </summary>
    [MaxLength(100)]
    public string? CurrentTask { get; set; }
    
    /// <summary>
    /// Spirit's mental state (Friendly, Neutral, Hostile)
    /// </summary>
    [MaxLength(20)]
    public string Disposition { get; set; } = "Neutral";
    
    // Spirit Attributes (derived from Force)
    public int Body => Force;
    public int Quickness => Force;
    public int Strength => Force;
    public int Charisma => Force;
    public int Intelligence => Force;
    public int Willpower => Force;
    public int Reaction => Force;
    public int Essence => Force * 2;
    
    /// <summary>
    /// Spirit's armor (typically Force * 2 for materialized spirits)
    /// </summary>
    public int Armor => Force * 2;
    
    /// <summary>
    /// Initiative (typically Force + 2D6)
    /// </summary>
    public string Initiative => $"{Force * 2} + 2D6";
    
    /// <summary>
    /// Whether spirit is currently materialized on physical plane
    /// </summary>
    public bool IsMaterialized { get; set; } = false;
    
    /// <summary>
    /// When spirit materialized
    /// </summary>
    public DateTime? MaterializedAt { get; set; }
    
    /// <summary>
    /// Damage taken by spirit
    /// </summary>
    public int Damage { get; set; } = 0;
    
    /// <summary>
    /// Spirit's condition monitor boxes
    /// </summary>
    public int ConditionMonitor => (Force + 8) / 2;
    
    /// <summary>
    /// Special abilities or powers this spirit type has
    /// </summary>
    [MaxLength(500)]
    public string? Powers { get; set; }
    
    /// <summary>
    /// Weaknesses or banes
    /// </summary>
    [MaxLength(200)]
    public string? Weaknesses { get; set; }
    
    /// <summary>
    /// Notes about this specific spirit
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    // Navigation property
    public virtual Character Character { get; set; } = null!;
    
    /// <summary>
    /// Use one service
    /// </summary>
    public void UseService()
    {
        if (ServicesOwed <= 0)
            throw new InvalidOperationException("Spirit has no services remaining");
        
        ServicesOwed--;
    }
    
    /// <summary>
    /// Check if spirit is still active
    /// </summary>
    public bool IsActive()
    {
        if (ServicesOwed <= 0)
            return false;
        
        if (ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Check if spirit is disrupted (damage exceeds condition monitor)
    /// </summary>
    public bool IsDisrupted()
    {
        return Damage >= ConditionMonitor;
    }
    
    /// <summary>
    /// Calculate time remaining before expiration
    /// </summary>
    public TimeSpan? TimeRemaining()
    {
        if (!ExpiresAt.HasValue)
            return null;
        
        var remaining = ExpiresAt.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}

/// <summary>
/// Spirit types by tradition
/// </summary>
public static class SpiritTypes
{
    // Hermetic elemental spirits
    public const string FireElemental = "Fire Elemental";
    public const string WaterElemental = "Water Elemental";
    public const string AirElemental = "Air Elemental";
    public const string EarthElemental = "Earth Elemental";
    
    // Shamanic nature spirits
    public const string CitySpirit = "City Spirit";
    public const string CountrySpirit = "Country Spirit";
    public const string ForestSpirit = "Forest Spirit";
    public const string MountainSpirit = "Mountain Spirit";
    public const string PrairieSpirit = "Prairie Spirit";
    public const string SeaSpirit = "Sea Spirit";
    public const string SkySpirit = "Sky Spirit";
    public const string SwampSpirit = "Swamp Spirit";
    
    // Totem spirits (Shamanic)
    public const string BearSpirit = "Bear Spirit";
    public const string CatSpirit = "Cat Spirit";
    public const string DogSpirit = "Dog Spirit";
    public const string EagleSpirit = "Eagle Spirit";
    public const string WolfSpirit = "Wolf Spirit";
    
    // Other traditions
    public const string Loa = "Loa"; // Voudoun
    public const string AllySpirit = "Ally Spirit"; // Hermetic/Universal
    
    public static readonly Dictionary<string, string[]> SpiritsByTradition = new()
    {
        ["Hermetic"] = new[] { FireElemental, WaterElemental, AirElemental, EarthElemental },
        ["Shamanic"] = new[] { CitySpirit, CountrySpirit, ForestSpirit, MountainSpirit, 
                               PrairieSpirit, SeaSpirit, SkySpirit, SwampSpirit,
                               BearSpirit, CatSpirit, DogSpirit, EagleSpirit, WolfSpirit }
    };
}

/// <summary>
/// Spirit dispositions
/// </summary>
public static class SpiritDispositions
{
    public const string Friendly = "Friendly";
    public const string Neutral = "Neutral";
    public const string Hostile = "Hostile";
    public const string Wild = "Wild"; // Uncontrolled
}
