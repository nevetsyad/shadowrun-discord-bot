using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Commands;

/// <summary>
/// Commands for the Dynamic Content Engine (Phase 5)
/// </summary>
public class DynamicContentCommands
{
    private readonly DynamicContentEngine _contentEngine;
    private readonly GameSessionService _sessionService;
    private readonly ContentDatabaseService _contentDb;
    private readonly ILogger<DynamicContentCommands> _logger;

    public DynamicContentCommands(
        DynamicContentEngine contentEngine,
        GameSessionService sessionService,
        ContentDatabaseService contentDb,
        ILogger<DynamicContentCommands> logger)
    {
        _contentEngine = contentEngine;
        _sessionService = sessionService;
        _contentDb = contentDb;
        _logger = logger;
    }

    /// <summary>
    /// Register all dynamic content commands
    /// </summary>
    public static async Task RegisterCommandsAsync(DiscordSocketClient client, ulong guildId)
    {
        var commands = new List<ApplicationCommandProperties>
        {
            // /difficulty
            new SlashCommandBuilder()
                .WithName("difficulty")
                .WithDescription("View or adjust current difficulty level")
                .AddOption("adjust", ApplicationCommandOptionType.SubCommand, 
                    "Manually adjust difficulty", 
                    opt => opt
                        .AddOption("level", ApplicationCommandOptionType.Integer, "New difficulty level (1-10)", true))
                .Build(),

            // /campaign
            new SlashCommandBuilder()
                .WithName("campaign")
                .WithDescription("Manage campaign arcs")
                .AddOption("arc", ApplicationCommandOptionType.SubCommand,
                    "View or manage campaign arcs",
                    opt => opt
                        .AddOption("action", ApplicationCommandOptionType.String, "Action (view/start/switch)", false)
                        .AddOption("name", ApplicationCommandOptionType.String, "Arc name", false)
                        .AddOption("description", ApplicationCommandOptionType.String, "Arc description", false))
                .Build(),

            // /content
            new SlashCommandBuilder()
                .WithName("content")
                .WithDescription("Manage dynamic content")
                .AddOption("regenerate", ApplicationCommandOptionType.SubCommand,
                    "Regenerate content with new parameters",
                    opt => opt
                        .AddOption("type", ApplicationCommandOptionType.String, "Content type (mission/npc/plothook/encounter)", true)
                        .AddOption("difficulty", ApplicationCommandOptionType.Integer, "Override difficulty (1-10)", false))
                .Build(),

            // /learning
            new SlashCommandBuilder()
                .WithName("learning")
                .WithDescription("View AI learning status")
                .AddOption("status", ApplicationCommandOptionType.SubCommand,
                    "View current learning status")
                .AddOption("reset", ApplicationCommandOptionType.SubCommand,
                    "Reset learning data for this session")
                .Build(),

            // /story
            new SlashCommandBuilder()
                .WithName("story")
                .WithDescription("Story evolution commands")
                .AddOption("evolve", ApplicationCommandOptionType.SubCommand,
                    "Evolve current story based on player choices")
                .AddOption("hook", ApplicationCommandOptionType.SubCommand,
                    "Generate a new plot hook",
                    opt => opt
                        .AddOption("type", ApplicationCommandOptionType.String, "Hook type (story/character/world/faction/mystery)", false))
                .Build(),

            // /npc
            new SlashCommandBuilder()
                .WithName("npc")
                .WithDescription("NPC management and learning")
                .AddOption("learn", ApplicationCommandOptionType.SubCommand,
                    "Record NPC learning data",
                    opt => opt
                        .AddOption("name", ApplicationCommandOptionType.String, "NPC name", true)
                        .AddOption("event", ApplicationCommandOptionType.String, "Event type", true)
                        .AddOption("description", ApplicationCommandOptionType.String, "Event description", false))
                .AddOption("profile", ApplicationCommandOptionType.SubCommand,
                    "View NPC learning profile",
                    opt => opt
                        .AddOption("name", ApplicationCommandOptionType.String, "NPC name", true))
                .Build()
        };

        try
        {
            await client.BulkOverwriteGuildCommandAsync(commands.ToArray(), guildId);
            _logger?.LogInformation("Registered Phase 5 dynamic content commands for guild {GuildId}", guildId);
        }
        catch (HttpException ex)
        {
            _logger?.LogError(ex, "Failed to register Phase 5 commands");
        }
    }

