using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;
using System.Text.Json;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Database service extensions for session management, breaks, history, and organization
/// </summary>
public partial class DatabaseService
{
    #region Session Break Operations

    /// <summary>
    /// Add a session break record
    /// </summary>
    public async Task<SessionBreak> AddSessionBreakAsync(SessionBreak sessionBreak)
    {
        _context.SessionBreaks.Add(sessionBreak);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added session break for session {SessionId}", sessionBreak.GameSessionId);

        return sessionBreak;
    }

    /// <summary>
    /// Update a session break record
    /// </summary>
    public async Task UpdateSessionBreakAsync(SessionBreak sessionBreak)
    {
        _context.SessionBreaks.Update(sessionBreak);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get the active (unended) break for a session
    /// </summary>
    public async Task<SessionBreak?> GetActiveSessionBreakAsync(int sessionId)
    {
        return await _context.SessionBreaks
            .Where(b => b.GameSessionId == sessionId && b.BreakEndedAt == null)
            .OrderByDescending(b => b.BreakStartedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get all breaks for a session
    /// </summary>
    public async Task<List<SessionBreak>> GetSessionBreaksAsync(int sessionId)
    {
        return await _context.SessionBreaks
            .Where(b => b.GameSessionId == sessionId)
            .OrderBy(b => b.BreakStartedAt)
            .ToListAsync();
    }

    #endregion

    #region Session Tag Operations

    /// <summary>
    /// Add a session tag
    /// </summary>
    public async Task<SessionTag> AddSessionTagAsync(SessionTag tag)
    {
        _context.SessionTags.Add(tag);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added tag '{TagName}' to session {SessionId}", tag.TagName, tag.GameSessionId);

        return tag;
    }

    /// <summary>
    /// Remove a session tag
    /// </summary>
    public async Task RemoveSessionTagAsync(int sessionId, string tagName)
    {
        var tag = await _context.SessionTags
            .FirstOrDefaultAsync(t => t.GameSessionId == sessionId && 
                                    t.TagName.ToLower() == tagName.ToLower());

        if (tag != null)
        {
            _context.SessionTags.Remove(tag);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed tag '{TagName}' from session {SessionId}", tagName, sessionId);
        }
    }

    /// <summary>
    /// Get all tags for a session
    /// </summary>
    public async Task<List<SessionTag>> GetSessionTagsAsync(int sessionId)
    {
        return await _context.SessionTags
            .Where(t => t.GameSessionId == sessionId)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.TagName)
            .ToListAsync();
    }

    /// <summary>
    /// Get sessions by tag
    /// </summary>
    public async Task<List<GameSession>> GetSessionsByTagAsync(ulong guildId, string tagName)
    {
        return await _context.SessionTags
            .Where(t => t.TagName.ToLower() == tagName.ToLower() && 
                       t.GameSession.DiscordGuildId == guildId)
            .Select(t => t.GameSession)
            .Distinct()
            .ToListAsync();
    }

    #endregion

    #region Session Note Operations

    /// <summary>
    /// Add a session note
    /// </summary>
    public async Task<SessionNote> AddSessionNoteAsync(SessionNote note)
    {
        _context.SessionNotes.Add(note);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added note to session {SessionId}", note.GameSessionId);

        return note;
    }

    /// <summary>
    /// Get all notes for a session
    /// </summary>
    public async Task<List<SessionNote>> GetSessionNotesAsync(int sessionId, bool pinnedOnly = false)
    {
        var query = _context.SessionNotes
            .Where(n => n.GameSessionId == sessionId);

        if (pinnedOnly)
        {
            query = query.Where(n => n.IsPinned);
        }

        return await query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Delete a session note
    /// </summary>
    public async Task DeleteSessionNoteAsync(int noteId)
    {
        var note = await _context.SessionNotes.FindAsync(noteId);
        if (note != null)
        {
            _context.SessionNotes.Remove(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted session note {NoteId}", noteId);
        }
    }

    #endregion

    #region Completed Session Operations

    /// <summary>
    /// Add a completed session
    /// </summary>
    public async Task<CompletedSession> AddCompletedSessionAsync(CompletedSession session)
    {
        _context.CompletedSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added completed session {SessionId}", session.Id);

        return session;
    }

    /// <summary>
    /// Get a completed session by ID
    /// </summary>
    public async Task<CompletedSession?> GetCompletedSessionAsync(int sessionId)
    {
        return await _context.CompletedSessions
            .Include(s => s.Tags)
            .Include(s => s.Notes)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    /// <summary>
    /// Get recent completed sessions for a guild
    /// </summary>
    public async Task<List<CompletedSession>> GetRecentCompletedSessionsAsync(ulong guildId, int limit = 10)
    {
        return await _context.CompletedSessions
            .Include(s => s.Tags)
            .Where(s => s.DiscordGuildId == guildId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Search completed sessions with filters
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
            .Include(s => s.Tags)
            .Include(s => s.Notes)
            .Where(s => s.DiscordGuildId == guildId);

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(s => 
                (s.SessionName != null && s.SessionName.Contains(searchTerm)) ||
                (s.Outcome != null && s.Outcome.Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(s => s.Category == category);
        }

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(s => s.Tags.Any(t => t.TagName.ToLower() == tag.ToLower()));
        }

        if (startDate.HasValue)
        {
            query = query.Where(s => s.StartedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.EndedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Update a completed session
    /// </summary>
    public async Task UpdateCompletedSessionAsync(CompletedSession session)
    {
        _context.CompletedSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Delete a completed session
    /// </summary>
    public async Task DeleteCompletedSessionAsync(int sessionId)
    {
        var session = await _context.CompletedSessions.FindAsync(sessionId);
        if (session != null)
        {
            _context.CompletedSessions.Remove(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted completed session {SessionId}", sessionId);
        }
    }

    #endregion

    #region Session Organization Queries

    /// <summary>
    /// Get all active sessions (for idle detection)
    /// </summary>
    public async Task<List<GameSession>> GetActiveSessionsAsync()
    {
        return await _context.GameSessions
            .Where(s => s.Status == SessionStatus.Active)
            .ToListAsync();
    }

    /// <summary>
    /// Get sessions by category
    /// </summary>
    public async Task<List<GameSession>> GetSessionsByCategoryAsync(ulong guildId, string category)
    {
        // Since category is stored in metadata JSON, we need to load and filter in memory
        var sessions = await _context.GameSessions
            .Where(s => s.DiscordGuildId == guildId && s.Metadata != null)
            .ToListAsync();

        return sessions
            .Where(s => 
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(s.Metadata!);
                    return metadata != null && 
                           metadata.TryGetValue("Category", out var cat) && 
                           cat.ToString() == category;
                }
                catch
                {
                    return false;
                }
            })
            .ToList();
    }

    /// <summary>
    /// Get child sessions (for campaign grouping)
    /// </summary>
    public async Task<List<GameSession>> GetChildSessionsAsync(int parentSessionId)
    {
        var sessions = await _context.GameSessions
            .Where(s => s.Metadata != null)
            .ToListAsync();

        return sessions
            .Where(s => 
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(s.Metadata!);
                    return metadata != null && 
                           metadata.TryGetValue("ParentSessionId", out var parentId) && 
                           parentId.ToString() == parentSessionId.ToString();
                }
                catch
                {
                    return false;
                }
            })
            .ToList();
    }

    /// <summary>
    /// Get session with full details (breaks, tags, notes)
    /// </summary>
    public async Task<GameSession?> GetSessionWithDetailsAsync(int sessionId)
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

    #endregion
}
