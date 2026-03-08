using System.Collections.Concurrent;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace ShadowrunDiscordBot.Core;

/// <summary>
/// Main Discord bot service with optimized connection handling
/// </summary>
public sealed class BotService : IHostedService, IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<BotService> _logger;
    private readonly BotConfig _config;
    private readonly CommandHandler _commandHandler;
    private readonly ConcurrentDictionary<ulong, DateTime> _rateLimitTracker = new();
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;

    private bool _disposed;

    public BotService(
        BotConfig config,
        CommandHandler commandHandler,
        ILogger<BotService> logger)
    {
        _config = config;
        _commandHandler = commandHandler;
        _logger = logger;

        // Configure Discord client with optimized settings
        var discordConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds |
                           GatewayIntents.GuildMembers |
                           GatewayIntents.GuildMessages |
                           GatewayIntents.MessageContent |
                           GatewayIntents.GuildPresences,
            AlwaysDownloadUsers = false, // Don't download all users on startup
            LargeThreshold = 250,
            HandlerTimeout = null, // No timeout for event handlers
            UseGatewayCompression = true,
            ConnectionTimeout = 30000
        };

        _client = new DiscordSocketClient(discordConfig);
        
        // Setup object pooling for performance
        var poolProvider = new DefaultObjectPoolProvider();
        _stringBuilderPool = poolProvider.CreateStringBuilderPool();

        SetupEventHandlers();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting Discord bot service...");

            await _commandHandler.InitializeAsync(_client);

            // Login with retry logic
            await LoginWithRetryAsync(cancellationToken);

            _logger.LogInformation("Discord bot service started successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Bot startup cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Discord bot service");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Stopping Discord bot service...");

            await _client.StopAsync();
            await _client.LogoutAsync();

            _logger.LogInformation("Discord bot service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Discord bot service");
        }
    }

    private async Task LoginWithRetryAsync(CancellationToken cancellationToken)
    {
        const int maxRetries = 5;
        const int baseDelayMs = 1000;
        const int maxDelayMs = 30000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Attempting Discord login (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                await _client.LoginAsync(TokenType.Bot, _config.Discord.Token);
                await _client.StartAsync();

                // Wait for ready state
                var tcs = new TaskCompletionSource<bool>();
                
                _client.Ready += () =>
                {
                    tcs.TrySetResult(true);
                    return Task.CompletedTask;
                };

                await Task.WhenAny(tcs.Task, Task.Delay(30000, cancellationToken));

                if (!tcs.Task.IsCompleted)
                    throw new TimeoutException("Discord connection timed out");

                _logger.LogInformation("Successfully connected to Discord as {Username}#{Discriminator}",
                    _client.CurrentUser?.Username, _client.CurrentUser?.Discriminator);

                return;
            }
            catch (HttpException httpEx) when (httpEx.HttpCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Discord authentication failed - invalid token");
                throw;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delay = Math.Min(baseDelayMs * (int)Math.Pow(2, attempt), maxDelayMs);
                _logger.LogWarning(ex, "Login attempt {Attempt} failed, retrying in {Delay}ms", attempt, delay);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Failed to login after {maxRetries} attempts");
    }

    private void SetupEventHandlers()
    {
        _client.Log += message =>
        {
            var logLevel = message.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Verbose => LogLevel.Debug,
                LogSeverity.Debug => LogLevel.Trace,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, "Discord: {Message}", message.Message);
            return Task.CompletedTask;
        };

        _client.Disconnected += exception =>
        {
            if (exception != null)
            {
                _logger.LogError(exception, "Discord client disconnected with error");
            }
            else
            {
                _logger.LogWarning("Discord client disconnected");
            }
            return Task.CompletedTask;
        };

        _client.Connected += () =>
        {
            _logger.LogInformation("Connected to Discord");
            return Task.CompletedTask;
        };

        _client.Ready += async () =>
        {
            _logger.LogInformation("Discord client is ready");
            await _commandHandler.RegisterCommandsAsync();
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        _client?.Dispose();
        _rateLimitTracker.Clear();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
