using System.Text;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Commands;

/// <summary>
/// Character management commands
/// </summary>
public class CharacterCommands : BaseCommandModule
{
    // Constants for character creation
    private const int StartingKarma = 5;
    private const int StartingNuyen = 5000;
    private const int DeckerBonusNuyen = 100000;
    private const int RiggerBonusNuyen = 50000;
    private const int DefaultAttribute = 3;
    private const int DefaultMagic = 6;

    // FIX: MED-002 - Define valid metatypes and archetypes for validation
    private static readonly HashSet<string> ValidMetatypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Human", "Elf", "Dwarf", "Ork", "Troll"
    };

    private static readonly HashSet<string> ValidArchetypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Mage", "Shaman", "Physical Adept", "Street Samurai", "Decker", "Rigger", "Face", "Samurai"
    };

    // FIX: MED-002 - Attribute bounds
    private const int MinAttributeValue = 1;
    private const int MaxAttributeValue = 10;

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
            var archetype = options.First(o => o.Name == "archetype").Value.ToString();

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
            if (!ValidMetatypes.Contains(metatype!))
            {
                await command.RespondAsync(
                    $"⚠️ Invalid metatype '{metatype}'. Valid options are: {string.Join(", ", ValidMetatypes)}",
                    ephemeral: true);
                return;
            }

            // FIX: MED-002 - Validate archetype
            if (!ValidArchetypes.Contains(archetype!))
            {
                await command.RespondAsync(
                    $"⚠️ Invalid archetype '{archetype}'. Valid options are: {string.Join(", ", ValidArchetypes)}",
                    ephemeral: true);
                return;
            }

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

            // Create character with archetype defaults
            var character = CreateCharacterWithArchetype(command.User.Id, name!, metatype!, archetype!);

            await Database.CreateCharacterAsync(character);

            var embed = BuildCharacterEmbed(character, "✨ Character Created");

            await command.RespondAsync(embed: embed);
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

    private ShadowrunCharacter CreateCharacterWithArchetype(ulong userId, string name, string metatype, string archetype)
    {
        var character = new ShadowrunCharacter
        {
            DiscordUserId = userId,
            Name = name,
            Metatype = metatype,
            Archetype = archetype,
            Karma = StartingKarma,
            Nuyen = StartingNuyen
        };

        // Apply metatype attribute modifiers
        ApplyMetatypeAttributes(character, metatype);

        // Apply archetype defaults
        ApplyArchetypeDefaults(character, archetype);

        return character;
    }

    private void ApplyMetatypeAttributes(ShadowrunCharacter character, string metatype)
    {
        // Base attributes are already set to DefaultAttribute (3)
        switch (metatype.ToLowerInvariant())
        {
            case "elf":
                character.Quickness += 1;
                character.Charisma += 2;
                break;
            case "dwarf":
                character.Body += 2;
                character.Strength += 2;
                character.Charisma -= 1;
                break;
            case "ork":
                character.Body += 3;
                character.Strength += 2;
                character.Charisma -= 1;
                character.Intelligence -= 1;
                break;
            case "troll":
                character.Body += 5;
                character.Strength += 4;
                character.Quickness -= 1;
                character.Charisma -= 2;
                character.Intelligence -= 2;
                break;
            // Human is default (no modifiers)
        }
    }

    private void ApplyArchetypeDefaults(ShadowrunCharacter character, string archetype)
    {
        var arch = archetype.ToLowerInvariant();
        
        if (arch is "mage" or "shaman" or "physical adept")
        {
            character.Magic = DefaultMagic;
        }

        switch (arch)
        {
            case "mage":
                character.Skills.Add(new CharacterSkill { SkillName = "Sorcery", Rating = DefaultMagic });
                character.Skills.Add(new CharacterSkill { SkillName = "Conjuring", Rating = 4 });
                break;
            case "shaman":
                character.Skills.Add(new CharacterSkill { SkillName = "Sorcery", Rating = 5 });
                character.Skills.Add(new CharacterSkill { SkillName = "Conjuring", Rating = DefaultMagic });
                break;
            case "physical adept":
                character.Skills.Add(new CharacterSkill { SkillName = "Unarmed Combat", Rating = DefaultMagic });
                character.Skills.Add(new CharacterSkill { SkillName = "Athletics", Rating = 5 });
                break;
            case "street samurai":
                character.Skills.Add(new CharacterSkill { SkillName = "Pistols", Rating = DefaultMagic });
                character.Skills.Add(new CharacterSkill { SkillName = "Edged Weapons", Rating = 5 });
                character.Skills.Add(new CharacterSkill { SkillName = "Athletics", Rating = 4 });
                break;
            case "decker":
                character.Skills.Add(new CharacterSkill { SkillName = "Computer", Rating = DefaultMagic });
                character.Skills.Add(new CharacterSkill { SkillName = "Electronics", Rating = 5 });
                character.Nuyen += DeckerBonusNuyen;
                break;
            case "rigger":
                character.Skills.Add(new CharacterSkill { SkillName = "Vehicle Operation", Rating = DefaultMagic });
                character.Skills.Add(new CharacterSkill { SkillName = "Gunnery", Rating = 5 });
                character.Nuyen += RiggerBonusNuyen;
                break;
        }
    }

    private Embed BuildCharacterEmbed(ShadowrunCharacter character, string title)
    {
        return new EmbedBuilder()
            .WithTitle(title)
            .WithColor(Config.Bot.DefaultColor)
            .AddField("Name", character.Name, true)
            .AddField("Metatype", character.Metatype, true)
            .AddField("Archetype", character.Archetype, true)
            .AddField("Attributes",
                $"Body: {character.Body} | Quickness: {character.Quickness} | Strength: {character.Strength}\n" +
                $"Charisma: {character.Charisma} | Intelligence: {character.Intelligence} | Willpower: {character.Willpower}",
                false)
            .AddField("Derived",
                $"Reaction: {character.Reaction} | Essence: {character.Essence / 100m:F2}",
                false)
            .AddField("Resources",
                $"Karma: {character.Karma} | Nuyen: {character.Nuyen:N0}¥",
                false)
            .Build();
    }

    private Embed BuildCharacterSheetEmbed(ShadowrunCharacter character)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"📜 {character.Name}")
            .WithColor(Config.Bot.DefaultColor)
            .WithDescription($"**{character.Metatype} {character.Archetype}**")
            .AddField("PHYSICAL ATTRIBUTES",
                FormatAttributes("💪 Body", character.Body, "🏃 Quickness", character.Quickness, "🏋️ Strength", character.Strength),
                true)
            .AddField("MENTAL ATTRIBUTES",
                FormatAttributes("💬 Charisma", character.Charisma, "🧠 Intelligence", character.Intelligence, "💪 Willpower", character.Willpower),
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

        return builder.Build();
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
}
