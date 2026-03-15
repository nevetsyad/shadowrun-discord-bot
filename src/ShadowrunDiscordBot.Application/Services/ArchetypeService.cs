using ShadowrunDiscordBot.Domain.Entities;

namespace ShadowrunDiscordBot.Application.Services;

/// <summary>
/// Service for managing character archetypes
/// SR3 COMPLIANCE: Archetypes now use FIXED attributes instead of ranges
/// Starting resources match SR3 official values
/// </summary>
public class ArchetypeService : IArchetypeService
{
    private readonly Dictionary<string, ArchetypeTemplate> _archetypes;

    public ArchetypeService()
    {
        _archetypes = InitializeArchetypes();
    }

    /// <summary>
    /// Get all available archetypes
    /// </summary>
    public List<ArchetypeTemplate> GetAllArchetypes()
    {
        return _archetypes.Values.ToList();
    }

    /// <summary>
    /// Get a specific archetype by name
    /// </summary>
    public ArchetypeTemplate? GetArchetype(string name)
    {
        return _archetypes.TryGetValue(name.ToLowerInvariant(), out var archetype)
            ? archetype
            : null;
    }

    /// <summary>
    /// Get archetype by ID (async version for interface)
    /// </summary>
    public async Task<ArchetypeTemplate?> GetArchetypeByIdAsync(string archetypeId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // No async operation needed
        return GetArchetype(archetypeId);
    }

