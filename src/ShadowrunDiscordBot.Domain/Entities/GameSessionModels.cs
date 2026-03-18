using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// Represents an active game session with state tracking for Shadowrun campaigns
/// Consolidates: GMGameState, SessionState, and related tracking
/// </summary>
public class GameSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    public ulong DiscordChannelId { get; set; }

    [Required]
    public ulong DiscordGuildId { get; set; }

    [Required]
    public ulong GameMasterUserId { get; set; }

    /// <summary>
    /// Session name (e.g., "Shadows of Seattle", "Bug City Run")
    /// </summary>
    [MaxLength(200)]
    public string? SessionName { get; set; }

    /// <summary>
    /// Current in-game date/time in the Shadowrun timeline
    /// </summary>
    public DateTime? InGameDateTime { get; set; } = new DateTime(2063, 1, 1); // Default to 2063

    /// <summary>
    /// Current location name (e.g., "The Big Rhino", "Renraku Arcology")
    /// </summary>
    [MaxLength(200)]
    public string CurrentLocation { get; set; } = "Seattle Downtown";

    /// <summary>
    /// Detailed location description
    /// </summary>
    [MaxLength(1000)]
    public string? LocationDescription { get; set; }

    /// <summary>
    /// Session status (Active, Paused, Ended)
    /// </summary>
    [Required]
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>
    /// When the session started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the session ended or was paused
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Last activity timestamp (for break detection)
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Active players in this session
    /// </summary>
    public virtual ICollection<SessionParticipant> Participants { get; set; } = new List<SessionParticipant>();

    /// <summary>
    /// Narrative context and story continuity
    /// </summary>
    public virtual ICollection<NarrativeEvent> NarrativeEvents { get; set; } = new List<NarrativeEvent>();

    /// <summary>
    /// Player choices and consequences
    /// </summary>
    public virtual ICollection<PlayerChoice> PlayerChoices { get; set; } = new List<PlayerChoice>();

    /// <summary>
    /// NPC relationships and attitudes
    /// </summary>
    public virtual ICollection<NPCRelationship> NPCRelationships { get; set; } = new List<NPCRelationship>();

    /// <summary>
    /// Active missions for this session
    /// </summary>
    public virtual ICollection<ActiveMission> ActiveMissions { get; set; } = new List<ActiveMission>();

    /// <summary>
    /// Session notes and GM reminders
    /// </summary>
    [MaxLength(5000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Custom metadata (JSON serialized)
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Participant in a game session
/// </summary>
public class SessionParticipant
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameSessionId { get; set; }

    [Required]
    public ulong DiscordUserId { get; set; }

    /// <summary>
    /// Character being played in this session
    /// </summary>
    public int? CharacterId { get; set; }

    /// <summary>
    /// When player joined the session
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Player's current karma (session-specific)
    /// </summary>
    public int SessionKarma { get; set; } = 0;

    /// <summary>
    /// Player's current nuyen (session-specific)
    /// </summary>
    public long SessionNuyen { get; set; } = 0;

    /// <summary>
    /// Notes about this player's participation
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Is this player currently active in the session
    /// </summary>
    public bool IsActive { get; set; } = true;

    [Required]
    public virtual GameSession GameSession { get; set; } = null!;

    public virtual Character? Character { get; set; }
}

/// <summary>
/// Narrative events for story continuity
/// Consolidates: NarrativeContext, StoryBeats
/// </summary>
public class NarrativeEvent
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameSessionId { get; set; }

    /// <summary>
    /// Event title/summary
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what happened
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of narrative event
    /// </summary>
    [Required]
    public NarrativeEventType EventType { get; set; } = NarrativeEventType.StoryBeat;

    /// <summary>
    /// When this event occurred (in-game time)
    /// </summary>
    public DateTime? InGameDateTime { get; set; }

    /// <summary>
    /// When this was recorded (real time)
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// NPCs involved in this event
    /// </summary>
    [MaxLength(500)]
    public string? NPCsInvolved { get; set; } // Comma-separated NPC names

    /// <summary>
    /// Location where event occurred
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    /// <summary>
    /// Tags for searching (comma-separated)
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Importance level (1-10)
    /// </summary>
    public int Importance { get; set; } = 5;

    [Required]
    public virtual GameSession GameSession { get; set; } = null!;
}

