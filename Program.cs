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
        try
        {
            var host = CreateHostBuilder(args).Build();
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

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Load and validate configuration
                var botConfig = BotConfig.Load(context.Configuration);
                services.AddSingleton(botConfig);

                // Core services
                services.AddSingleton<BotService>();
                services.AddSingleton<CommandHandler>();
                services.AddSingleton<ErrorHandler>();
                services.AddSingleton<DiceService>();
                services.AddSingleton<DatabaseService>();
                
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
