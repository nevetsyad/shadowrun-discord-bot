using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Commands;

/// <summary>
/// Discord commands for the Interactive Storytelling System
/// </summary>
[ModuleLifespan(ModuleLifespan.Transient)]
public class InteractiveStoryCommands : BaseCommandModule
{
    private readonly InteractiveStoryService _storyService;
    private readonly GameSessionService _sessionService;
    private readonly NarrativeContextService _narrativeService;

    public InteractiveStoryCommands(
        InteractiveStoryService storyService,
        GameSessionService sessionService,
        NarrativeContextService narrativeService)
    {
        _storyService = storyService;
        _sessionService = sessionService;
        _narrativeService = narrativeService;
    }

    #region Roleplay Commands

    [Command("roleplay")]
    [Aliases("rp", "emote", "me")]
    [Description("Perform a character action or narration")]
    [RequireActiveSession]
    public async Task RoleplayCommand(CommandContext ctx, [RemainingText] string action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            await ctx.RespondAsync("❌ Please describe your action. Example: `/roleplay carefully opens the door`");
            return;
        }

        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/roleplay {action}");

        await SendStoryResponseAsync(ctx, response);
    }

    #endregion

    #region Skill Check Commands

    [Command("check")]
    [Aliases("roll", "test")]
    [Description("Make a skill check using Shadowrun 3e rules")]
    [RequireActiveSession]
    public async Task CheckCommand(CommandContext ctx, [RemainingText] string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            await ctx.RespondAsync("❌ Please specify a skill to check. Example: `/check stealth`");
            return;
        }

        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/check {skillName}");

        await SendStoryResponseAsync(ctx, response);
    }

    [Command("checkcombat")]
    [Aliases("combat")]
    [Description("Make a combat skill check")]
    [RequireActiveSession]
    public async Task CheckCombatCommand(CommandContext ctx, [RemainingText] string skillName)
    {
        var combatSkills = new[] { "firearms", "edged weapons", "clubs", "unarmed combat", "thrown weapons", "assault rifles", "shotguns", "heavy weapons" };
        
        if (string.IsNullOrWhiteSpace(skillName) || !combatSkills.Any(s => skillName.ToLower().Contains(s)))
        {
            await ctx.RespondAsync($"❌ Combat skills: {string.Join(", ", combatSkills)}");
            return;
        }

        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/check {skillName}");

        await SendStoryResponseAsync(ctx, response);
    }

    [Command("checksocial")]
    [Aliases("social")]
    [Description("Make a social skill check")]
    [RequireActiveSession]
    public async Task CheckSocialCommand(CommandContext ctx, [RemainingText] string skillName)
    {
        var socialSkills = new[] { "etiquette", "negotiation", "leadership", "interrogation", "con" };

        if (string.IsNullOrWhiteSpace(skillName) || !socialSkills.Any(s => skillName.ToLower().Contains(s)))
        {
            await ctx.RespondAsync($"❌ Social skills: {string.Join(", ", socialSkills)}");
            return;
        }

        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/check {skillName}");

        await SendStoryResponseAsync(ctx, response);
    }

    [Command("checkmagic")]
    [Aliases("magic")]
    [Description("Make a magic skill check")]
    [RequireActiveSession]
    public async Task CheckMagicCommand(CommandContext ctx, [RemainingText] string skillName)
    {
        var magicSkills = new[] { "sorcery", "conjuring", "enchanting", "centering" };

        if (string.IsNullOrWhiteSpace(skillName) || !magicSkills.Any(s => skillName.ToLower().Contains(s)))
        {
            await ctx.RespondAsync($"❌ Magic skills: {string.Join(", ", magicSkills)}");
            return;
        }

        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/check {skillName}");

        await SendStoryResponseAsync(ctx, response);
    }

    #endregion

    #region Investigation Commands

    [Command("investigate")]
    [Aliases("examine", "look")]
    [Description("Investigate a specific target or area")]
    [RequireActiveSession]
    public async Task InvestigateCommand(CommandContext ctx, [RemainingText] string target)
    {
        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/investigate {target ?? "area"}");

        await SendStoryResponseAsync(ctx, response);
    }

    [Command("search")]
    [Description("Search the current area thoroughly")]
    [RequireActiveSession]
    public async Task SearchCommand(CommandContext ctx, [RemainingText] string query = "")
    {
        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/search {query}".TrimEnd());

        await SendStoryResponseAsync(ctx, response);
    }

    [Command("listen")]
    [Description("Listen carefully for sounds")]
    [RequireActiveSession]
    public async Task ListenCommand(CommandContext ctx)
    {
        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            "/listen");

        await SendStoryResponseAsync(ctx, response);
    }

    #endregion

    #region Interaction Commands

    [Command("interact")]
    [Aliases("use")]
    [Description("Interact with an object in the environment")]
    [RequireActiveSession]
    public async Task InteractCommand(CommandContext ctx, [RemainingText] string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            await ctx.RespondAsync("❌ Please specify what to interact with. Example: `/interact door`");
            return;
        }

        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/interact {objectName}");

        await SendStoryResponseAsync(ctx, response);
    }

    [Command("useitem")]
    [Aliases("item")]
    [Description("Use an item from your inventory")]
    [RequireActiveSession]
    public async Task UseItemCommand(CommandContext ctx, [RemainingText] string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            await ctx.RespondAsync("❌ Please specify an item to use. Example: `/useitem medkit`");
            return;
        }

        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/use {itemName}");

        await SendStoryResponseAsync(ctx, response);
    }

    #endregion

    #region NPC Dialogue Commands

    [Command("talk")]
    [Aliases("speak")]
    [Description("Start a conversation with an NPC")]
    [RequireActiveSession]
    public async Task TalkCommand(CommandContext ctx, string npcName, [RemainingText] string message = "")
    {
        if (string.IsNullOrWhiteSpace(npcName))
        {
            await ctx.RespondAsync("❌ Please specify who to talk to. Example: `/talk Johnson`");
            return;
        }

        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/talk {npcName} {message}".TrimEnd());

        await SendStoryResponseAsync(ctx, response);
    }

    [Command("dialogue")]
    [Aliases("say")]
    [Description("Say something specific to an NPC")]
    [RequireActiveSession]
    public async Task DialogueCommand(CommandContext ctx, string npcName, [RemainingText] string message)
    {
        if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(message))
        {
            await ctx.RespondAsync("❌ Please specify both NPC and message. Example: `/dialogue Johnson I'll take the job`");
            return;
        }

        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            $"/dialogue {npcName} {message}");

        await SendStoryResponseAsync(ctx, response);
    }

    [Command("relationships")]
    [Aliases("contacts", "npcs")]
    [Description("Show your relationships with NPCs")]
    [RequireActiveSession]
    public async Task RelationshipsCommand(CommandContext ctx)
    {
        var session = await _sessionService.GetActiveSessionAsync(ctx.Channel.Id);
        if (session == null)
        {
            await ctx.RespondAsync("❌ No active session.");
            return;
        }

        var relationships = await _narrativeService.GetAllNPCRelationshipsAsync(ctx.Channel.Id);

        if (!relationships.Any())
        {
            await ctx.RespondAsync("📋 You haven't established any NPC relationships yet.");
            return;
        }

        var embed = new DiscordEmbedBuilder
        {
            Title = "📋 NPC Relationships",
            Color = DiscordColor.Blue
        };

        foreach (var npc in relationships.Take(10))
        {
            var attitudeEmoji = npc.Attitude switch
            {
                >= 7 => "💚",
                >= 4 => "😊",
                >= 1 => "😐",
                0 => "🤔",
                >= -3 => "😒",
                _ => "😠"
            };

            var trustEmoji = npc.TrustLevel switch
            {
                >= 8 => "🤝",
                >= 5 => "👍",
                >= 3 => "👌",
                _ => "❓"
            };

            var status = npc.IsActive ? "" : " ~~(Inactive)~~";

            embed.AddField(
                $"{attitudeEmoji} {trustEmoji} {npc.NPCName}{status}",
                $"**Role:** {npc.NPCRole ?? "Unknown"}\n" +
                $"**Org:** {npc.Organization ?? "Independent"}\n" +
                $"**Attitude:** {npc.Attitude}/10 | **Trust:** {npc.TrustLevel}/10",
                inline: true
            );
        }

        await ctx.RespondAsync(embed: embed);
    }

    #endregion

    #region Scene Commands

    [Command("describe")]
    [Aliases("scene", "look")]
    [Description("Get a description of the current scene")]
    [RequireActiveSession]
    public async Task DescribeCommand(CommandContext ctx)
    {
        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            "/describe");

        await SendStoryResponseAsync(ctx, response);
    }

    [Command("story")]
    [Aliases("history", "summary")]
    [Description("Get a summary of the story so far")]
    [RequireActiveSession]
    public async Task StoryCommand(CommandContext ctx)
    {
        var summary = await _narrativeService.GenerateStorySummaryAsync(ctx.Channel.Id, 15);
        await ctx.RespondAsync(summary);
    }

    #endregion

    #region Encounter Commands (GM Only)

    [Command("encounter")]
    [Description("Generate an on-the-fly encounter (GM only)")]
    [RequireOwnerOrGM]
    public async Task EncounterCommand(CommandContext ctx, string type = "random")
    {
        var trigger = new EncounterTrigger
        {
            Type = type.ToLower(),
            Count = 1
        };

        var encounter = await _storyService.GenerateEncounterAsync(ctx.Channel.Id, trigger);

        var embed = new DiscordEmbedBuilder
        {
            Title = $"⚔️ Encounter Generated: {encounter.EncounterType}",
            Description = encounter.Description,
            Color = DiscordColor.Red
        };

        embed.AddField("Difficulty", $"⭐ {encounter.Difficulty}/6", true);
        embed.AddField("Type", encounter.EncounterType.ToString(), true);

        if (encounter.Enemies.Any())
        {
            var enemyList = string.Join("\n", encounter.Enemies.Select(e => 
                $"• **{e.Name}** ({e.Type}) - Body: {e.Body}, Weapon: {e.PrimaryWeapon}"));
            embed.AddField("Enemies", enemyList);
        }

        if (encounter.EnvironmentalFactors.Any())
        {
            embed.AddField("Environment", string.Join("\n", encounter.EnvironmentalFactors.Select(f => $"• {f}")));
        }

        if (encounter.PotentialOutcomes.Any())
        {
            embed.AddField("Possible Outcomes", string.Join("\n", encounter.PotentialOutcomes.Select(o => $"• {o}")));
        }

        await ctx.RespondAsync(embed: embed);
    }

    #endregion

    #region Help Commands

    [Command("storyhelp")]
    [Aliases("shelp", "gamehelp")]
    [Description("Show help for interactive story commands")]
    public async Task StoryHelpCommand(CommandContext ctx)
    {
        var response = await _storyService.ProcessPlayerInputAsync(
            ctx.Channel.Id,
            ctx.User.Id,
            "/help");

        var embed = new DiscordEmbedBuilder
        {
            Title = "📖 Interactive Story Commands",
            Description = response.Message,
            Color = DiscordColor.Blurple
        };

        await ctx.RespondAsync(embed: embed);
    }

    [Command("skills")]
    [Description("Show available skills for checks")]
    public async Task SkillsCommand(CommandContext ctx)
    {
        var embed = new DiscordEmbedBuilder
        {
            Title = "🎯 Available Skills",
            Color = DiscordColor.Blurple
        };

        embed.AddField("⚔️ Combat", "firearms, edged weapons, clubs, unarmed combat, thrown weapons, assault rifles, shotguns, heavy weapons");
        embed.AddField("🏃 Physical", "athletics, stealth, driving, pilot aircraft, biotech");
        embed.AddField("💬 Social", "etiquette, negotiation, leadership, interrogation, con");
        embed.AddField("🔧 Technical", "computers, electronics, demolitions, building");
        embed.AddField("✨ Magic", "sorcery, conjuring, enchanting, centering");
        embed.AddField("📚 Knowledge", "lore, knowledge, investigation, perception");

        embed.AddField("Usage", "Use `/check [skill]` to make a skill check.\nExample: `/check stealth`");

        await ctx.RespondAsync(embed: embed);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Send story response with appropriate formatting
    /// </summary>
    private async Task SendStoryResponseAsync(CommandContext ctx, StoryResponse response)
    {
        if (!response.Success)
        {
            await ctx.RespondAsync($"❌ {response.Message}");
            return;
        }

        var embed = new DiscordEmbedBuilder();

        switch (response.ResponseType)
        {
            case ResponseType.SkillCheck:
                embed = BuildSkillCheckEmbed(response);
                break;

            case ResponseType.Dialogue:
                embed = BuildDialogueEmbed(response);
                break;

            case ResponseType.Investigation:
            case ResponseType.Search:
            case ResponseType.Listen:
                embed = BuildInvestigationEmbed(response);
                break;

            case ResponseType.Interaction:
                embed = BuildInteractionEmbed(response);
                break;

            case ResponseType.Description:
                embed = BuildDescriptionEmbed(response);
                break;

            default:
                embed = BuildNarrativeEmbed(response);
                break;
        }

        await ctx.RespondAsync(embed: embed);

        // Handle follow-up if needed
        if (response.RequiresFollowUp && response.Triggers.Any())
        {
            await HandleTriggersAsync(ctx, response.Triggers);
        }
    }

    private DiscordEmbedBuilder BuildSkillCheckEmbed(StoryResponse response)
    {
        var result = response.SkillCheckResult!;
        var color = result.Successes switch
        {
            0 when result.CriticalGlitch => DiscordColor.DarkRed,
            0 when result.Glitch => DiscordColor.Orange,
            0 => DiscordColor.Red,
            >= 4 => DiscordColor.Green,
            >= 2 => DiscordColor.Blue,
            _ => DiscordColor.LightGray
        };

        var embed = new DiscordEmbedBuilder
        {
            Title = $"🎯 Skill Check: {result.SkillName}",
            Description = response.Message,
            Color = color
        };

        var emoji = result.Successes switch
        {
            0 when result.CriticalGlitch => "💀",
            0 => "❌",
            1 => "勉强",
            2 => "✅",
            3 => "⭐",
            _ => "🌟"
        };

        embed.AddField("Result", $"{emoji} **{result.Successes}** success{(result.Successes != 1 ? "es" : "")}", true);
        embed.AddField("Difficulty", result.Difficulty, true);
        embed.AddField("Dice Pool", $"{result.PoolSize} dice (TN {result.TargetNumber})", true);

        if (result.Glitch && !result.CriticalGlitch)
        {
            embed.AddField("⚠️ Glitch", "Something went slightly wrong!");
        }

        return embed;
    }

    private DiscordEmbedBuilder BuildDialogueEmbed(StoryResponse response)
    {
        var dialogue = response.NPCDialogue!;

        var embed = new DiscordEmbedBuilder
        {
            Title = $"💬 Conversation with {dialogue.NPCName}",
            Description = dialogue.Response,
            Color = dialogue.Attitude >= 0 ? DiscordColor.Green : DiscordColor.Red
        };

        if (!string.IsNullOrEmpty(dialogue.Opening))
        {
            embed.AddField("Opening", dialogue.Opening);
        }

        if (!string.IsNullOrEmpty(dialogue.Offer))
        {
            embed.AddField("💭 Offer", $"*{dialogue.Offer}*");
        }

        if (!string.IsNullOrEmpty(dialogue.Warning))
        {
            embed.AddField("⚠️ Warning", $"*{dialogue.Warning}*");
        }

        if (dialogue.FollowUpOptions.Any())
        {
            var options = string.Join("\n", dialogue.FollowUpOptions.Select((o, i) => $"{i + 1}. {o}"));
            embed.AddField("Options", options);
        }

        return embed;
    }

    private DiscordEmbedBuilder BuildInvestigationEmbed(StoryResponse response)
    {
        var icon = response.ResponseType switch
        {
            ResponseType.Listen => "👂",
            ResponseType.Search => "🔍",
            _ => "🔎"
        };

        return new DiscordEmbedBuilder
        {
            Title = $"{icon} {response.ResponseType}",
            Description = response.Message,
            Color = DiscordColor.Purple
        };
    }

    private DiscordEmbedBuilder BuildInteractionEmbed(StoryResponse response)
    {
        return new DiscordEmbedBuilder
        {
            Title = "🤚 Interaction",
            Description = response.Message,
            Color = DiscordColor.Orange
        };
    }

    private DiscordEmbedBuilder BuildDescriptionEmbed(StoryResponse response)
    {
        return new DiscordEmbedBuilder
        {
            Title = "📍 Current Scene",
            Description = response.Message,
            Color = DiscordColor.Teal
        };
    }

    private DiscordEmbedBuilder BuildNarrativeEmbed(StoryResponse response)
    {
        return new DiscordEmbedBuilder
        {
            Description = response.Message,
            Color = DiscordColor.Blurple
        };
    }

    private async Task HandleTriggersAsync(CommandContext ctx, List<StoryTrigger> triggers)
    {
        var combatTrigger = triggers.FirstOrDefault(t => t.Type == "combat");
        if (combatTrigger != null)
        {
            await ctx.RespondAsync($"⚠️ **Combat Triggered!** {combatTrigger.Description}");
        }

        var alarmTrigger = triggers.FirstOrDefault(t => t.Type == "alarm");
        if (alarmTrigger != null)
        {
            await ctx.RespondAsync($"🚨 **Alert!** {alarmTrigger.Description}");
        }
    }

    #endregion
}

#region Attributes

/// <summary>
/// Requires an active game session
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequireActiveSessionAttribute : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        var sessionService = ctx.Services.GetService(typeof(GameSessionService)) as GameSessionService;
        if (sessionService == null) return false;

        var session = await sessionService.GetActiveSessionAsync(ctx.Channel.Id);
        if (session == null)
        {
            if (!help)
            {
                await ctx.RespondAsync("❌ No active game session in this channel. Use `/session start` to begin.");
            }
            return false;
        }

        return true;
    }
}

/// <summary>
/// Requires the user to be the owner or GM
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequireOwnerOrGMAttribute : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        // Check if owner
        var app = await ctx.Client.GetCurrentApplicationAsync();
        if (app.Owners.Any(o => o.Id == ctx.User.Id))
            return true;

        // Check if GM
        var sessionService = ctx.Services.GetService(typeof(GameSessionService)) as GameSessionService;
        if (sessionService != null)
        {
            var session = await sessionService.GetActiveSessionAsync(ctx.Channel.Id);
            if (session != null && session.GameMasterUserId == ctx.User.Id)
                return true;
        }

        if (!help)
        {
            await ctx.RespondAsync("❌ This command is restricted to the bot owner or game master.");
        }

        return false;
    }
}

#endregion