    /// <summary>
    /// Handle slash command
    /// </summary>
    public async Task HandleCommandAsync(SocketSlashCommand command)
    {
        var channelId = command.Channel.Id;

        try
        {
            var response = command.Data.Name switch
            {
                "difficulty" => await HandleDifficultyCommand(command),
                "campaign" => await HandleCampaignCommand(command),
                "content" => await HandleContentCommand(command),
                "learning" => await HandleLearningCommand(command),
                "story" => await HandleStoryCommand(command),
                "npc" => await HandleNpcCommand(command),
                _ => "Unknown command"
            };

            await command.RespondAsync(response, ephemeral: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling dynamic content command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    #region Command Handlers

    /// <summary>
    /// Handle /difficulty command
    /// </summary>
    private async Task<string> HandleDifficultyCommand(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First().Name;
        var channelId = command.Channel.Id;

        if (subCommand == "adjust")
        {
            var level = (int)(command.Data.Options.First().Options.First(o => o.Name == "level").Value as long? ?? 5);
            var status = await _contentEngine.AdjustDifficultyAsync(channelId, level, "Manual adjustment by GM");

            return FormatDifficultyStatus(status);
        }

        // View current difficulty
        var currentStatus = await _contentEngine.GetDifficultyAsync(channelId);
        return FormatDifficultyStatus(currentStatus);
    }

    /// <summary>
    /// Format difficulty status for display
    /// </summary>
    private string FormatDifficultyStatus(DifficultyStatus status)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("**📊 Difficulty Status**");
        sb.AppendLine();
        sb.AppendLine($"**Current Difficulty:** {status.CurrentDifficulty}/10");
        sb.AppendLine($"**Performance Score:** {status.PerformanceScore:F1}/100");
        sb.AppendLine($"**Recommendation:** {status.Recommendation}");

        if (status.LastAdjustedAt.HasValue)
        {
            sb.AppendLine($"**Last Adjusted:** {status.LastAdjustedAt:yyyy-MM-dd HH:mm} UTC");
        }

        sb.AppendLine();
        sb.AppendLine("**Performance Metrics:**");
        sb.AppendLine($"• Skill Check Rate: {status.Metrics.SkillCheckSuccessRate:P0}");
        sb.AppendLine($"• Mission Completion: {status.Metrics.MissionCompletionRate:P0}");
        sb.AppendLine($"• Social Interactions: {status.Metrics.PositiveInteractionRate:P0}");
        sb.AppendLine($"• Combat Victories: {status.Metrics.CombatVictoryRate:P0}");

        if (status.AdjustmentHistory.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**Recent Adjustments:**");
            foreach (var adj in status.AdjustmentHistory.Take(5))
            {
                var emoji = adj.IsAutomatic ? "🤖" : "👤";
                sb.AppendLine($"• {emoji} {adj.FromDifficulty} → {adj.ToDifficulty}: {adj.Reason}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Handle /campaign command
    /// </summary>
    private async Task<string> HandleCampaignCommand(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First().Name;
        var channelId = command.Channel.Id;

        if (subCommand == "arc")
        {
            var options = command.Data.Options.First().Options.ToList();
            var action = options.FirstOrDefault(o => o.Name == "action")?.Value as string ?? "view";

            if (action == "start" || action == "switch")
            {
                var name = options.FirstOrDefault(o => o.Name == "name")?.Value as string;
                var description = options.FirstOrDefault(o => o.Name == "description")?.Value as string;

                if (string.IsNullOrEmpty(name))
                    return "Please provide an arc name.";

                var status = await _contentEngine.StartCampaignArcAsync(channelId, name, description ?? "");
                return FormatArcStatus(status);
            }

            // View current arc
            var currentStatus = await _contentEngine.GetCampaignArcStatusAsync(channelId);
            return FormatArcStatus(currentStatus);
        }

        return "Unknown campaign subcommand.";
    }

    /// <summary>
    /// Format campaign arc status for display
    /// </summary>
    private string FormatArcStatus(CampaignArcStatus status)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("**🎯 Campaign Arc Status**");
        sb.AppendLine();
        sb.AppendLine($"**Current Arc:** {status.CurrentArc}");
        sb.AppendLine($"**Progress:** {status.ArcProgress}% ({status.ProgressPercentage:F1}%)");

        if (!string.IsNullOrEmpty(status.ArcDescription))
        {
            sb.AppendLine($"**Description:** {status.ArcDescription}");
        }

        if (status.StartedAt.HasValue)
        {
            sb.AppendLine($"**Started:** {status.StartedAt:yyyy-MM-dd HH:mm} UTC");
        }

        if (status.StoryArcs.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**Story Arcs:**");
            foreach (var arc in status.StoryArcs.Take(5))
            {
                var emoji = arc.IsCompleted ? "✅" : "🔄";
                sb.AppendLine($"• {emoji} {arc.Name}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Handle /content command
    /// </summary>
    private async Task<string> HandleContentCommand(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First().Name;
        var channelId = command.Channel.Id;

        if (subCommand == "regenerate")
        {
            var options = command.Data.Options.First().Options.ToList();
            var typeStr = options.FirstOrDefault(o => o.Name == "type")?.Value as string ?? "mission";
            var difficulty = options.FirstOrDefault(o => o.Name == "difficulty")?.Value as long?;

            if (!Enum.TryParse<ContentType>(typeStr, true, out var contentType))
            {
                contentType = ContentType.Mission;
            }

            var parameters = new RegenerationParameters
            {
                OverrideDifficulty = difficulty.HasValue ? (int)difficulty.Value : null,
                UseCurrentContext = true
            };

            var result = await _contentEngine.RegenerateContentAsync(channelId, contentType, parameters);

            return $"**🔄 Content Regenerated**\n\n" +
                   $"**Type:** {result.ContentType}\n" +
                   $"**Generated At:** {result.RegeneratedAt:HH:mm:ss}\n\n" +
                   $"```json\n{result.Content}\n```";
        }

        return "Unknown content subcommand.";
    }

    /// <summary>
    /// Handle /learning command
    /// </summary>
    private async Task<string> HandleLearningCommand(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First().Name;
        var channelId = command.Channel.Id;

        if (subCommand == "reset")
        {
            // Reset learning data would require implementation
            return "⚠️ Learning data reset is not yet implemented.";
        }

        // status
        var status = await _contentEngine.GetLearningStatusAsync(channelId);

        return FormatLearningStatus(status);
    }

    /// <summary>
    /// Format learning status for display
    /// </summary>
    private string FormatLearningStatus(LearningStatus status)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("**🧠 AI Learning Status**");
        sb.AppendLine();
        sb.AppendLine($"**Learning Enabled:** {(status.LearningEnabled ? "✅" : "❌")}");
        sb.AppendLine($"**NPC Learning:** {(status.NPCLearningActive ? "✅" : "❌")}");
        sb.AppendLine($"**NPC Profiles Learned:** {status.TotalNPCProfilesLearned}");
        sb.AppendLine($"**Story Preference Tracking:** {(status.StoryPreferenceTracking ? "✅" : "❌")}");

        if (status.LastLearningUpdate.HasValue)
        {
            sb.AppendLine($"**Last Update:** {status.LastLearningUpdate:yyyy-MM-dd HH:mm} UTC");
        }

        sb.AppendLine();
        sb.AppendLine("**Story Preferences:**");
        sb.AppendLine($"• Combat: {status.StoryPreferences.CombatPreference:F1}/10");
        sb.AppendLine($"• Social: {status.StoryPreferences.SocialPreference:F1}/10");
        sb.AppendLine($"• Stealth: {status.StoryPreferences.StealthPreference:F1}/10");
        sb.AppendLine($"• Tech: {status.StoryPreferences.TechPreference:F1}/10");
        sb.AppendLine($"• Risk Tolerance: {status.StoryPreferences.RiskTolerance:F1}/10");
        sb.AppendLine($"• Heroic Tendency: {status.StoryPreferences.HeroicTendency:F1}/10");

        sb.AppendLine();
        sb.AppendLine($"**Total Choices Recorded:** {status.StoryPreferences.TotalChoicesRecorded}");

        sb.AppendLine();
        sb.AppendLine("**Learning Metrics:**");
        sb.AppendLine($"• Avg NPC Adaptation: {status.Metrics.AverageNPCAdaptation:F1} interactions");
        sb.AppendLine($"• Preference Confidence: {status.Metrics.PreferenceConfidence:F1}%");
        sb.AppendLine($"• Effectiveness Score: {status.Metrics.EffectivenessScore:F1}/100");

        return sb.ToString();
    }

    /// <summary>
    /// Handle /story command
    /// </summary>
    private async Task<string> HandleStoryCommand(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First().Name;
        var channelId = command.Channel.Id;

        if (subCommand == "hook")
        {
            var options = command.Data.Options.First().Options.ToList();
            var typeStr = options.FirstOrDefault(o => o.Name == "type")?.Value as string ?? "story";

            var hookType = typeStr.ToLower() switch
            {
                "character" => PlotHookType.CharacterFocused,
                "world" => PlotHookType.WorldEvent,
                "faction" => PlotHookType.FactionConflict,
                "mystery" => PlotHookType.Mystery,
                _ => PlotHookType.StoryDriven
            };

            var hook = await _contentEngine.GeneratePlotHookAsync(channelId, hookType);

            return FormatPlotHook(hook);
        }

        // evolve
        var result = await _contentEngine.EvolveStoryAsync(channelId);

        return FormatStoryEvolution(result);
    }

    /// <summary>
    /// Format plot hook for display
    /// </summary>
    private string FormatPlotHook(PlotHook hook)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("**🎣 Plot Hook Generated**");
        sb.AppendLine();
        sb.AppendLine($"**Type:** {hook.HookType}");
        sb.AppendLine($"**Title:** {hook.Title}");
        sb.AppendLine();
        sb.AppendLine(hook.Description);

        if (hook.RelatedNPCs.Any())
        {
            sb.AppendLine();
            sb.AppendLine($"**Related NPCs:** {string.Join(", ", hook.RelatedNPCs)}");
        }

        if (hook.SuggestedMissions.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**Suggested Missions:**");
            foreach (var mission in hook.SuggestedMissions)
            {
                sb.AppendLine($"• {mission}");
            }
        }

        if (hook.PotentialConsequences.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**Potential Consequences:**");
            foreach (var consequence in hook.PotentialConsequences)
            {
                sb.AppendLine($"• {consequence}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format story evolution for display
    /// </summary>
    private string FormatStoryEvolution(StoryEvolutionResult result)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("**📖 Story Evolution**");
        sb.AppendLine();
        sb.AppendLine(result.EvolutionDescription);

        if (result.ArcProgressIncrease > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"**Arc Progress:** +{result.ArcProgressIncrease}%");
        }

        if (result.ArcCompleted)
        {
            sb.AppendLine();
            sb.AppendLine("🎉 **Campaign Arc Completed!**");
            if (!string.IsNullOrEmpty(result.ArcCompletionSummary))
            {
                sb.AppendLine(result.ArcCompletionSummary);
            }
        }

        if (result.NewPlotHooks.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**New Plot Hooks Available:**");
            foreach (var hook in result.NewPlotHooks.Take(3))
            {
                sb.AppendLine($"• {hook.Title}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Handle /npc command
    /// </summary>
    private async Task<string> HandleNpcCommand(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First().Name;
        var channelId = command.Channel.Id;

        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            return "No active session. Start a session first with /session start.";

        if (subCommand == "learn")
        {
            var options = command.Data.Options.First().Options.ToList();
            var name = options.FirstOrDefault(o => o.Name == "name")?.Value as string ?? "Unknown";
            var eventType = options.FirstOrDefault(o => o.Name == "event")?.Value as string ?? "interaction";
            var description = options.FirstOrDefault(o => o.Name == "description")?.Value as string ?? "";

            await _contentDb.RecordNPCLearningEventAsync(session.Id, name, eventType, description);

            return $"📝 **NPC Learning Recorded**\n\n" +
                   $"**NPC:** {name}\n" +
                   $"**Event:** {eventType}\n" +
                   $"**Description:** {description}\n\n" +
                   $"The AI will adapt {name}'s personality based on this interaction.";
        }

        // profile
        var profileOptions = command.Data.Options.First().Options.ToList();
        var npcName = profileOptions.FirstOrDefault(o => o.Name == "name")?.Value as string ?? "Unknown";

        var personalityData = await _contentDb.GetNPCPersonalityDataAsync(session.Id, npcName);
        var learningHistory = await _contentDb.GetNPCLearningHistoryAsync(session.Id, npcName, 10);

        return FormatNPCProfile(npcName, personalityData, learningHistory);
    }

    /// <summary>
    /// Format NPC profile for display
    /// </summary>
    private string FormatNPCProfile(string name, NPCPersonalityData? data, List<NPCLearningEvent> history)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"**👤 NPC Profile: {name}**");
        sb.AppendLine();

        if (data != null)
        {
            sb.AppendLine($"**Total Interactions:** {data.InteractionCount}");
            sb.AppendLine($"**Profile Created:** {data.CreatedAt:yyyy-MM-dd}");
            sb.AppendLine($"**Last Updated:** {data.UpdatedAt:yyyy-MM-dd}");

            if (!string.IsNullOrEmpty(data.PersonalityJson))
            {
                try
                {
                    var personality = System.Text.Json.JsonSerializer.Deserialize<NPCPersonalityModel>(data.PersonalityJson);
                    if (personality != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine("**Personality Traits:**");
                        sb.AppendLine($"• Trust Tendency: {personality.TrustTendency:F1}/10");
                        sb.AppendLine($"• Aggression: {personality.AggressionLevel:F1}/10");
                        sb.AppendLine($"• Formality: {personality.FormalityLevel:F1}/10");
                        sb.AppendLine($"• Traits: {string.Join(", ", personality.Traits)}");

                        if (personality.Secrets.Any())
                        {
                            sb.AppendLine($"• Secrets: {personality.Secrets.Count} hidden");
                        }
                    }
                }
                catch
                {
                    sb.AppendLine("*Personality data could not be parsed*");
                }
            }
        }
        else
        {
            sb.AppendLine("*No personality data yet. Interact with this NPC to build their profile.*");
        }

        if (history.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**Recent Learning Events:**");
            foreach (var evt in history.Take(5))
            {
                sb.AppendLine($"• [{evt.RecordedAt:MM-dd HH:mm}] {evt.EventType}: {evt.Description}");
            }
        }

        return sb.ToString();
    }

    #endregion
}

// Helper enum for content types (duplicated from DynamicContentEngine to avoid dependency)
public enum ContentType
{
    Mission,
    NPC,
    PlotHook,
    Encounter
}
