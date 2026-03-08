using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;
using System.Text.Json;

namespace ShadowrunDiscordBot.Commands;

/// <summary>
/// Character management commands
/// </summary>
public class CharacterCommands : BaseCommandModule
{
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
            var characters = await Database.GetUserCharactersAsync(command.User.Id);

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
            var character = await Database.GetCharacterByNameAsync(command.User.Id, name!);

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
            var character = await Database.GetCharacterByNameAsync(command.User.Id, name!);

            if (character == null)
            {
                await command.RespondAsync($"❌ Character '{name}' not found.", ephemeral: true);
                return;
            }

            // TODO: Add confirmation dialog

            await Database.DeleteCharacterAsync(character.Id);

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
            Karma = 5, // Starting karma
            Nuyen = 5000 // Starting nuyen
        };

        // Apply metatype attribute modifiers
        ApplyMetatypeAttributes(character, metatype);

        // Apply archetype defaults
        ApplyArchetypeDefaults(character, archetype);

        return character;
    }

    private void ApplyMetatypeAttributes(ShadowrunCharacter character, string metatype)
    {
        // Base attributes are already set to 3
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
        switch (archetype.ToLowerInvariant())
        {
            case "mage":
                character.Magic = 6;
                character.Skills.Add(new CharacterSkill { SkillName = "Sorcery", Rating = 6 });
                character.Skills.Add(new CharacterSkill { SkillName = "Conjuring", Rating = 4 });
                break;
            case "shaman":
                character.Magic = 6;
                character.Skills.Add(new CharacterSkill { SkillName = "Sorcery", Rating = 5 });
                character.Skills.Add(new CharacterSkill { SkillName = "Conjuring", Rating = 6 });
                break;
            case "physical adept":
                character.Magic = 6;
                character.Skills.Add(new CharacterSkill { SkillName = "Unarmed Combat", Rating = 6 });
                character.Skills.Add(new CharacterSkill { SkillName = "Athletics", Rating = 5 });
                break;
            case "street samurai":
                character.Skills.Add(new CharacterSkill { SkillName = "Pistols", Rating = 6 });
                character.Skills.Add(new CharacterSkill { SkillName = "Edged Weapons", Rating = 5 });
                character.Skills.Add(new CharacterSkill { SkillName = "Athletics", Rating = 4 });
                break;
            case "decker":
                character.Skills.Add(new CharacterSkill { SkillName = "Computer", Rating = 6 });
                character.Skills.Add(new CharacterSkill { SkillName = "Electronics", Rating = 5 });
                character.Nuyen += 100000; // Extra for cyberdeck
                break;
            case "rigger":
                character.Skills.Add(new CharacterSkill { SkillName = "Vehicle Operation", Rating = 6 });
                character.Skills.Add(new CharacterSkill { SkillName = "Gunnery", Rating = 5 });
                character.Nuyen += 50000; // Extra for vehicle/drone
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
                $"💪 Body: {character.Body}\n" +
                $"🏃 Quickness: {character.Quickness}\n" +
                $"🏋️ Strength: {character.Strength}",
                true)
            .AddField("MENTAL ATTRIBUTES",
                $"💬 Charisma: {character.Charisma}\n" +
                $"🧠 Intelligence: {character.Intelligence}\n" +
                $"💪 Willpower: {character.Willpower}",
                true)
            .AddField("DERIVED",
                $"⚡ Reaction: {character.Reaction}\n" +
                $"✨ Essence: {character.Essence / 100m:F2}\n" +
                (character.Magic > 0 ? $"🔮 Magic: {character.Magic}\n" : ""),
                true)
            .AddField("CONDITION MONITORS",
                $"❤️ Physical: {new string('□', character.PhysicalConditionMonitor)} ({character.PhysicalDamage}/{character.PhysicalConditionMonitor})\n" +
                $"💫 Stun: {new string('□', character.StunConditionMonitor)} ({character.StunDamage}/{character.StunConditionMonitor})",
                false)
            .AddField("RESOURCES",
                $"🌟 Karma: {character.Karma}\n" +
                $"💰 Nuyen: {character.Nuyen:N0}¥",
                true);

        if (character.Skills?.Count > 0)
        {
            var skillsText = string.Join("\n", character.Skills.Take(10).Select(s =>
                $"• {s.SkillName}: {s.Rating}" + (s.Specialization != null ? $" ({s.Specialization})" : "")));

            builder.AddField("SKILLS", skillsText, false);
        }

        if (character.Cyberware?.Count > 0)
        {
            var cyberwareText = string.Join("\n", character.Cyberware.Select(c =>
                $"• {c.Name} (Rating {c.Rating}) - {c.EssenceCost:F2} essence"));

            builder.AddField("CYBERWARE/BIOWARE", cyberwareText, false);
        }

        return builder.Build();
    }
}
