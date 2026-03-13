namespace ShadowrunDiscordBot.Core.Plugins;

using Discord.WebSocket;

/// <summary>
/// Interface for managing plugin lifecycle
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Load a plugin from a file path
    /// </summary>
    Task<PluginInfo> LoadPluginAsync(string pluginPath);
    
    /// <summary>
    /// Unload a plugin by ID
    /// </summary>
    Task<bool> UnloadPluginAsync(string pluginId);
    
    /// <summary>
    /// Get information about a specific plugin
    /// </summary>
    Task<PluginInfo?> GetPluginInfoAsync(string pluginId);
    
    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    Task<List<PluginInfo>> GetAllPluginsAsync();
    
    /// <summary>
    /// Reload a plugin by ID
    /// </summary>
    Task<PluginInfo> ReloadPluginAsync(string pluginId);
    
    /// <summary>
    /// Notify all plugins of a received message
    /// </summary>
    Task OnMessageReceivedAsync(SocketMessage message);
    
    /// <summary>
    /// Notify all plugins of a received command
    /// </summary>
    Task OnCommandReceivedAsync(SocketSlashCommand command);
    
    /// <summary>
    /// Notify all plugins of a reaction added
    /// </summary>
    Task OnMessageReactionAddedAsync(SocketReaction reaction);
    
    /// <summary>
    /// Notify all plugins of a new guild member
    /// </summary>
    Task OnGuildMemberAddedAsync(SocketGuildUser member);
    
    /// <summary>
    /// Notify all plugins of a guild member removed
    /// </summary>
    Task OnGuildMemberRemovedAsync(SocketGuildUser member);
    
    /// <summary>
    /// Notify all plugins that the bot is ready
    /// </summary>
    Task OnReadyAsync();
    
    /// <summary>
    /// Shutdown all plugins
    /// </summary>
    Task ShutdownAllAsync();
}