/// <summary>
/// Player choices and their consequences
/// </summary>
public class PlayerChoice
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameSessionId { get; set; }

    /// <summary>
    /// Player who made the choice
    /// </summary>
    [Required]
    public ulong DiscordUserId { get; set; }

    /// <summary>
    /// The choice presented to the player
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ChoiceDescription { get; set; } = string.Empty;

    /// <summary>
    /// What the player chose
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string PlayerDecision { get; set; } = string.Empty;

    /// <summary>
    /// Consequences of this choice (filled in later)
    /// </summary>
    [MaxLength(1000)]
    public string? Consequences { get; set; }

    /// <summary>
    /// When the choice was made
    /// </summary>
    public DateTime MadeAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Was this choice resolved?
    /// </summary>
    public bool IsResolved { get; set; } = false;

    /// <summary>
    /// Related narrative event (if any)
    /// </summary>
    public int? RelatedNarrativeEventId { get; set; }

    [Required]
    public virtual GameSession GameSession { get; set; } = null!;

    public virtual NarrativeEvent? RelatedNarrativeEvent { get; set; }
}

/// <summary>
/// NPC relationships and attitudes toward players/factions
/// </summary>
public class NPCRelationship
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameSessionId { get; set; }

    /// <summary>
    /// NPC name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string NPCName { get; set; } = string.Empty;

    /// <summary>
    /// NPC role/occupation
    /// </summary>
    [MaxLength(100)]
    public string? NPCRole { get; set; }

    /// <summary>
    /// NPC organization/faction
    /// </summary>
    [MaxLength(100)]
    public string? Organization { get; set; }

    /// <summary>
    /// Overall attitude toward the team (-10 to +10)
    /// </summary>
    public int Attitude { get; set; } = 0; // 0 = neutral, -10 = hostile, +10 = allied

    /// <summary>
    /// Trust level (0-10)
    /// </summary>
    public int TrustLevel { get; set; } = 0;

    /// <summary>
    /// Notes about this NPC
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// How this NPC first met the team
    /// </summary>
    [MaxLength(500)]
    public string? FirstMeeting { get; set; }

    /// <summary>
    /// Key interactions with this NPC
    /// </summary>
    [MaxLength(2000)]
    public string? InteractionHistory { get; set; }

    /// <summary>
    /// When this relationship was established
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last interaction with this NPC
    /// </summary>
    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Is this NPC still alive/active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    [Required]
    public virtual GameSession GameSession { get; set; } = null!;
}

/// <summary>
/// Active mission/run for the session
/// </summary>
public class ActiveMission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameSessionId { get; set; }

    /// <summary>
    /// Mission name/title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string MissionName { get; set; } = string.Empty;

    /// <summary>
    /// Johnson or mission giver
    /// </summary>
    [MaxLength(100)]
    public string? Johnson { get; set; }

    /// <summary>
    /// Mission type (extraction, assassination, datasteal, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string MissionType { get; set; } = "Datasteal";

    /// <summary>
    /// Mission objective description
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Objective { get; set; } = string.Empty;

    /// <summary>
    /// Mission status
    /// </summary>
    [Required]
    public MissionStatus Status { get; set; } = MissionStatus.Planning;

    /// <summary>
    /// Offered payment in nuyen
    /// </summary>
    public long PaymentOffered { get; set; } = 0;

    /// <summary>
    /// Actual payment received
    /// </summary>
    public long? PaymentReceived { get; set; }

    /// <summary>
    /// Karma reward
    /// </summary>
    public int KarmaReward { get; set; } = 0;

    /// <summary>
    /// Target location
    /// </summary>
    [MaxLength(200)]
    public string? TargetLocation { get; set; }

    /// <summary>
    /// Target organization/corporation
    /// </summary>
    [MaxLength(100)]
    public string? TargetOrganization { get; set; }

    /// <summary>
    /// Mission deadline
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Mission notes and intel
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// When mission was accepted
    /// </summary>
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When mission was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    [Required]
    public virtual GameSession GameSession { get; set; } = null!;
}

/// <summary>
/// Session status enumeration
/// </summary>
public enum SessionStatus
{
    Active,
    Paused,
    Ended
}

/// <summary>
/// Narrative event types
/// </summary>
public enum NarrativeEventType
{
    StoryBeat,
    Combat,
    Social,
    Investigation,
    PlotTwist,
    CharacterDevelopment,
    WorldEvent,
    PlayerChoice
}

/// <summary>
/// Mission status enumeration
/// </summary>
public enum MissionStatus
{
    Planning,
    InProgress,
    Completed,
    Failed,
    Aborted
}

