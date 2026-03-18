using System.ComponentModel.DataAnnotations;

namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// Combat action record
/// </summary>
public class CombatAction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CombatSessionId { get; set; }

    [Required]
    public int ActorId { get; set; }

    [MaxLength(100)]
    public string? ActorName { get; set; }

    public int? TargetId { get; set; }

    [MaxLength(100)]
    public string? TargetName { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = "Attack";

    [MaxLength(500)]
    public string? Description { get; set; }

    public int DiceRolled { get; set; } = 0;

    public int Successes { get; set; } = 0;

    public int Damage { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
