namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// Represents a pre-made character archetype from Shadowrun 3rd Edition
/// SR3 COMPLIANCE: Uses FIXED attributes instead of ranges
/// Archetypes have predetermined attributes, skills, and resources
/// No user customization during creation - just metatype modifiers
/// </summary>
public class ArchetypeTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AllowedMetatypes { get; set; } = new();
    public bool IsActive { get; set; } = true; // For future use: enable/disable archetypes
    
    // Alias for backward compatibility
    public List<string> CompatibleMetatypes => AllowedMetatypes;

    // SR3 COMPLIANCE: FIXED attributes (not min/max ranges)
    // Users CANNOT customize these - must match exactly
    public int Body { get; set; }
    public int Quickness { get; set; }
    public int Strength { get; set; }
    public int Charisma { get; set; }
    public int Intelligence { get; set; }
    public int Willpower { get; set; }

    // Starting resources (SR3-compliant values)
    public int StartingNuyen { get; set; }
    public int StartingKarma { get; set; } = 5;

    // Skill bonuses (skill name -> rating)
    public Dictionary<string, int> SkillBonuses { get; set; } = new();

    // Special abilities
    public bool IsAwakened { get; set; } // Can use magic
    public bool IsDecker { get; set; } // Has cyberdeck
    public bool IsRigger { get; set; } // Has vehicle control rig

    /// <summary>
    /// Calculate attribute modifier from metatype (for use with FIXED archetype values)
    /// Example: Elf Street Samurai gets +1 Quickness and +2 Charisma from metatype
    /// </summary>
    public Dictionary<string, int> CalculateAttributeModifiers(string metatype)
    {
        var modifiers = new Dictionary<string, int>();

        // Metatype base values
        var baseValues = PriorityTable.RacialBaseValues.TryGetValue(metatype, out var baseVals)
            ? baseVals
            : new Dictionary<string, int>();

        // Metatype maximums
        var maxValues = PriorityTable.RacialMaximums.TryGetValue(metatype, out var maxVals)
            ? maxVals
            : new Dictionary<string, int>();

        // Calculate modifiers from base values
        modifiers["Body"] = Body - baseValues.GetValueOrDefault("Body", 0);
        modifiers["Quickness"] = Quickness - baseValues.GetValueOrDefault("Quickness", 0);
        modifiers["Strength"] = Strength - baseValues.GetValueOrDefault("Strength", 0);
        modifiers["Charisma"] = Charisma - baseValues.GetValueOrDefault("Charisma", 0);
        modifiers["Intelligence"] = Intelligence - baseValues.GetValueOrDefault("Intelligence", 0);
        modifiers["Willpower"] = Willpower - baseValues.GetValueOrDefault("Willpower", 0);

        return modifiers;
    }

    /// <summary>
    /// Apply metatype modifiers to archetype fixed attributes
    /// Used when creating characters using archetype templates
    /// </summary>
    public void ApplyMetatypeModifiers(ref int body, ref int quickness, ref int strength,
        ref int charisma, ref int intelligence, ref int willpower, string metatype)
    {
        var modifiers = CalculateAttributeModifiers(metatype);

        body += modifiers.GetValueOrDefault("Body", 0);
        quickness += modifiers.GetValueOrDefault("Quickness", 0);
        strength += modifiers.GetValueOrDefault("Strength", 0);
        charisma += modifiers.GetValueOrDefault("Charisma", 0);
        intelligence += modifiers.GetValueOrDefault("Intelligence", 0);
        willpower += modifiers.GetValueOrDefault("Willpower", 0);

        // Ensure attributes don't exceed racial maximums
        var maxValues = PriorityTable.RacialMaximums[metatype];
        body = Math.Min(body, maxValues["Body"]);
        quickness = Math.Min(quickness, maxValues["Quickness"]);
        strength = Math.Min(strength, maxValues["Strength"]);
        charisma = Math.Min(charisma, maxValues["Charisma"]);
        intelligence = Math.Min(intelligence, maxValues["Intelligence"]);
        willpower = Math.Min(willpower, maxValues["Willpower"]);
    }
}
