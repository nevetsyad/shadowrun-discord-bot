using Discord;
using Microsoft.Extensions.Configuration;

namespace ShadowrunDiscordBot;

/// <summary>
/// Configuration management with SecretRef support for environment variables
/// </summary>
public class BotConfig
{
    public DiscordConfig Discord { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public BotSettings Bot { get; set; } = new();
    public WebUIConfig WebUI { get; set; } = new();
    public RateLimitConfig RateLimiting { get; set; } = new();

    public static BotConfig Load(IConfiguration configuration)
    {
        var config = new BotConfig();
        configuration.GetSection("Discord").Bind(config.Discord);
        configuration.GetSection("Database").Bind(config.Database);
        configuration.GetSection("Bot").Bind(config.Bot);
        configuration.GetSection("WebUI").Bind(config.WebUI);
        configuration.GetSection("RateLimiting").Bind(config.RateLimiting);

        // Resolve SecretRefs (environment variables)
        config.Discord.Token = ResolveSecretRef(config.Discord.Token);
        config.Discord.ClientId = ResolveSecretRef(config.Discord.ClientId);
        config.Discord.GuildId = ResolveSecretRef(config.Discord.GuildId);
        config.WebUI.JwtSecret = ResolveSecretRef(config.WebUI.JwtSecret);

        ValidateConfig(config);
        return config;
    }

    private static string ResolveSecretRef(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Check if it's a SecretRef pattern: ${ENV_VAR_NAME}
        if (value.StartsWith("${") && value.EndsWith("}"))
        {
            var envVarName = value[2..^1];
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            return envValue ?? throw new InvalidOperationException($"Environment variable '{envVarName}' not found");
        }

        return value;
    }

    private static void ValidateConfig(BotConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Discord.Token))
            throw new InvalidOperationException("Discord token is required");

        if (string.IsNullOrWhiteSpace(config.Discord.ClientId))
            throw new InvalidOperationException("Discord Client ID is required");

        if (config.WebUI.Port < 1 || config.WebUI.Port > 65535)
            throw new InvalidOperationException("Web UI port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(config.WebUI.JwtSecret) || config.WebUI.JwtSecret.Length < 32)
            throw new InvalidOperationException("JWT secret must be at least 32 characters long");
    }
}

public class DiscordConfig
{
    public string Token { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string GuildId { get; set; } = string.Empty;
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = "Data Source=shadowrun.db";
    public bool EnableSensitiveDataLogging { get; set; }
}

public class BotSettings
{
    public string Prefix { get; set; } = "!";
    public uint DefaultColorInt { get; set; } = 5814783;
    public Color DefaultColor { get { return new Color(DefaultColorInt); }  }
    public int MaxCharactersPerUser { get; set; } = 5;
    public int MaxDiceRolls { get; set; } = 10;
}

public class WebUIConfig
{
    public int Port { get; set; } = 5000;
    public bool EnableSwagger { get; set; } = true;
    public string JwtSecret { get; set; } = string.Empty;
}

public class RateLimitConfig
{
    public int PermitLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
