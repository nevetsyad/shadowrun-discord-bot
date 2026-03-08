using System.ComponentModel.DataAnnotations;

namespace ShadowrunDiscordBot.Models;

/// <summary>
/// Astral projection and perception tracking
/// </summary>
public class AstralState
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    public bool IsProjecting { get; set; } = false;

    public bool HasPerception { get; set; } = false;

    public DateTime? ProjectionStartedAt { get; set; }

    public int HoursProjected { get; set; } = 0;

    // Astral combat stats
    public int AstralCombatPool { get; set; } = 0;

    public int AstralDamage { get; set; } = 0;

    [MaxLength(200)]
    public string? CurrentLocation { get; set; }

    [Required]
    public virtual ShadowrunCharacter Character { get; set; } = null!;
}

/// <summary>
/// Astral signature tracking
/// </summary>
public class AstralSignature
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string SignatureType { get; set; } = "Spell"; // Spell, Spirit, Emotional

    public int Force { get; set; } = 0;

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public int? CharacterId { get; set; }

    public virtual ShadowrunCharacter? Character { get; set; }
}
