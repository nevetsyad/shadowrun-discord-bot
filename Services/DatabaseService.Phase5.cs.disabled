// DatabaseService extension for Phase 5: Dynamic Content Engine
// This file adds the GetContext() method and Phase 5 related operations

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Infrastructure.Data;

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

    // Game Session Extended Operations and Session Management Operations
    // are defined in DatabaseService.GameSession.cs and DatabaseService.SessionManagement.cs
    // Removed duplicate methods from this file to resolve CS0111 errors
}
