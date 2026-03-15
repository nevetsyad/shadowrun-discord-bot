// Example integration of Phase 5 Dynamic Content Engine
// Add this code to your existing Program.cs or service configuration

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShadowrunDiscordBot.Extensions;
using ShadowrunDiscordBot.Services;
using ShadowrunDiscordBot.Commands;
using ShadowrunDiscordBot.Models;
using ContentType = ShadowrunDiscordBot.Services.ContentType;

namespace ShadowrunDiscordBot;

public partial class Program
{
    /// <summary>
    /// Example: How to register Phase 5 services in your DI container
    /// </summary>
    private static void ConfigurePhase5Services(IServiceCollection services)
    {
        // === Add after your existing service registrations ===
        
        // Phase 5: Dynamic Content Engine
        services.AddPhase5DynamicContent();
    }

    /// <summary>
    /// Example: How to initialize Phase 5 services at startup
    /// </summary>
    private static async Task InitializePhase5(IServiceProvider services)
    {
        // === Call this after database initialization ===
        
        await services.InitializePhase5Async();
    }

    /// <summary>
    /// Example: How to register Phase 5 commands
    /// </summary>
    private static async Task RegisterPhase5Commands(
        DiscordSocketClient client, 
        IServiceProvider services,
        ulong guildId)
    {
        // === Add to your command registration method ===
        
        var contentCommands = services.GetRequiredService<DynamicContentCommands>();
        await DynamicContentCommands.RegisterCommandsAsync(client, guildId);
    }

    /// <summary>
    /// Example: How to handle Phase 5 commands in your slash command handler
    /// </summary>
    private static async Task HandlePhase5Command(
        SocketSlashCommand command,
        IServiceProvider services)
    {
        // === Add to your slash command handler ===
        
        var phase5Commands = new[] { "difficulty", "campaign", "content", "learning", "story", "npc" };
        
        if (phase5Commands.Contains(command.Data.Name))
        {
            var contentCommands = services.GetRequiredService<DynamicContentCommands>();
            await contentCommands.HandleCommandAsync(command);
        }
    }
}

/// <summary>
/// Example: Complete Program.cs integration
/// </summary>
public class Phase5IntegrationExample
{
    public static async Task Main(string[] args)
    {
        // Create host builder
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Existing services (Phases 1-4)
                services.AddSingleton<DatabaseService>();
                services.AddSingleton<GameSessionService>();
                services.AddSingleton<NarrativeContextService>();
                services.AddSingleton<AutonomousMissionService>();
                services.AddSingleton<InteractiveStoryService>();
                services.AddSingleton<SessionManagementService>();
                services.AddSingleton<DiceService>();
                services.AddSingleton<GMService>();
                
                // Phase 5 services
                services.AddPhase5DynamicContent();
                
                // Discord client
                services.AddSingleton<DiscordSocketClient>();
            })
            .Build();

        // Initialize database
        var database = host.Services.GetRequiredService<DatabaseService>();
        await database.InitializeAsync();

        // Initialize Phase 5
        await host.Services.InitializePhase5Async();

        // Start Discord client
        var client = host.Services.GetRequiredService<DiscordSocketClient>();
        
        // Register commands when ready
        client.Ready += async () =>
        {
            // Replace with your guild ID
            ulong guildId = 123456789012345678;
            
            // Register Phase 5 commands
            var contentCommands = host.Services.GetRequiredService<DynamicContentCommands>();
            await DynamicContentCommands.RegisterCommandsAsync(client, guildId);
        };

        // Handle slash commands
        client.SlashCommandExecuted += async (command) =>
        {
            var phase5Commands = new[] { "difficulty", "campaign", "content", "learning", "story", "npc" };
            
            if (phase5Commands.Contains(command.Data.Name))
            {
                var contentCommands = host.Services.GetRequiredService<DynamicContentCommands>();
                await contentCommands.HandleCommandAsync(command);
            }
        };

        await host.RunAsync();
    }
}

/// <summary>
/// Example: Using DynamicContentEngine directly in your code
/// </summary>
public class DynamicContentEngineUsageExample
{
    private readonly DynamicContentEngine _engine;
    private readonly ulong _channelId;

