using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Controllers;

/// <summary>
/// API controller for dice rolling
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiceController : ControllerBase
{
    private readonly DiceService _diceService;
    private readonly ILogger<DiceController> _logger;

    public DiceController(DiceService diceService, ILogger<DiceController> logger)
    {
        _diceService = diceService;
        _logger = logger;
    }

    /// <summary>
    /// Roll standard dice notation (e.g., 2d6+3)
    /// </summary>
    [HttpPost("roll")]
    public IActionResult Roll([FromBody] DiceRollRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Notation))
            {
                return BadRequest(new { success = false, error = "Dice notation is required" });
            }

            var result = _diceService.ParseAndRoll(request.Notation);

            return Ok(new
            {
                success = true,
                result = new
                {
                    result.Notation,
                    result.Dice,
                    result.Modifier,
                    result.Total,
                    result.Rolls
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to roll dice: {Notation}", request.Notation);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Roll Shadowrun dice pool
    /// </summary>
    [HttpPost("shadowrun")]
    public IActionResult ShadowrunRoll([FromBody] ShadowrunRollRequest request)
    {
        try
        {
            if (request.PoolSize < 1 || request.PoolSize > 100)
            {
                return BadRequest(new { success = false, error = "Pool size must be between 1 and 100" });
            }

            var result = _diceService.RollShadowrun(request.PoolSize, request.TargetNumber ?? 5);

            return Ok(new
            {
                success = true,
                result = new
                {
                    PoolSize = result.PoolSize,
                    TargetNumber = result.TargetNumber,
                    Hits = result.Successes,
                    Glitches = result.Glitch ? (result.CriticalGlitch ? "critical" : "yes") : "no",
                    IsCriticalGlitch = result.CriticalGlitch,
                    Rolls = result.Rolls
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to roll Shadowrun dice");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Roll initiative for Shadowrun
    /// </summary>
    [HttpPost("initiative")]
    public IActionResult RollInitiative([FromBody] InitiativeRollRequest request)
    {
        try
        {
            // Initiative = (Intuition + Reaction) + 1d6 (or more with wired reflexes)
            var baseInitiative = request.Intuition + request.Reaction;
            var diceCount = request.ExtraInitiativeDice + 1; // Always at least 1d6

            var diceRoll = 0;
            for (int i = 0; i < diceCount; i++)
            {
                diceRoll += _diceService.Roll(1, 6).Total;
            }

            var totalInitiative = baseInitiative + diceRoll;

            return Ok(new
            {
                success = true,
                result = new
                {
                    BaseInitiative = baseInitiative,
                    DiceRoll = diceRoll,
                    DiceCount = diceCount,
                    TotalInitiative = totalInitiative
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to roll initiative");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Roll edge (explode sixes)
    /// </summary>
    [HttpPost("edge")]
    public IActionResult RollEdge([FromBody] EdgeRollRequest request)
    {
        try
        {
            if (request.PoolSize < 1 || request.PoolSize > 100)
            {
                return BadRequest(new { success = false, error = "Pool size must be between 1 and 100" });
            }

            var result = _diceService.RollEdge(request.PoolSize);

            return Ok(new
            {
                success = true,
                result = new
                {
                    PoolSize = result.PoolSize,
                    Hits = result.Successes,
                    Glitches = result.Glitch ? (result.CriticalGlitch ? "critical" : "yes") : "no",
                    Sixes = result.Sixes,
                    Rolls = result.Rolls
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to roll edge");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

/// <summary>
/// Standard dice roll request
/// </summary>
public class DiceRollRequest
{
    public string Notation { get; set; } = "1d6";
}

/// <summary>
/// Shadowrun dice roll request
/// </summary>
public class ShadowrunRollRequest
{
    public int PoolSize { get; set; }
    public int? TargetNumber { get; set; } = 5;
}

/// <summary>
/// Initiative roll request
/// </summary>
public class InitiativeRollRequest
{
    public int Intuition { get; set; }
    public int Reaction { get; set; }
    public int ExtraInitiativeDice { get; set; } = 0;
}

/// <summary>
/// Edge roll request (exploding sixes)
/// </summary>
public class EdgeRollRequest
{
    public int PoolSize { get; set; }
}
