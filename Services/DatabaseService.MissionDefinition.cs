using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Database service extensions for mission definition management
/// </summary>
public partial class DatabaseService
{
    #region Mission Definition Operations

    /// <summary>
    /// Add a new mission definition
    /// </summary>
    public async Task<MissionDefinition> AddMissionDefinitionAsync(MissionDefinition mission)
    {
        _context.MissionDefinitions.Add(mission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added mission definition {MissionId} to session {SessionId}",
            mission.Id, mission.GameSessionId);

        return mission;
    }

    /// <summary>
    /// Get a mission definition by ID
    /// </summary>
    public async Task<MissionDefinition?> GetMissionDefinitionAsync(int missionId)
    {
        return await _context.MissionDefinitions
            .FirstOrDefaultAsync(m => m.Id == missionId);
    }

    /// <summary>
    /// Get the active mission definition for a session
    /// </summary>
    public async Task<MissionDefinition?> GetActiveMissionDefinitionAsync(int sessionId)
    {
        return await _context.MissionDefinitions
            .FirstOrDefaultAsync(m => m.GameSessionId == sessionId && 
                                     (m.Status == MissionStatus.Planning || m.Status == MissionStatus.InProgress));
    }

    /// <summary>
    /// Update a mission definition
    /// </summary>
    public async Task UpdateMissionDefinitionAsync(MissionDefinition mission)
    {
        _context.MissionDefinitions.Update(mission);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Updated mission definition {MissionId}", mission.Id);
    }

    /// <summary>
    /// Get all mission definitions for a session
    /// </summary>
    public async Task<List<MissionDefinition>> GetSessionMissionDefinitionsAsync(int sessionId)
    {
        return await _context.MissionDefinitions
            .Where(m => m.GameSessionId == sessionId)
            .OrderByDescending(m => m.GeneratedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Delete a mission definition
    /// </summary>
    public async Task DeleteMissionDefinitionAsync(int missionId)
    {
        var mission = await _context.MissionDefinitions.FindAsync(missionId);
        if (mission != null)
        {
            _context.MissionDefinitions.Remove(mission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted mission definition {MissionId}", missionId);
        }
    }

    #endregion
}
