using Microsoft.AspNetCore.Mvc;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Controllers;

/// <summary>
/// API controller for combat management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CombatController : ControllerBase
{
    private readonly CombatService _combatService;
    private readonly ILogger<CombatController> _logger;

    public CombatController(
        CombatService combatService,
        ILogger<CombatController> logger)
    {
        _combatService = combatService;
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
            var combat = await _combatService.GetActiveCombatAsync(channelId);

            if (combat == null)
            {
                return Ok(new { success = true, active = false, message = "No active combat session" });
            }

            return Ok(new { success = true, active = true, combat });
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
            var sessions = await _combatService.GetAllCombatSessionsAsync(limit);
            return Ok(new { success = true, sessions });
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
            var combat = await _combatService.GetCombatSessionByIdAsync(sessionId);

            if (combat == null)
            {
                return NotFound(new { success = false, error = "Combat session not found" });
            }

            // Get actions for this session
            var actions = await _combatService.GetCombatActionsAsync(sessionId);

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
                    combat.Participants,
                    Actions = actions.OrderByDescending(a => a.Timestamp).Take(50)
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
            var combat = await _combatService.StartCombatApiAsync(request.ChannelId, request.GuildId);

            _logger.LogInformation("Started combat session {SessionId} in channel {ChannelId}",
                combat.Id, request.ChannelId);

            return Ok(new { success = true, combat });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
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
            var participant = await _combatService.AddParticipantApiAsync(
                sessionId,
                request.Name,
                request.Type,
                request.Initiative,
                request.InitiativeDice,
                request.Wounds,
                request.CharacterId);

            _logger.LogInformation("Added participant {ParticipantName} to combat {SessionId}",
                participant.Name, sessionId);

            return Ok(new { success = true, participant });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
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
            var removed = await _combatService.RemoveParticipantAsync(sessionId, participantId);

            if (!removed)
            {
                return NotFound(new { success = false, error = "Participant not found" });
            }

            _logger.LogInformation("Removed participant {ParticipantId} from combat {SessionId}",
                participantId, sessionId);

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
            var result = await _combatService.NextTurnApiAsync(sessionId);
            return Ok(new { success = true, result.Round, result.Turn, nextParticipant = result.NextParticipant });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
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
            var result = await _combatService.EndCombatApiAsync(sessionId);

            _logger.LogInformation("Ended combat session {SessionId}", sessionId);

            return Ok(new
            {
                success = result.Success,
                message = result.Message,
                duration = result.Duration
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
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
            var action = await _combatService.LogActionAsync(
                sessionId,
                request.ActorId,
                request.ActorName,
                request.ActionType,
                request.TargetId,
                request.TargetName,
                request.Description,
                request.Damage);

            return Ok(new { success = true, action });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
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
