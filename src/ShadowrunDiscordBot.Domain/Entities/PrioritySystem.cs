namespace ShadowrunDiscordBot.Domain.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Shadowrun 3rd Edition Priority System for character creation
/// </summary>
public class PriorityAllocation
{
    public int Id { get; set; }
    
    [Required]
    public int CharacterId { get; set; }
    
    /// <summary>
    /// Priority level (A, B, C, D, or E)
    /// </summary>
    [Required]
    [MaxLength(1)]
    public string Priority { get; set; } = "E";
    
    /// <summary>
    /// Category: Attributes, Skills, Resources, Magic, Metatype
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Category { get; set; } = "Attributes";
    
    /// <summary>
    /// Points allocated to this priority
    /// </summary>
    public int PointsAllocated { get; set; } = 0;
    
    /// <summary>
    /// Maximum points available at this priority level
    /// </summary>
    public int MaxPoints { get; set; } = 0;
    
    /// <summary>
    /// Additional data as JSON (for flexibility)
    /// </summary>
    public string? Metadata { get; set; }
    
    // Navigation property
    public virtual Character Character { get; set; } = null!;
}

/// <summary>
/// Priority table constants for Shadowrun 3rd Edition
/// </summary>
public static class PriorityTable
{
    public class PriorityLevel
    {
        public string Name { get; set; } = string.Empty;
        public int AttributePoints { get; set; }
        public int SkillPoints { get; set; }
        public long Nuyen { get; set; }
        public string[] RacialRestrictions { get; set; } = Array.Empty<string>();
    }
    
    public static readonly Dictionary<string, PriorityLevel> Table = new()
    {
        ["A"] = new PriorityLevel
        {
            Name = "Full Magician",
            AttributePoints = 30,
            SkillPoints = 50,
            Nuyen = 1000000,
            RacialRestrictions = new[] { "Human", "Elf", "Dwarf", "Ork", "Troll" }
        },
        ["B"] = new PriorityLevel
        {
            Name = "Adept/Aspected Magician",
            AttributePoints = 27,
            SkillPoints = 40,
            Nuyen = 400000,
            RacialRestrictions = new[] { "Human", "Elf", "Dwarf", "Ork", "Troll" }
        },
        ["C"] = new PriorityLevel
        {
            Name = "Elf/Troll",
            AttributePoints = 24,
            SkillPoints = 34,
            Nuyen = 90000,
            RacialRestrictions = new[] { "Elf", "Troll" }
        },
        ["D"] = new PriorityLevel
        {
            Name = "Dwarf/Ork",
            AttributePoints = 21,
            SkillPoints = 30,
            Nuyen = 20000,
            RacialRestrictions = new[] { "Dwarf", "Ork" }
        },
        ["E"] = new PriorityLevel
        {
            Name = "Human",
            AttributePoints = 18,
            SkillPoints = 27,
            Nuyen = 5000,
            RacialRestrictions = new[] { "Human" }
        }
    };
    
    public static readonly Dictionary<string, Dictionary<string, int>> RacialMaximums = new()
    {
        ["Human"] = new Dictionary<string, int>
        {
            ["Body"] = 9, ["Quickness"] = 9, ["Strength"] = 9,
            ["Charisma"] = 9, ["Intelligence"] = 9, ["Willpower"] = 9
        },
        ["Elf"] = new Dictionary<string, int>
        {
            ["Body"] = 9, ["Quickness"] = 11, ["Strength"] = 9,
            ["Charisma"] = 12, ["Intelligence"] = 9, ["Willpower"] = 9
        },
        ["Dwarf"] = new Dictionary<string, int>
        {
            ["Body"] = 11, ["Quickness"] = 9, ["Strength"] = 12,
            ["Charisma"] = 9, ["Intelligence"] = 9, ["Willpower"] = 11
        },
        ["Ork"] = new Dictionary<string, int>
        {
            ["Body"] = 14, ["Quickness"] = 9, ["Strength"] = 12,
            ["Charisma"] = 8, ["Intelligence"] = 8, ["Willpower"] = 9
        },
        ["Troll"] = new Dictionary<string, int>
        {
            ["Body"] = 17, ["Quickness"] = 8, ["Strength"] = 15,
            ["Charisma"] = 6, ["Intelligence"] = 6, ["Willpower"] = 9
        }
    };
    
    public static readonly Dictionary<string, Dictionary<string, int>> RacialBaseValues = new()
    {
        ["Human"] = new Dictionary<string, int>
        {
            ["Body"] = 1, ["Quickness"] = 1, ["Strength"] = 1,
            ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 1
        },
        ["Elf"] = new Dictionary<string, int>
        {
            ["Body"] = 1, ["Quickness"] = 2, ["Strength"] = 1,
            ["Charisma"] = 2, ["Intelligence"] = 1, ["Willpower"] = 1
        },
        ["Dwarf"] = new Dictionary<string, int>
        {
            ["Body"] = 2, ["Quickness"] = 1, ["Strength"] = 2,
            ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 2
        },
        ["Ork"] = new Dictionary<string, int>
        {
            ["Body"] = 3, ["Quickness"] = 1, ["Strength"] = 3,
            ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 1
        },
        ["Troll"] = new Dictionary<string, int>
        {
            ["Body"] = 5, ["Quickness"] = 1, ["Strength"] = 5,
            ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 1
        }
    };
    
    public static readonly Dictionary<string, int> StartingKarma = new()
    {
        ["Human"] = 3,
        ["Elf"] = 0,
        ["Dwarf"] = 0,
        ["Ork"] = 0,
        ["Troll"] = 0
    };
}
