using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Service for managing narrative context, story continuity, player choices, and NPC relationships
/// Consolidates: NarrativeContextService, PlayerChoice tracking, NPC relationship management
/// </summary>
public class NarrativeContextService
{
    private readonly DatabaseService _database;
    private readonly ILogger<NarrativeContextService> _logger;
    private readonly GameSessionService _sessionService;

    public NarrativeContextService(
        DatabaseService database,
        GameSessionService sessionService,
        ILogger<NarrativeContextService> logger)
    {
        _database = database;
        _sessionService = sessionService;
        _logger = logger;
    }

    #region Narrative Events

    /// <summary>
    /// Record a narrative event for story continuity
    /// </summary>
    public async Task<NarrativeEvent> RecordEventAsync(
        ulong channelId,
        string title,
        string description,
        NarrativeEventType eventType = NarrativeEventType.StoryBeat,
        string? location = null,
        string? npcsInvolved = null,
        int importance = 5,
        string? tags = null)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            throw new InvalidOperationException("No active session in this channel");
        }

        var narrativeEvent = new NarrativeEvent
        {
            GameSessionId = session.Id,
            Title = title,
            Description = description,
            EventType = eventType,
            InGameDateTime = session.InGameDateTime,
            RecordedAt = DateTime.UtcNow,
            Location = location ?? session.CurrentLocation,
            NPCsInvolved = npcsInvolved,
            Importance = importance,
            Tags = tags
        };

        await _database.AddNarrativeEventAsync(narrativeEvent);

        // Update session activity
        await _sessionService.UpdateActivityAsync(channelId);

        _logger.LogInformation("Recorded narrative event '{Title}' in session {SessionId}", title, session.Id);

        return narrativeEvent;
    }

    /// <summary>
    /// Get recent narrative events for a session
    /// </summary>
    public async Task<List<NarrativeEvent>> GetRecentEventsAsync(ulong channelId, int count = 10)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            return new List<NarrativeEvent>();
        }

        return session.NarrativeEvents
            .OrderByDescending(e => e.RecordedAt)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Search narrative events by tags or keywords
    /// </summary>
    public async Task<List<NarrativeEvent>> SearchEventsAsync(ulong channelId, string searchTerm)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            return new List<NarrativeEvent>();
        }

        return session.NarrativeEvents
            .Where(e => 
                (e.Tags != null && e.Tags.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                e.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.RecordedAt)
            .ToList();
    }

    /// <summary>
    /// Get events involving a specific NPC
    /// </summary>
    public async Task<List<NarrativeEvent>> GetNPCEventsAsync(ulong channelId, string npcName)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            return new List<NarrativeEvent>();
        }

        return session.NarrativeEvents
            .Where(e => e.NPCsInvolved != null && 
                       e.NPCsInvolved.Contains(npcName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.RecordedAt)
            .ToList();
    }

    /// <summary>
    /// Generate a story summary for the session
    /// </summary>
    public async Task<string> GenerateStorySummaryAsync(ulong channelId, int maxEvents = 20)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            return "No active session found.";
        }

        var events = await GetRecentEventsAsync(channelId, maxEvents);
        if (events.Count == 0)
        {
            return "No narrative events recorded yet.";
        }

        var summary = $"**Session: {session.SessionName ?? "Unnamed"}**\n";
        summary += $"**Location:** {session.CurrentLocation}\n\n";
        summary += "**Story So Far:**\n";

        foreach (var evt in events.OrderBy(e => e.RecordedAt))
        {
            var emoji = evt.EventType switch
            {
                NarrativeEventType.Combat => "⚔️",
                NarrativeEventType.Social => "💬",
                NarrativeEventType.Investigation => "🔍",
                NarrativeEventType.PlotTwist => "🎭",
                NarrativeEventType.CharacterDevelopment => "📖",
                NarrativeEventType.WorldEvent => "🌍",
                NarrativeEventType.PlayerChoice => "🎯",
                _ => "📝"
            };

            summary += $"{emoji} **{evt.Title}**\n{evt.Description}\n";
            if (!string.IsNullOrEmpty(evt.Location))
            {
                summary += $"   📍 {evt.Location}\n";
            }
            summary += "\n";
        }

        return summary;
    }

    #endregion

    #region Player Choices

    /// <summary>
    /// Record a player choice
    /// </summary>
    public async Task<PlayerChoice> RecordChoiceAsync(
        ulong channelId,
        ulong userId,
        string choiceDescription,
        string playerDecision,
        string? consequences = null)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            throw new InvalidOperationException("No active session in this channel");
        }

        var choice = new PlayerChoice
        {
            GameSessionId = session.Id,
            DiscordUserId = userId,
            ChoiceDescription = choiceDescription,
            PlayerDecision = playerDecision,
            Consequences = consequences,
            MadeAt = DateTime.UtcNow,
            IsResolved = !string.IsNullOrEmpty(consequences)
        };

        await _database.AddPlayerChoiceAsync(choice);

        _logger.LogInformation("Recorded player choice for user {UserId} in session {SessionId}", userId, session.Id);

        return choice;
    }

    /// <summary>
    /// Record a player choice (mission decision version)
    /// </summary>
    public async Task<PlayerChoice> RecordChoiceAsync(
        ulong channelId,
        string decisionId,
        string optionId,
        string? consequences = null)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            throw new InvalidOperationException("No active session in this channel");
        }

        var choice = new PlayerChoice
        {
            GameSessionId = session.Id,
            DiscordUserId = 0, // System choice
            ChoiceDescription = decisionId,
            PlayerDecision = optionId,
            Consequences = consequences,
            MadeAt = DateTime.UtcNow,
            IsResolved = !string.IsNullOrEmpty(consequences)
        };

        await _database.AddPlayerChoiceAsync(choice);

        _logger.LogInformation("Recorded mission choice {DecisionId} -> {OptionId} in session {SessionId}", 
            decisionId, optionId, session.Id);

        return choice;
    }

    /// <summary>
    /// Update consequences for a previously made choice
    /// </summary>
    public async Task ResolveChoiceAsync(int choiceId, string consequences)
    {
        var choice = await _database.GetPlayerChoiceAsync(choiceId);
        if (choice != null)
        {
            choice.Consequences = consequences;
            choice.IsResolved = true;
            await _database.UpdatePlayerChoiceAsync(choice);

            _logger.LogInformation("Resolved player choice {ChoiceId}", choiceId);
        }
    }

    /// <summary>
    /// Get unresolved choices for a session
    /// </summary>
    public async Task<List<PlayerChoice>> GetUnresolvedChoicesAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            return new List<PlayerChoice>();
        }

        return session.PlayerChoices
            .Where(c => !c.IsResolved)
            .OrderBy(c => c.MadeAt)
            .ToList();
    }

    /// <summary>
    /// Get all choices made by a specific player
    /// </summary>
    public async Task<List<PlayerChoice>> GetPlayerChoicesAsync(ulong channelId, ulong userId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            return new List<PlayerChoice>();
        }

        return session.PlayerChoices
            .Where(c => c.DiscordUserId == userId)
            .OrderByDescending(c => c.MadeAt)
            .ToList();
    }

    #endregion

    #region NPC Relationships

    /// <summary>
    /// Create or update an NPC relationship
    /// </summary>
    public async Task<NPCRelationship> UpdateNPCRelationshipAsync(
        ulong channelId,
        string npcName,
        string? npcRole = null,
        string? organization = null,
        int attitudeDelta = 0,
        int trustDelta = 0,
        string? notes = null,
        string? interaction = null)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            throw new InvalidOperationException("No active session in this channel");
        }

        // Find existing relationship
        var relationship = session.NPCRelationships
            .FirstOrDefault(r => r.NPCName.Equals(npcName, StringComparison.OrdinalIgnoreCase));

        if (relationship == null)
        {
            // Create new relationship
            relationship = new NPCRelationship
            {
                GameSessionId = session.Id,
                NPCName = npcName,
                NPCRole = npcRole,
                Organization = organization,
                Attitude = Math.Clamp(attitudeDelta, -10, 10),
                TrustLevel = Math.Clamp(trustDelta, 0, 10),
                Notes = notes,
                InteractionHistory = interaction,
                CreatedAt = DateTime.UtcNow,
                LastInteraction = DateTime.UtcNow,
                IsActive = true
            };

            await _database.AddNPCRelationshipAsync(relationship);

            _logger.LogInformation("Created new NPC relationship with {NPCName} in session {SessionId}", 
                npcName, session.Id);
        }
        else
        {
            // Update existing relationship
            if (!string.IsNullOrEmpty(npcRole))
                relationship.NPCRole = npcRole;
            
            if (!string.IsNullOrEmpty(organization))
                relationship.Organization = organization;

            relationship.Attitude = Math.Clamp(relationship.Attitude + attitudeDelta, -10, 10);
            relationship.TrustLevel = Math.Clamp(relationship.TrustLevel + trustDelta, 0, 10);

            if (!string.IsNullOrEmpty(notes))
                relationship.Notes = notes;

            if (!string.IsNullOrEmpty(interaction))
            {
                relationship.InteractionHistory += $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {interaction}";
            }

            relationship.LastInteraction = DateTime.UtcNow;

            await _database.UpdateNPCRelationshipAsync(relationship);

            _logger.LogDebug("Updated NPC relationship with {NPCName} (Attitude: {Attitude}, Trust: {Trust})", 
                npcName, relationship.Attitude, relationship.TrustLevel);
        }

        return relationship;
    }

    /// <summary>
    /// Get relationship with a specific NPC
    /// </summary>
    public async Task<NPCRelationship?> GetNPCRelationshipAsync(ulong channelId, string npcName)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            return null;
        }

        return session.NPCRelationships
            .FirstOrDefault(r => r.NPCName.Equals(npcName, StringComparison.OrdinalIgnoreCase) && r.IsActive);
    }

    /// <summary>
    /// Get all active NPC relationships for a session
    /// </summary>
    public async Task<List<NPCRelationship>> GetAllNPCRelationshipsAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            return new List<NPCRelationship>();
        }

        return session.NPCRelationships
            .Where(r => r.IsActive)
            .OrderBy(r => r.NPCName)
            .ToList();
    }

    /// <summary>
    /// Get NPCs by organization/faction
    /// </summary>
    public async Task<List<NPCRelationship>> GetNPCsByOrganizationAsync(ulong channelId, string organization)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            return new List<NPCRelationship>();
        }

        return session.NPCRelationships
            .Where(r => r.IsActive && 
                       r.Organization != null && 
                       r.Organization.Contains(organization, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.NPCName)
            .ToList();
    }

    /// <summary>
    /// Mark an NPC as inactive (dead, retired, etc.)
    /// </summary>
    public async Task DeactivateNPCAsync(ulong channelId, string npcName, string reason)
    {
        var relationship = await GetNPCRelationshipAsync(channelId, npcName);
        if (relationship != null)
        {
            relationship.IsActive = false;
            relationship.Notes = string.IsNullOrEmpty(relationship.Notes) 
                ? $"Deactivated: {reason}" 
                : $"{relationship.Notes}\nDeactivated: {reason}";
            
            await _database.UpdateNPCRelationshipAsync(relationship);

            _logger.LogInformation("Deactivated NPC {NPCName}: {Reason}", npcName, reason);
        }
    }

    /// <summary>
    /// Generate a relationship summary for display
    /// </summary>
    public string FormatRelationshipSummary(NPCRelationship relationship)
    {
        var attitudeEmoji = relationship.Attitude switch
        {
            >= 7 => "💚", // Allied
            >= 4 => "😊", // Friendly
            >= 1 => "😐", // Neutral-positive
            0 => "🤔", // Neutral
            >= -3 => "😒", // Wary
            >= -6 => "😠", // Unfriendly
            _ => "💀" // Hostile
        };

        var trustEmoji = relationship.TrustLevel switch
        {
            >= 8 => "🤝", // Complete trust
            >= 5 => "👍", // Good trust
            >= 3 => "👌", // Some trust
            _ => "❓" // Little/no trust
        };

        var status = relationship.IsActive ? "" : " ~~(Inactive)~~";

        var summary = $"{attitudeEmoji} {trustEmoji} **{relationship.NPCName}**{status}\n";
        
        if (!string.IsNullOrEmpty(relationship.NPCRole))
            summary += $"   📋 {relationship.NPCRole}\n";
        
        if (!string.IsNullOrEmpty(relationship.Organization))
            summary += $"   🏢 {relationship.Organization}\n";
        
        summary += $"   💭 Attitude: {relationship.Attitude}/10 | Trust: {relationship.TrustLevel}/10\n";
        
        if (!string.IsNullOrEmpty(relationship.Notes))
            summary += $"   📝 {relationship.Notes}\n";

        return summary;
    }

    #endregion

    #region Mission Management

    /// <summary>
    /// Add a new mission to the session
    /// </summary>
    public async Task<ActiveMission> AddMissionAsync(
        ulong channelId,
        string missionName,
        string missionType,
        string objective,
        long payment,
        int karmaReward,
        string? johnson = null,
        string? targetLocation = null,
        string? targetOrganization = null,
        DateTime? deadline = null)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            throw new InvalidOperationException("No active session in this channel");
        }

        var mission = new ActiveMission
        {
            GameSessionId = session.Id,
            MissionName = missionName,
            MissionType = missionType,
            Objective = objective,
            Johnson = johnson,
            PaymentOffered = payment,
            KarmaReward = karmaReward,
            TargetLocation = targetLocation,
            TargetOrganization = targetOrganization,
            Deadline = deadline,
            Status = MissionStatus.Planning,
            AcceptedAt = DateTime.UtcNow
        };

        await _database.AddActiveMissionAsync(mission);

        _logger.LogInformation("Added mission '{MissionName}' to session {SessionId}", missionName, session.Id);

        return mission;
    }

    /// <summary>
    /// Update mission status
    /// </summary>
    public async Task UpdateMissionStatusAsync(int missionId, MissionStatus status, string? notes = null)
    {
        var mission = await _database.GetActiveMissionAsync(missionId);
        if (mission != null)
        {
            mission.Status = status;
            
            if (!string.IsNullOrEmpty(notes))
                mission.Notes = notes;

            if (status == MissionStatus.Completed || status == MissionStatus.Failed || status == MissionStatus.Aborted)
            {
                mission.CompletedAt = DateTime.UtcNow;
            }

            await _database.UpdateActiveMissionAsync(mission);

            _logger.LogInformation("Updated mission {MissionId} status to {Status}", missionId, status);
        }
    }

    /// <summary>
    /// Get all active missions for a session
    /// </summary>
    public async Task<List<ActiveMission>> GetActiveMissionsAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
        {
            return new List<ActiveMission>();
        }

        return session.ActiveMissions
            .Where(m => m.Status == MissionStatus.Planning || m.Status == MissionStatus.InProgress)
            .OrderBy(m => m.Deadline ?? DateTime.MaxValue)
            .ToList();
    }

    /// <summary>
    /// Generate a mission summary for display
    /// </summary>
    public string FormatMissionSummary(ActiveMission mission)
    {
        var statusEmoji = mission.Status switch
        {
            MissionStatus.Planning => "📋",
            MissionStatus.InProgress => "⚔️",
            MissionStatus.Completed => "✅",
            MissionStatus.Failed => "❌",
            MissionStatus.Aborted => "🚫",
            _ => "❓"
        };

        var summary = $"{statusEmoji} **{mission.MissionName}** ({mission.MissionType})\n";
        summary += $"   🎯 {mission.Objective}\n";
        
        if (!string.IsNullOrEmpty(mission.Johnson))
            summary += $"   👤 Johnson: {mission.Johnson}\n";
        
        summary += $"   💰 Payment: {mission.PaymentOffered:N0}¥ | Karma: {mission.KarmaReward}\n";
        
        if (!string.IsNullOrEmpty(mission.TargetLocation))
            summary += $"   📍 Target: {mission.TargetLocation}\n";
        
        if (!string.IsNullOrEmpty(mission.TargetOrganization))
            summary += $"   🏢 Org: {mission.TargetOrganization}\n";
        
        if (mission.Deadline.HasValue)
            summary += $"   ⏰ Deadline: {mission.Deadline:yyyy-MM-dd HH:mm}\n";

        return summary;
    }

    #endregion
}
