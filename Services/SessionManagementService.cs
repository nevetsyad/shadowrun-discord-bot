using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;
using System.Text.Json;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Comprehensive session lifecycle management service for Shadowrun campaigns.
/// Handles break detection, history tracking, organization, time tracking, and session metadata.
/// </summary>
public class SessionManagementService
{
    private readonly DatabaseService _database;
    private readonly GameSessionService _sessionService;
    private readonly NarrativeContextService _narrativeService;
    private readonly ILogger<SessionManagementService> _logger;
    
    // Configuration constants
    private static readonly TimeSpan BreakThreshold = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AutoResumeTimeout = TimeSpan.FromHours(2);
    private static readonly TimeSpan BreakReminderInterval = TimeSpan.FromMinutes(15);

    public SessionManagementService(
        DatabaseService database,
        GameSessionService sessionService,
        NarrativeContextService narrativeService,
        ILogger<SessionManagementService> logger)
    {
        _database = database;
        _sessionService = sessionService;
        _narrativeService = narrativeService;
        _logger = logger;
    }

    #region Break Handling

    /// <summary>
    /// Check for idle sessions and automatically pause them after the break threshold.
    /// Should be called periodically by a background service.
    /// </summary>
    public async Task<List<SessionBreak>> CheckForIdleSessionsAsync()
    {
        var autoBreaks = new List<SessionBreak>();

        try
        {
            // Get all active sessions
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            // FIX: MED-003 - Uses GetActiveSessionsAsync which now includes related data
            var activeSessions = await _database.GetActiveSessionsAsync().ConfigureAwait(false);

            foreach (var session in activeSessions)
            {
                var timeSinceLastActivity = DateTime.UtcNow - session.LastActivityAt;

                // Check if session should be auto-paused
                if (timeSinceLastActivity > BreakThreshold && session.Status == SessionStatus.Active)
                {
                    _logger.LogInformation("Session {SessionId} has been idle for {Minutes} minutes, auto-pausing",
                        session.Id, timeSinceLastActivity.TotalMinutes);

                    var sessionBreak = await StartBreakAsync(
                        session.DiscordChannelId,
                        reason: "Automatic break - no activity detected",
                        isAutomatic: true).ConfigureAwait(false);

                    autoBreaks.Add(sessionBreak);
                }
            }

            if (autoBreaks.Count > 0)
            {
                _logger.LogInformation("Auto-paused {Count} idle sessions", autoBreaks.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for idle sessions");
        }

        return autoBreaks;
    }

    /// <summary>
    /// Start a break for a session (manual or automatic)
    /// </summary>
    public async Task<SessionBreak> StartBreakAsync(
        ulong channelId,
        string? reason = null,
        bool isAutomatic = false,
        ulong? initiatedByUserId = null)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _sessionService.GetActiveSessionAsync(channelId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("No active session found");
        }

        // Pause the session
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _sessionService.PauseSessionAsync(channelId).ConfigureAwait(false);

        // Create break record
        var sessionBreak = new SessionBreak
        {
            GameSessionId = session.Id,
            BreakStartedAt = DateTime.UtcNow,
            Reason = reason ?? (isAutomatic ? "Automatic break" : "Manual break"),
            IsAutomatic = isAutomatic,
            InitiatedByUserId = initiatedByUserId,
            NotificationSent = false
        };

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.AddSessionBreakAsync(sessionBreak).ConfigureAwait(false);

        _logger.LogInformation("Started {BreakType} break for session {SessionId}: {Reason}",
            isAutomatic ? "automatic" : "manual", session.Id, sessionBreak.Reason);

        return sessionBreak;
    }

    /// <summary>
    /// End a break and resume the session
    /// </summary>
    public async Task<SessionBreak> EndBreakAsync(ulong channelId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _sessionService.GetPausedSessionAsync(channelId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("No paused session found");
        }

        // Get the active break
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var activeBreak = await _database.GetActiveSessionBreakAsync(session.Id).ConfigureAwait(false);
        if (activeBreak == null)
        {
            throw new InvalidOperationException("No active break found for this session");
        }

        // End the break
        activeBreak.BreakEndedAt = DateTime.UtcNow;
        activeBreak.DurationMinutes = (int)(activeBreak.BreakEndedAt.Value - activeBreak.BreakStartedAt).TotalMinutes;

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.UpdateSessionBreakAsync(activeBreak).ConfigureAwait(false);

        // Resume the session
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _sessionService.ResumeSessionAsync(channelId).ConfigureAwait(false);

        _logger.LogInformation("Ended break for session {SessionId}, duration: {Minutes} minutes",
            session.Id, activeBreak.DurationMinutes);

        return activeBreak;
    }

    /// <summary>
    /// Get all breaks for a session
    /// </summary>
    public async Task<List<SessionBreak>> GetSessionBreaksAsync(int sessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetSessionBreaksAsync(sessionId).ConfigureAwait(false);
    }

    /// <summary>
    /// Get break statistics for a session
    /// </summary>
    public async Task<SessionBreakStatistics> GetBreakStatisticsAsync(int sessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var breaks = await GetSessionBreaksAsync(sessionId).ConfigureAwait(false);

        var stats = new SessionBreakStatistics
        {
            TotalBreaks = breaks.Count,
            TotalBreakMinutes = breaks.Sum(b => b.DurationMinutes ?? 0),
            AutomaticBreaks = breaks.Count(b => b.IsAutomatic),
            ManualBreaks = breaks.Count(b => !b.IsAutomatic),
            AverageBreakMinutes = breaks.Any() ? breaks.Average(b => b.DurationMinutes ?? 0) : 0,
            LongestBreakMinutes = breaks.Any() ? breaks.Max(b => b.DurationMinutes ?? 0) : 0
        };

        return stats;
    }

    #endregion

    #region Session History & Archiving

    /// <summary>
    /// Archive a completed session to the history
    /// </summary>
    public async Task<CompletedSession> ArchiveSessionAsync(int sessionId, string? outcome = null)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _database.GetGameSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        if (session.Status != SessionStatus.Ended)
        {
            throw new InvalidOperationException("Can only archive ended sessions");
        }

        // Calculate session statistics
        var duration = session.EndedAt.HasValue 
            ? (session.EndedAt.Value - session.StartedAt) 
            : TimeSpan.Zero;

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var breaks = await GetSessionBreaksAsync(sessionId).ConfigureAwait(false);
        var totalBreakMinutes = breaks.Sum(b => b.DurationMinutes ?? 0);

        // Create completed session record
        var completedSession = new CompletedSession
        {
            OriginalSessionId = session.Id,
            DiscordChannelId = session.DiscordChannelId,
            DiscordGuildId = session.DiscordGuildId,
            GameMasterUserId = session.GameMasterUserId,
            SessionName = session.SessionName,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt ?? DateTime.UtcNow,
            DurationMinutes = (int)duration.TotalMinutes,
            ParticipantCount = session.Participants?.Count(p => p.IsActive) ?? 0,
            TotalBreaks = breaks.Count,
            TotalBreakMinutes = totalBreakMinutes,
            Outcome = outcome ?? session.Notes,
            Category = "General",
            ArchivedAt = DateTime.UtcNow
        };

        // Copy tags
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var sessionTags = await GetSessionTagsAsync(sessionId).ConfigureAwait(false);
        foreach (var tag in sessionTags)
        {
            completedSession.Tags.Add(new CompletedSessionTag
            {
                TagName = tag.TagName,
                Category = tag.Category
            });
        }

        // Copy notes
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var sessionNotes = await GetSessionNotesAsync(sessionId).ConfigureAwait(false);
        foreach (var note in sessionNotes)
        {
            completedSession.Notes.Add(new CompletedSessionNote
            {
                Content = note.Content,
                NoteType = note.NoteType,
                CreatedAt = note.CreatedAt,
                CreatedByUserId = note.CreatedByUserId
            });
        }

        // Save to database
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.AddCompletedSessionAsync(completedSession).ConfigureAwait(false);

        _logger.LogInformation("Archived session {SessionId} as completed session {CompletedId}",
            sessionId, completedSession.Id);

        return completedSession;
    }

