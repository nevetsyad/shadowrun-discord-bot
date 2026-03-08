// DatabaseService extension for Phase 5: Dynamic Content Engine
// This file adds the GetContext() method and Phase 5 related operations

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

public partial class DatabaseService
{
    /// <summary>
    /// Get the DbContext for use by ContentDatabaseService
    /// </summary>
    public ShadowrunDbContext GetContext()
    {
        return _context;
    }

    #region Phase 5: Session Content Data Operations

    /// <summary>
    /// Get session content data for dynamic content engine
    /// </summary>
    public async Task<SessionContentData?> GetSessionContentDataAsync(int sessionId)
    {
        return await _context.SessionContentData
            .FirstOrDefaultAsync(d => d.GameSessionId == sessionId);
    }

    /// <summary>
    /// Create or update session content data
    /// </summary>
    public async Task<SessionContentData> UpsertSessionContentDataAsync(SessionContentData data)
    {
        var existing = await GetSessionContentDataAsync(data.GameSessionId);

        if (existing == null)
        {
            data.CreatedAt = DateTime.UtcNow;
            data.UpdatedAt = DateTime.UtcNow;
            _context.SessionContentData.Add(data);
        }
        else
        {
            existing.CurrentDifficulty = data.CurrentDifficulty;
            existing.DifficultyLastAdjusted = data.DifficultyLastAdjusted;
            existing.DifficultyHistory = data.DifficultyHistory;
            existing.CurrentCampaignArc = data.CurrentCampaignArc;
            existing.ArcDescription = data.ArcDescription;
            existing.ArcProgress = data.ArcProgress;
            existing.ArcStartedAt = data.ArcStartedAt;
            existing.StoryArcsJson = data.StoryArcsJson;
            existing.StoryThemesJson = data.StoryThemesJson;
            existing.StoryPreferencesJson = data.StoryPreferencesJson;
            existing.LastLearningUpdate = data.LastLearningUpdate;
            existing.PendingConsequencesJson = data.PendingConsequencesJson;
            existing.UpdatedAt = DateTime.UtcNow;
            data = existing;
        }

        await _context.SaveChangesAsync();
        return data;
    }

    #endregion

    #region Phase 5: NPC Personality Operations

    /// <summary>
    /// Get NPC personality data
    /// </summary>
    public async Task<NPCPersonalityData?> GetNPCPersonalityDataAsync(int sessionId, string npcName)
    {
        return await _context.NPCPersonalityData
            .FirstOrDefaultAsync(d => d.GameSessionId == sessionId && d.NPCName == npcName);
    }

    /// <summary>
    /// Get all NPC personality data for a session
    /// </summary>
    public async Task<List<NPCPersonalityData>> GetAllNPCPersonalityDataAsync(int sessionId)
    {
        return await _context.NPCPersonalityData
            .Where(d => d.GameSessionId == sessionId)
            .ToListAsync();
    }

    /// <summary>
    /// Upsert NPC personality data
    /// </summary>
    public async Task<NPCPersonalityData> UpsertNPCPersonalityDataAsync(NPCPersonalityData data)
    {
        var existing = await GetNPCPersonalityDataAsync(data.GameSessionId, data.NPCName);

        if (existing == null)
        {
            data.CreatedAt = DateTime.UtcNow;
            data.UpdatedAt = DateTime.UtcNow;
            _context.NPCPersonalityData.Add(data);
        }
        else
        {
            existing.PersonalityJson = data.PersonalityJson;
            existing.InteractionCount = data.InteractionCount;
            existing.UpdatedAt = DateTime.UtcNow;
            data = existing;
        }

        await _context.SaveChangesAsync();
        return data;
    }

    /// <summary>
    /// Add NPC learning event
    /// </summary>
    public async Task<NPCLearningEvent> AddNPCLearningEventAsync(NPCLearningEvent evt)
    {
        evt.RecordedAt = DateTime.UtcNow;
        _context.NPCLearningEvents.Add(evt);
        await _context.SaveChangesAsync();
        return evt;
    }

