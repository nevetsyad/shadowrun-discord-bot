namespace ShadowrunDiscordBot.Core.Localization;

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of the localization service
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly Dictionary<string, Dictionary<string, string>> _locales = new();
    private readonly string _localesDirectory;
    
    private string _currentCulture = "en-US";
    public string CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (IsCultureSupported(value))
            {
                _currentCulture = value;
                CultureInfo.CurrentCulture = new CultureInfo(value);
                CultureInfo.CurrentUICulture = new CultureInfo(value);
            }
            else
            {
                _logger.LogWarning("Attempted to set unsupported culture: {Culture}", value);
            }
        }
    }
    
    public string CurrentUICulture
    {
        get => CurrentCulture;
        set => CurrentCulture = value;
    }
    
    public LocalizationService(ILogger<LocalizationService> logger, string? localesDirectory = null)
    {
        _logger = logger;
        _localesDirectory = localesDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Locales");
        
        LoadAllLocales();
    }
    
    private void LoadAllLocales()
    {
        try
        {
            if (!Directory.Exists(_localesDirectory))
            {
                _logger.LogWarning("Locales directory not found: {LocalesDirectory}", _localesDirectory);
                Directory.CreateDirectory(_localesDirectory);
                CreateDefaultLocale();
                return;
            }
            
            var localeFiles = Directory.GetFiles(_localesDirectory, "*.json");
            
            foreach (var file in localeFiles)
            {
                var cultureCode = Path.GetFileNameWithoutExtension(file);
                LoadLocale(cultureCode, file);
            }
            
            _logger.LogInformation("Loaded {Count} locales", _locales.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading locales");
        }
    }
    
    private void LoadLocale(string cultureCode, string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var locale = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            
            if (locale != null)
            {
                _locales[cultureCode] = locale;
                _logger.LogDebug("Loaded locale {CultureCode} with {Count} keys", cultureCode, locale.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load locale {CultureCode} from {FilePath}", cultureCode, filePath);
        }
    }
    
    private void CreateDefaultLocale()
    {
        var defaultLocale = new Dictionary<string, string>
        {
            // Character commands
            ["chat.character.created"] = "Character **{0}** created successfully!",
            ["chat.character.updated"] = "Character **{0}** updated successfully!",
            ["chat.character.deleted"] = "Character **{0}** deleted successfully.",
            ["chat.character.not_found"] = "Character not found.",
            ["chat.character.already_exists"] = "You already have a character named **{0}**.",
            ["chat.character.display"] = "**{0}** ({1} {2})\nBody: {3}, Quickness: {4}, Strength: {5}\nCharisma: {6}, Intelligence: {7}, Willpower: {8}\nReaction: {9}, Essence: {10:F2}",
            
            // Combat commands
            ["chat.combat.started"] = "⚔️ Combat started! Round 1, Pass 1",
            ["chat.combat.ended"] = "⚔️ Combat ended.",
            ["chat.combat.joined"] = "**{0}** joined combat with initiative {1}.",
            ["chat.combat.turn"] = "**{0}**'s turn (Initiative: {1})",
            ["chat.combat.next_pass"] = "Pass {0} of Round {1}",
            ["chat.combat.next_round"] = "Round {0}",
            
            // Dice rolls
            ["chat.dice.roll"] = "🎲 Rolling {0} dice vs TN {1}: **{2}** successes",
            ["chat.dice.glitch"] = "💀 Glitch! {0} ones rolled.",
            ["chat.dice.critical_glitch"] = "💀 CRITICAL GLITCH!",
            ["chat.dice.success"] = "✅ Success! {0} hits (needed {1})",
            ["chat.dice.failure"] = "❌ Failed. {0} hits (needed {1})",
            
            // Errors
            ["errors.invalid_parameter"] = "Invalid parameter: {0}",
            ["errors.command_failed"] = "Command failed: {0}",
            ["errors.not_authorized"] = "You are not authorized to perform this action.",
            ["errors.character_not_found"] = "Character not found: {0}",
            ["errors.combat_not_active"] = "No active combat in this channel.",
            ["errors.database_error"] = "Database error occurred.",
            
            // General
            ["general.success"] = "✅ Success!",
            ["general.error"] = "❌ An error occurred.",
            ["general.not_implemented"] = "🚧 This feature is not yet implemented.",
            ["general.help"] = "Type `/help` for a list of commands."
        };
        
        var json = JsonSerializer.Serialize(defaultLocale, new JsonSerializerOptions { WriteIndented = true });
        var filePath = Path.Combine(_localesDirectory, "en-US.json");
        File.WriteAllText(filePath, json);
        
        _locales["en-US"] = defaultLocale;
        _logger.LogInformation("Created default locale file: {FilePath}", filePath);
    }
    
    public string GetString(string key, params object[] args)
    {
        return GetString(CurrentCulture, key, args);
    }
    
    public string GetString(string culture, string key, params object[] args)
    {
        // Try to get the string from the requested culture
        if (_locales.TryGetValue(culture, out var locale))
        {
            if (locale.TryGetValue(key, out var value))
            {
                try
                {
                    return args.Length > 0 ? string.Format(value, args) : value;
                }
                catch (FormatException ex)
                {
                    _logger.LogError(ex, "Failed to format localization string {Key}", key);
                    return value;
                }
            }
        }
        
        // Fallback to en-US
        if (culture != "en-US" && _locales.TryGetValue("en-US", out var enLocale))
        {
            if (enLocale.TryGetValue(key, out var value))
            {
                _logger.LogDebug("Key {Key} not found in {Culture}, using en-US fallback", key, culture);
                return args.Length > 0 ? string.Format(value, args) : value;
            }
        }
        
        // Key not found anywhere
        _logger.LogWarning("Localization key not found: {Key}", key);
        return $"[{key}]";
    }
    
    public IEnumerable<string> GetSupportedCultures()
    {
        return _locales.Keys;
    }
    
    public bool IsCultureSupported(string culture)
    {
        return _locales.ContainsKey(culture);
    }
}
