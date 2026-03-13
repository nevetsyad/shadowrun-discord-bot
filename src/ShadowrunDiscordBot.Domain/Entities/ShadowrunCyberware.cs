namespace ShadowrunDiscordBot.Domain.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Enhanced Shadowrun Cyberware and Bioware with detailed tracking
/// </summary>
public class ShadowrunCyberware
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int CharacterId { get; set; }
    
    /// <summary>
    /// Cyberware/bioware name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Category: Cyberware or Bioware
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Category { get; set; } = "Cyberware";
    
    /// <summary>
    /// Subcategory (e.g., Headware, Bodyware, Cyberlimb, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? Subcategory { get; set; }
    
    /// <summary>
    /// Rating/grade of the cyberware (1-10 typically)
    /// </summary>
    public int Rating { get; set; } = 0;
    
    /// <summary>
    /// Essence cost (stored as decimal, e.g., 0.5)
    /// </summary>
    [Required]
    public decimal EssenceCost { get; set; } = 0m;
    
    /// <summary>
    /// Original essence cost before grade modifiers
    /// </summary>
    public decimal BaseEssenceCost { get; set; } = 0m;
    
    /// <summary>
    /// Cost in nuyen
    /// </summary>
    public long NuyenCost { get; set; } = 0;
    
    /// <summary>
    /// Cyberware grade: Standard, Alpha, Beta, Delta
    /// </summary>
    [MaxLength(20)]
    public string Grade { get; set; } = "Standard";
    
    /// <summary>
    /// Whether the cyberware is installed
    /// </summary>
    public bool IsInstalled { get; set; } = true;
    
    /// <summary>
    /// Body location (e.g., Left Arm, Head, Torso)
    /// </summary>
    [MaxLength(50)]
    public string? Location { get; set; }
    
    /// <summary>
    /// When it was installed
    /// </summary>
    public DateTime? InstalledAt { get; set; }
    
    /// <summary>
    /// Bonuses provided (JSON or structured data)
    /// </summary>
    [MaxLength(500)]
    public string? Bonuses { get; set; }
    
    /// <summary>
    /// Malfunctions or drawbacks
    /// </summary>
    [MaxLength(500)]
    public string? Drawbacks { get; set; }
    
    /// <summary>
    /// Whether cyberware requires maintenance
    /// </summary>
    public bool RequiresMaintenance { get; set; } = false;
    
    /// <summary>
    /// Last maintenance date
    /// </summary>
    public DateTime? LastMaintenance { get; set; }
    
    /// <summary>
    /// Notes about this cyberware
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Source book and page
    /// </summary>
    [MaxLength(50)]
    public string? Source { get; set; }
    
    /// <summary>
    /// Whether this is cultured bioware (requires character-specific growth)
    /// </summary>
    public bool IsCultured { get; set; } = false;
    
    /// <summary>
    /// Availability rating
    /// </summary>
    [MaxLength(20)]
    public string? Availability { get; set; }
    
    /// <summary>
    /// Legality code
    /// </summary>
    [MaxLength(20)]
    public string? Legality { get; set; }
    
    /// <summary>
    /// Street index (cost multiplier on black market)
    /// </summary>
    public decimal? StreetIndex { get; set; }
    
    // Navigation property
    public virtual Character Character { get; set; } = null!;
    
    /// <summary>
    /// Calculate essence cost based on grade
    /// </summary>
    public decimal CalculateGradeAdjustedEssenceCost()
    {
        return Grade switch
        {
            "Alpha" => BaseEssenceCost * 0.8m,
            "Beta" => BaseEssenceCost * 0.6m,
            "Delta" => BaseEssenceCost * 0.5m,
            "Standard" => BaseEssenceCost,
            _ => BaseEssenceCost
        };
    }
    
    /// <summary>
    /// Calculate nuyen cost based on grade
    /// </summary>
    public long CalculateGradeAdjustedNuyenCost()
    {
        var multiplier = Grade switch
        {
            "Alpha" => 2,
            "Beta" => 4,
            "Delta" => 10,
            "Standard" => 1,
            _ => 1
        };
        
        return NuyenCost * multiplier;
    }
}

/// <summary>
/// Cyberware categories
/// </summary>
public static class CyberwareCategories
{
    public const string Cyberware = "Cyberware";
    public const string Bioware = "Bioware";
}

/// <summary>
/// Cyberware grades with essence and cost multipliers
/// </summary>
public static class CyberwareGrades
{
    public const string Standard = "Standard";
    public const string Alpha = "Alpha";
    public const string Beta = "Beta";
    public const string Delta = "Delta";
    
    public static readonly Dictionary<string, (decimal EssenceMultiplier, int CostMultiplier)> GradeMultipliers = new()
    {
        [Standard] = (1.0m, 1),
        [Alpha] = (0.8m, 2),
        [Beta] = (0.6m, 4),
        [Delta] = (0.5m, 10)
    };
}

/// <summary>
/// Common cyberware subcategories
/// </summary>
public static class CyberwareSubcategories
{
    public const string Headware = "Headware";
    public const string Bodyware = "Bodyware";
    public const string Cyberlimb = "Cyberlimb";
    public const string Cyberweapon = "Cyberweapon";
    public const string Cybersense = "Cybersense";
    public const string Neuralware = "Neuralware";
}
