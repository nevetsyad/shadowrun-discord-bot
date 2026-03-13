using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Service for managing game sessions, state tracking, and participants
/// Consolidates: GameSessionService, SessionBreakService
/// </summary>
public class GameSessionService
{
    private readonly DatabaseService _database;
    private readonly ILogger<GameSessionService> _logger;
    private readonly TimeSpan _breakThreshold = TimeSpan.FromMinutes(30);

    public GameSessionService(DatabaseService database, ILogger<GameSessionService> logger)
    {
        _database = database;
        _logger = logger;
    }

    #region Session Management

    /// <summary>
    /// Start a new game session in a Discord channel
    /// </summary>
    public async Task<GameSession> StartSessionAsync(ulong guildId, ulong channelId, ulong gmUserId, string? sessionName = null)
    {
        try
        {
            // Check if there's already an active session in this channel
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            var existingSession = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
            if (existingSession != null)
            {
                throw new InvalidOperationException($"Active session already exists in this channel (Session #{existingSession.Id})");
            }

            var session = new GameSession
            {
                DiscordGuildId = guildId,
                DiscordChannelId = channelId,
                GameMasterUserId = gmUserId,
                SessionName = sessionName ?? $"Shadowrun Session {DateTime.UtcNow:yyyy-MM-dd}",
                Status = SessionStatus.Active,
                StartedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                CurrentLocation = "Seattle Downtown"
            };

            // FIX: HIGH-001 - Added ConfigureAwait(false)
            await _database.AddGameSessionAsync(session).ConfigureAwait(false);

            _logger.LogInformation("Started game session {SessionId} in channel {ChannelId} by GM {GMUserId}",
                session.Id, channelId, gmUserId);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start game session in channel {ChannelId}", channelId);
            throw;
        }
    }

    /// <summary>
    /// End an active game session
    /// </summary>
    public async Task<GameSession> EndSessionAsync(ulong channelId)
    {
        try
        {
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            var session = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
            if (session == null)
            {
                throw new InvalidOperationException("No active session found in this channel");
            }

            session.Status = SessionStatus.Ended;
            session.EndedAt = DateTime.UtcNow;
            
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            await _database.UpdateGameSessionAsync(session).ConfigureAwait(false);

            _logger.LogInformation("Ended game session {SessionId} in channel {ChannelId}", session.Id, channelId);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end game session in channel {ChannelId}", channelId);
            throw;
        }
    }

    /// <summary>
    /// Pause an active game session
    /// </summary>
    public async Task<GameSession> PauseSessionAsync(ulong channelId)
    {
        try
        {
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            var session = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
            if (session == null)
            {
                throw new InvalidOperationException("No active session found in this channel");
            }

            session.Status = SessionStatus.Paused;
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            await _database.UpdateGameSessionAsync(session).ConfigureAwait(false);

            _logger.LogInformation("Paused game session {SessionId} in channel {ChannelId}", session.Id, channelId);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause game session in channel {ChannelId}", channelId);
            throw;
        }
    }

    /// <summary>
    /// Resume a paused game session
    /// </summary>
    public async Task<GameSession> ResumeSessionAsync(ulong channelId)
    {
        try
        {
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            var session = await GetPausedSessionAsync(channelId).ConfigureAwait(false);
            if (session == null)
            {
                throw new InvalidOperationException("No paused session found in this channel");
            }

            session.Status = SessionStatus.Active;
            session.LastActivityAt = DateTime.UtcNow;
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            await _database.UpdateGameSessionAsync(session).ConfigureAwait(false);

            _logger.LogInformation("Resumed game session {SessionId} in channel {ChannelId}", session.Id, channelId);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume game session in channel {ChannelId}", channelId);
            throw;
        }
    }

    /// <summary>
    /// Get the active session in a channel
    /// </summary>
    public async Task<GameSession?> GetActiveSessionAsync(ulong channelId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetActiveGameSessionAsync(channelId).ConfigureAwait(false);
    }

    /// <summary>
    /// Get a paused session in a channel
    /// </summary>
    public async Task<GameSession?> GetPausedSessionAsync(ulong channelId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetPausedGameSessionAsync(channelId).ConfigureAwait(false);
    }

