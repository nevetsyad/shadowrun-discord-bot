using ShadowrunDiscordBot.Domain.Common;

namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// Game session entity
/// </summary>
public class GameSession : BaseEntity
{
    public new DateTime? CreatedAt { get; set; }
    public new DateTime? UpdatedAt { get; set; }

    public ulong DiscordGuildId { get; set; }
    public ulong DiscordChannelId { get; set; }
    public ulong? DMUserId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? StoryNotes { get; set; }
}
