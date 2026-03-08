using System.ComponentModel.DataAnnotations;

namespace ShadowrunDiscordBot.Models;

/// <summary>
/// Combat session tracking for turn-based mechanics
/// </summary>
public class CombatSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    public ulong DiscordChannelId { get; set; }

    [Required]
    public ulong DiscordGuildId { get; set; }

    public bool IsActive { get; set; } = true;

    public int CurrentPass { get; set; } = 1;

    public int CurrentTurn { get; set; } = 1;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    // Navigation properties
    public virtual ICollection<CombatParticipant> Participants { get; set; } = new List<CombatParticipant>();
    public virtual ICollection<CombatAction> Actions { get; set; } = new List<CombatAction>();
}

/// <summary>
/// Participant in a combat session
/// </summary>
public class CombatParticipant
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CombatSessionId { get; set; }

    public int? CharacterId { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; } // For NPCs

    [Required]
    public int Initiative { get; set; }

    [Required]
    public int InitiativePasses { get; set; } = 1;

    public int CurrentPass { get; set; } = 1;

    public bool HasActed { get; set; } = false;

    public bool IsNPC { get; set; } = false;

    [Required]
    public virtual CombatSession CombatSession { get; set; } = null!;

    public virtual ShadowrunCharacter? Character { get; set; }
}

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

    public int? TargetId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = "Attack"; // Attack, Defense, Spell, Matrix, etc.

    [MaxLength(500)]
    public string? Description { get; set; }

    public int DiceRolled { get; set; } = 0;

    public int Successes { get; set; } = 0;

    public int Damage { get; set; } = 0;

    [MaxLength(50)]
    public string? DamageType { get; set; } // Physical, Stun

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    public virtual CombatSession CombatSession { get; set; } = null!;

    [Required]
    public virtual CombatParticipant Actor { get; set; } = null!;

    public virtual CombatParticipant? Target { get; set; }
}

/// <summary>
/// Combat pool allocation for a character
/// </summary>
public class CombatPoolAllocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    public int CombatSessionId { get; set; }

    public int AttackPoolAllocated { get; set; } = 0;

    public int DefensePoolAllocated { get; set; } = 0;

    public int RemainingPool { get; set; } = 0;

    [Required]
    public virtual ShadowrunCharacter Character { get; set; } = null!;

    [Required]
    public virtual CombatSession CombatSession { get; set; } = null!;
}
