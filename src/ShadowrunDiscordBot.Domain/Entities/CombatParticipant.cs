using ShadowrunDiscordBot.Domain.Common;

namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// Combat participant entity
/// </summary>
public class CombatParticipant : BaseEntity
{
    public int CombatSessionId { get; set; }
    public int CharacterId { get; set; }
    public ulong DiscordUserId { get; set; }
    public int TeamId { get; set; }
    public int HitPoints { get; set; }
    public int DamageTrack { get; set; } // 0=physical, 1=stun
    public bool IsEliminated { get; set; }
    public DateTime? JoinedAt { get; set; }
    public DateTime? EliminatedAt { get; set; }
    public Character? Character { get; set; }
}
