using System.Text.Json.Serialization;

namespace ShadowrunDiscordBot.Models;

/// <summary>
/// Result of a dice roll operation
/// </summary>
public class DiceRollResult
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("rolls")]
    public List<int> Rolls { get; set; } = new List<int>();
    
    [JsonPropertyName("successes")]
    public int Successes { get; set; }
    
    [JsonPropertyName("explosion")]
    public bool Explosion { get; set; }
    
    [JsonPropertyName("criticalGlitch")]
    public bool CriticalGlitch { get; set; }
    
    [JsonPropertyName("ruleOfSix")]
    public bool RuleOfSix { get; set; }
    
    [JsonPropertyName("pool")]
    public int Pool { get; set; }
    
    [JsonPropertyName("keep")]
    public int Keep { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
