using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Database service extensions for game session and narrative management
/// </summary>
public partial class DatabaseService
{
    #region Game Session Operations

    /// <summary>
    /// Add a new game session
    /// </summary>
    public async Task<GameSession> AddGameSessionAsync(GameSession session)
    {
        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added game session {SessionId} in channel {ChannelId}",
            session.Id, session.DiscordChannelId);

        return session;
    }

    /// <summary>
    /// Update an existing game session
    /// </summary>
    public async Task UpdateGameSessionAsync(GameSession session)
    {
        _context.GameSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get an active game session by channel ID
    /// </summary>
    public async Task<GameSession?> GetActiveGameSessionAsync(ulong channelId)
    {
        return await _context.GameSessions
            .Include(s => s.Participants)
            .Include(s => s.NarrativeEvents)
            .Include(s => s.PlayerChoices)
            .Include(s => s.NPCRelationships)
            .Include(s => s.ActiveMissions)
            .FirstOrDefaultAsync(s => s.DiscordChannelId == channelId && s.Status == SessionStatus.Active);
    }

    /// <summary>
    /// Get a paused game session by channel ID
    /// </summary>
    public async Task<GameSession?> GetPausedGameSessionAsync(ulong channelId)
    {
        return await _context.GameSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.DiscordChannelId == channelId && s.Status == SessionStatus.Paused);
    }

    /// <summary>
    /// Get a game session by ID
    /// </summary>
    public async Task<GameSession?> GetGameSessionAsync(int sessionId)
    {
        return await _context.GameSessions
            .Include(s => s.Participants)
                .ThenInclude(p => p.Character)
            .Include(s => s.NarrativeEvents)
            .Include(s => s.PlayerChoices)
            .Include(s => s.NPCRelationships)
            .Include(s => s.ActiveMissions)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    /// <summary>
    /// Get all sessions for a guild
    /// </summary>
    public async Task<List<GameSession>> GetGuildGameSessionsAsync(ulong guildId, int limit = 10)
    {
        return await _context.GameSessions
            .Where(s => s.DiscordGuildId == guildId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    #endregion

    #region Session Participant Operations

    /// <summary>
    /// Add a session participant
    /// </summary>
    public async Task<SessionParticipant> AddSessionParticipantAsync(SessionParticipant participant)
    {
        _context.SessionParticipants.Add(participant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added participant {UserId} to session {SessionId}",
            participant.DiscordUserId, participant.GameSessionId);

        return participant;
    }

    /// <summary>
    /// Update a session participant
    /// </summary>
    public async Task UpdateSessionParticipantAsync(SessionParticipant participant)
    {
        _context.SessionParticipants.Update(participant);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get all participants for a session
    /// </summary>
    public async Task<List<SessionParticipant>> GetSessionParticipantsAsync(int sessionId)
    {
        return await _context.SessionParticipants
            .Include(p => p.Character)
            .Where(p => p.GameSessionId == sessionId)
            .ToListAsync();
    }

    #endregion

    #region Narrative Event Operations

    /// <summary>
    /// Add a narrative event
    /// </summary>
    public async Task<NarrativeEvent> AddNarrativeEventAsync(NarrativeEvent narrativeEvent)
    {
        _context.NarrativeEvents.Add(narrativeEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added narrative event '{Title}' to session {SessionId}",
            narrativeEvent.Title, narrativeEvent.GameSessionId);

        return narrativeEvent;
    }

    /// <summary>
    /// Get a narrative event by ID
    /// </summary>
    public async Task<NarrativeEvent?> GetNarrativeEventAsync(int eventId)
    {
        return await _context.NarrativeEvents.FindAsync(eventId);
    }

    /// <summary>
    /// Get all narrative events for a session
    /// </summary>
    public async Task<List<NarrativeEvent>> GetSessionNarrativeEventsAsync(int sessionId)
    {
        return await _context.NarrativeEvents
            .Where(e => e.GameSessionId == sessionId)
            .OrderByDescending(e => e.RecordedAt)
            .ToListAsync();
    }

    #endregion

    #region Player Choice Operations

    /// <summary>
    /// Add a player choice
    /// </summary>
    public async Task<PlayerChoice> AddPlayerChoiceAsync(PlayerChoice choice)
    {
        _context.PlayerChoices.Add(choice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added player choice for user {UserId} in session {SessionId}",
            choice.DiscordUserId, choice.GameSessionId);

        return choice;
    }

    /// <summary>
    /// Get a player choice by ID
    /// </summary>
    public async Task<PlayerChoice?> GetPlayerChoiceAsync(int choiceId)
    {
        return await _context.PlayerChoices.FindAsync(choiceId);
    }

    /// <summary>
    /// Update a player choice
    /// </summary>
    public async Task UpdatePlayerChoiceAsync(PlayerChoice choice)
    {
        _context.PlayerChoices.Update(choice);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get all player choices for a session
    /// </summary>
    public async Task<List<PlayerChoice>> GetSessionPlayerChoicesAsync(int sessionId)
    {
        return await _context.PlayerChoices
            .Where(c => c.GameSessionId == sessionId)
            .OrderBy(c => c.MadeAt)
            .ToListAsync();
    }

    #endregion

    #region NPC Relationship Operations

    /// <summary>
    /// Add an NPC relationship
    /// </summary>
    public async Task<NPCRelationship> AddNPCRelationshipAsync(NPCRelationship relationship)
    {
        _context.NPCRelationships.Add(relationship);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added NPC relationship with {NPCName} in session {SessionId}",
            relationship.NPCName, relationship.GameSessionId);

        return relationship;
    }

    /// <summary>
    /// Get an NPC relationship by ID
    /// </summary>
    public async Task<NPCRelationship?> GetNPCRelationshipAsync(int relationshipId)
    {
        return await _context.NPCRelationships.FindAsync(relationshipId);
    }

    /// <summary>
    /// Update an NPC relationship
    /// </summary>
    public async Task UpdateNPCRelationshipAsync(NPCRelationship relationship)
    {
        _context.NPCRelationships.Update(relationship);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get all NPC relationships for a session
    /// </summary>
    public async Task<List<NPCRelationship>> GetSessionNPCRelationshipsAsync(int sessionId)
    {
        return await _context.NPCRelationships
            .Where(r => r.GameSessionId == sessionId)
            .OrderBy(r => r.NPCName)
            .ToListAsync();
    }

    #endregion

    #region Active Mission Operations

    /// <summary>
    /// Add an active mission
    /// </summary>
    public async Task<ActiveMission> AddActiveMissionAsync(ActiveMission mission)
    {
        _context.ActiveMissions.Add(mission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added mission '{MissionName}' to session {SessionId}",
            mission.MissionName, mission.GameSessionId);

        return mission;
    }

    /// <summary>
    /// Get an active mission by ID
    /// </summary>
    public async Task<ActiveMission?> GetActiveMissionAsync(int missionId)
    {
        return await _context.ActiveMissions.FindAsync(missionId);
    }

    /// <summary>
    /// Update an active mission
    /// </summary>
    public async Task UpdateActiveMissionAsync(ActiveMission mission)
    {
        _context.ActiveMissions.Update(mission);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get all active missions for a session
    /// </summary>
    public async Task<List<ActiveMission>> GetSessionActiveMissionsAsync(int sessionId)
    {
        return await _context.ActiveMissions
            .Where(m => m.GameSessionId == sessionId)
            .OrderBy(m => m.AcceptedAt)
            .ToListAsync();
    }

    #endregion
}
