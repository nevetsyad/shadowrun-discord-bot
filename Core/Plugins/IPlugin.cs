namespace ShadowrunDiscordBot.Core.Plugins;

using Discord.WebSocket;

/// <summary>
/// Interface for plugins that can extend the bot's functionality
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Unique name of the plugin
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Plugin version (semantic versioning)
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Plugin author
    /// </summary>
    string Author { get; }
    
    /// <summary>
    /// Initialize the plugin with dependency injection services
    /// </summary>
    Task InitializeAsync(IServiceProvider services);
    
    /// <summary>
    /// Called when a message is received
    /// </summary>
    Task OnMessageReceivedAsync(SocketMessage message);
    
    /// <summary>
    /// Called when a slash command is received
    /// </summary>
    Task OnCommandReceivedAsync(SocketSlashCommand command);
    
    /// <summary>
    /// Called when a reaction is added to a message
    /// </summary>
    Task OnMessageReactionAddedAsync(SocketReaction reaction);
    
    /// <summary>
    /// Called when a new member joins a guild
    /// </summary>
    Task OnGuildMemberAddedAsync(SocketGuildUser member);
    
    /// <summary>
    /// Called when a member leaves a guild
    /// </summary>
    Task OnGuildMemberRemovedAsync(SocketGuildUser member);
    
    /// <summary>
    /// Called when the bot is ready and connected
    /// </summary>
    Task OnReadyAsync();
    
    /// <summary>
    /// Shutdown the plugin and cleanup resources
    /// </summary>
    Task ShutdownAsync();
}

/// <summary>
/// Information about a loaded plugin
/// </summary>
public class PluginInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
    public DateTime LoadedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
