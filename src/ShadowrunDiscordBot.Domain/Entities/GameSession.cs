using ShadowrunDiscordBot.Domain.Common;
using ShadowrunDiscordBot.Models;

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
    public List<Character> Participants { get; set; } = [];
    public SessionStatus? Status { get; set; }
    public List<NarrativeEvent> NarrativeEvents { get; set; } = [];
    public List<PlayerChoice> PlayerChoices { get; set; } = [];
    public List<NPCRelationship> NPCRelationships { get; set; } = [];
    public List<Mission> ActiveMissions { get; set; } = [];
}
