using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Mappers;
using ShadowrunDiscordBot.Domain.Entities;

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
    public async Task<Domain.Entities.GameSession> AddGameSessionAsync(Domain.Entities.GameSession session)
    {
        var model = GameSessionMapper.ToModel(session);
        _context.GameSessions.Add(model);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added game session {SessionId} in channel {ChannelId}",
            model.Id, model.DiscordChannelId);

        return GameSessionMapper.ToDomain(model);
    }

    /// <summary>
    /// Update an existing game session
    /// </summary>
    public async Task UpdateGameSessionAsync(Domain.Entities.GameSession session)
    {
        var model = GameSessionMapper.ToModel(session);
        _context.GameSessions.Update(model);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get an active game session by channel ID
    /// </summary>
    public async Task<Domain.Entities.GameSession?> GetActiveGameSessionAsync(ulong channelId)
    {
        var model = await _context.GameSessions
            .Include(s => s.Participants)
            .Include(s => s.NarrativeEvents)
            .Include(s => s.PlayerChoices)
            .Include(s => s.NPCRelationships)
            .Include(s => s.ActiveMissions)
            .FirstOrDefaultAsync(s => s.DiscordChannelId == channelId && s.Status == SessionStatus.Active);

        return model != null ? GameSessionMapper.ToDomain(model) : null;
    }

    /// <summary>
    /// Get a paused game session by channel ID
    /// </summary>
    public async Task<Domain.Entities.GameSession?> GetPausedGameSessionAsync(ulong channelId)
    {
        var model = await _context.GameSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.DiscordChannelId == channelId && s.Status == SessionStatus.Paused);

        return model != null ? GameSessionMapper.ToDomain(model) : null;
    }

    /// <summary>
    /// Get a game session by ID
    /// </summary>
    public async Task<Domain.Entities.GameSession?> GetGameSessionAsync(int sessionId)
    {
        var model = await _context.GameSessions
            .Include(s => s.Participants)
                .ThenInclude(p => p.Character)
            .Include(s => s.NarrativeEvents)
            .Include(s => s.PlayerChoices)
            .Include(s => s.NPCRelationships)
            .Include(s => s.ActiveMissions)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        return model != null ? GameSessionMapper.ToDomain(model) : null;
    }

    /// <summary>
    /// Get all sessions for a guild
    /// FIX: MED-003 - Added Includes to prevent N+1 query problems
    /// </summary>
    public async Task<List<Domain.Entities.GameSession>> GetGuildGameSessionsAsync(ulong guildId, int limit = 10)
    {
        var models = await _context.GameSessions
            .Include(s => s.Participants)
                .ThenInclude(p => p.Character)
            .Include(s => s.ActiveMissions)
            .Where(s => s.DiscordGuildId == guildId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        return models.Select(m => GameSessionMapper.ToDomain(m)).ToList();
    }

    #endregion

    #region Session Participant Operations

    /// <summary>
    /// Add a session participant
    /// </summary>
    public async Task<Domain.Entities.SessionParticipant> AddSessionParticipantAsync(Domain.Entities.SessionParticipant participant)
    {
        var model = GameSessionMapper.ToModel(participant);
        _context.SessionParticipants.Add(model);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added participant {UserId} to session {SessionId}",
            model.DiscordUserId, model.GameSessionId);

        return GameSessionMapper.ToDomain(model);
    }

    /// <summary>
    /// Update a session participant
    /// </summary>
    public async Task UpdateSessionParticipantAsync(Domain.Entities.SessionParticipant participant)
    {
        var model = GameSessionMapper.ToModel(participant);
        _context.SessionParticipants.Update(model);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get all participants for a session
    /// </summary>
    public async Task<List<Domain.Entities.SessionParticipant>> GetSessionParticipantsAsync(int sessionId)
    {
        var models = await _context.SessionParticipants
            .Include(p => p.Character)
            .Where(p => p.GameSessionId == sessionId)
            .ToListAsync();

        return models.Select(m => GameSessionMapper.ToDomain(m)).ToList();
    }

    #endregion

    #region Narrative Event Operations

    /// <summary>
    /// Add a narrative event
    /// </summary>
    public async Task<Domain.Entities.NarrativeEvent> AddNarrativeEventAsync(Domain.Entities.NarrativeEvent narrativeEvent)
    {
        var model = GameSessionMapper.ToModel(narrativeEvent);
        _context.NarrativeEvents.Add(model);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added narrative event '{Title}' to session {SessionId}",
            model.Title, model.GameSessionId);

        return GameSessionMapper.ToDomain(model);
    }

    /// <summary>
    /// Get a narrative event by ID
    /// </summary>
    public async Task<Domain.Entities.NarrativeEvent?> GetNarrativeEventAsync(int eventId)
    {
        var model = await _context.NarrativeEvents.FindAsync(eventId);
        return model != null ? GameSessionMapper.ToDomain(model) : null;
    }

    /// <summary>
    /// Get all narrative events for a session
    /// </summary>
    public async Task<List<Domain.Entities.NarrativeEvent>> GetSessionNarrativeEventsAsync(int sessionId)
    {
        var models = await _context.NarrativeEvents
            .Where(e => e.GameSessionId == sessionId)
            .OrderByDescending(e => e.RecordedAt)
            .ToListAsync();

        return models.Select(m => GameSessionMapper.ToDomain(m)).ToList();
    }

    #endregion

    #region Player Choice Operations

    /// <summary>
    /// Add a player choice
    /// </summary>
    public async Task<Domain.Entities.PlayerChoice> AddPlayerChoiceAsync(Domain.Entities.PlayerChoice choice)
    {
        var model = GameSessionMapper.ToModel(choice);
        _context.PlayerChoices.Add(model);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added player choice for user {UserId} in session {SessionId}",
            model.DiscordUserId, model.GameSessionId);

        return GameSessionMapper.ToDomain(model);
    }

    /// <summary>
    /// Get a player choice by ID
    /// </summary>
    public async Task<Domain.Entities.PlayerChoice?> GetPlayerChoiceAsync(int choiceId)
    {
        var model = await _context.PlayerChoices.FindAsync(choiceId);
        return model != null ? GameSessionMapper.ToDomain(model) : null;
    }

    /// <summary>
    /// Update a player choice
    /// </summary>
    public async Task UpdatePlayerChoiceAsync(Domain.Entities.PlayerChoice choice)
    {
        var model = GameSessionMapper.ToModel(choice);
        _context.PlayerChoices.Update(model);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get all player choices for a session
    /// </summary>
    public async Task<List<Domain.Entities.PlayerChoice>> GetSessionPlayerChoicesAsync(int sessionId)
    {
        var models = await _context.PlayerChoices
            .Where(c => c.GameSessionId == sessionId)
            .OrderBy(c => c.MadeAt)
            .ToListAsync();

        return models.Select(m => GameSessionMapper.ToDomain(m)).ToList();
    }

    #endregion

    #region NPC Relationship Operations

    /// <summary>
    /// Add an NPC relationship
    /// </summary>
    public async Task<Domain.Entities.NPCRelationship> AddNPCRelationshipAsync(Domain.Entities.NPCRelationship relationship)
    {
        var model = GameSessionMapper.ToModel(relationship);
        _context.NPCRelationships.Add(model);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added NPC relationship with {NPCName} in session {SessionId}",
            model.NPCName, model.GameSessionId);

        return GameSessionMapper.ToDomain(model);
    }

    /// <summary>
    /// Get an NPC relationship by ID
    /// </summary>
    public async Task<Domain.Entities.NPCRelationship?> GetNPCRelationshipAsync(int relationshipId)
    {
        var model = await _context.NPCRelationships.FindAsync(relationshipId);
        return model != null ? GameSessionMapper.ToDomain(model) : null;
    }

    /// <summary>
    /// Update an NPC relationship
    /// </summary>
    public async Task UpdateNPCRelationshipAsync(Domain.Entities.NPCRelationship relationship)
    {
        var model = GameSessionMapper.ToModel(relationship);
        _context.NPCRelationships.Update(model);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get all NPC relationships for a session
    /// </summary>
    public async Task<List<Domain.Entities.NPCRelationship>> GetSessionNPCRelationshipsAsync(int sessionId)
    {
        var models = await _context.NPCRelationships
            .Where(r => r.GameSessionId == sessionId)
            .OrderBy(r => r.NPCName)
            .ToListAsync();

        return models.Select(m => GameSessionMapper.ToDomain(m)).ToList();
    }

    #endregion

    #region Active Mission Operations

    /// <summary>
    /// Add an active mission
    /// </summary>
    public async Task<Domain.Entities.ActiveMission> AddActiveMissionAsync(Domain.Entities.ActiveMission mission)
    {
        var model = GameSessionMapper.ToModel(mission);
        _context.ActiveMissions.Add(model);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added mission '{MissionName}' to session {SessionId}",
            model.MissionName, model.GameSessionId);

        return GameSessionMapper.ToDomain(model);
    }

    /// <summary>
    /// Get an active mission by ID
    /// </summary>
    public async Task<Domain.Entities.ActiveMission?> GetActiveMissionAsync(int missionId)
    {
        var model = await _context.ActiveMissions.FindAsync(missionId);
        return model != null ? GameSessionMapper.ToDomain(model) : null;
    }

    /// <summary>
    /// Update an active mission
    /// </summary>
    public async Task UpdateActiveMissionAsync(Domain.Entities.ActiveMission mission)
    {
        var model = GameSessionMapper.ToModel(mission);
        _context.ActiveMissions.Update(model);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get all active missions for a session
    /// </summary>
    public async Task<List<Domain.Entities.ActiveMission>> GetSessionActiveMissionsAsync(int sessionId)
    {
        var models = await _context.ActiveMissions
            .Where(m => m.GameSessionId == sessionId)
            .OrderBy(m => m.AcceptedAt)
            .ToListAsync();

        return models.Select(m => GameSessionMapper.ToDomain(m)).ToList();
    }

    #endregion
}