    /// <summary>
    /// Search completed session history
    /// </summary>
    public async Task<List<CompletedSession>> SearchSessionHistoryAsync(
        ulong guildId,
        string? searchTerm = null,
        string? category = null,
        string? tag = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 20)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.SearchCompletedSessionsAsync(
            guildId, searchTerm, category, tag, startDate, endDate, limit).ConfigureAwait(false);
    }

    /// <summary>
    /// Get completed session by ID
    /// </summary>
    public async Task<CompletedSession?> GetCompletedSessionAsync(int completedSessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetCompletedSessionAsync(completedSessionId).ConfigureAwait(false);
    }

    /// <summary>
    /// Get recent completed sessions for a guild
    /// </summary>
    public async Task<List<CompletedSession>> GetRecentCompletedSessionsAsync(ulong guildId, int limit = 10)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetRecentCompletedSessionsAsync(guildId, limit).ConfigureAwait(false);
    }

    #endregion

    #region Session Organization

    /// <summary>
    /// Add a tag to a session
    /// </summary>
    public async Task<SessionTag> AddSessionTagAsync(
        int sessionId,
        string tagName,
        string? category = null,
        ulong addedByUserId = 0)
    {
        // Check if tag already exists
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var existingTags = await GetSessionTagsAsync(sessionId).ConfigureAwait(false);
        if (existingTags.Any(t => t.TagName.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Tag '{tagName}' already exists for this session");
        }

        var tag = new SessionTag
        {
            GameSessionId = sessionId,
            TagName = tagName,
            Category = category ?? "General",
            AddedAt = DateTime.UtcNow,
            AddedByUserId = addedByUserId
        };

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.AddSessionTagAsync(tag).ConfigureAwait(false);

        _logger.LogInformation("Added tag '{TagName}' to session {SessionId}", tagName, sessionId);

        return tag;
    }

    /// <summary>
    /// Remove a tag from a session
    /// </summary>
    public async Task RemoveSessionTagAsync(int sessionId, string tagName)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.RemoveSessionTagAsync(sessionId, tagName).ConfigureAwait(false);

        _logger.LogInformation("Removed tag '{TagName}' from session {SessionId}", tagName, sessionId);
    }

    /// <summary>
    /// Get all tags for a session
    /// </summary>
    public async Task<List<SessionTag>> GetSessionTagsAsync(int sessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetSessionTagsAsync(sessionId).ConfigureAwait(false);
    }

    /// <summary>
    /// Set session category (campaign, arc, one-shot, etc.)
    /// </summary>
    public async Task SetSessionCategoryAsync(int sessionId, string category)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _database.GetGameSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        // Store category in metadata
        var metadata = string.IsNullOrEmpty(session.Metadata)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(session.Metadata) 
              ?? new Dictionary<string, object>();

        metadata["Category"] = category;
        session.Metadata = JsonSerializer.Serialize(metadata);

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.UpdateGameSessionAsync(session).ConfigureAwait(false);

        _logger.LogInformation("Set category '{Category}' for session {SessionId}", category, sessionId);
    }

    /// <summary>
    /// Set parent session for campaign grouping
    /// </summary>
    public async Task SetParentSessionAsync(int sessionId, int? parentSessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _database.GetGameSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        // Store parent in metadata
        var metadata = string.IsNullOrEmpty(session.Metadata)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(session.Metadata) 
              ?? new Dictionary<string, object>();

        if (parentSessionId.HasValue)
        {
            metadata["ParentSessionId"] = parentSessionId.Value;
        }
        else
        {
            metadata.Remove("ParentSessionId");
        }

        session.Metadata = JsonSerializer.Serialize(metadata);
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.UpdateGameSessionAsync(session).ConfigureAwait(false);

        _logger.LogInformation("Set parent session {ParentId} for session {SessionId}",
            parentSessionId, sessionId);
    }

    /// <summary>
    /// Get sessions by tag
    /// </summary>
    public async Task<List<GameSession>> GetSessionsByTagAsync(ulong guildId, string tagName)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetSessionsByTagAsync(guildId, tagName).ConfigureAwait(false);
    }

    /// <summary>
    /// Get sessions by category
    /// </summary>
    public async Task<List<GameSession>> GetSessionsByCategoryAsync(ulong guildId, string category)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetSessionsByCategoryAsync(guildId, category).ConfigureAwait(false);
    }

    /// <summary>
    /// Get child sessions (for campaign grouping)
    /// </summary>
    public async Task<List<GameSession>> GetChildSessionsAsync(int parentSessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetChildSessionsAsync(parentSessionId).ConfigureAwait(false);
    }

    #endregion

    #region Time Tracking

    /// <summary>
    /// Get comprehensive time tracking statistics for a session
    /// </summary>
    public async Task<SessionTimeStatistics> GetTimeStatisticsAsync(int sessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _database.GetGameSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var breaks = await GetSessionBreaksAsync(sessionId).ConfigureAwait(false);
        var totalDuration = session.EndedAt.HasValue
            ? (session.EndedAt.Value - session.StartedAt)
            : (DateTime.UtcNow - session.StartedAt);

        var totalBreakTime = TimeSpan.FromMinutes(breaks.Sum(b => b.DurationMinutes ?? 0));
        var activeTime = totalDuration - totalBreakTime;

        var stats = new SessionTimeStatistics
        {
            SessionId = sessionId,
            SessionName = session.SessionName ?? "Unnamed",
            TotalDuration = totalDuration,
            ActiveTime = activeTime,
            TotalBreakTime = totalBreakTime,
            TotalBreaks = breaks.Count,
            AverageBreakDuration = breaks.Any() 
                ? TimeSpan.FromMinutes(breaks.Average(b => b.DurationMinutes ?? 0))
                : TimeSpan.Zero,
            Status = session.Status,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            LastActivityAt = session.LastActivityAt
        };

        return stats;
    }

    /// <summary>
    /// Get time statistics for all sessions in a guild
    /// </summary>
    public async Task<GuildTimeStatistics> GetGuildTimeStatisticsAsync(ulong guildId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var sessions = await _database.GetGuildGameSessionsAsync(guildId, 100).ConfigureAwait(false);
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var completedSessions = await _database.GetRecentCompletedSessionsAsync(guildId, 100).ConfigureAwait(false);

        var allSessions = new List<(int Id, string? Name, TimeSpan Duration, TimeSpan ActiveTime, int Breaks)>();

        // Add active/paused sessions
        foreach (var session in sessions.Where(s => s.Status != SessionStatus.Ended))
        {
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            var stats = await GetTimeStatisticsAsync(session.Id).ConfigureAwait(false);
            allSessions.Add((session.Id, session.SessionName, stats.TotalDuration, stats.ActiveTime, stats.TotalBreaks));
        }

        // Add completed sessions
        foreach (var completed in completedSessions)
        {
            var duration = TimeSpan.FromMinutes(completed.DurationMinutes);
            var breakTime = TimeSpan.FromMinutes(completed.TotalBreakMinutes);
            allSessions.Add((completed.Id, completed.SessionName, duration, duration - breakTime, completed.TotalBreaks));
        }

        var guildStats = new GuildTimeStatistics
        {
            TotalSessions = allSessions.Count,
            TotalDuration = allSessions.Aggregate(TimeSpan.Zero, (sum, s) => sum.Add(s.Duration)),
            TotalActiveTime = allSessions.Aggregate(TimeSpan.Zero, (sum, s) => sum.Add(s.ActiveTime)),
            TotalBreaks = allSessions.Sum(s => s.Breaks),
            AverageSessionDuration = allSessions.Any()
                ? TimeSpan.FromMinutes(allSessions.Average(s => s.Duration.TotalMinutes))
                : TimeSpan.Zero,
            LongestSession = allSessions.Any()
                ? allSessions.Max(s => s.Duration)
                : TimeSpan.Zero,
            SessionBreakdown = allSessions
                .OrderByDescending(s => s.Duration)
                .Take(10)
                .Select(s => new SessionTimeInfo
                {
                    SessionId = s.Id,
                    SessionName = s.Name ?? "Unnamed",
                    Duration = s.Duration,
                    ActiveTime = s.ActiveTime
                })
                .ToList()
        };

        return guildStats;
    }

    #endregion

    #region Session Notes & Metadata

    /// <summary>
    /// Add a note to a session
    /// </summary>
    public async Task<SessionNote> AddSessionNoteAsync(
        int sessionId,
        string content,
        string? noteType = null,
        ulong createdByUserId = 0,
        bool isPinned = false)
    {
        var note = new SessionNote
        {
            GameSessionId = sessionId,
            Content = content,
            NoteType = noteType ?? "General",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            IsPinned = isPinned
        };

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.AddSessionNoteAsync(note).ConfigureAwait(false);

        _logger.LogInformation("Added note to session {SessionId}: {Content}", sessionId, 
            content.Length > 50 ? content.Substring(0, 50) + "..." : content);

        return note;
    }

    /// <summary>
    /// Get all notes for a session
    /// </summary>
    public async Task<List<SessionNote>> GetSessionNotesAsync(int sessionId, bool pinnedOnly = false)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        return await _database.GetSessionNotesAsync(sessionId, pinnedOnly).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete a session note
    /// </summary>
    public async Task DeleteSessionNoteAsync(int noteId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.DeleteSessionNoteAsync(noteId).ConfigureAwait(false);

        _logger.LogInformation("Deleted session note {NoteId}", noteId);
    }

    /// <summary>
    /// Set custom metadata for a session
    /// </summary>
    public async Task SetSessionMetadataAsync(int sessionId, string key, object value)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _database.GetGameSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        var metadata = string.IsNullOrEmpty(session.Metadata)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(session.Metadata) 
              ?? new Dictionary<string, object>();

        metadata[key] = value;
        session.Metadata = JsonSerializer.Serialize(metadata);

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.UpdateGameSessionAsync(session).ConfigureAwait(false);

        _logger.LogInformation("Set metadata '{Key}' for session {SessionId}", key, sessionId);
    }

    /// <summary>
    /// Get custom metadata from a session
    /// </summary>
    public async Task<T?> GetSessionMetadataAsync<T>(int sessionId, string key)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _database.GetGameSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null || string.IsNullOrEmpty(session.Metadata))
        {
            return default;
        }

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(session.Metadata);
        if (metadata == null || !metadata.TryGetValue(key, out var value))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value.ToString() ?? "");
    }

    #endregion

    #region Session Completion

    /// <summary>
    /// Complete a session and optionally archive it
    /// </summary>
    public async Task<CompletedSession> CompleteSessionAsync(
        ulong channelId,
        string? outcome = null,
        bool autoArchive = true)
    {
        // End the session
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _sessionService.EndSessionAsync(channelId).ConfigureAwait(false);

        // Archive if requested
        if (autoArchive)
        {
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            return await ArchiveSessionAsync(session.Id, outcome).ConfigureAwait(false);
        }

        // Create minimal completed session record
        var duration = session.EndedAt.HasValue 
            ? (session.EndedAt.Value - session.StartedAt) 
            : TimeSpan.Zero;

        var completedSession = new CompletedSession
        {
            OriginalSessionId = session.Id,
            DiscordChannelId = session.DiscordChannelId,
            DiscordGuildId = session.DiscordGuildId,
            GameMasterUserId = session.GameMasterUserId,
            SessionName = session.SessionName,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt ?? DateTime.UtcNow,
            DurationMinutes = (int)duration.TotalMinutes,
            ParticipantCount = session.Participants?.Count(p => p.IsActive) ?? 0,
            Outcome = outcome ?? session.Notes,
            ArchivedAt = DateTime.UtcNow
        };

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        await _database.AddCompletedSessionAsync(completedSession).ConfigureAwait(false);

        _logger.LogInformation("Completed session {SessionId} without archiving", session.Id);

        return completedSession;
    }

    /// <summary>
    /// Get session summary for display
    /// </summary>
    public async Task<SessionSummary> GetSessionSummaryAsync(int sessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _database.GetGameSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var timeStats = await GetTimeStatisticsAsync(sessionId).ConfigureAwait(false);
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var breakStats = await GetBreakStatisticsAsync(sessionId).ConfigureAwait(false);
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var tags = await GetSessionTagsAsync(sessionId).ConfigureAwait(false);
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var notes = await GetSessionNotesAsync(sessionId, pinnedOnly: true).ConfigureAwait(false);
        var participants = (session.Participants ?? new List<SessionParticipant>()).Where(p => p.IsActive).ToList();

        var summary = new SessionSummary
        {
            SessionId = sessionId,
            SessionName = session.SessionName ?? "Unnamed Session",
            Status = session.Status,
            GameMasterUserId = session.GameMasterUserId,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Duration = timeStats.TotalDuration,
            ActiveTime = timeStats.ActiveTime,
            CurrentLocation = session.CurrentLocation,
            ParticipantCount = participants.Count,
            Participants = participants.Select(p => new ParticipantSummary
            {
                UserId = p.DiscordUserId,
                CharacterName = p.Character?.Name,
                SessionKarma = p.SessionKarma,
                SessionNuyen = p.SessionNuyen
            }).ToList(),
            Tags = tags.Select(t => t.TagName).ToList(),
            PinnedNotes = notes.Select(n => n.Content).ToList(),
            TotalBreaks = breakStats.TotalBreaks,
            TotalBreakTime = TimeSpan.FromMinutes(breakStats.TotalBreakMinutes),
            NarrativeEvents = session.NarrativeEvents?.Count ?? 0,
            PlayerChoices = session.PlayerChoices?.Count ?? 0,
            ActiveMissions = (session.ActiveMissions ?? new List<ActiveMission>()).Count(m => m.Status == MissionStatus.InProgress),
            CompletedMissions = (session.ActiveMissions ?? new List<ActiveMission>()).Count(m => m.Status == MissionStatus.Completed)
        };

        return summary;
    }

    #endregion
}

