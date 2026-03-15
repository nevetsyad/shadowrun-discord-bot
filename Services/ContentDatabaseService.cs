using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;
using System.Text.Json;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Persistent content storage service for dynamic content engine.
/// Handles storage of generated content, performance metrics, learning data,
/// and enables content regeneration.
/// </summary>
public class ContentDatabaseService
{
    private readonly DatabaseService _database;
    private readonly ILogger<ContentDatabaseService> _logger;

    public ContentDatabaseService(DatabaseService database, ILogger<ContentDatabaseService> logger)
    {
        _database = database;
        _logger = logger;
    }

    #region Session Content Data

    /// <summary>
    /// Get session content data
    /// </summary>
    public async Task<SessionContentData?> GetSessionContentDataAsync(int sessionId)
    {
        try
        {
            var context = _database.GetContext();
            return await context.SessionContentData
                .FirstOrDefaultAsync(d => d.GameSessionId == sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session content data for session {SessionId}", sessionId);
            return null;
        }
    }

    /// <summary>
    /// Get or create session content data
    /// </summary>
    public async Task<SessionContentData> GetOrCreateSessionContentDataAsync(int sessionId)
    {
        var data = await GetSessionContentDataAsync(sessionId);

        if (data != null)
            return data;

        // Create new
        data = new SessionContentData
        {
            GameSessionId = sessionId,
            CurrentDifficulty = 5,
            DifficultyLastAdjusted = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            var context = _database.GetContext();
            context.SessionContentData.Add(data);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created content data for session {SessionId}", sessionId);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session content data for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Update session content data
    /// </summary>
    public async Task UpdateSessionContentDataAsync(SessionContentData data)
    {
        try
        {
            data.UpdatedAt = DateTime.UtcNow;

            var context = _database.GetContext();
            context.SessionContentData.Update(data);
            await context.SaveChangesAsync();

            _logger.LogDebug("Updated content data for session {SessionId}", data.GameSessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session content data for session {SessionId}", data.GameSessionId);
            throw;
        }
    }

    #endregion

    #region NPC Personality Data

    /// <summary>
    /// Get NPC personality data
    /// </summary>
    public async Task<NPCPersonalityData?> GetNPCPersonalityDataAsync(int sessionId, string npcName)
    {
        try
        {
            var context = _database.GetContext();
            return await context.NPCPersonalityData
                .FirstOrDefaultAsync(d => d.GameSessionId == sessionId && d.NPCName == npcName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get NPC personality data for {NPCName} in session {SessionId}", npcName, sessionId);
            return null;
        }
    }

    /// <summary>
    /// Get all NPC personality data for a session
    /// </summary>
    public async Task<List<NPCPersonalityData>> GetSessionNPCPersonalityDataAsync(int sessionId)
    {
        try
        {
            var context = _database.GetContext();
            return await context.NPCPersonalityData
                .Where(d => d.GameSessionId == sessionId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get NPC personality data for session {SessionId}", sessionId);
            return new List<NPCPersonalityData>();
        }
    }

    /// <summary>
    /// Update NPC personality data
    /// </summary>
    public async Task UpdateNPCPersonalityDataAsync(int sessionId, string npcName, string personalityJson)
    {
        try
        {
            var context = _database.GetContext();
            var data = await context.NPCPersonalityData
                .FirstOrDefaultAsync(d => d.GameSessionId == sessionId && d.NPCName == npcName);

            if (data == null)
            {
                data = new NPCPersonalityData
                {
                    GameSessionId = sessionId,
                    NPCName = npcName,
                    PersonalityJson = personalityJson,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    InteractionCount = 1
                };
                context.NPCPersonalityData.Add(data);
            }
            else
            {
                data.PersonalityJson = personalityJson;
                data.UpdatedAt = DateTime.UtcNow;
                data.InteractionCount++;
                context.NPCPersonalityData.Update(data);
            }

            await context.SaveChangesAsync();

            _logger.LogDebug("Updated NPC personality data for {NPCName} in session {SessionId}", npcName, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update NPC personality data for {NPCName}", npcName);
        }
    }

    /// <summary>
    /// Record NPC learning event
    /// </summary>
    public async Task RecordNPCLearningEventAsync(int sessionId, string npcName, string eventType, string description)
    {
        try
        {
            var context = _database.GetContext();
            var learningEvent = new NPCLearningEvent
            {
                GameSessionId = sessionId,
                NPCName = npcName,
                EventType = eventType,
                Description = description,
                RecordedAt = DateTime.UtcNow
            };

            context.NPCLearningEvents.Add(learningEvent);
            await context.SaveChangesAsync();

            _logger.LogDebug("Recorded NPC learning event for {NPCName}: {EventType}", npcName, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record NPC learning event for {NPCName}", npcName);
        }
    }

    /// <summary>
    /// Get NPC learning history
    /// </summary>
    public async Task<List<NPCLearningEvent>> GetNPCLearningHistoryAsync(int sessionId, string npcName, int limit = 50)
    {
        try
        {
            var context = _database.GetContext();
            return await context.NPCLearningEvents
                .Where(e => e.GameSessionId == sessionId && e.NPCName == npcName)
                .OrderByDescending(e => e.RecordedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get NPC learning history for {NPCName}", npcName);
            return new List<NPCLearningEvent>();
        }
    }

    #endregion

    #region Generated Content Storage

    /// <summary>
    /// Store generated content
    /// </summary>
    public async Task<int> StoreGeneratedContentAsync(
        int sessionId,
        string contentType,
        string contentJson,
        int? difficulty = null,
        string? context = null)
    {
        try
        {
            var context2 = _database.GetContext();
            var content = new GeneratedContent
            {
                GameSessionId = sessionId,
                ContentType = contentType,
                ContentJson = contentJson,
                Difficulty = difficulty,
                GenerationContext = context,
                GeneratedAt = DateTime.UtcNow,
                WasUsed = false,
                UserRating = null
            };

            context2.GeneratedContent.Add(content);
            await context2.SaveChangesAsync();

            _logger.LogInformation("Stored generated content of type {ContentType} for session {SessionId}", 
                contentType, sessionId);

            return content.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store generated content for session {SessionId}", sessionId);
            return 0;
        }
    }

    /// <summary>
    /// Get generated content by ID
    /// </summary>
    public async Task<GeneratedContent?> GetGeneratedContentAsync(int contentId)
    {
        try
        {
            var context = _database.GetContext();
            return await context.GeneratedContent.FindAsync(contentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get generated content {ContentId}", contentId);
            return null;
        }
    }

    /// <summary>
    /// Get generated content for session by type
    /// </summary>
    public async Task<List<GeneratedContent>> GetSessionGeneratedContentAsync(
        int sessionId,
        string? contentType = null,
        int limit = 20)
    {
        try
        {
            var context = _database.GetContext();
            var query = context.GeneratedContent
                .Where(c => c.GameSessionId == sessionId);

            if (!string.IsNullOrEmpty(contentType))
                query = query.Where(c => c.ContentType == contentType);

            return await query
                .OrderByDescending(c => c.GeneratedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get generated content for session {SessionId}", sessionId);
            return new List<GeneratedContent>();
        }
    }

    /// <summary>
    /// Mark content as used
    /// </summary>
    public async Task MarkContentUsedAsync(int contentId)
    {
        try
        {
            var context = _database.GetContext();
            var content = await context.GeneratedContent.FindAsync(contentId);
            if (content != null)
            {
                content.WasUsed = true;
                content.UsedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark content {ContentId} as used", contentId);
        }
    }

    /// <summary>
    /// Rate generated content
    /// </summary>
    public async Task RateContentAsync(int contentId, int rating, string? feedback = null)
    {
        try
        {
            var context = _database.GetContext();
            var content = await context.GeneratedContent.FindAsync(contentId);
            if (content != null)
            {
                content.UserRating = Math.Clamp(rating, 1, 5);
                content.UserFeedback = feedback;
                await context.SaveChangesAsync();

                _logger.LogInformation("Rated content {ContentId} as {Rating}/5", contentId, rating);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rate content {ContentId}", contentId);
        }
    }

    #endregion

    #region Performance Metrics Storage

    /// <summary>
    /// Store performance metrics snapshot
    /// </summary>
    public async Task StorePerformanceMetricsAsync(
        int sessionId,
        double skillCheckRate,
        double missionCompletionRate,
        double socialRate,
        double combatRate,
        double overallScore)
    {
        try
        {
            var context = _database.GetContext();
            var metrics = new PerformanceMetricsRecord
            {
                GameSessionId = sessionId,
                SkillCheckSuccessRate = skillCheckRate,
                MissionCompletionRate = missionCompletionRate,
                PositiveInteractionRate = socialRate,
                CombatVictoryRate = combatRate,
                OverallScore = overallScore,
                RecordedAt = DateTime.UtcNow
            };

            context.PerformanceMetricsRecords.Add(metrics);
            await context.SaveChangesAsync();

            _logger.LogDebug("Stored performance metrics for session {SessionId}: Score {Score:F1}", 
                sessionId, overallScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store performance metrics for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Get performance metrics history
    /// </summary>
    public async Task<List<PerformanceMetricsRecord>> GetPerformanceMetricsHistoryAsync(int sessionId, int limit = 30)
    {
        try
        {
            var context = _database.GetContext();
            return await context.PerformanceMetricsRecords
                .Where(m => m.GameSessionId == sessionId)
                .OrderByDescending(m => m.RecordedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics history for session {SessionId}", sessionId);
            return new List<PerformanceMetricsRecord>();
        }
    }

    /// <summary>
    /// Get average performance over time
    /// </summary>
    public async Task<PerformanceMetricsRecord> GetAveragePerformanceAsync(int sessionId, int days = 7)
    {
        var history = await GetPerformanceMetricsHistoryAsync(sessionId, days * 4); // ~4 snapshots per day max

        if (!history.Any())
        {
            return new PerformanceMetricsRecord
            {
                GameSessionId = sessionId,
                RecordedAt = DateTime.UtcNow
            };
        }

        return new PerformanceMetricsRecord
        {
            GameSessionId = sessionId,
            SkillCheckSuccessRate = history.Average(h => h.SkillCheckSuccessRate),
            MissionCompletionRate = history.Average(h => h.MissionCompletionRate),
            PositiveInteractionRate = history.Average(h => h.PositiveInteractionRate),
            CombatVictoryRate = history.Average(h => h.CombatVictoryRate),
            OverallScore = history.Average(h => h.OverallScore),
            RecordedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Story Preferences Storage

    /// <summary>
    /// Update story preferences
    /// </summary>
    public async Task UpdateStoryPreferencesAsync(int sessionId, StoryPreferences preferences)
    {
        try
        {
            var context = _database.GetContext();
            var existing = await context.StoryPreferencesRecords
                .FirstOrDefaultAsync(p => p.GameSessionId == sessionId);

            if (existing == null)
            {
                existing = new StoryPreferencesRecord
                {
                    GameSessionId = sessionId,
                    CreatedAt = DateTime.UtcNow
                };
                context.StoryPreferencesRecords.Add(existing);
            }

            existing.CombatPreference = preferences.CombatPreference;
            existing.SocialPreference = preferences.SocialPreference;
            existing.StealthPreference = preferences.StealthPreference;
            existing.TechPreference = preferences.TechPreference;
            existing.RiskTolerance = preferences.RiskTolerance;
            existing.HeroicTendency = preferences.HeroicTendency;
            existing.TotalChoicesRecorded = preferences.TotalChoicesRecorded;
            existing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogDebug("Updated story preferences for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update story preferences for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Get story preferences
    /// </summary>
    public async Task<StoryPreferences?> GetStoryPreferencesAsync(int sessionId)
    {
        try
        {
            var context = _database.GetContext();
            var record = await context.StoryPreferencesRecords
                .FirstOrDefaultAsync(p => p.GameSessionId == sessionId);

            if (record == null) return null;

            return new StoryPreferences
            {
                CombatPreference = record.CombatPreference,
                SocialPreference = record.SocialPreference,
                StealthPreference = record.StealthPreference,
                TechPreference = record.TechPreference,
                RiskTolerance = record.RiskTolerance,
                HeroicTendency = record.HeroicTendency,
                TotalChoicesRecorded = record.TotalChoicesRecorded
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get story preferences for session {SessionId}", sessionId);
            return null;
        }
    }

    #endregion

    #region Content Regeneration

    /// <summary>
    /// Add content regeneration record
    /// </summary>
    public async Task AddContentRegenerationAsync(int sessionId, ContentType contentType, string content)
    {
        try
        {
            var context = _database.GetContext();
            var regen = new ContentRegeneration
            {
                GameSessionId = sessionId,
                ContentType = contentType.ToString(),
                ContentJson = content,
                RegeneratedAt = DateTime.UtcNow
            };

            context.ContentRegenerations.Add(regen);
            await context.SaveChangesAsync();

            _logger.LogInformation("Added content regeneration of type {ContentType} for session {SessionId}",
                contentType, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add content regeneration for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Get regeneration history
    /// </summary>
    public async Task<List<ContentRegeneration>> GetRegenerationHistoryAsync(int sessionId, int limit = 20)
    {
        try
        {
            var context = _database.GetContext();
            return await context.ContentRegenerations
                .Where(r => r.GameSessionId == sessionId)
                .OrderByDescending(r => r.RegeneratedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get regeneration history for session {SessionId}", sessionId);
            return new List<ContentRegeneration>();
        }
    }

    #endregion

    #region Campaign Arc Storage

    /// <summary>
    /// Store campaign arc
    /// </summary>
    public async Task StoreCampaignArcAsync(int sessionId, string arcName, string description, List<string> milestones)
    {
        try
        {
            var context = _database.GetContext();
            var arc = new CampaignArcRecord
            {
                GameSessionId = sessionId,
                ArcName = arcName,
                Description = description,
                MilestonesJson = JsonSerializer.Serialize(milestones),
                StartedAt = DateTime.UtcNow,
                IsCompleted = false
            };

            context.CampaignArcRecords.Add(arc);
            await context.SaveChangesAsync();

            _logger.LogInformation("Stored campaign arc '{ArcName}' for session {SessionId}", arcName, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store campaign arc for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Get active campaign arc
    /// </summary>
    public async Task<CampaignArcRecord?> GetActiveCampaignArcAsync(int sessionId)
    {
        try
        {
            var context = _database.GetContext();
            return await context.CampaignArcRecords
                .FirstOrDefaultAsync(a => a.GameSessionId == sessionId && !a.IsCompleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active campaign arc for session {SessionId}", sessionId);
            return null;
        }
    }

    /// <summary>
    /// Complete campaign arc
    /// </summary>
    public async Task CompleteCampaignArcAsync(int arcId, string? summary = null)
    {
        try
        {
            var context = _database.GetContext();
            var arc = await context.CampaignArcRecords.FindAsync(arcId);
            if (arc != null)
            {
                arc.IsCompleted = true;
                arc.CompletedAt = DateTime.UtcNow;
                arc.CompletionSummary = summary;
                await context.SaveChangesAsync();

                _logger.LogInformation("Completed campaign arc {ArcId}", arcId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete campaign arc {ArcId}", arcId);
        }
    }

    /// <summary>
    /// Get all campaign arcs for session
    /// </summary>
    public async Task<List<CampaignArcRecord>> GetCampaignArcsAsync(int sessionId)
    {
        try
        {
            var context = _database.GetContext();
            return await context.CampaignArcRecords
                .Where(a => a.GameSessionId == sessionId)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get campaign arcs for session {SessionId}", sessionId);
            return new List<CampaignArcRecord>();
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Cleanup old content data
    /// </summary>
    public async Task CleanupOldDataAsync(int daysToKeep = 90)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var context = _database.GetContext();

            // Cleanup old performance metrics
            var oldMetrics = await context.PerformanceMetricsRecords
                .Where(m => m.RecordedAt < cutoffDate)
                .ToListAsync();
            context.PerformanceMetricsRecords.RemoveRange(oldMetrics);

            // Cleanup old unused generated content
            var oldContent = await context.GeneratedContent
                .Where(c => c.GeneratedAt < cutoffDate && !c.WasUsed)
                .ToListAsync();
            context.GeneratedContent.RemoveRange(oldContent);

            // Cleanup old regenerations
            var oldRegens = await context.ContentRegenerations
                .Where(r => r.RegeneratedAt < cutoffDate)
                .ToListAsync();
            context.ContentRegenerations.RemoveRange(oldRegens);

            // Cleanup old NPC learning events
            var oldEvents = await context.NPCLearningEvents
                .Where(e => e.RecordedAt < cutoffDate)
                .ToListAsync();
            context.NPCLearningEvents.RemoveRange(oldEvents);

            await context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {MetricsCount} metrics, {ContentCount} content, {RegenCount} regens, {EventCount} events older than {Days} days",
                oldMetrics.Count, oldContent.Count, oldRegens.Count, oldEvents.Count, daysToKeep);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old data");
        }
    }

    /// <summary>
    /// Get content statistics for session
    /// </summary>
    public async Task<ContentStatistics> GetContentStatisticsAsync(int sessionId)
    {
        try
        {
            var context = _database.GetContext();

            var stats = new ContentStatistics
            {
                SessionId = sessionId,
                TotalGeneratedContent = await context.GeneratedContent
                    .CountAsync(c => c.GameSessionId == sessionId),
                UsedContent = await context.GeneratedContent
                    .CountAsync(c => c.GameSessionId == sessionId && c.WasUsed),
                AverageRating = await context.GeneratedContent
                    .Where(c => c.GameSessionId == sessionId && c.UserRating.HasValue)
                    .AverageAsync(c => c.UserRating!.Value),
                TotalNPCProfiles = await context.NPCPersonalityData
                    .CountAsync(p => p.GameSessionId == sessionId),
                TotalLearningEvents = await context.NPCLearningEvents
                    .CountAsync(e => e.GameSessionId == sessionId),
                PerformanceSnapshots = await context.PerformanceMetricsRecords
                    .CountAsync(m => m.GameSessionId == sessionId),
                CampaignArcs = await context.CampaignArcRecords
                    .CountAsync(a => a.GameSessionId == sessionId),
                Regenerations = await context.ContentRegenerations
                    .CountAsync(r => r.GameSessionId == sessionId)
            };

            stats.UsageRate = stats.TotalGeneratedContent > 0
                ? (double)stats.UsedContent / stats.TotalGeneratedContent
                : 0;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content statistics for session {SessionId}", sessionId);
            return new ContentStatistics { SessionId = sessionId };
        }
    }

    #endregion
}

#region Database Entities

/// <summary>
/// Session content data entity
/// </summary>
public class SessionContentData
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    
    // Difficulty tracking
    public int CurrentDifficulty { get; set; } = 5;
    public DateTime? DifficultyLastAdjusted { get; set; }
    public string? DifficultyHistory { get; set; } // JSON
    
    // Campaign tracking
    public string? CurrentCampaignArc { get; set; }
    public string? ArcDescription { get; set; }
    public int ArcProgress { get; set; }
    public DateTime? ArcStartedAt { get; set; }
    public string? StoryArcsJson { get; set; } // JSON list of StoryArc
    
    // Story themes
    public string? StoryThemesJson { get; set; } // JSON list of strings
    
    // Learning data
    public string? StoryPreferencesJson { get; set; } // JSON
    public DateTime? LastLearningUpdate { get; set; }
    
    // Pending consequences
    public string? PendingConsequencesJson { get; set; } // JSON
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// NPC personality data entity
/// </summary>
public class NPCPersonalityData
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public string NPCName { get; set; } = string.Empty;
    public string? PersonalityJson { get; set; } // JSON NPCPersonalityModel
    public int InteractionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// NPC learning event entity
/// </summary>
public class NPCLearningEvent
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public string NPCName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}

/// <summary>
/// Generated content entity
/// </summary>
public class GeneratedContent
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
    public int? Difficulty { get; set; }
    public string? GenerationContext { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool WasUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public int? UserRating { get; set; }
    public string? UserFeedback { get; set; }
}

/// <summary>
/// Performance metrics record entity
/// </summary>
public class PerformanceMetricsRecord
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public double SkillCheckSuccessRate { get; set; }
    public double MissionCompletionRate { get; set; }
    public double PositiveInteractionRate { get; set; }
    public double CombatVictoryRate { get; set; }
    public double OverallScore { get; set; }
    public DateTime RecordedAt { get; set; }
}

/// <summary>
/// Story preferences record entity
/// </summary>
public class StoryPreferencesRecord
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public double CombatPreference { get; set; }
    public double SocialPreference { get; set; }
    public double StealthPreference { get; set; }
    public double TechPreference { get; set; }
    public double RiskTolerance { get; set; }
    public double HeroicTendency { get; set; }
    public int TotalChoicesRecorded { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Content regeneration entity
/// </summary>
public class ContentRegeneration
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
    public DateTime RegeneratedAt { get; set; }
}

/// <summary>
/// Campaign arc record entity
/// </summary>
public class CampaignArcRecord
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public string ArcName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? MilestonesJson { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
    public string? CompletionSummary { get; set; }
}

#endregion

#region Data Transfer Objects

/// <summary>
/// Content statistics summary
/// </summary>
public class ContentStatistics
{
    public int SessionId { get; set; }
    public int TotalGeneratedContent { get; set; }
    public int UsedContent { get; set; }
    public double UsageRate { get; set; }
    public double AverageRating { get; set; }
    public int TotalNPCProfiles { get; set; }
    public int TotalLearningEvents { get; set; }
    public int PerformanceSnapshots { get; set; }
    public int CampaignArcs { get; set; }
    public int Regenerations { get; set; }
}

#endregion