    /// <summary>
    /// Get NPC learning events
    /// </summary>
    public async Task<List<NPCLearningEvent>> GetNPCLearningEventsAsync(int sessionId, string npcName, int limit = 50)
    {
        return await _context.NPCLearningEvents
            .Where(e => e.GameSessionId == sessionId && e.NPCName == npcName)
            .OrderByDescending(e => e.RecordedAt)
            .Take(limit)
            .ToListAsync();
    }

    #endregion

    #region Phase 5: Generated Content Operations

    /// <summary>
    /// Add generated content
    /// </summary>
    public async Task<GeneratedContent> AddGeneratedContentAsync(GeneratedContent content)
    {
        content.GeneratedAt = DateTime.UtcNow;
        _context.GeneratedContent.Add(content);
        await _context.SaveChangesAsync();
        return content;
    }

    /// <summary>
    /// Get generated content by ID
    /// </summary>
    public async Task<GeneratedContent?> GetGeneratedContentAsync(int contentId)
    {
        return await _context.GeneratedContent.FindAsync(contentId);
    }

    /// <summary>
    /// Get generated content for session
    /// </summary>
    public async Task<List<GeneratedContent>> GetSessionGeneratedContentAsync(int sessionId, string? contentType = null, int limit = 20)
    {
        var query = _context.GeneratedContent
            .Where(c => c.GameSessionId == sessionId);

        if (!string.IsNullOrEmpty(contentType))
            query = query.Where(c => c.ContentType == contentType);

        return await query
            .OrderByDescending(c => c.GeneratedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Update generated content
    /// </summary>
    public async Task UpdateGeneratedContentAsync(GeneratedContent content)
    {
        _context.GeneratedContent.Update(content);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Phase 5: Performance Metrics Operations

    /// <summary>
    /// Add performance metrics record
    /// </summary>
    public async Task<PerformanceMetricsRecord> AddPerformanceMetricsAsync(PerformanceMetricsRecord metrics)
    {
        metrics.RecordedAt = DateTime.UtcNow;
        _context.PerformanceMetricsRecords.Add(metrics);
        await _context.SaveChangesAsync();
        return metrics;
    }

    /// <summary>
    /// Get performance metrics history
    /// </summary>
    public async Task<List<PerformanceMetricsRecord>> GetPerformanceMetricsHistoryAsync(int sessionId, int limit = 30)
    {
        return await _context.PerformanceMetricsRecords
            .Where(m => m.GameSessionId == sessionId)
            .OrderByDescending(m => m.RecordedAt)
            .Take(limit)
            .ToListAsync();
    }

    #endregion

    #region Phase 5: Story Preferences Operations

    /// <summary>
    /// Get story preferences
    /// </summary>
    public async Task<StoryPreferencesRecord?> GetStoryPreferencesAsync(int sessionId)
    {
        return await _context.StoryPreferencesRecords
            .FirstOrDefaultAsync(p => p.GameSessionId == sessionId);
    }

    /// <summary>
    /// Upsert story preferences
    /// </summary>
    public async Task<StoryPreferencesRecord> UpsertStoryPreferencesAsync(StoryPreferencesRecord preferences)
    {
        var existing = await GetStoryPreferencesAsync(preferences.GameSessionId);

        if (existing == null)
        {
            preferences.CreatedAt = DateTime.UtcNow;
            preferences.UpdatedAt = DateTime.UtcNow;
            _context.StoryPreferencesRecords.Add(preferences);
        }
        else
        {
            existing.CombatPreference = preferences.CombatPreference;
            existing.SocialPreference = preferences.SocialPreference;
            existing.StealthPreference = preferences.StealthPreference;
            existing.TechPreference = preferences.TechPreference;
            existing.RiskTolerance = preferences.RiskTolerance;
            existing.HeroicTendency = preferences.HeroicTendency;
            existing.TotalChoicesRecorded = preferences.TotalChoicesRecorded;
            existing.UpdatedAt = DateTime.UtcNow;
            preferences = existing;
        }

        await _context.SaveChangesAsync();
        return preferences;
    }

    #endregion

    #region Phase 5: Campaign Arc Operations

    /// <summary>
    /// Add campaign arc
    /// </summary>
    public async Task<CampaignArcRecord> AddCampaignArcAsync(CampaignArcRecord arc)
    {
        arc.StartedAt = DateTime.UtcNow;
        _context.CampaignArcRecords.Add(arc);
        await _context.SaveChangesAsync();
        return arc;
    }

    /// <summary>
    /// Get active campaign arc for session
    /// </summary>
    public async Task<CampaignArcRecord?> GetActiveCampaignArcAsync(int sessionId)
    {
        return await _context.CampaignArcRecords
            .FirstOrDefaultAsync(a => a.GameSessionId == sessionId && !a.IsCompleted);
    }

    /// <summary>
    /// Get all campaign arcs for session
    /// </summary>
    public async Task<List<CampaignArcRecord>> GetCampaignArcsAsync(int sessionId)
    {
        return await _context.CampaignArcRecords
            .Where(a => a.GameSessionId == sessionId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Update campaign arc
    /// </summary>
    public async Task UpdateCampaignArcAsync(CampaignArcRecord arc)
    {
        _context.CampaignArcRecords.Update(arc);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Phase 5: Content Regeneration Operations

    /// <summary>
    /// Add content regeneration record
    /// </summary>
    public async Task<ContentRegeneration> AddContentRegenerationAsync(ContentRegeneration regen)
    {
        regen.RegeneratedAt = DateTime.UtcNow;
        _context.ContentRegenerations.Add(regen);
        await _context.SaveChangesAsync();
        return regen;
    }

    /// <summary>
    /// Get regeneration history
    /// </summary>
    public async Task<List<ContentRegeneration>> GetRegenerationHistoryAsync(int sessionId, int limit = 20)
    {
        return await _context.ContentRegenerations
            .Where(r => r.GameSessionId == sessionId)
            .OrderByDescending(r => r.RegeneratedAt)
            .Take(limit)
            .ToListAsync();
    }

    #endregion

    #region Phase 5: Game Session Extended Operations

    /// <summary>
    /// Get all active sessions for break detection
    /// </summary>
    public async Task<List<GameSession>> GetActiveSessionsAsync()
    {
        return await _context.GameSessions
            .Where(s => s.Status == SessionStatus.Active)
            .ToListAsync();
    }

    /// <summary>
    /// Get game session by ID
    /// </summary>
    public async Task<GameSession?> GetGameSessionAsync(int sessionId)
    {
        return await _context.GameSessions
            .Include(s => s.Participants)
            .Include(s => s.NarrativeEvents)
            .Include(s => s.PlayerChoices)
            .Include(s => s.NPCRelationships)
            .Include(s => s.ActiveMissions)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    /// <summary>
    /// Update game session
    /// </summary>
    public async Task UpdateGameSessionAsync(GameSession session)
    {
        _context.GameSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get guild game sessions
    /// </summary>
    public async Task<List<GameSession>> GetGuildGameSessionsAsync(ulong guildId, int limit = 50)
    {
        return await _context.GameSessions
            .Where(s => s.DiscordGuildId == guildId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Get completed sessions for guild
    /// </summary>
    public async Task<List<CompletedSession>> GetRecentCompletedSessionsAsync(ulong guildId, int limit = 10)
    {
        return await _context.CompletedSessions
            .Where(s => s.DiscordGuildId == guildId)
            .OrderByDescending(s => s.ArchivedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Add completed session
    /// </summary>
    public async Task<CompletedSession> AddCompletedSessionAsync(CompletedSession session)
    {
        session.ArchivedAt = DateTime.UtcNow;
        _context.CompletedSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    /// <summary>
    /// Get completed session by ID
    /// </summary>
    public async Task<CompletedSession?> GetCompletedSessionAsync(int completedSessionId)
    {
        return await _context.CompletedSessions
            .Include(s => s.Tags)
            .Include(s => s.Notes)
            .FirstOrDefaultAsync(s => s.Id == completedSessionId);
    }

    /// <summary>
    /// Search completed sessions
    /// </summary>
    public async Task<List<CompletedSession>> SearchCompletedSessionsAsync(
        ulong guildId,
        string? searchTerm = null,
        string? category = null,
        string? tag = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 20)
    {
        var query = _context.CompletedSessions
            .Where(s => s.DiscordGuildId == guildId);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(s =>
                (s.SessionName != null && s.SessionName.Contains(searchTerm)) ||
                (s.Outcome != null && s.Outcome.Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(category))
            query = query.Where(s => s.Category == category);

        if (startDate.HasValue)
            query = query.Where(s => s.StartedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.EndedAt <= endDate.Value);

        // Tag filtering would require joining with tags table
        // For now, skip tag filtering in this simplified implementation

        return await query
            .OrderByDescending(s => s.ArchivedAt)
            .Take(limit)
            .ToListAsync();
    }

    #endregion

    #region Phase 5: Session Management Operations

    /// <summary>
    /// Add session break
    /// </summary>
    public async Task<SessionBreak> AddSessionBreakAsync(SessionBreak sessionBreak)
    {
        sessionBreak.BreakStartedAt = DateTime.UtcNow;
        _context.SessionBreaks.Add(sessionBreak);
        await _context.SaveChangesAsync();
        return sessionBreak;
    }

    /// <summary>
    /// Get active session break
    /// </summary>
    public async Task<SessionBreak?> GetActiveSessionBreakAsync(int sessionId)
    {
        return await _context.SessionBreaks
            .FirstOrDefaultAsync(b => b.GameSessionId == sessionId && b.BreakEndedAt == null);
    }

    /// <summary>
    /// Get session breaks
    /// </summary>
    public async Task<List<SessionBreak>> GetSessionBreaksAsync(int sessionId)
    {
        return await _context.SessionBreaks
            .Where(b => b.GameSessionId == sessionId)
            .OrderBy(b => b.BreakStartedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Update session break
    /// </summary>
    public async Task UpdateSessionBreakAsync(SessionBreak sessionBreak)
    {
        _context.SessionBreaks.Update(sessionBreak);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Add session tag
    /// </summary>
    public async Task<SessionTag> AddSessionTagAsync(SessionTag tag)
    {
        tag.AddedAt = DateTime.UtcNow;
        _context.SessionTags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    /// <summary>
    /// Remove session tag
    /// </summary>
    public async Task RemoveSessionTagAsync(int sessionId, string tagName)
    {
        var tag = await _context.SessionTags
            .FirstOrDefaultAsync(t => t.GameSessionId == sessionId && t.TagName == tagName);

        if (tag != null)
        {
            _context.SessionTags.Remove(tag);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get session tags
    /// </summary>
    public async Task<List<SessionTag>> GetSessionTagsAsync(int sessionId)
    {
        return await _context.SessionTags
            .Where(t => t.GameSessionId == sessionId)
            .OrderBy(t => t.TagName)
            .ToListAsync();
    }

    /// <summary>
    /// Add session note
    /// </summary>
    public async Task<SessionNote> AddSessionNoteAsync(SessionNote note)
    {
        note.CreatedAt = DateTime.UtcNow;
        _context.SessionNotes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    /// <summary>
    /// Get session notes
    /// </summary>
    public async Task<List<SessionNote>> GetSessionNotesAsync(int sessionId, bool pinnedOnly = false)
    {
        var query = _context.SessionNotes
            .Where(n => n.GameSessionId == sessionId);

        if (pinnedOnly)
            query = query.Where(n => n.IsPinned);

        return await query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Delete session note
    /// </summary>
    public async Task DeleteSessionNoteAsync(int noteId)
    {
        var note = await _context.SessionNotes.FindAsync(noteId);
        if (note != null)
        {
            _context.SessionNotes.Remove(note);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get sessions by tag
    /// </summary>
    public async Task<List<GameSession>> GetSessionsByTagAsync(ulong guildId, string tagName)
    {
        return await _context.SessionTags
            .Where(t => t.TagName == tagName && t.GameSession.DiscordGuildId == guildId)
            .Select(t => t.GameSession)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Get sessions by category (from metadata)
    /// </summary>
    public async Task<List<GameSession>> GetSessionsByCategoryAsync(ulong guildId, string category)
    {
        // Category is stored in metadata JSON, simplified implementation
        return await _context.GameSessions
            .Where(s => s.DiscordGuildId == guildId && s.Metadata != null && s.Metadata.Contains(category))
            .ToListAsync();
    }

    /// <summary>
    /// Get child sessions (for campaign grouping)
    /// </summary>
    public async Task<List<GameSession>> GetChildSessionsAsync(int parentSessionId)
    {
        // Parent stored in metadata JSON, simplified implementation
        return await _context.GameSessions
            .Where(s => s.Metadata != null && s.Metadata.Contains($"\"ParentSessionId\":{parentSessionId}"))
            .ToListAsync();
    }

    #endregion

    #region Phase 5: Narrative Operations

    /// <summary>
    /// Add narrative event
    /// </summary>
    public async Task<NarrativeEvent> AddNarrativeEventAsync(NarrativeEvent evt)
    {
        evt.RecordedAt = DateTime.UtcNow;
        _context.NarrativeEvents.Add(evt);
        await _context.SaveChangesAsync();
        return evt;
    }

    /// <summary>
    /// Add player choice
    /// </summary>
    public async Task<PlayerChoice> AddPlayerChoiceAsync(PlayerChoice choice)
    {
        choice.MadeAt = DateTime.UtcNow;
        _context.PlayerChoices.Add(choice);
        await _context.SaveChangesAsync();
        return choice;
    }

    /// <summary>
    /// Get player choice
    /// </summary>
    public async Task<PlayerChoice?> GetPlayerChoiceAsync(int choiceId)
    {
        return await _context.PlayerChoices.FindAsync(choiceId);
    }

    /// <summary>
    /// Update player choice
    /// </summary>
    public async Task UpdatePlayerChoiceAsync(PlayerChoice choice)
    {
        _context.PlayerChoices.Update(choice);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Add NPC relationship
    /// </summary>
    public async Task<NPCRelationship> AddNPCRelationshipAsync(NPCRelationship relationship)
    {
        relationship.CreatedAt = DateTime.UtcNow;
        relationship.LastInteraction = DateTime.UtcNow;
        _context.NPCRelationships.Add(relationship);
        await _context.SaveChangesAsync();
        return relationship;
    }

    /// <summary>
    /// Update NPC relationship
    /// </summary>
    public async Task UpdateNPCRelationshipAsync(NPCRelationship relationship)
    {
        relationship.LastInteraction = DateTime.UtcNow;
        _context.NPCRelationships.Update(relationship);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Add active mission
    /// </summary>
    public async Task<ActiveMission> AddActiveMissionAsync(ActiveMission mission)
    {
        mission.AcceptedAt = DateTime.UtcNow;
        _context.ActiveMissions.Add(mission);
        await _context.SaveChangesAsync();
        return mission;
    }

    /// <summary>
    /// Get active mission
    /// </summary>
    public async Task<ActiveMission?> GetActiveMissionAsync(int missionId)
    {
        return await _context.ActiveMissions.FindAsync(missionId);
    }

    /// <summary>
    /// Update active mission
    /// </summary>
    public async Task UpdateActiveMissionAsync(ActiveMission mission)
    {
        _context.ActiveMissions.Update(mission);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Add mission definition
    /// </summary>
    public async Task<MissionDefinition> AddMissionDefinitionAsync(MissionDefinition mission)
    {
        mission.GeneratedAt = DateTime.UtcNow;
        _context.MissionDefinitions.Add(mission);
        await _context.SaveChangesAsync();
        return mission;
    }

    #endregion
}
