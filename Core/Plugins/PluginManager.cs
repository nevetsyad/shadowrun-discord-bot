namespace ShadowrunDiscordBot.Core.Plugins;

using System.Reflection;
using System.Runtime.Loader;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of the plugin manager
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, PluginInfo> _plugins = new();
    private readonly Dictionary<string, IPlugin> _pluginInstances = new();
    private readonly Dictionary<string, AssemblyLoadContext> _loadContexts = new();
    
    public PluginManager(ILogger<PluginManager> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }
    
    public async Task<PluginInfo> LoadPluginAsync(string pluginPath)
    {
        _logger.LogInformation("Loading plugin from {PluginPath}", pluginPath);
        
        try
        {
            if (!File.Exists(pluginPath))
            {
                throw new FileNotFoundException($"Plugin file not found: {pluginPath}");
            }
            
            // Create a new assembly load context for this plugin
            var pluginName = Path.GetFileNameWithoutExtension(pluginPath);
            var loadContext = new AssemblyLoadContext(pluginName, isCollectible: true);
            _loadContexts[pluginName] = loadContext;
            
            // Load the assembly
            var assembly = loadContext.LoadFromAssemblyPath(pluginPath);
            
            // Find all types that implement IPlugin
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();
            
            if (pluginTypes.Count == 0)
            {
                throw new InvalidOperationException($"No IPlugin implementations found in {pluginPath}");
            }
            
            // Instantiate the first plugin type found
            var pluginType = pluginTypes.First();
            var plugin = (IPlugin?)Activator.CreateInstance(pluginType);
            
            if (plugin == null)
            {
                throw new InvalidOperationException($"Failed to instantiate plugin type {pluginType.Name}");
            }
            
            // Initialize the plugin
            await plugin.InitializeAsync(_services);
            
            // Create plugin info
            var pluginInfo = new PluginInfo
            {
                Id = pluginName,
                Name = plugin.Name,
                Version = plugin.Version,
                Description = plugin.Description,
                Author = plugin.Author,
                FilePath = pluginPath,
                IsLoaded = true,
                LoadedAt = DateTime.UtcNow
            };
            
            // Store plugin
            _plugins[pluginInfo.Id] = pluginInfo;
            _pluginInstances[pluginInfo.Id] = plugin;
            
            _logger.LogInformation(
                "Plugin {PluginName} v{PluginVersion} loaded successfully",
                plugin.Name,
                plugin.Version);
            
            return pluginInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from {PluginPath}", pluginPath);
            
            var errorInfo = new PluginInfo
            {
                Id = Path.GetFileNameWithoutExtension(pluginPath),
                FilePath = pluginPath,
                IsLoaded = false,
                ErrorMessage = ex.Message
            };
            
            _plugins[errorInfo.Id] = errorInfo;
            return errorInfo;
        }
    }
    
    public async Task<bool> UnloadPluginAsync(string pluginId)
    {
        _logger.LogInformation("Unloading plugin {PluginId}", pluginId);
        
        try
        {
            if (!_pluginInstances.TryGetValue(pluginId, out var plugin))
            {
                _logger.LogWarning("Plugin {PluginId} not found", pluginId);
                return false;
            }
            
            // Shutdown the plugin
            await plugin.ShutdownAsync();
            
            // Remove from collections
            _pluginInstances.Remove(pluginId);
            _plugins.Remove(pluginId);
            
            // Unload the assembly context
            if (_loadContexts.TryGetValue(pluginId, out var loadContext))
            {
                loadContext.Unload();
                _loadContexts.Remove(pluginId);
            }
            
            _logger.LogInformation("Plugin {PluginId} unloaded successfully", pluginId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin {PluginId}", pluginId);
            return false;
        }
    }
    
    public Task<PluginInfo?> GetPluginInfoAsync(string pluginId)
    {
        _plugins.TryGetValue(pluginId, out var info);
        return Task.FromResult(info);
    }
    
    public Task<List<PluginInfo>> GetAllPluginsAsync()
    {
        return Task.FromResult(_plugins.Values.ToList());
    }
    
    public async Task<PluginInfo> ReloadPluginAsync(string pluginId)
    {
        _logger.LogInformation("Reloading plugin {PluginId}", pluginId);
        
        if (_plugins.TryGetValue(pluginId, out var existingInfo))
        {
            await UnloadPluginAsync(pluginId);
            return await LoadPluginAsync(existingInfo.FilePath);
        }
        
        throw new InvalidOperationException($"Plugin {pluginId} not found");
    }
    
    public async Task OnMessageReceivedAsync(SocketMessage message)
    {
        foreach (var plugin in _pluginInstances.Values)
        {
            try
            {
                await plugin.OnMessageReceivedAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Plugin {PluginName} failed to handle message received",
                    plugin.Name);
            }
        }
    }
    
    public async Task OnCommandReceivedAsync(SocketSlashCommand command)
    {
        foreach (var plugin in _pluginInstances.Values)
        {
            try
            {
                await plugin.OnCommandReceivedAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Plugin {PluginName} failed to handle command received",
                    plugin.Name);
            }
        }
    }
    
    public async Task OnMessageReactionAddedAsync(SocketReaction reaction)
    {
        foreach (var plugin in _pluginInstances.Values)
        {
            try
            {
                await plugin.OnMessageReactionAddedAsync(reaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Plugin {PluginName} failed to handle reaction added",
                    plugin.Name);
            }
        }
    }
    
    public async Task OnGuildMemberAddedAsync(SocketGuildUser member)
    {
        foreach (var plugin in _pluginInstances.Values)
        {
            try
            {
                await plugin.OnGuildMemberAddedAsync(member);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Plugin {PluginName} failed to handle guild member added",
                    plugin.Name);
            }
        }
    }
    
    public async Task OnGuildMemberRemovedAsync(SocketGuildUser member)
    {
        foreach (var plugin in _pluginInstances.Values)
        {
            try
            {
                await plugin.OnGuildMemberRemovedAsync(member);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Plugin {PluginName} failed to handle guild member removed",
                    plugin.Name);
            }
        }
    }
    
    public async Task OnReadyAsync()
    {
        foreach (var plugin in _pluginInstances.Values)
        {
            try
            {
                await plugin.OnReadyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Plugin {PluginName} failed to handle ready event",
                    plugin.Name);
            }
        }
    }
    
    public async Task ShutdownAllAsync()
    {
        _logger.LogInformation("Shutting down all plugins");
        
        foreach (var pluginId in _pluginInstances.Keys.ToList())
        {
            await UnloadPluginAsync(pluginId);
        }
    }
}
