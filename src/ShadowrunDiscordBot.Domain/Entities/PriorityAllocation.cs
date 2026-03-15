using System.ComponentModel.DataAnnotations;

namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// Shadowrun 3rd Edition Priority Allocation Entity
/// Represents full A-E priority allocation across all 5 categories:
/// Metatype, Attributes, Magic, Skills, Resources
/// </summary>
public class PriorityAllocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    /// <summary>
    /// Metatype priority (A-E)
    /// </summary>
    [Required]
    [MaxLength(1)]
    public string MetatypePriority { get; set; } = "E";

    /// <summary>
    /// Attributes priority (A-E) - Determines attribute point budget (18-30 points)
    /// </summary>
    [Required]
    [MaxLength(1)]
    public string AttributesPriority { get; set; } = "C";

    /// <summary>
    /// Magic/Resonance priority (A-E)
    /// </summary>
    [Required]
    [MaxLength(1)]
    public string MagicPriority { get; set; } = "E";

    /// <summary>
    /// Skills priority (A-E) - Determines skill point budget (27-50 points)
    /// </summary>
    [Required]
    [MaxLength(1)]
    public string SkillsPriority { get; set; } = "C";

    /// <summary>
    /// Resources priority (A-E) - Determines starting nuyen (¥5,000 - ¥1,000,000)
    /// </summary>
    [Required]
    [MaxLength(1)]
    public string ResourcesPriority { get; set; } = "D";

    /// <summary>
    /// Priority value (A-E) for quick lookup
    /// </summary>
    [Required]
    [MaxLength(1)]
    public string Priority { get; set; } = "C";

    /// <summary>
    /// Date allocated
    /// </summary>
    public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// JSON-serialized detailed allocation information
    /// </summary>
    public string? DetailedAllocation { get; set; }

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
    
    /// <summary>
    /// SR3 RACIAL MODIFIERS - Applied to BASE attributes to get FINAL attributes
    /// These are the modifiers that get added to base attributes
    /// </summary>
    public static readonly Dictionary<string, Dictionary<string, int>> RacialModifiers = new()
    {
        ["Human"] = new Dictionary<string, int>
        {
            ["Body"] = 0, ["Quickness"] = 0, ["Strength"] = 0,
            ["Charisma"] = 0, ["Intelligence"] = 0, ["Willpower"] = 0
        },
        ["Elf"] = new Dictionary<string, int>
        {
            ["Body"] = 0, ["Quickness"] = 1, ["Strength"] = 0,
            ["Charisma"] = 2, ["Intelligence"] = 0, ["Willpower"] = 0
        },
        ["Dwarf"] = new Dictionary<string, int>
        {
            ["Body"] = 1, ["Quickness"] = 0, ["Strength"] = 2,
            ["Charisma"] = 0, ["Intelligence"] = 0, ["Willpower"] = 1
        },
        ["Ork"] = new Dictionary<string, int>
        {
            ["Body"] = 3, ["Quickness"] = 0, ["Strength"] = 3,
            ["Charisma"] = -1, ["Intelligence"] = 1, ["Willpower"] = 0
        },
        ["Troll"] = new Dictionary<string, int>
        {
            ["Body"] = 5, ["Quickness"] = -1, ["Strength"] = 5,
            ["Charisma"] = -2, ["Intelligence"] = -2, ["Willpower"] = 0
        }
    };
    
    /// <summary>
    /// Calculate final attribute from base attribute and racial modifier
    /// </summary>
    public static int CalculateFinalAttribute(string metatype, string attributeName, int baseValue)
    {
        var modifiers = RacialModifiers.TryGetValue(metatype, out var m) ? m : RacialModifiers["Human"];
        var modifier = modifiers.TryGetValue(attributeName, out var mod) ? mod : 0;
        return baseValue + modifier;
    }
    
    /// <summary>
    /// Get all final attributes for a metatype given base attributes
    /// </summary>
    public static Dictionary<string, int> CalculateFinalAttributes(
        string metatype,
        int baseBody, int baseQuickness, int baseStrength,
        int baseCharisma, int baseIntelligence, int baseWillpower)
    {
        var modifiers = RacialModifiers.TryGetValue(metatype, out var m) ? m : RacialModifiers["Human"];
        
        return new Dictionary<string, int>
        {
            ["Body"] = baseBody + modifiers["Body"],
            ["Quickness"] = baseQuickness + modifiers["Quickness"],
            ["Strength"] = baseStrength + modifiers["Strength"],
            ["Charisma"] = baseCharisma + modifiers["Charisma"],
            ["Intelligence"] = baseIntelligence + modifiers["Intelligence"],
            ["Willpower"] = baseWillpower + modifiers["Willpower"]
        };
    }
    
    /// <summary>
    /// Validate that final attributes don't exceed racial maximums
    /// </summary>
    public static (bool IsValid, List<string> Errors) ValidateFinalAttributes(string metatype, Dictionary<string, int> finalAttributes)
    {
        var errors = new List<string>();
        var maximums = RacialMaximums.TryGetValue(metatype, out var m) ? m : RacialMaximums["Human"];
        
        foreach (var attr in finalAttributes)
        {
            if (attr.Value > maximums[attr.Key])
            {
                errors.Add($"{attr.Key} ({attr.Value}) exceeds {metatype} maximum ({maximums[attr.Key]})");
            }
            if (attr.Value < 1)
            {
                errors.Add($"{attr.Key} ({attr.Value}) is below minimum (1)");
            }
        }
        
        return (errors.Count == 0, errors);
    }

    public static readonly Dictionary<string, Dictionary<string, int>> RacialBaseValues = new()
    {
        ["Human"] = new Dictionary<string, int>
        {
            ["Body"] = 1, ["Quickness"] = 1, ["Strength"] = 1,
            ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 1
        },
        ["Elf"] = new Dictionary<string, int>
        {
            ["Body"] = 1, ["Quickness"] = 1, ["Strength"] = 1,
            ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 1
        },
        ["Dwarf"] = new Dictionary<string, int>
        {
            ["Body"] = 1, ["Quickness"] = 1, ["Strength"] = 1,
            ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 1
        },
        ["Ork"] = new Dictionary<string, int>
        {
            ["Body"] = 1, ["Quickness"] = 1, ["Strength"] = 1,
            ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 1
        },
        ["Troll"] = new Dictionary<string, int>
        {
            ["Body"] = 1, ["Quickness"] = 1, ["Strength"] = 1,
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
