namespace ShadowrunDiscordBot.Domain.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Character origin, background, and personal details
/// </summary>
public class CharacterOrigin
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int CharacterId { get; set; }
    
    /// <summary>
    /// Character's real name (if known)
    /// </summary>
    [MaxLength(100)]
    public string? RealName { get; set; }
    
    /// <summary>
    /// Street name / alias
    /// </summary>
    [MaxLength(100)]
    public string? StreetName { get; set; }
    
    /// <summary>
    /// Age
    /// </summary>
    public int? Age { get; set; }
    
    /// <summary>
    /// Gender
    /// </summary>
    [MaxLength(50)]
    public string? Gender { get; set; }
    
    /// <summary>
    /// Ethnicity/nationality
    /// </summary>
    [MaxLength(100)]
    public string? Ethnicity { get; set; }
    
    /// <summary>
    /// Height in centimeters
    /// </summary>
    public int? HeightCm { get; set; }
    
    /// <summary>
    /// Weight in kilograms
    /// </summary>
    public int? WeightKg { get; set; }
    
    /// <summary>
    /// Physical description
    /// </summary>
    [MaxLength(1000)]
    public string? Appearance { get; set; }
    
    /// <summary>
    /// Distinguishing features (tattoos, scars, etc.)
    /// </summary>
    [MaxLength(500)]
    public string? DistinguishingFeatures { get; set; }
    
    /// <summary>
    /// Character's personality traits
    /// </summary>
    [MaxLength(1000)]
    public string? Personality { get; set; }
    
    /// <summary>
    /// Character's backstory/history
    /// </summary>
    public string? Backstory { get; set; }
    
    /// <summary>
    /// Family background
    /// </summary>
    [MaxLength(1000)]
    public string? Family { get; set; }
    
    /// <summary>
    /// Education/training
    /// </summary>
    [MaxLength(500)]
    public string? Education { get; set; }
    
    /// <summary>
    /// Former occupation before shadowrunning
    /// </summary>
    [MaxLength(200)]
    public string? FormerOccupation { get; set; }
    
    /// <summary>
    /// Why they became a shadowrunner
    /// </summary>
    [MaxLength(500)]
    public string? ReasonForRunning { get; set; }
    
    /// <summary>
    /// Long-term goals
    /// </summary>
    [MaxLength(500)]
    public string? Goals { get; set; }
    
    /// <summary>
    /// Fears/phobias
    /// </summary>
    [MaxLength(300)]
    public string? Fears { get; set; }
    
    /// <summary>
    /// Hobbies/interests
    /// </summary>
    [MaxLength(300)]
    public string? Hobbies { get; set; }
    
    /// <summary>
    /// Place of birth
    /// </summary>
    [MaxLength(200)]
    public string? Birthplace { get; set; }
    
    /// <summary>
    /// Current residence
    /// </summary>
    [MaxLength(200)]
    public string? Residence { get; set; }
    
    /// <summary>
    /// SIN status (SINless, Criminal SIN, Corporate SIN, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? SinStatus { get; set; }
    
    /// <summary>
    /// Lifestyle level (Squatter, Low, Middle, High, Luxury)
    /// </summary>
    [MaxLength(50)]
    public string? Lifestyle { get; set; }
    
    /// <summary>
    /// Monthly lifestyle cost in nuyen
    /// </summary>
    public long? LifestyleCost { get; set; }
    
    /// <summary>
    /// Known associates/contacts
    /// </summary>
    [MaxLength(500)]
    public string? KnownContacts { get; set; }
    
    /// <summary>
    /// Enemies/antagonists
    /// </summary>
    [MaxLength(500)]
    public string? Enemies { get; set; }
    
    /// <summary>
    /// Organizations the character is affiliated with
    /// </summary>
    [MaxLength(300)]
    public string? Affiliations { get; set; }
    
    /// <summary>
    /// Criminal record
    /// </summary>
    [MaxLength(500)]
    public string? CriminalRecord { get; set; }
    
    /// <summary>
    /// Moral code/ethics
    /// </summary>
    [MaxLength(500)]
    public string? MoralCode { get; set; }
    
    /// <summary>
    /// Religion/belief system
    /// </summary>
    [MaxLength(200)]
    public string? Religion { get; set; }
    
    /// <summary>
    /// Voice/speech patterns
    /// </summary>
    [MaxLength(300)]
    public string? VoiceDescription { get; set; }
    
    /// <summary>
    /// Quote or catchphrase
    /// </summary>
    [MaxLength(200)]
    public string? Quote { get; set; }
    
    // Navigation property
    public virtual Character Character { get; set; } = null!;
}

/// <summary>
/// Character contacts (NPCs the character knows)
/// </summary>
public class CharacterContact
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int CharacterId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ContactName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of contact (Fixer, Street Doc, Arms Dealer, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContactType { get; set; } = string.Empty;
    
    /// <summary>
    /// Connection rating (1-6)
    /// </summary>
    [Required]
    public int ConnectionRating { get; set; } = 1;
    
    /// <summary>
    /// Loyalty rating (1-6)
    /// </summary>
    [Required]
    public int LoyaltyRating { get; set; } = 1;
    
    /// <summary>
    /// How they met
    /// </summary>
    [MaxLength(500)]
    public string? Backstory { get; set; }
    
    /// <summary>
    /// What services they provide
    /// </summary>
    [MaxLength(300)]
    public string? Services { get; set; }
    
    /// <summary>
    /// Location
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }
    
    /// <summary>
    /// Notes about this contact
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Whether this contact is still active/available
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    // Navigation property
    public virtual Character Character { get; set; } = null!;
}

/// <summary>
/// Lifestyle types
/// </summary>
public static class Lifestyles
{
    public const string Squatter = "Squatter";
    public const string Low = "Low";
    public const string Middle = "Middle";
    public const string High = "High";
    public const string Luxury = "Luxury";
    
    public static readonly Dictionary<string, long> MonthlyCosts = new()
    {
        [Squatter] = 0,
        [Low] = 1000,
        [Middle] = 5000,
        [High] = 10000,
        [Luxury] = 100000
    };
}

/// <summary>
/// SIN status types
/// </summary>
public static class SinStatuses
{
    public const string Sinless = "SINless";
    public const string CriminalSIN = "Criminal SIN";
    public const string NationalSIN = "National SIN";
    public const string CorporateSIN = "Corporate SIN";
    public const string LimitedCorporateSIN = "Limited Corporate SIN";
}