#region Data Transfer Objects

/// <summary>
/// Statistics about session breaks
/// </summary>
public class SessionBreakStatistics
{
    public int TotalBreaks { get; set; }
    public int TotalBreakMinutes { get; set; }
    public int AutomaticBreaks { get; set; }
    public int ManualBreaks { get; set; }
    public double AverageBreakMinutes { get; set; }
    public int LongestBreakMinutes { get; set; }
}

/// <summary>
/// Time tracking statistics for a session
/// </summary>
public class SessionTimeStatistics
{
    public int SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan ActiveTime { get; set; }
    public TimeSpan TotalBreakTime { get; set; }
    public int TotalBreaks { get; set; }
    public TimeSpan AverageBreakDuration { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}

/// <summary>
/// Time tracking statistics for a guild
/// </summary>
public class GuildTimeStatistics
{
    public int TotalSessions { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan TotalActiveTime { get; set; }
    public int TotalBreaks { get; set; }
    public TimeSpan AverageSessionDuration { get; set; }
    public TimeSpan LongestSession { get; set; }
    public List<SessionTimeInfo> SessionBreakdown { get; set; } = new();
}

/// <summary>
/// Individual session time information
/// </summary>
public class SessionTimeInfo
{
    public int SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public TimeSpan ActiveTime { get; set; }
}

/// <summary>
/// Session summary for display
/// </summary>
public class SessionSummary
{
    public int SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public SessionStatus Status { get; set; }
    public ulong GameMasterUserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public TimeSpan ActiveTime { get; set; }
    public string CurrentLocation { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public List<ParticipantSummary> Participants { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> PinnedNotes { get; set; } = new();
    public int TotalBreaks { get; set; }
    public TimeSpan TotalBreakTime { get; set; }
    public int NarrativeEvents { get; set; }
    public int PlayerChoices { get; set; }
    public int ActiveMissions { get; set; }
    public int CompletedMissions { get; set; }
}

/// <summary>
/// Participant summary information
/// </summary>
public class ParticipantSummary
{
    public ulong UserId { get; set; }
    public string? CharacterName { get; set; }
    public int SessionKarma { get; set; }
    public long SessionNuyen { get; set; }
}

#endregion
