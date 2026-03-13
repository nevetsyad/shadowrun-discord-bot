using Microsoft.AspNetCore.Mvc;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Controllers;

/// <summary>
/// API controller for the GM Dashboard UI
/// </summary>
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly CharacterService _characterService;
    private readonly CombatService _combatService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        CharacterService characterService,
        CombatService combatService,
        ILogger<DashboardController> logger)
    {
        _characterService = characterService;
        _combatService = combatService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard summary data.
    /// </summary>
    [HttpGet("api/dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var characters = await _characterService.GetCharactersPageAsync(0, 50); // GPT-5.4 FIX: Dashboard should use explicit pagination
            var activeCombats = await _combatService.GetAllCombatSessionsAsync(1);

            var recentActions = new List<CombatActionDto>();
            if (activeCombats.Any(c => c.IsActive))
            {
                var activeSession = activeCombats.First(c => c.IsActive);
                recentActions = await _combatService.GetCombatActionsAsync(activeSession.Id, 10);
            }

            return Ok(new
            {
                success = true,
                dashboard = new
                {
                    totalCharacters = characters.Count,
                    activeCombats = activeCombats.Count(c => c.IsActive),
                    recentActions = recentActions.Select(a => new
                    {
                        a.Id,
                        a.ActorName,
                        a.ActionType,
                        a.TargetName,
                        a.Description,
                        a.Timestamp
                    }),
                    serverTime = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard data");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Serve the GM Dashboard HTML page.
    /// </summary>
    [HttpGet("/")]
    public IActionResult GetDashboardPage()
    {
        var htmlPath = Path.Combine(
            ControllerContext.HttpContext!.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath,
            "Controllers",
            "DashboardController.html");

        var html = System.IO.File.ReadAllText(htmlPath);
        return Content(html, "text/html");
    }
}
