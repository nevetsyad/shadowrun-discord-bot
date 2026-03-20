using Discord;
using Microsoft.Extensions.Configuration;

namespace ShadowrunDiscordBot;

public interface IBotConfig
{
    DiscordConfig Discord { get; }
    DatabaseConfig Database { get; }
    BotSettings Bot { get; }
    WebUIConfig WebUI { get; }
    RateLimitConfig RateLimiting { get; }
}

/// <summary>
/// Configuration management with SecretRef support for environment variables
/// </summary>
public class BotConfig : IBotConfig
{
    public BotConfig(IConfiguration config) {
        Load(config);
    }

    public DiscordConfig Discord { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public BotSettings Bot { get; set; } = new();
    public WebUIConfig WebUI { get; set; } = new();
    public RateLimitConfig RateLimiting { get; set; } = new();

    public void Load(IConfiguration configuration)
    {
        configuration.GetSection("Discord").Bind(Discord);
        configuration.GetSection("Database").Bind(Database);
        configuration.GetSection("Bot").Bind(Bot);
        configuration.GetSection("WebUI").Bind(WebUI);
        configuration.GetSection("RateLimiting").Bind(RateLimiting);

        IsValid();
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

    private void IsValid()
    {
        if (string.IsNullOrWhiteSpace(Discord.Token))
            throw new InvalidOperationException("Discord token is required");

        if (string.IsNullOrWhiteSpace(Discord.ClientId))
            throw new InvalidOperationException("Discord Client ID is required");

        if (WebUI.Port < 1 || WebUI.Port > 65535)
            throw new InvalidOperationException("Web UI port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(WebUI.JwtSecret) || WebUI.JwtSecret.Length < 32)
            throw new InvalidOperationException("JWT secret must be at least 32 characters long");
    }
}

public class AbstractConfig
{
    protected static string ResolveSecretRef(string? value)
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
}

public class DiscordConfig : AbstractConfig
{
    private string? _token;
    private string? _clientId;
    private string? _guildId;
    public string? Token { get { return _token; } set { _token = ResolveSecretRef(value); } }
    public string? ClientId { get { return _clientId; } set { _clientId = ResolveSecretRef(value); } }
    public string? GuildId { get { return _guildId; } set { _guildId = ResolveSecretRef(value); } }
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

public class WebUIConfig : AbstractConfig
{
    private string? _jwtSecret;
    public int Port { get; set; } = 5000;
    public bool EnableSwagger { get; set; } = true;
    public string? JwtSecret { get { return _jwtSecret; } set { _jwtSecret = ResolveSecretRef(value); } }
}

public class RateLimitConfig
{
    public int PermitLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