    public DynamicContentEngineUsageExample(DynamicContentEngine engine, ulong channelId)
    {
        _engine = engine;
        _channelId = channelId;
    }

    /// <summary>
    /// Example: Get current difficulty
    /// </summary>
    public async Task ShowDifficulty()
    {
        var status = await _engine.GetDifficultyAsync(_channelId);
        
        Console.WriteLine($"Current Difficulty: {status.CurrentDifficulty}");
        Console.WriteLine($"Performance Score: {status.PerformanceScore}");
        Console.WriteLine($"Recommendation: {status.Recommendation}");
    }

    /// <summary>
    /// Example: Generate scaled mission parameters
    /// </summary>
    public async Task GenerateMission()
    {
        var missionParams = await _engine.GenerateScaledMissionParamsAsync(_channelId, "extraction");
        
        Console.WriteLine($"Difficulty: {missionParams.Difficulty}");
        Console.WriteLine($"Decision Points: {missionParams.DecisionPointCount}");
        Console.WriteLine($"Obstacles: {missionParams.ObstacleCount}");
        Console.WriteLine($"Security Level: {missionParams.SecurityLevel}");
        Console.WriteLine($"Base Reward: {missionParams.BaseReward.BaseNuyen}¥");
    }

    /// <summary>
    /// Example: Generate a plot hook
    /// </summary>
    public async Task GeneratePlotHook()
    {
        var hook = await _engine.GeneratePlotHookAsync(_channelId, PlotHookType.StoryDriven);
        
        Console.WriteLine($"Title: {hook.Title}");
        Console.WriteLine($"Description: {hook.Description}");
        Console.WriteLine($"Related NPCs: {string.Join(", ", hook.RelatedNPCs)}");
    }

    /// <summary>
    /// Example: Evolve the story
    /// </summary>
    public async Task EvolveStory()
    {
        var result = await _engine.EvolveStoryAsync(_channelId);
        
        Console.WriteLine(result.EvolutionDescription);
        
        if (result.ArcCompleted)
        {
            Console.WriteLine("🎉 Campaign Arc Completed!");
        }
    }

    /// <summary>
    /// Example: Generate dynamic NPC dialogue
    /// </summary>
    public async Task GenerateDialogue()
    {
        var response = await _engine.GenerateDynamicDialogueAsync(
            _channelId,
            "Ghostslicer",
            "I need information about the Renraku compound",
            DialogueSituation.InformationGathering);
        
        Console.WriteLine($"NPC: {response.NPCName}");
        Console.WriteLine($"Opening: {response.OpeningLine}");
        Console.WriteLine($"Response: {response.MainResponse}");
        Console.WriteLine($"Mood: {response.MoodIndicator}");
    }

    /// <summary>
    /// Example: Manage campaign arcs
    /// </summary>
    public async Task ManageCampaignArc()
    {
        // Start a new arc
        await _engine.StartCampaignArcAsync(
            _channelId,
            "Rise of the Technomancers",
            "A campaign arc about the emergence of technomancers in Seattle");
        
        // Check arc status
        var status = await _engine.GetCampaignArcStatusAsync(_channelId);
        
        Console.WriteLine($"Current Arc: {status.CurrentArc}");
        Console.WriteLine($"Progress: {status.ArcProgress}%");
    }

    /// <summary>
    /// Example: Record player choice for learning
    /// </summary>
    public async Task RecordChoice(PlayerChoice choice)
    {
        await _engine.RecordChoiceForLearningAsync(_channelId, choice);
        
        var learningStatus = await _engine.GetLearningStatusAsync(_channelId);
        
        Console.WriteLine($"Combat Preference: {learningStatus.StoryPreferences.CombatPreference}");
        Console.WriteLine($"Social Preference: {learningStatus.StoryPreferences.SocialPreference}");
    }

    /// <summary>
    /// Example: Regenerate content
    /// </summary>
    public async Task RegenerateContent()
    {
        var result = await _engine.RegenerateContentAsync(
            _channelId,
            ContentType.Mission,
            new RegenerationParameters
            {
                OverrideDifficulty = 7,
                CustomParameters = new Dictionary<string, object>
                {
                    ["missionType"] = "assassination"
                }
            });
        
        Console.WriteLine($"Regenerated: {result.ContentType}");
        Console.WriteLine(result.Content);
    }
}
