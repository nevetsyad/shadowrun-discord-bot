using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Core;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot;

/// <summary>
/// Main entry point for the Shadowrun Discord Bot
/// </summary>
class Program
{
    private static readonly CancellationTokenSource _cts = new();

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("Shadowrun Discord Bot - Setup");
        Console.WriteLine("==========================================");
        Console.WriteLine();

        // Check if token is provided via command line or environment
        var tokenFromArgs = args.FirstOrDefault(a => a.StartsWith("--token="))?.Substring(8);
        var tokenFromEnv = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        
        string? discordToken = tokenFromArgs ?? tokenFromEnv;

        // If no token provided, prompt interactively
        if (string.IsNullOrWhiteSpace(discordToken))
        {
            Console.Write("Enter your Discord Bot Token: ");
            discordToken = Console.ReadLine();
        }

        if (string.IsNullOrWhiteSpace(discordToken))
        {
            Console.WriteLine("Error: Discord Token cannot be empty.");
            Console.WriteLine("Set DISCORD_TOKEN environment variable, use --token=YOUR_TOKEN, or enter interactively.");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("Token received. Starting bot...");
        Console.WriteLine();

        try
        {
            var host = CreateHostBuilder(args, discordToken).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting Shadowrun Discord Bot...");

            // Setup graceful shutdown
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                logger.LogInformation("Shutdown signal received...");
                _cts.Cancel();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                logger.LogInformation("Process exit signal received...");
                _cts.Cancel();
            };

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("✨ Shadowrun Discord Bot Started! ✨");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine("Commands available:");
            Console.WriteLine("  /character create/list/view/delete - Character management");
            Console.WriteLine("  /dice [notation] - Roll dice (e.g., 2d6+3)");
            Console.WriteLine("  /shadowrun-dice basic/initiative - Shadowrun dice rolls");
            Console.WriteLine("  /magic status/spells/foci/cast/summon - Magic system");
            Console.WriteLine("  /combat start/status/end/add/remove/next/attack/reroll-init - Combat system");
            Console.WriteLine("  /matrix deck-info - Matrix commands");
            Console.WriteLine("  /cyberware list - Cyberware management");
            Console.WriteLine();
            Console.WriteLine("  Dynamic Content (Phase 5):");
            Console.WriteLine("  /difficulty - View/adjust difficulty");
            Console.WriteLine("  /campaign arc - Campaign arc management");
            Console.WriteLine("  /content regenerate - Regenerate content");
            Console.WriteLine("  /learning status - AI learning status");
            Console.WriteLine("  /story evolve/hook - Story evolution");
            Console.WriteLine("  /npc learn/profile - NPC learning");
            Console.WriteLine();
            Console.WriteLine("  /help - Get help with commands");
            Console.WriteLine();
            Console.WriteLine("Press Ctrl+C to exit...");
            Console.WriteLine();

            await host.RunAsync(_cts.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex}");
            return 1;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args, string discordToken) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
                
                // Add runtime token to configuration
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Discord:Token"] = discordToken
                });
            })
            .ConfigureServices((context, services) =>
            {
                // Load and validate configuration
                var botConfig = BotConfig.Load(context.Configuration);
                
                // Override token if provided interactively
                if (!string.IsNullOrWhiteSpace(discordToken))
                {
                    botConfig.Discord.Token = discordToken;
                }
                
                services.AddSingleton(botConfig);

                // Core services
                services.AddSingleton<BotService>();
                services.AddSingleton<CommandHandler>();
                services.AddSingleton<ErrorHandler>();
                services.AddSingleton<DiceService>();
                services.AddSingleton<DatabaseService>();
                services.AddSingleton<CombatService>();
                services.AddSingleton<GMService>();
                
                // Game Session Management
                services.AddSingleton<GameSessionService>();
                services.AddSingleton<NarrativeContextService>();
                
                // Autonomous Mission System (Phase 2)
                services.AddSingleton<AutonomousMissionService>();
                
                // Interactive Storytelling System (Phase 3)
                services.AddSingleton<InteractiveStoryService>();
                
                // Session Management System (Phase 4)
                services.AddSingleton<SessionManagementService>();
                
                // Dynamic Content Engine (Phase 5)
                services.AddSingleton<ContentDatabaseService>();
                services.AddSingleton<DynamicContentEngine>();
                services.AddSingleton<Commands.DynamicContentCommands>();
                
                // Web UI
                services.AddHostedService<WebUIService>();

                // Main bot service (hosted)
                services.AddHostedService(provider => provider.GetRequiredService<BotService>());
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();

                if (context.HostingEnvironment.IsDevelopment())
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                }
                else
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                }
            });
}
