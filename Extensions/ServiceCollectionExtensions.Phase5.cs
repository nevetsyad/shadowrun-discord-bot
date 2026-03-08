using Microsoft.Extensions.DependencyInjection;
using ShadowrunDiscordBot.Commands;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Extensions;

/// <summary>
/// Extension methods for registering Phase 5 services
/// </summary>
public static class Phase5ServiceExtensions
{
    /// <summary>
    /// Add Phase 5 Dynamic Content Engine services
    /// </summary>
    public static IServiceCollection AddPhase5DynamicContent(this IServiceCollection services)
    {
        // Register the content database service
        services.AddSingleton<ContentDatabaseService>();

        // Register the dynamic content engine
        services.AddSingleton<DynamicContentEngine>();

        // Register the command handlers
        services.AddSingleton<DynamicContentCommands>();

        return services;
    }

    /// <summary>
    /// Initialize Phase 5 services (call after database is initialized)
    /// </summary>
    public static async Task InitializePhase5Async(this IServiceProvider services)
    {
        // Initialize content database tables if needed
        var contentDb = services.GetRequiredService<ContentDatabaseService>();
        
        // Run cleanup of old data (optional)
        // await contentDb.CleanupOldDataAsync(90);
    }
}