    /// <summary>
    /// Get all sessions for a guild (active and recent)
    /// </summary>
    public async Task<List<GameSession>> GetGuildSessionsAsync(ulong guildId, int limit = 10)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetGuildGameSessionsAsync(guildId, limit).ConfigureAwait(false);
    }

    /// <summary>
    /// Update session activity timestamp (for break detection)
    /// </summary>
    public async Task UpdateActivityAsync(ulong channelId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            await _database.UpdateGameSessionAsync(session).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Check if a session has been on break and needs resumption
    /// </summary>
    public async Task<SessionBreakStatus> CheckBreakStatusAsync(ulong channelId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
        if (session == null)
        {
            return new SessionBreakStatus { HasActiveSession = false };
        }

        var timeSinceLastActivity = DateTime.UtcNow - session.LastActivityAt;
        var isOnBreak = timeSinceLastActivity > _breakThreshold;

        return new SessionBreakStatus
        {
            HasActiveSession = true,
            IsOnBreak = isOnBreak,
            TimeSinceLastActivity = timeSinceLastActivity,
            LastActivityAt = session.LastActivityAt,
            SessionId = session.Id
        };
    }

    #endregion

    #region Participant Management

    /// <summary>
    /// Add a participant to the session
    /// </summary>
    public async Task<SessionParticipant> AddParticipantAsync(ulong channelId, ulong userId, int? characterId = null)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("No active session in this channel");
        }

        // FIX: CRIT-001 - Add null check for Participants
        var participants = session.Participants ?? new List<SessionParticipant>();
        
        // Check if already a participant
        var existing = participants.FirstOrDefault(p => p.DiscordUserId == userId && p.IsActive);
        if (existing != null)
        {
            return existing;
        }

        var participant = new SessionParticipant
        {
            GameSessionId = session.Id,
            DiscordUserId = userId,
            CharacterId = characterId,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.AddSessionParticipantAsync(participant).ConfigureAwait(false);

        _logger.LogInformation("Added participant {UserId} to session {SessionId}", userId, session.Id);

        return participant;
    }

    /// <summary>
    /// Remove a participant from the session
    /// </summary>
    public async Task RemoveParticipantAsync(ulong channelId, ulong userId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("No active session in this channel");
        }

        // FIX: CRIT-001 - Add null check for Participants
        var participant = (session.Participants ?? new List<SessionParticipant>())
            .FirstOrDefault(p => p.DiscordUserId == userId && p.IsActive);
        
        if (participant != null)
        {
            participant.IsActive = false;
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            await _database.UpdateSessionParticipantAsync(participant).ConfigureAwait(false);

            _logger.LogInformation("Removed participant {UserId} from session {SessionId}", userId, session.Id);
        }
    }

    /// <summary>
    /// Get all active participants in a session
    /// </summary>
    public async Task<List<SessionParticipant>> GetActiveParticipantsAsync(ulong channelId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
        // FIX: CRIT-001 - Add null check for Participants
        return (session?.Participants ?? new List<SessionParticipant>()).Where(p => p.IsActive).ToList();
    }

    /// <summary>
    /// Update participant karma/nuyen
    /// </summary>
    public async Task UpdateParticipantRewardsAsync(ulong channelId, ulong userId, int karmaDelta, long nuyenDelta)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
        if (session == null) return;

        // FIX: CRIT-001 - Add null check for Participants
        var participant = (session.Participants ?? new List<SessionParticipant>())
            .FirstOrDefault(p => p.DiscordUserId == userId && p.IsActive);
        
        if (participant != null)
        {
            // FIX: MED-001 - Wrap multiple database updates in a transaction
            await _database.ExecuteInTransactionAsync(async () =>
            {
                participant.SessionKarma += karmaDelta;
                participant.SessionNuyen += nuyenDelta;
                await _database.UpdateSessionParticipantAsync(participant).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
    }

    #endregion

    #region Location Management

    /// <summary>
    /// Update the current location for the session
    /// </summary>
    public async Task UpdateLocationAsync(ulong channelId, string location, string? description = null)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
        if (session != null)
        {
            session.CurrentLocation = location;
            session.LocationDescription = description;
            session.LastActivityAt = DateTime.UtcNow;
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            await _database.UpdateGameSessionAsync(session).ConfigureAwait(false);

            _logger.LogDebug("Updated location to {Location} for session {SessionId}", location, session.Id);
        }
    }

    #endregion

    #region Session Progress

    /// <summary>
    /// Get session progress summary
    /// </summary>
    public async Task<SessionProgress> GetSessionProgressAsync(ulong channelId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await GetActiveSessionAsync(channelId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("No active session in this channel");
        }

        var duration = DateTime.UtcNow - session.StartedAt;
        // FIX: CRIT-001 - Add null check for Participants
        var participants = session.Participants ?? new List<SessionParticipant>();
        var activeParticipants = participants.Count(p => p.IsActive);
        // FIX: CRIT-001 - Add null check for NarrativeEvents
        var narrativeEvents = (session.NarrativeEvents ?? new List<NarrativeEvent>()).Count;
        // FIX: CRIT-001 - Add null check for PlayerChoices
        var playerChoices = (session.PlayerChoices ?? new List<PlayerChoice>()).Count;
        // FIX: CRIT-001 - Add null check for ActiveMissions
        var activeMissions = (session.ActiveMissions ?? new List<ActiveMission>())
            .Count(m => m.Status == MissionStatus.InProgress || m.Status == MissionStatus.Planning);
        var completedMissions = (session.ActiveMissions ?? new List<ActiveMission>())
            .Count(m => m.Status == MissionStatus.Completed);

        return new SessionProgress
        {
            SessionId = session.Id,
            SessionName = session.SessionName ?? "Unnamed Session",
            Duration = duration,
            ActiveParticipants = activeParticipants,
            CurrentLocation = session.CurrentLocation,
            NarrativeEvents = narrativeEvents,
            PlayerChoices = playerChoices,
            ActiveMissions = activeMissions,
            CompletedMissions = completedMissions,
            Status = session.Status
        };
    }

    #endregion
}

/// <summary>
/// Session break status information
/// </summary>
public class SessionBreakStatus
{
    public bool HasActiveSession { get; set; }
    public bool IsOnBreak { get; set; }
    public TimeSpan TimeSinceLastActivity { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int SessionId { get; set; }
}

/// <summary>
/// Session progress summary
/// </summary>
public class SessionProgress
{
    public int SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int ActiveParticipants { get; set; }
    public string CurrentLocation { get; set; } = string.Empty;
    public int NarrativeEvents { get; set; }
    public int PlayerChoices { get; set; }
    public int ActiveMissions { get; set; }
    public int CompletedMissions { get; set; }
    public SessionStatus Status { get; set; }
}
