using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Controllers;

/// <summary>
/// API controller for combat management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CombatController : ControllerBase
{
    private readonly ShadowrunDbContext _dbContext;
    private readonly DiceService _diceService;
    private readonly ILogger<CombatController> _logger;

    public CombatController(
        ShadowrunDbContext dbContext,
        DiceService diceService,
        ILogger<CombatController> logger)
    {
        _dbContext = dbContext;
        _diceService = diceService;
        _logger = logger;
    }

    /// <summary>
    /// Get active combat session for a channel
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCombat([FromQuery] ulong? channelId)
    {
        try
        {
            var query = _dbContext.CombatSessions
                .Include(c => c.Participants)
                    .ThenInclude(p => p.Character)
                .Where(c => c.IsActive);

            if (channelId.HasValue)
            {
                query = query.Where(c => c.DiscordChannelId == channelId.Value);
            }

            var combat = await query.FirstOrDefaultAsync();

            if (combat == null)
            {
                return Ok(new { success = true, active = false, message = "No active combat session" });
            }

            return Ok(new
            {
                success = true,
                active = true,
                combat = new
                {
                    combat.Id,
                    combat.DiscordChannelId,
                    combat.DiscordGuildId,
                    combat.StartedAt,
                    combat.Round,
                    combat.CurrentTurn,
                    Participants = combat.Participants
                        .OrderByDescending(p => p.Initiative)
                        .ThenBy(p => p.Tiebreaker)
                        .Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.Type,
                            p.Initiative,
                            p.Tiebreaker,
                            p.HasActed,
                            p.Wounds,
                            CharacterId = p.Character?.Id,
                            CharacterName = p.Character?.Name
                        })
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active combat");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get all combat sessions (active and recent)
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetCombatSessions([FromQuery] int limit = 10)
    {
        try
        {
            var sessions = await _dbContext.CombatSessions
                .Include(c => c.Participants)
                .OrderByDescending(c => c.StartedAt)
                .Take(limit)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                sessions = sessions.Select(s => new
                {
                    s.Id,
                    s.DiscordChannelId,
                    s.IsActive,
                    s.StartedAt,
                    s.EndedAt,
                    s.Round,
                    ParticipantCount = s.Participants.Count
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combat sessions");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific combat session
    /// </summary>
    [HttpGet("{sessionId}")]
    public async Task<IActionResult> GetCombatSession(int sessionId)
    {
        try
        {
            var combat = await _dbContext.CombatSessions
                .Include(c => c.Participants)
                    .ThenInclude(p => p.Character)
                .Include(c => c.Actions)
                .FirstOrDefaultAsync(c => c.Id == sessionId);

            if (combat == null)
            {
                return NotFound(new { success = false, error = "Combat session not found" });
            }

            return Ok(new
            {
                success = true,
                combat = new
                {
                    combat.Id,
                    combat.DiscordChannelId,
                    combat.DiscordGuildId,
                    combat.IsActive,
                    combat.StartedAt,
                    combat.EndedAt,
                    combat.Round,
                    combat.CurrentTurn,
                    Participants = combat.Participants
                        .OrderByDescending(p => p.Initiative)
                        .ThenBy(p => p.Tiebreaker)
                        .Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.Type,
                            p.Initiative,
                            p.Tiebreaker,
                            p.HasActed,
                            p.Wounds,
                            CharacterId = p.Character?.Id,
                            CharacterName = p.Character?.Name
                        }),
                    Actions = combat.Actions.OrderByDescending(a => a.Timestamp).Take(50)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combat session {SessionId}", sessionId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Start a new combat session
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartCombat([FromBody] StartCombatRequest request)
    {
        try
        {
            // Check if there's already an active combat in this channel
            var existingCombat = await _dbContext.CombatSessions
                .FirstOrDefaultAsync(c => c.DiscordChannelId == request.ChannelId && c.IsActive);

            if (existingCombat != null)
            {
                return BadRequest(new { success = false, error = "Combat already active in this channel" });
            }

            var combat = new CombatSession
            {
                DiscordChannelId = request.ChannelId,
                DiscordGuildId = request.GuildId,
                IsActive = true,
                StartedAt = DateTime.UtcNow,
                Round = 1,
                CurrentTurn = 0
            };

            _dbContext.CombatSessions.Add(combat);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Started combat session {SessionId} in channel {ChannelId}",
                combat.Id, request.ChannelId);

            return Ok(new
            {
                success = true,
                combat = new
                {
                    combat.Id,
                    combat.DiscordChannelId,
                    combat.DiscordGuildId,
                    combat.IsActive,
                    combat.StartedAt,
                    combat.Round,
                    combat.CurrentTurn
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start combat");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Add a participant to combat
    /// </summary>
    [HttpPost("{sessionId}/add-participant")]
    public async Task<IActionResult> AddParticipant(int sessionId, [FromBody] AddParticipantRequest request)
    {
        try
        {
            var combat = await _dbContext.CombatSessions
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == sessionId);

            if (combat == null)
            {
                return NotFound(new { success = false, error = "Combat session not found" });
            }

            if (!combat.IsActive)
            {
                return BadRequest(new { success = false, error = "Combat session is not active" });
            }

            // Check for duplicate name
            if (combat.Participants.Any(p => p.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { success = false, error = "Participant with this name already exists" });
            }

            // Roll initiative if not provided
            var initiative = request.Initiative ?? _diceService.RollShadowrun(request.InitiativeDice ?? 1, 4).Hits;
            var tiebreaker = _diceService.Roll(1, 6).Total;

            var participant = new CombatParticipant
            {
                CombatSessionId = sessionId,
                Name = request.Name,
                Type = request.Type ?? "NPC",
                Initiative = initiative,
                Tiebreaker = tiebreaker,
                HasActed = false,
                Wounds = request.Wounds ?? 0,
                CharacterId = request.CharacterId
            };

            _dbContext.CombatParticipants.Add(participant);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Added participant {ParticipantName} to combat {SessionId}",
                participant.Name, sessionId);

            return Ok(new
            {
                success = true,
                participant = new
                {
                    participant.Id,
                    participant.Name,
                    participant.Type,
                    participant.Initiative,
                    participant.Tiebreaker,
                    participant.HasActed,
                    participant.Wounds,
                    participant.CharacterId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add participant to combat {SessionId}", sessionId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Remove a participant from combat
    /// </summary>
    [HttpDelete("{sessionId}/participants/{participantId}")]
    public async Task<IActionResult> RemoveParticipant(int sessionId, int participantId)
    {
        try
        {
            var participant = await _dbContext.CombatParticipants
                .FirstOrDefaultAsync(p => p.Id == participantId && p.CombatSessionId == sessionId);

            if (participant == null)
            {
                return NotFound(new { success = false, error = "Participant not found" });
            }

            _dbContext.CombatParticipants.Remove(participant);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Removed participant {ParticipantName} from combat {SessionId}",
                participant.Name, sessionId);

            return Ok(new { success = true, message = "Participant removed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove participant from combat");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Advance to next turn
    /// </summary>
    [HttpPost("{sessionId}/next-turn")]
    public async Task<IActionResult> NextTurn(int sessionId)
    {
        try
        {
            var combat = await _dbContext.CombatSessions
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == sessionId);

            if (combat == null)
            {
                return NotFound(new { success = false, error = "Combat session not found" });
            }

            if (!combat.IsActive)
            {
                return BadRequest(new { success = false, error = "Combat session is not active" });
            }

            // Mark current participant as having acted
            var currentParticipant = combat.Participants
                .OrderByDescending(p => p.Initiative)
                .ThenBy(p => p.Tiebreaker)
                .Skip(combat.CurrentTurn)
                .FirstOrDefault();

            if (currentParticipant != null)
            {
                currentParticipant.HasActed = true;
            }

            // Advance turn
            combat.CurrentTurn++;

            // Check if round is complete
            var remainingActors = combat.Participants
                .Where(p => !p.HasActed)
                .OrderByDescending(p => p.Initiative)
                .ThenBy(p => p.Tiebreaker)
                .ToList();

            if (remainingActors.Count == 0)
            {
                // New round
                combat.Round++;
                combat.CurrentTurn = 0;
                foreach (var p in combat.Participants)
                {
                    p.HasActed = false;
                }
            }

            await _dbContext.SaveChangesAsync();

            // Get next participant
            var nextParticipant = combat.Participants
                .Where(p => !p.HasActed)
                .OrderByDescending(p => p.Initiative)
                .ThenBy(p => p.Tiebreaker)
                .FirstOrDefault();

            return Ok(new
            {
                success = true,
                round = combat.Round,
                turn = combat.CurrentTurn,
                nextParticipant = nextParticipant != null ? new
                {
                    nextParticipant.Id,
                    nextParticipant.Name,
                    nextParticipant.Initiative
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to advance turn in combat {SessionId}", sessionId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// End combat session
    /// </summary>
    [HttpPost("{sessionId}/end")]
    public async Task<IActionResult> EndCombat(int sessionId)
    {
        try
        {
            var combat = await _dbContext.CombatSessions.FindAsync(sessionId);

            if (combat == null)
            {
                return NotFound(new { success = false, error = "Combat session not found" });
            }

            combat.IsActive = false;
            combat.EndedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Ended combat session {SessionId}", sessionId);

            return Ok(new
            {
                success = true,
                message = "Combat ended",
                duration = combat.EndedAt.Value - combat.StartedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end combat {SessionId}", sessionId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Log a combat action
    /// </summary>
    [HttpPost("{sessionId}/action")]
    public async Task<IActionResult> LogAction(int sessionId, [FromBody] LogActionRequest request)
    {
        try
        {
            var combat = await _dbContext.CombatSessions.FindAsync(sessionId);

            if (combat == null)
            {
                return NotFound(new { success = false, error = "Combat session not found" });
            }

            var action = new CombatAction
            {
                CombatSessionId = sessionId,
                ActorId = request.ActorId,
                ActorName = request.ActorName,
                ActionType = request.ActionType,
                TargetId = request.TargetId,
                TargetName = request.TargetName,
                Description = request.Description,
                Damage = request.Damage,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.CombatActions.Add(action);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, action });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log action in combat {SessionId}", sessionId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

/// <summary>
/// Request to start a combat session
/// </summary>
public class StartCombatRequest
{
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
}

/// <summary>
/// Request to add a participant
/// </summary>
public class AddParticipantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int? Initiative { get; set; }
    public int? InitiativeDice { get; set; } = 1;
    public int? Wounds { get; set; }
    public int? CharacterId { get; set; }
}

/// <summary>
/// Request to log a combat action
/// </summary>
public class LogActionRequest
{
    public int? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int? TargetId { get; set; }
    public string? TargetName { get; set; }
    public string? Description { get; set; }
    public int? Damage { get; set; }
}