    /// <summary>
    /// Check if archetype is valid for given metatype
    /// </summary>
    public bool IsMetatypeAllowed(string archetypeName, string metatype)
    {
        var archetype = GetArchetype(archetypeName);
        if (archetype == null) return false;

        return archetype.AllowedMetatypes.Contains(metatype, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if metatype is compatible (async version for interface)
    /// </summary>
    public async Task<bool> IsMetatypeCompatibleAsync(string archetypeId, string metatype, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // No async operation needed
        return IsMetatypeAllowed(archetypeId, metatype);
    }

    /// <summary>
    /// Validate attributes against archetype constraints
    /// SR3 COMPLIANCE: Only returns true if attributes match the FIXED archetype values exactly
    /// </summary>
    public bool ValidateAttributes(string archetypeName, int body, int quickness,
        int strength, int charisma, int intelligence, int willpower)
    {
        var archetype = GetArchetype(archetypeName);
        if (archetype == null) return false;

        // SR3 COMPLIANCE: Check FIXED attributes exactly (not ranges)
        return body == archetype.Body &&
               quickness == archetype.Quickness &&
               strength == archetype.Strength &&
               charisma == archetype.Charisma &&
               intelligence == archetype.Intelligence &&
               willpower == archetype.Willpower;
    }

    /// <summary>
    /// Validate attributes (async version for interface)
    /// SR3 COMPLIANCE: Only returns true if attributes match the FIXED archetype values exactly
    /// </summary>
    public async Task<(bool IsValid, List<string> Errors)> ValidateAttributesAsync(
        string archetypeId,
        int body,
        int quickness,
        int strength,
        int charisma,
        int intelligence,
        int willpower,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // No async operation needed

        var errors = new List<string>();
        var archetype = GetArchetype(archetypeId);

        if (archetype == null)
        {
            return (false, new List<string> { $"Archetype '{archetypeId}' not found" });
        }

        // SR3 COMPLIANCE: Check FIXED attributes exactly (not ranges)
        if (body != archetype.Body)
        {
            errors.Add($"Body must be exactly {archetype.Body} (not {body})");
        }

        if (quickness != archetype.Quickness)
        {
            errors.Add($"Quickness must be exactly {archetype.Quickness} (not {quickness})");
        }

        if (strength != archetype.Strength)
        {
            errors.Add($"Strength must be exactly {archetype.Strength} (not {strength})");
        }

        if (charisma != archetype.Charisma)
        {
            errors.Add($"Charisma must be exactly {archetype.Charisma} (not {charisma})");
        }

        if (intelligence != archetype.Intelligence)
        {
            errors.Add($"Intelligence must be exactly {archetype.Intelligence} (not {intelligence})");
        }

        if (willpower != archetype.Willpower)
        {
            errors.Add($"Willpower must be exactly {archetype.Willpower} (not {willpower})");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Initialize archetypes with SR3-compliant fixed attributes and starting resources
    /// </summary>
    private Dictionary<string, ArchetypeTemplate> InitializeArchetypes()
    {
        return new Dictionary<string, ArchetypeTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            // STREET SAMURAI: Combat specialist with high physical stats
            ["Street Samurai"] = new ArchetypeTemplate
            {
                Name = "Street Samurai",
                Description = "Combat specialist with enhanced reflexes and combat skills (SR3 compliant)",
                AllowedMetatypes = { "Human", "Elf", "Ork", "Troll" },
                // FIXED attributes (not ranges)
                Body = 5,
                Quickness = 6,
                Strength = 5,
                Charisma = 3,
                Intelligence = 4,
                Willpower = 3,
                // SR3-compliant starting resources
                StartingNuyen = 400000, // Not 10,000 - matches SR3
                StartingKarma = 0,
                SkillBonuses = new Dictionary<string, int>
                {
                    ["Edged Weapons"] = 6,  // SR3 starting skills
                    ["Pistols"] = 5,
                    ["Athletics"] = 4,
                    ["Stealth"] = 2
                }
            },

            // MAGE: Hermetic spellcaster
            ["Mage"] = new ArchetypeTemplate
            {
                Name = "Mage",
                Description = "Hermetic magician with spellcasting abilities (SR3 compliant)",
                AllowedMetatypes = { "Human", "Elf", "Dwarf" },
                // FIXED attributes
                Body = 2,
                Quickness = 3,
                Strength = 2,
                Charisma = 4,
                Intelligence = 5,
                Willpower = 5,
                // SR3-compliant starting resources
                StartingNuyen = 10000,
                StartingKarma = 5,
                IsAwakened = true,
                SkillBonuses = new Dictionary<string, int>
                {
                    ["Sorcery"] = 5,
                    ["Conjuring"] = 4,
                    ["Spell Design"] = 3,
                    ["Aura Reading"] = 3
                }
            },

            // DECKER: Matrix specialist
            ["Decker"] = new ArchetypeTemplate
            {
                Name = "Decker",
                Description = "Matrix specialist with cyberdeck and hacking skills (SR3 compliant)",
                AllowedMetatypes = { "Human", "Elf", "Dwarf", "Ork" },
                // FIXED attributes
                Body = 2,
                Quickness = 3,
                Strength = 2,
                Charisma = 3,
                Intelligence = 6,
                Willpower = 4,
                // SR3-compliant starting resources (cyberdeck is 100,000¥)
                StartingNuyen = 100000,
                StartingKarma = 5,
                IsDecker = true,
                SkillBonuses = new Dictionary<string, int>
                {
                    ["Computer"] = 5,
                    ["Electronics"] = 4,
                    ["Cyberdeck Design"] = 3,
                    ["Security Systems"] = 3
                }
            },

            // SHAMAN: Spirit-based magician
            ["Shaman"] = new ArchetypeTemplate
            {
                Name = "Shaman",
                Description = "Spirit-based magician with totemic magic (SR3 compliant)",
                AllowedMetatypes = { "Human", "Elf", "Ork" },
                // FIXED attributes
                Body = 3,
                Quickness = 3,
                Strength = 3,
                Charisma = 5,
                Intelligence = 4,
                Willpower = 4,
                // SR3-compliant starting resources
                StartingNuyen = 5000,
                StartingKarma = 5,
                IsAwakened = true,
                SkillBonuses = new Dictionary<string, int>
                {
                    ["Conjuring"] = 5,
                    ["Sorcery"] = 4,
                    ["Animal Handling"] = 3,
                    ["Spirit Combat"] = 2
                }
            },

            // RIGGER: Vehicle specialist
            ["Rigger"] = new ArchetypeTemplate
            {
                Name = "Rigger",
                Description = "Vehicle specialist with control rig and drone operation (SR3 compliant)",
                AllowedMetatypes = { "Human", "Dwarf", "Ork" },
                // FIXED attributes
                Body = 3,
                Quickness = 4,
                Strength = 3,
                Charisma = 2,
                Intelligence = 5,
                Willpower = 3,
                // SR3-compliant starting resources (control rig is 50,000¥)
                StartingNuyen = 50000,
                StartingKarma = 5,
                IsRigger = true,
                SkillBonuses = new Dictionary<string, int>
                {
                    ["Vehicle Operation"] = 5,
                    ["Gunnery"] = 4,
                    ["Drone Operation"] = 4,
                    ["Sensor"] = 3
                }
            },

            // FACE: Social specialist
            ["Face"] = new ArchetypeTemplate
            {
                Name = "Face",
                Description = "Social specialist with negotiation and influence skills (SR3 compliant)",
                AllowedMetatypes = { "Human", "Elf" },
                // FIXED attributes
                Body = 2,
                Quickness = 3,
                Strength = 2,
                Charisma = 6,
                Intelligence = 4,
                Willpower = 3,
                // SR3-compliant starting resources
                StartingNuyen = 30000,
                StartingKarma = 5,
                SkillBonuses = new Dictionary<string, int>
                {
                    ["Negotiation"] = 5,
                    ["Etiquette"] = 4,
                    ["Interrogation"] = 3,
                    ["Influence"] = 3
                }
            },

            // PHYSICAL ADEPT: Magically-enhanced martial artist
            ["Physical Adept"] = new ArchetypeTemplate
            {
                Name = "Physical Adept",
                Description = "Magically-enhanced martial artist (SR3 compliant)",
                AllowedMetatypes = { "Human", "Elf", "Ork", "Troll" },
                // FIXED attributes
                Body = 5,
                Quickness = 5,
                Strength = 5,
                Charisma = 3,
                Intelligence = 3,
                Willpower = 4,
                // SR3-compliant starting resources
                StartingNuyen = 10000,
                StartingKarma = 5,
                IsAwakened = true,
                SkillBonuses = new Dictionary<string, int>
                {
                    ["Unarmed Combat"] = 5,
                    ["Dodge"] = 4,
                    ["Athletics"] = 4,
                    ["Blades"] = 3
                }
            }
        };
    }
}
