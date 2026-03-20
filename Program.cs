using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Domain.Interfaces;
using ShadowrunDiscordBot.Infrastructure.Repositories;
using Serilog;

namespace ShadowrunDiscordBot;

/// <summary>
/// Main entry point for the Shadowrun Discord Bot
/// DDD Architecture - Using Domain/Application/Infrastructure layers
/// </summary>
class Program
{
    private static readonly CancellationTokenSource _cts = new();

    static async Task<int> Main(string[] args)
    {
        // Configure Serilog early
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/shadowrun-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();

        try
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("Shadowrun Discord Bot - DDD Architecture");
            Console.WriteLine("==========================================");
            Console.WriteLine();

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

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("Shadowrun Discord Bot Started!");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine("DDD Architecture Active:");
            Console.WriteLine("  - Domain Layer: Entities, Value Objects, Domain Events");
            Console.WriteLine("  - Application Layer: Services, CQRS Handlers");
            Console.WriteLine("  - Infrastructure Layer: Repositories, Data Access");
            Console.WriteLine("  - Presentation Layer: Discord Integration");
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
            Log.Fatal(ex, "Fatal error occurred");
            Console.Error.WriteLine($"Fatal error: {ex}");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Register repositories from Infrastructure layer
                services.AddSingleton<IBotConfig, BotConfig>();
                services.AddScoped<ICharacterRepository, CharacterRepository>();
                services.AddScoped<ICombatSessionRepository, CombatSessionRepository>();
                services.AddScoped<ICombatParticipantRepository, CombatParticipantRepository>();
                services.AddScoped<IGameSessionRepository, GameSessionRepository>();
                services.AddScoped<IMatrixSessionRepository, MatrixSessionRepository>();

                // MediatR for CQRS
                services.AddMediatR(cfg => 
                {
                    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                });
            });
}
