using System.Text;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Application.Services;
using ShadowrunDiscordBot.Services;

using CharacterSkill = ShadowrunDiscordBot.Models.CharacterSkill;

namespace ShadowrunDiscordBot.Commands;

/// <summary>
/// Character management commands
/// </summary>
public class CharacterCommands : BaseCommandModule
{


    // Constants for character creation
    private const int _startingKarma = 5;
    private const int _startingNuyen = 5000;
    private const int _deckerBonusNuyen = 100000;
    private const int _riggerBonusNuyen = 50000;
    private const int _defaultAttribute = 3;
    private const int _defaultMagic = 6;

    // FIX: MED-002 - Define valid metatypes and archetypes for validation
    // TODO: convert to enum and maintain a list of all enums for selection  this is sloppy and relies on string comparisons across the app.
    // This enum should be moved into a common file that's used throughout the application
    private static readonly HashSet<string> _validMetatypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Human", "Elf", "Dwarf", "Ork", "Troll"
    };

    // GPT-5.4 FIX: Updated to use archetype IDs instead of display names
    // Only 3 archetypes are available: street-samurai, mage, decker
    // use anotations on enum to provide additional details or formating
    private static readonly HashSet<string> _validArchetypeIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "street-samurai",
        "mage",
        "decker"
    };

    // GPT-5.4 FIX: Display name mapping for archetypes
    private static readonly Dictionary<string, string> _archetypeDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "street-samurai", "Street Samurai" },
        { "mage", "Mage" },
        { "decker", "Decker" }
    };

    // FIX: MED-002 - Attribute bounds
    private const int _minAttributeValue = 1;
    private const int _maxAttributeValue = 10;

    public CharacterCommands(
        ILogger<CharacterCommands> logger,
        BotConfig config,
        DatabaseService database,
        DiceService diceService) : base(logger, config, database, diceService)
    {
    }

    public async Task CreateCharacterAsync(SocketSlashCommand command)
    {
        await LogCommandExecutionAsync(command, "Create Character");

        try
        {
            var options = command.Data.Options.First().Options.ToList();
            var name = options.First(o => o.Name == "name").Value.ToString();
            var metatype = options.First(o => o.Name == "metatype").Value.ToString();

            // Priority System: Optional archetype selection (for backward compatibility)
            var archetypeId = options.FirstOrDefault(o => o.Name == "archetype")?.Value.ToString();

            // FIX: MED-002 - Validate character name
            if (string.IsNullOrWhiteSpace(name))
            {
                await command.RespondAsync("⚠️ Character name cannot be empty.", ephemeral: true);
                return;
            }

            if (name.Length > 50)
            {
                await command.RespondAsync("⚠️ Character name cannot exceed 50 characters.", ephemeral: true);
                return;
            }

            // FIX: MED-002 - Validate metatype
            if (!_validMetatypes.Contains(metatype!))
            {
                await command.RespondAsync(
                    $"⚠️ Invalid metatype '{metatype}'. Valid options are: {string.Join(", ", _validMetatypes)}",
                    ephemeral: true);
                return;
            }

            // GPT-5.4 FIX: Archetype is now OPTIONAL - only validate if provided
            // If no archetype is selected, this is a priority-based character creation
            if (!string.IsNullOrWhiteSpace(archetypeId))
            {
                if (!_validArchetypeIds.Contains(archetypeId!))
                {
                    await command.RespondAsync(
                        $"⚠️ Invalid archetype '{archetypeId}'. Valid options are: {string.Join(", ", _validArchetypeIds)}",
                        ephemeral: true);
                    return;
                }

                // Check if user already has too many characters
                var existingChars = await Database.GetUserCharactersAsync(command.User.Id);
                if (existingChars?.Count >= Config.Bot.MaxCharactersPerUser)
                {
                    await command.RespondAsync(
                        $"⚠️ You already have the maximum number of characters ({Config.Bot.MaxCharactersPerUser}). " +
                        "Please delete one before creating a new one.",
                        ephemeral: true);
                    return;
                }

                // Check if character name already exists
                if (existingChars?.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? false )
                {
                    await command.RespondAsync($"⚠️ You already have a character named '{name}'.", ephemeral: true);
                    return;
                }

                // Legacy archetype system
                var character = CreateCharacterWithArchetypeSystem(command.User.Id, name!, metatype!, archetypeId!);
                character.PriorityLevel = null; // No priority system used

                await Database.CreateCharacterAsync(character);

                var embed = BuildCharacterEmbed(character, "✨ Character Created (Archetype)");

                await command.RespondAsync(embed: embed);
                return;
            }
            else
            {

                // Priority System: Create character with priority allocation
                // Check if user already has too many characters
                var existingChars = await Database.GetUserCharactersAsync(command.User.Id);
                if (existingChars.Count >= Config.Bot.MaxCharactersPerUser)
                {
                    await command.RespondAsync(
                        $"⚠️ You already have the maximum number of characters ({Config.Bot.MaxCharactersPerUser}). " +
                        "Please delete one before creating a new one.",
                        ephemeral: true);
                    return;
                }

                // Check if character name already exists
                if (existingChars.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    await command.RespondAsync($"⚠️ You already have a character named '{name}'.", ephemeral: true);
                    return;
                }

                // Priority System: Create character with priority allocation
                var character = CreateCharacterWithPrioritySystem(command.User.Id, name!, metatype!);

                await Database.CreateCharacterAsync(character);

                var embed = BuildCharacterEmbedWithPriority(character, "✨ Character Created (Priority)");

                await command.RespondAsync(embed: embed);
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(command, ex, "CreateCharacter");
        }
    }

    public async Task ListCharactersAsync(SocketSlashCommand command)
    {
        await LogCommandExecutionAsync(command, "List Characters");

        try
        {
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            var characters = await Database.GetUserCharactersAsync(command.User.Id).ConfigureAwait(false);

            if (characters.Count == 0)
            {
                await command.RespondAsync("📋 You don't have any characters yet. Use `/character create` to make one!", ephemeral: true);
                return;
            }

            var embedBuilder = new EmbedBuilder()
                .WithTitle($"📋 {command.User.Username}'s Characters")
                .WithColor(Config.Bot.DefaultColor);

            foreach (var character in characters)
            {
                embedBuilder.AddField(
                    character.Name,
                    $"**{character.Metatype} {character.Archetype}**\n" +
                    $"Body: {character.Body} | Quickness: {character.Quickness} | Strength: {character.Strength}\n" +
                    $"Charisma: {character.Charisma} | Intelligence: {character.Intelligence} | Willpower: {character.Willpower}\n" +
                    $"Essence: {character.Essence / 100m:F2} | Karma: {character.Karma} | Nuyen: {character.Nuyen:N0}¥",
                    inline: false);
            }

            await command.RespondAsync(embed: embedBuilder.Build());
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(command, ex, "ListCharacters");
        }
    }

    public async Task ViewCharacterAsync(SocketSlashCommand command)
    {
        await LogCommandExecutionAsync(command, "View Character");

        try
        {
            var name = command.Data.Options.First().Options.First(o => o.Name == "name").Value.ToString();
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            var character = await Database.GetCharacterByNameAsync(command.User.Id, name!).ConfigureAwait(false);

            if (character == null)
            {
                await command.RespondAsync($"❌ Character '{name}' not found.", ephemeral: true);
                return;
            }

            var embed = BuildCharacterSheetEmbed(character);
            await command.RespondAsync(embed: embed);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(command, ex, "ViewCharacter");
        }
    }

    public async Task DeleteCharacterAsync(SocketSlashCommand command)
    {
        await LogCommandExecutionAsync(command, "Delete Character");

        try
        {
            var name = command.Data.Options.First().Options.First(o => o.Name == "name").Value.ToString();
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            var character = await Database.GetCharacterByNameAsync(command.User.Id, name!).ConfigureAwait(false);

            if (character == null)
            {
                await command.RespondAsync($"❌ Character '{name}' not found.", ephemeral: true);
                return;
            }

            // TODO: Add confirmation dialog

            // FIX: HIGH-001 - Added ConfigureAwait(false)
            await Database.DeleteCharacterAsync(character.Id).ConfigureAwait(false);

            await command.RespondAsync($"🗑️ Character '{character.Name}' has been deleted.");
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(command, ex, "DeleteCharacter");
        }
    }

    // SR3 COMPLIANCE: Create character with base attributes and apply racial modifiers
    private ShadowrunCharacter CreateCharacterWithArchetype(ulong userId, string name, string metatype, string archetype)
    {
        // SR3 COMPLIANCE: Start with base attributes (default 3 for all)
        var baseBody = _defaultAttribute;
        var baseQuickness = _defaultAttribute;
        var baseStrength = _defaultAttribute;
        var baseCharisma = _defaultAttribute;
        var baseIntelligence = _defaultAttribute;
        var baseWillpower = _defaultAttribute;

        var character = new ShadowrunCharacter
        {
            DiscordUserId = userId,
            Name = name,
            Metatype = metatype,
            Archetype = archetype,
            Karma = _startingKarma,
            Nuyen = _startingNuyen,
            IsCustomBuild = true,
            
            // Store base attributes
            BaseBody = baseBody,
            BaseQuickness = baseQuickness,
            BaseStrength = baseStrength,
            BaseCharisma = baseCharisma,
            BaseIntelligence = baseIntelligence,
            BaseWillpower = baseWillpower
        };

        // SR3 COMPLIANCE: Apply racial modifiers to get final attributes
        ApplyRacialModifiers(character, metatype);

        // Apply archetype defaults
        ApplyArchetypeDefaults(character, archetype);

        return character;
    }

    // PRIORITY SYSTEM: Create character using priority-based allocation with SR3 compliance
    private ShadowrunCharacter CreateCharacterWithPrioritySystem(ulong userId, string name, string metatype)
    {
        var character = new ShadowrunCharacter
        {
            DiscordUserId = userId,
            Name = name,
            Metatype = metatype,
            Archetype = "Custom Priority Build",
            IsCustomBuild = true,
            
            // Initialize base attributes to minimum
            BaseBody = 1,
            BaseQuickness = 1,
            BaseStrength = 1,
            BaseCharisma = 1,
            BaseIntelligence = 1,
            BaseWillpower = 1
        };

        // Calculate priority-based attributes and resources
        ApplyPriorityAllocations(character, "C", metatype);

        return character;
    }

    // PRIORITY SYSTEM: Apply priority-based allocations with SR3 racial modifier support
    private void ApplyPriorityAllocations(ShadowrunCharacter character, string priority, string metatype)
    {
        var table = PriorityTable.Table[priority];
        var racialModifiers = PriorityTable.RacialModifiers.TryGetValue(metatype, out var mods) 
            ? mods 
            : PriorityTable.RacialModifiers["Human"];
        var maxValues = PriorityTable.RacialMaximums[metatype];

        // SR3 COMPLIANCE: Start with base attributes (user input, default to 1)
        var baseAttributes = new Dictionary<string, int>
        {
            ["Body"] = character.BaseBody,
            ["Quickness"] = character.BaseQuickness,
            ["Strength"] = character.BaseStrength,
            ["Charisma"] = character.BaseCharisma,
            ["Intelligence"] = character.BaseIntelligence,
            ["Willpower"] = character.BaseWillpower
        };

        // Allocate attribute points to BASE attributes based on priority
        var remainingPoints = table.AttributePoints;
        var attributes = new List<(string Name, int Value)>
        {
            ("Body", baseAttributes["Body"]),
            ("Quickness", baseAttributes["Quickness"]),
            ("Strength", baseAttributes["Strength"]),
            ("Charisma", baseAttributes["Charisma"]),
            ("Intelligence", baseAttributes["Intelligence"]),
            ("Willpower", baseAttributes["Willpower"])
        };

        // Simple allocation strategy: distribute points evenly to base attributes
        while (remainingPoints > 0)
        {
            var minAttr = attributes.OrderBy(a => a.Value).First();
            if (minAttr.Value >= 6) break; // Cap for base attributes

            // Calculate what final value would be with this increase
            var newBaseValue = minAttr.Value + 1;
            var finalValue = newBaseValue + racialModifiers[minAttr.Name];
            
            // Check against racial maximums
            if (finalValue <= maxValues[minAttr.Name])
            {
                minAttr.Value++;
                remainingPoints--;
            }
            else
            {
                // This attribute would exceed max, try next
                break;
            }
        }

        // Update base attributes
        character.BaseBody = attributes.First(a => a.Name == "Body").Value;
        character.BaseQuickness = attributes.First(a => a.Name == "Quickness").Value;
        character.BaseStrength = attributes.First(a => a.Name == "Strength").Value;
        character.BaseCharisma = attributes.First(a => a.Name == "Charisma").Value;
        character.BaseIntelligence = attributes.First(a => a.Name == "Intelligence").Value;
        character.BaseWillpower = attributes.First(a => a.Name == "Willpower").Value;

        // SR3 COMPLIANCE: Calculate final attributes by applying racial modifiers
        character.Body = character.BaseBody + racialModifiers["Body"];
        character.Quickness = character.BaseQuickness + racialModifiers["Quickness"];
        character.Strength = character.BaseStrength + racialModifiers["Strength"];
        character.Charisma = character.BaseCharisma + racialModifiers["Charisma"];
        character.Intelligence = character.BaseIntelligence + racialModifiers["Intelligence"];
        character.Willpower = character.BaseWillpower + racialModifiers["Willpower"];
        
        // Store applied racial modifiers for display
        character.AppliedRacialModifiers = new Dictionary<string, int>(racialModifiers);

        // Set resources based on priority
        character.Nuyen = table.Nuyen;
        character.Karma = PriorityTable.StartingKarma[metatype];

        // Set magic/resonance based on priority and metatype
        character.Magic = 0;

        if (priority == "A")
        {
            character.Magic = 6;
            character.Skills.Add(new CharacterSkill { SkillName = "Sorcery", Rating = 4 });
            character.Skills.Add(new CharacterSkill { SkillName = "Conjuring", Rating = 4 });
        }
        else if (priority == "B")
        {
            character.Magic = 5;
            character.Skills.Add(new CharacterSkill { SkillName = "Unarmed Combat", Rating = 5 });
            character.Skills.Add(new CharacterSkill { SkillName = "Athletics", Rating = 4 });
        }

        // Allocate skill points based on priority
        var skillPoints = table.SkillPoints;
        var skills = new List<(string Name, int Rating)>
        {
            ("Pistols", 2),
            ("Edged Weapons", 2),
            ("Athletics", 2),
            ("Stealth", 1),
            ("Computer", 1)
        };

        foreach (var skill in skills)
        {
            if (skillPoints >= skill.Rating)
            {
                character.Skills.Add(new CharacterSkill { SkillName = skill.Name, Rating = skill.Rating });
                skillPoints -= skill.Rating;
            }
        }

        // Save priority allocation information
        character.PriorityLevel = priority;
        character.AttributeModifiers = new Dictionary<string, int>
        {
            ["Body"] = racialModifiers["Body"],
            ["Quickness"] = racialModifiers["Quickness"],
            ["Strength"] = racialModifiers["Strength"],
            ["Charisma"] = racialModifiers["Charisma"],
            ["Intelligence"] = racialModifiers["Intelligence"],
            ["Willpower"] = racialModifiers["Willpower"]
        };
    }

    // GPT-5.4 FIX: Create character using archetype system with SR3 racial modifier support
    private ShadowrunCharacter CreateCharacterWithArchetypeSystem(ulong userId, string name, string metatype, string archetypeId)
    {
        var archetypeName = _archetypeDisplayNames.GetValueOrDefault(archetypeId, archetypeId);

        // SR3 COMPLIANCE: Define base attributes for each archetype
        // These are the BASE values before racial modifiers
        int baseBody, baseQuickness, baseStrength, baseCharisma, baseIntelligence, baseWillpower;

        switch (archetypeId.ToLowerInvariant())
        {
            case "street-samurai":
                baseBody = 5; baseQuickness = 5; baseStrength = 5;
                baseCharisma = 2; baseIntelligence = 3; baseWillpower = 3;
                break;
            case "mage":
                baseBody = 2; baseQuickness = 3; baseStrength = 2;
                baseCharisma = 4; baseIntelligence = 5; baseWillpower = 5;
                break;
            case "decker":
                baseBody = 2; baseQuickness = 3; baseStrength = 2;
                baseCharisma = 3; baseIntelligence = 6; baseWillpower = 4;
                break;
            default:
                baseBody = _defaultAttribute;
                baseQuickness = _defaultAttribute;
                baseStrength = _defaultAttribute;
                baseCharisma = _defaultAttribute;
                baseIntelligence = _defaultAttribute;
                baseWillpower = _defaultAttribute;
                break;
        }

        var character = new ShadowrunCharacter
        {
            DiscordUserId = userId,
            Name = name,
            Metatype = metatype,
            Archetype = archetypeName,
            ArchetypeId = archetypeId,
            IsCustomBuild = false,
            
            // Store base attributes
            BaseBody = baseBody,
            BaseQuickness = baseQuickness,
            BaseStrength = baseStrength,
            BaseCharisma = baseCharisma,
            BaseIntelligence = baseIntelligence,
            BaseWillpower = baseWillpower
        };

        // SR3 COMPLIANCE: Apply racial modifiers to get final attributes
        ApplyRacialModifiers(character, metatype);

        // Apply archetype-specific bonuses
        ApplyArchetypeBonuses(character, archetypeId);

        return character;
    }

    /// <summary>
    /// SR3 COMPLIANCE: Apply racial modifiers to base attributes to get final attributes
    /// </summary>
    private void ApplyRacialModifiers(ShadowrunCharacter character, string metatype)
    {
        var racialModifiers = PriorityTable.RacialModifiers.TryGetValue(metatype, out var mods) 
            ? mods 
            : PriorityTable.RacialModifiers["Human"];

        // Calculate final attributes from base + racial modifiers
        character.Body = character.BaseBody + racialModifiers["Body"];
        character.Quickness = character.BaseQuickness + racialModifiers["Quickness"];
        character.Strength = character.BaseStrength + racialModifiers["Strength"];
        character.Charisma = character.BaseCharisma + racialModifiers["Charisma"];
        character.Intelligence = character.BaseIntelligence + racialModifiers["Intelligence"];
        character.Willpower = character.BaseWillpower + racialModifiers["Willpower"];
        
        // Store applied racial modifiers for display
        character.AppliedRacialModifiers = new Dictionary<string, int>(racialModifiers);
        
        _logger.LogInformation(
            "Applied {Metatype} racial modifiers to {Name}: " +
            "Body {BaseBody}→{FinalBody}, Quickness {BaseQuickness}→{FinalQuickness}, " +
            "Strength {BaseStrength}→{FinalStrength}, Charisma {BaseCharisma}→{FinalCharisma}, " +
            "Intelligence {BaseIntelligence}→{FinalIntelligence}, Willpower {BaseWillpower}→{FinalWillpower}",
            metatype, character.Name,
            character.BaseBody, character.Body,
            character.BaseQuickness, character.Quickness,
            character.BaseStrength, character.Strength,
            character.BaseCharisma, character.Charisma,
            character.BaseIntelligence, character.Intelligence,
            character.BaseWillpower, character.Willpower);
    }

    // GPT-5.4 FIX: Legacy method kept for backward compatibility
    private void ApplyArchetypeDefaults(ShadowrunCharacter character, string archetype)
    {
        var arch = archetype.ToLowerInvariant();

        if (arch is "mage" or "shaman" or "physical adept")
        {
            character.Magic = _defaultMagic;
        }

        switch (arch)
        {
            case "mage":
                character.Skills.Add(new CharacterSkill { SkillName = "Sorcery", Rating = _defaultMagic });
                character.Skills.Add(new CharacterSkill { SkillName = "Conjuring", Rating = 4 });
                break;
            case "shaman":
                character.Skills.Add(new CharacterSkill { SkillName = "Sorcery", Rating = 5 });
                character.Skills.Add(new CharacterSkill { SkillName = "Conjuring", Rating = _defaultMagic });
                break;
            case "physical adept":
                character.Skills.Add(new CharacterSkill { SkillName = "Unarmed Combat", Rating = _defaultMagic });
                character.Skills.Add(new CharacterSkill { SkillName = "Athletics", Rating = 5 });
                break;
            case "street samurai":
                character.Skills.Add(new CharacterSkill { SkillName = "Pistols", Rating = _defaultMagic });
                character.Skills.Add(new CharacterSkill { SkillName = "Edged Weapons", Rating = 5 });
                character.Skills.Add(new CharacterSkill { SkillName = "Athletics", Rating = 4 });
                break;
            case "decker":
                character.Skills.Add(new CharacterSkill { SkillName = "Computer", Rating = _defaultMagic });
                character.Skills.Add(new CharacterSkill { SkillName = "Electronics", Rating = 5 });
                character.Nuyen += _deckerBonusNuyen;
                break;
            case "rigger":
                character.Skills.Add(new CharacterSkill { SkillName = "Vehicle Operation", Rating = _defaultMagic });
                character.Skills.Add(new CharacterSkill { SkillName = "Gunnery", Rating = 5 });
                character.Nuyen += _riggerBonusNuyen;
                break;
        }
    }

    // GPT-5.4 FIX: New method applying archetype system bonuses based on SR3 standards
    private void ApplyArchetypeBonuses(ShadowrunCharacter character, string archetypeId)
    {
        switch (archetypeId.ToLowerInvariant())
        {
            case "street-samurai":
                // Street Samurai: Combat specialist
                character.Skills.Add(new CharacterSkill { SkillName = "Pistols", Rating = 4 });
                character.Skills.Add(new CharacterSkill { SkillName = "Edged Weapons", Rating = 4 });
                character.Skills.Add(new CharacterSkill { SkillName = "Athletics", Rating = 3 });
                character.Skills.Add(new CharacterSkill { SkillName = "Unarmed Combat", Rating = 3 });
                character.Skills.Add(new CharacterSkill { SkillName = "Stealth", Rating = 2 });
                character.Nuyen = 5000;
                character.Karma = 5;
                // Street Samurai recommended attributes
                character.Body = 5;
                character.Quickness = 5;
                character.Strength = 5;
                character.Charisma = 2;
                character.Intelligence = 3;
                character.Willpower = 3;
                break;

            case "mage":
                // Mage: Awakened spellcaster
                character.Skills.Add(new CharacterSkill { SkillName = "Sorcery", Rating = 5 });
                character.Skills.Add(new CharacterSkill { SkillName = "Conjuring", Rating = 4 });
                character.Skills.Add(new CharacterSkill { SkillName = "Spell Design", Rating = 3 });
                character.Skills.Add(new CharacterSkill { SkillName = "Aura Reading", Rating = 3 });
                character.Skills.Add(new CharacterSkill { SkillName = "Magical Theory", Rating = 2, IsKnowledgeSkill = true });
                character.Nuyen = 3000;
                character.Karma = 5;
                character.Magic = 6; // Starting Magic attribute
                // Mage recommended attributes
                character.Body = 2;
                character.Quickness = 3;
                character.Strength = 2;
                character.Charisma = 4;
                character.Intelligence = 5;
                character.Willpower = 5;
                break;

            case "decker":
                // Decker: Matrix specialist
                character.Skills.Add(new CharacterSkill { SkillName = "Computer", Rating = 5 });
                character.Skills.Add(new CharacterSkill { SkillName = "Electronics", Rating = 4 });
                character.Skills.Add(new CharacterSkill { SkillName = "Cyberdeck Design", Rating = 3 });
                character.Skills.Add(new CharacterSkill { SkillName = "Data Brokerage", Rating = 2, IsKnowledgeSkill = true });
                character.Skills.Add(new CharacterSkill { SkillName = "Security Systems", Rating = 3 });
                character.Nuyen = 100000; // Extra nuyen for cyberdeck
                character.Karma = 5;
                // Decker recommended attributes
                character.Body = 2;
                character.Quickness = 3;
                character.Strength = 2;
                character.Charisma = 3;
                character.Intelligence = 6;
                character.Willpower = 4;
                break;
        }
    }

    private Embed BuildCharacterEmbed(ShadowrunCharacter character, string title)
    {
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithColor(Config.Bot.DefaultColor)
            .AddField("Name", character.Name, true)
            .AddField("Metatype", character.Metatype, true)
            .AddField("Archetype", character.Archetype, true);

        if (!string.IsNullOrWhiteSpace(character.PriorityLevel))
        {
            embed.AddField("Priority", character.PriorityLevel, true);
        }

        embed.AddField("Attributes",
            $"Body: {character.Body} | Quickness: {character.Quickness} | Strength: {character.Strength}\n" +
            $"Charisma: {character.Charisma} | Intelligence: {character.Intelligence} | Willpower: {character.Willpower}",
            false)
            .AddField("Derived",
                $"Reaction: {character.Reaction} | Essence: {character.Essence / 100m:F2}",
                false)
            .AddField("Resources",
                $"Karma: {character.Karma} | Nuyen: {character.Nuyen:N0}¥",
                false);

        return embed.Build();
    }

    private Embed BuildCharacterEmbedWithPriority(ShadowrunCharacter character, string title)
    {
        var priorityLevel = character.PriorityLevel ?? "N/A";
        var points = PriorityTable.Table.ContainsKey(priorityLevel) ? PriorityTable.Table[priorityLevel].AttributePoints : 0;

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithColor(Config.Bot.DefaultColor)
            .AddField("Name", character.Name, true)
            .AddField("Metatype", character.Metatype, true)
            .AddField("Archetype", character.Archetype, true)
            .AddField("Priority", priorityLevel, true)
            .AddField("Attribute Points", points.ToString(), true)
            .AddField("Skills Points",
                PriorityTable.Table.ContainsKey(priorityLevel) ? PriorityTable.Table[priorityLevel].SkillPoints.ToString() : "N/A",
                true)
            .AddField("Resources",
                $"Nuyen: {character.Nuyen:N0}¥ | Karma: {character.Karma}",
                false)
            .AddField("Attributes",
                $"Body: {character.Body} | Quickness: {character.Quickness} | Strength: {character.Strength}\n" +
                $"Charisma: {character.Charisma} | Intelligence: {character.Intelligence} | Willpower: {character.Willpower}",
                false)
            .AddField("Derived",
                $"Reaction: {character.Reaction} | Essence: {character.Essence / 100m:F2}",
                false);

        return embed.Build();
    }

    private Embed BuildCharacterSheetEmbed(ShadowrunCharacter character)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"📜 {character.Name}")
            .WithColor(Config.Bot.DefaultColor)
            .WithDescription($"**{character.Metatype} {character.Archetype}**");

        if (!string.IsNullOrWhiteSpace(character.PriorityLevel))
        {
            var priority = character.PriorityLevel;
            var points = PriorityTable.Table.ContainsKey(priority) ? PriorityTable.Table[priority].AttributePoints : 0;
            builder.AddField("Priority System",
                $"**Priority:** {priority}\n" +
                $"**Attribute Points:** {points}\n" +
                $"**Skills Points:** {PriorityTable.Table[priority].SkillPoints}\n" +
                $"**Nuyen:** {character.Nuyen:N0}¥",
                false);
        }

        // SR3 COMPLIANCE: Show attributes with racial modifier breakdown
        var racialModifiers = character.AppliedRacialModifiers ?? new Dictionary<string, int>();
        
        builder.AddField("PHYSICAL ATTRIBUTES",
            FormatAttributesWithModifiers(
                "💪 Body", character.BaseBody, character.Body, racialModifiers.GetValueOrDefault("Body", 0),
                "🏃 Quickness", character.BaseQuickness, character.Quickness, racialModifiers.GetValueOrDefault("Quickness", 0),
                "🏋️ Strength", character.BaseStrength, character.Strength, racialModifiers.GetValueOrDefault("Strength", 0)),
            true)
            .AddField("MENTAL ATTRIBUTES",
            FormatAttributesWithModifiers(
                "💬 Charisma", character.BaseCharisma, character.Charisma, racialModifiers.GetValueOrDefault("Charisma", 0),
                "🧠 Intelligence", character.BaseIntelligence, character.Intelligence, racialModifiers.GetValueOrDefault("Intelligence", 0),
                "🎯 Willpower", character.BaseWillpower, character.Willpower, racialModifiers.GetValueOrDefault("Willpower", 0)),
            true)
            .AddField("DERIVED",
                FormatDerivedStats(character),
                true)
            .AddField("CONDITION MONITORS",
                FormatConditionMonitors(character),
                false)
            .AddField("RESOURCES",
                $"🌟 Karma: {character.Karma}\n💰 Nuyen: {character.Nuyen:N0}¥",
                true);

        if (character.Skills?.Count > 0)
        {
            builder.AddField("SKILLS", FormatSkills(character.Skills), false);
        }

        if (character.Cyberware?.Count > 0)
        {
            builder.AddField("CYBERWARE/BIOWARE", FormatCyberware(character.Cyberware), false);
        }

        // SR3 COMPLIANCE: Display selected gear
        if (character.Gear?.Count > 0)
        {
            builder.AddField("GEAR & EQUIPMENT", FormatGear(character.Gear), false);
        }

        return builder.Build();
    }
    
    /// <summary>
    /// SR3 COMPLIANCE: Format attributes showing base → final with modifier indicator
    /// </summary>
    private static string FormatAttributesWithModifiers(
        string label1, int base1, int final1, int mod1,
        string label2, int base2, int final2, int mod2,
        string label3, int base3, int final3, int mod3)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine(FormatAttributeWithModifier(label1, base1, final1, mod1));
        sb.AppendLine(FormatAttributeWithModifier(label2, base2, final2, mod2));
        sb.Append(FormatAttributeWithModifier(label3, base3, final3, mod3));
        
        return sb.ToString();
    }
    
    /// <summary>
    /// SR3 COMPLIANCE: Format single attribute with base → final and modifier indicator
    /// </summary>
    private static string FormatAttributeWithModifier(string label, int baseVal, int finalVal, int modifier)
    {
        if (modifier == 0)
        {
            // No modifier, just show final value
            return $"{label}: **{finalVal}**";
        }
        else
        {
            // Show base → final with modifier
            var modStr = modifier > 0 ? $"+{modifier}" : modifier.ToString();
            return $"{label}: {baseVal} → **{finalVal}** ({modStr})";
        }
    }

    private static string FormatAttributes(string label1, int value1, string label2, int value2, string label3, int value3)
    {
        return $"{label1}: {value1}\n{label2}: {value2}\n{label3}: {value3}";
    }

    private static string FormatDerivedStats(ShadowrunCharacter character)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"⚡ Reaction: {character.Reaction}");
        sb.AppendLine($"✨ Essence: {character.Essence / 100m:F2}");
        
        if (character.Magic > 0)
        {
            sb.AppendLine($"🔮 Magic: {character.Magic}");
        }
        
        return sb.ToString();
    }

    private static string FormatConditionMonitors(ShadowrunCharacter character)
    {
        var physicalBoxes = new string('□', character.PhysicalConditionMonitor);
        var stunBoxes = new string('□', character.StunConditionMonitor);
        
        return $"❤️ Physical: {physicalBoxes} ({character.PhysicalDamage}/{character.PhysicalConditionMonitor})\n" +
               $"💫 Stun: {stunBoxes} ({character.StunDamage}/{character.StunConditionMonitor})";
    }

    private static string FormatSkills(ICollection<CharacterSkill> skills)
    {
        var sb = new StringBuilder();
        
        foreach (var s in skills.Take(10))
        {
            sb.Append($"• {s.SkillName}: {s.Rating}");
            if (s.Specialization != null)
            {
                sb.Append($" ({s.Specialization})");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string FormatCyberware(ICollection<CharacterCyberware> cyberware)
    {
        var sb = new StringBuilder();
        
        foreach (var c in cyberware)
        {
            sb.AppendLine($"• {c.Name} (Rating {c.Rating}) - {c.EssenceCost:F2} essence");
        }

        return sb.ToString();
    }

    private static string FormatGear(ICollection<CharacterGear> gear)
    {
        var sb = new StringBuilder();
        
        // Group gear by category for better display
        var gearByCategory = gear.GroupBy(g => g.Category);
        
        foreach (var group in gearByCategory)
        {
            sb.AppendLine($"**{group.Key}:**");
            foreach (var item in group.Take(5)) // Limit to 5 items per category
            {
                var equipped = item.IsEquipped ? " ✓" : "";
                sb.AppendLine($"  • {item.Name} x{item.Quantity}{equipped} - {item.Value:N0}¥");
            }
            
            if (group.Count() > 5)
            {
                sb.AppendLine($"  _... and {group.Count() - 5} more_");
            }
        }

        return sb.ToString();
    }

    // ============== GEAR COMMANDS ==============

    /// <summary>
    /// List gear by category
    /// </summary>
    public async Task ListGearAsync(SocketSlashCommand command)
    {
        await LogCommandExecutionAsync(command, "List Gear");

        try
        {
            var options = command.Data.Options.First().Options.ToList();
            var category = options.FirstOrDefault(o => o.Name == "category")?.Value?.ToString();

            var gearService = new GearSelectionService();
            var categories = gearService.GetGearCategories();

            if (string.IsNullOrWhiteSpace(category))
            {
                // Show all categories
                var embed = new EmbedBuilder()
                    .WithTitle("📦 Gear Categories")
                    .WithColor(Config.Bot.DefaultColor)
                    .WithDescription("Use `/gear list <category>` to see items in a category\n\n" +
                        string.Join("\n", categories.Select(c => $"• **{c}**")));

                await command.RespondAsync(embed: embed.Build());
                return;
            }

            // Show gear in specific category
            var gearItems = await gearService.GetGearByCategoryAsync(category);

            if (gearItems.Count == 0)
            {
                await command.RespondAsync($"⚠️ No gear found in category '{category}'", ephemeral: true);
                return;
            }

            var gearEmbed = new EmbedBuilder()
                .WithTitle($"📦 {category} Gear")
                .WithColor(Config.Bot.DefaultColor);

            foreach (var item in gearItems.Take(10))
            {
                var stats = string.Join(" | ", item.Stats.Select(s => $"{s.Key}: {s.Value}"));
                var essence = item.EssenceCost > 0 ? $" | Essence: {item.EssenceCost:F1}" : "";
                var legal = item.IsLegal ? "" : " ⚠️";

                gearEmbed.AddField(
                    $"{item.Name}{legal}",
                    $"**ID:** {item.Id}\n**Cost:** {item.Cost:N0}¥{essence}\n**Stats:** {stats}\n**Description:** {item.Description ?? "N/A"}",
                    inline: false);
            }

            if (gearItems.Count > 10)
            {
                gearEmbed.WithFooter($"Showing 10 of {gearItems.Count} items");
            }

            await command.RespondAsync(embed: gearEmbed.Build());
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(command, ex, "ListGear");
        }
    }

    /// <summary>
    /// Add gear to character
    /// </summary>
    public async Task AddGearAsync(SocketSlashCommand command)
    {
        await LogCommandExecutionAsync(command, "Add Gear");

        try
        {
            var options = command.Data.Options.First().Options.ToList();
            var characterName = options.First(o => o.Name == "character").Value.ToString();
            var gearId = options.First(o => o.Name == "gear").Value.ToString();
            var quantity = options.FirstOrDefault(o => o.Name == "quantity")?.Value as int? ?? 1;

            var character = await Database.GetCharacterByNameAsync(command.User.Id, characterName!);
            
            if (character == null)
            {
                await command.RespondAsync($"❌ Character '{characterName}' not found.", ephemeral: true);
                return;
            }

            var gearService = new GearSelectionService();
            var (success, message) = await gearService.AddGearToCharacterAsync(character, gearId!, quantity);

            if (!success)
            {
                await command.RespondAsync($"⚠️ {message}", ephemeral: true);
                return;
            }

            // Update character in database
            await Database.UpdateCharacterAsync(character);

            var embed = new EmbedBuilder()
                .WithTitle("✅ Gear Added")
                .WithColor(Config.Bot.DefaultColor)
                .WithDescription(message)
                .AddField("Remaining Nuyen", $"{character.Nuyen:N0}¥", true);

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(command, ex, "AddGear");
        }
    }

    /// <summary>
    /// Remove gear from character
    /// </summary>
    public async Task RemoveGearAsync(SocketSlashCommand command)
    {
        await LogCommandExecutionAsync(command, "Remove Gear");

        try
        {
            var options = command.Data.Options.First().Options.ToList();
            var characterName = options.First(o => o.Name == "character").Value.ToString();
            var gearId = options.First(o => o.Name == "gear").Value.ToString();

            var character = await Database.GetCharacterByNameAsync(command.User.Id, characterName!);
            
            if (character == null)
            {
                await command.RespondAsync($"❌ Character '{characterName}' not found.", ephemeral: true);
                return;
            }

            var gearService = new GearSelectionService();
            var (success, message) = await gearService.RemoveGearFromCharacterAsync(character, gearId!);

            if (!success)
            {
                await command.RespondAsync($"⚠️ {message}", ephemeral: true);
                return;
            }

            // Update character in database
            await Database.UpdateCharacterAsync(character);

            var embed = new EmbedBuilder()
                .WithTitle("✅ Gear Removed")
                .WithColor(Config.Bot.DefaultColor)
                .WithDescription(message)
                .AddField("Remaining Nuyen", $"{character.Nuyen:N0}¥", true);

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(command, ex, "RemoveGear");
        }
    }

    /// <summary>
    /// Pre-load gear for archetype or priority
    /// </summary>
    public async Task PreLoadGearAsync(SocketSlashCommand command)
    {
        await LogCommandExecutionAsync(command, "Pre-Load Gear");

        try
        {
            var options = command.Data.Options.First().Options.ToList();
            var characterName = options.First(o => o.Name == "character").Value.ToString();
            var loadType = options.First(o => o.Name == "type").Value.ToString();
            var loadValue = options.First(o => o.Name == "value").Value.ToString();

            var character = await Database.GetCharacterByNameAsync(command.User.Id, characterName!);
            
            if (character == null)
            {
                await command.RespondAsync($"❌ Character '{characterName}' not found.", ephemeral: true);
                return;
            }

            var gearService = new GearSelectionService();
            List<GearDatabase.GearItem> gearItems;

            if (loadType?.ToLowerInvariant() == "archetype")
            {
                gearItems = await gearService.GetPreLoadByArchetypeAsync(loadValue!);
            }
            else if (loadType?.ToLowerInvariant() == "priority")
            {
                gearItems = await gearService.GetPreLoadByPriorityAsync(loadValue!);
            }
            else
            {
                await command.RespondAsync($"⚠️ Invalid load type '{loadType}'. Use 'archetype' or 'priority'.", ephemeral: true);
                return;
            }

            if (gearItems.Count == 0)
            {
                await command.RespondAsync($"⚠️ No pre-load gear found for {loadType} '{loadValue}'", ephemeral: true);
                return;
            }

            // Calculate total cost
            var totalCost = gearItems.Sum(g => g.Cost);
            
            if (totalCost > character.Nuyen)
            {
                await command.RespondAsync(
                    $"⚠️ Cannot afford pre-load gear (Cost: {totalCost:N0}¥, Available: {character.Nuyen:N0}¥)",
                    ephemeral: true);
                return;
            }

            // Add all gear items
            var addedCount = 0;
            foreach (var gearItem in gearItems)
            {
                var (success, _) = await gearService.AddGearToCharacterAsync(character, gearItem.Id);
                if (success)
                    addedCount++;
            }

            // Update character in database
            await Database.UpdateCharacterAsync(character);

            var embed = new EmbedBuilder()
                .WithTitle("✅ Pre-Load Gear Added")
                .WithColor(Config.Bot.DefaultColor)
                .WithDescription($"Added {addedCount} gear items for {loadType} '{loadValue}'")
                .AddField("Total Cost", $"{totalCost:N0}¥", true)
                .AddField("Remaining Nuyen", $"{character.Nuyen:N0}¥", true);

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(command, ex, "PreLoadGear");
        }
    }

    /// <summary>
    /// View character's gear inventory
    /// </summary>
    public async Task ViewGearAsync(SocketSlashCommand command)
    {
        await LogCommandExecutionAsync(command, "View Gear");

        try
        {
            var options = command.Data.Options.First().Options.ToList();
            var characterName = options.First(o => o.Name == "character").Value.ToString();

            var character = await Database.GetCharacterByNameAsync(command.User.Id, characterName!);
            
            if (character == null)
            {
                await command.RespondAsync($"❌ Character '{characterName}' not found.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"🎒 {character.Name}'s Gear")
                .WithColor(Config.Bot.DefaultColor);

            // Group gear by category
            var gearByCategory = character.Gear.GroupBy(g => g.Category);
            
            foreach (var group in gearByCategory)
            {
                var gearList = new StringBuilder();
                foreach (var item in group)
                {
                    gearList.AppendLine($"• **{item.Name}** x{item.Quantity} - {item.Value:N0}¥");
                    if (!string.IsNullOrEmpty(item.Description))
                        gearList.AppendLine($"  _{item.Description}_");
                }

                embed.AddField(group.Key, gearList.ToString(), inline: false);
            }

            // Add cyberware/bioware if present
            if (character.Cyberware?.Count > 0)
            {
                var cyberwareList = new StringBuilder();
                foreach (var cyber in character.Cyberware)
                {
                    cyberwareList.AppendLine($"• **{cyber.Name}** - {cyber.EssenceCost:F2} essence");
                }
                embed.AddField("Cyberware/Bioware", cyberwareList.ToString(), inline: false);
            }

            if (character.Gear.Count == 0 && (character.Cyberware?.Count ?? 0) == 0)
            {
                embed.WithDescription("No gear equipped");
            }

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(command, ex, "ViewGear");
        }
    }
}
