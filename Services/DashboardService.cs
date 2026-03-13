using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Service for dashboard data aggregation
/// </summary>
public class DashboardService
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        DatabaseService databaseService,
        ILogger<DashboardService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard summary data
    /// </summary>
    public async Task<DashboardDataDto> GetDashboardDataAsync()
    {
        try
        {
            var characters = await _databaseService.GetCharactersPageAsync(0, 50); // GPT-5.4 FIX: Dashboard should use explicit pagination
            var recentSessions = await _databaseService.GetRecentCombatSessionsAsync(10);
            var activeCombats = recentSessions.Where(s => s.IsActive).ToList();
            
            // Get recent combat actions from active combats
            var recentActions = new List<CombatActionSummaryDto>();
            if (activeCombats.Any())
            {
                var activeSession = activeCombats.First();
                var actions = await _databaseService.GetCombatActionsAsync(activeSession.Id, 10);
                recentActions = actions.Select(a => new CombatActionSummaryDto
                {
                    Id = a.Id,
                    ActorName = a.ActorName,
                    ActionType = a.ActionType ?? "Unknown",
                    TargetName = a.TargetName,
                    Description = a.Description,
                    Timestamp = a.Timestamp
                }).ToList();
            }

            return new DashboardDataDto
            {
                TotalCharacters = characters.Count,
                ActiveCombats = activeCombats.Count,
                RecentActions = recentActions,
                ServerTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard data");
            throw;
        }
    }
}

/// <summary>
/// Dashboard data DTO
/// </summary>
public class DashboardDataDto
{
    public int TotalCharacters { get; set; }
    public int ActiveCombats { get; set; }
    public List<CombatActionSummaryDto> RecentActions { get; set; } = new();
    public DateTime ServerTime { get; set; }
}

/// <summary>
/// Combat action summary DTO
/// </summary>
public class CombatActionSummaryDto
{
    public int Id { get; set; }
    public string? ActorName { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? TargetName { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
}
