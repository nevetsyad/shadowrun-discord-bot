using ShadowrunDiscordBot.Domain.Common;

namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// Matrix run/session entity
/// </summary>
public class MatrixRun : BaseEntity
{
    public new DateTime? CreatedAt { get; set; }
    public new DateTime? UpdatedAt { get; set; }

    public int CharacterId { get; set; }
    public ulong DiscordUserId { get; set; }
    public string RunName { get; set; } = string.Empty;
    public string? RunType { get; set; } // "ice_breaker", "data_dive", "hot_sims", etc.
    public int CurrentNode { get; set; }
    public int TotalNodes { get; set; }
    public int Score { get; set; }
    public int RequiredScore { get; set; }
    public bool IsActive { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
