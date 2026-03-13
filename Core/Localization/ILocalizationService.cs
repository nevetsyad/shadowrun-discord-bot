namespace ShadowrunDiscordBot.Core.Localization;

/// <summary>
/// Interface for localization and internationalization
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Get a localized string by key with optional formatting arguments
    /// </summary>
    string GetString(string key, params object[] args);
    
    /// <summary>
    /// Get a localized string for a specific culture with optional formatting arguments
    /// </summary>
    string GetString(string culture, string key, params object[] args);
    
    /// <summary>
    /// Current culture code (e.g., "en-US")
    /// </summary>
    string CurrentCulture { get; set; }
    
    /// <summary>
    /// Current UI culture code (e.g., "en-US")
    /// </summary>
    string CurrentUICulture { get; set; }
    
    /// <summary>
    /// Get all supported cultures
    /// </summary>
    IEnumerable<string> GetSupportedCultures();
    
    /// <summary>
    /// Check if a culture is supported
    /// </summary>
    bool IsCultureSupported(string culture);
}