/// <summary>
/// Session break tracking for automatic pause/resume functionality
/// </summary>
public class SessionBreak
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameSessionId { get; set; }

    /// <summary>
    /// When the break started
    /// </summary>
    public DateTime BreakStartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the break ended (null if still on break)
    /// </summary>
    public DateTime? BreakEndedAt { get; set; }

    /// <summary>
    /// Reason for the break (automatic, manual, etc.)
    /// </summary>
    [MaxLength(200)]
    public string? Reason { get; set; }

    /// <summary>
    /// Duration of the break in minutes
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Whether this break was automatically detected
    /// </summary>
    public bool IsAutomatic { get; set; } = false;

    /// <summary>
    /// User who initiated the break (if manual)
    /// </summary>
    public ulong? InitiatedByUserId { get; set; }

    /// <summary>
    /// Notification sent about this break
    /// </summary>
    public bool NotificationSent { get; set; } = false;

    [Required]
    public virtual GameSession GameSession { get; set; } = null!;
}

/// <summary>
/// Session tags for organization and categorization
/// </summary>
public class SessionTag
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameSessionId { get; set; }

    /// <summary>
    /// Tag name/label
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Tag category (campaign, arc, group, theme, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// When this tag was added
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who added the tag
    /// </summary>
    public ulong AddedByUserId { get; set; }

    [Required]
    public virtual GameSession GameSession { get; set; } = null!;
}

/// <summary>
/// Session notes for metadata and GM reminders
/// </summary>
public class SessionNote
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameSessionId { get; set; }

    /// <summary>
    /// Note content
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Note type (general, player, outcome, reminder, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? NoteType { get; set; }

    /// <summary>
    /// When the note was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created the note
    /// </summary>
    public ulong CreatedByUserId { get; set; }

    /// <summary>
    /// Is this note pinned/important
    /// </summary>
    public bool IsPinned { get; set; } = false;

    [Required]
    public virtual GameSession GameSession { get; set; } = null!;
}

/// <summary>
/// Completed/archived session for history tracking
/// </summary>
public class CompletedSession
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Original session ID
    /// </summary>
    public int OriginalSessionId { get; set; }

    [Required]
    public ulong DiscordChannelId { get; set; }

    [Required]
    public ulong DiscordGuildId { get; set; }

    [Required]
    public ulong GameMasterUserId { get; set; }

    /// <summary>
    /// Session name
    /// </summary>
    [MaxLength(200)]
    public string? SessionName { get; set; }

    /// <summary>
    /// When the session started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the session ended
    /// </summary>
    public DateTime EndedAt { get; set; }

    /// <summary>
    /// Total session duration in minutes
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Number of participants
    /// </summary>
    public int ParticipantCount { get; set; }

    /// <summary>
    /// Total breaks taken
    /// </summary>
    public int TotalBreaks { get; set; }

    /// <summary>
    /// Total break time in minutes
    /// </summary>
    public int TotalBreakMinutes { get; set; }

    /// <summary>
    /// Session outcome/summary
    /// </summary>
    [MaxLength(2000)]
    public string? Outcome { get; set; }

    /// <summary>
    /// Session category (campaign, one-shot, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Parent session ID (for campaign grouping)
    /// </summary>
    public int? ParentSessionId { get; set; }

    /// <summary>
    /// When this was archived
    /// </summary>
    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Serialized metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Total karma awarded during the session (for tracking rewards)
    /// </summary>
    public int TotalKarmaAwarded { get; set; }

    /// <summary>
    /// Total nuyen awarded during the session (for tracking rewards)
    /// </summary>
    public long TotalNuyenAwarded { get; set; }

    /// <summary>
    /// Summary of session outcomes and key events
    /// </summary>
    [MaxLength(3000)]
    public string? Summary { get; set; }

    /// <summary>
    /// Tags associated with this session
    /// </summary>
    public virtual ICollection<CompletedSessionTag> Tags { get; set; } = new List<CompletedSessionTag>();

    /// <summary>
    /// Notes from this session
    /// </summary>
    public virtual ICollection<CompletedSessionNote> Notes { get; set; } = new List<CompletedSessionNote>();
}

/// <summary>
/// Tags for completed sessions
/// </summary>
public class CompletedSessionTag
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompletedSessionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string TagName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Category { get; set; }

    [Required]
    public virtual CompletedSession CompletedSession { get; set; } = null!;
}

/// <summary>
/// Notes for completed sessions
/// </summary>
public class CompletedSessionNote
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompletedSessionId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? NoteType { get; set; }

    public DateTime CreatedAt { get; set; }

    public ulong CreatedByUserId { get; set; }

    [Required]
    public virtual CompletedSession CompletedSession { get; set; } = null!;
}
