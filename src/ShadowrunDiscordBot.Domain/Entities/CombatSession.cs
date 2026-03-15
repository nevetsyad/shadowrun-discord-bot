using ShadowrunDiscordBot.Domain.Common;

namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// Combat session entity
/// </summary>
public class CombatSession : BaseEntity
{
    public ulong DiscordChannelId { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public ulong? WinnerId { get; set; }
    public string? WinnerType { get; set; } // "team", "elimination", etc.
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
