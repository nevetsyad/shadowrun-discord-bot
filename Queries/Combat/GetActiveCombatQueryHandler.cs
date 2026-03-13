using MediatR;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Queries.Combat;

/// <summary>
/// Handler for getting active combat session
/// </summary>
public class GetActiveCombatQueryHandler : IRequestHandler<GetActiveCombatQuery, GetActiveCombatResponse>
{
    private readonly DatabaseService _databaseService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetActiveCombatQueryHandler> _logger;

    public GetActiveCombatQueryHandler(
        DatabaseService databaseService,
        ICacheService cacheService,
        ILogger<GetActiveCombatQueryHandler> logger)
    {
        _databaseService = databaseService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<GetActiveCombatResponse> Handle(
        GetActiveCombatQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Combat sessions change frequently, so we use a short cache duration
            var cacheKey = CacheKeys.ActiveCombatSession(request.ChannelId);
            var cached = await _cacheService.GetAsync<Models.CombatSession>(cacheKey).ConfigureAwait(false);
            
            if (cached != null)
            {
                return new GetActiveCombatResponse
                {
                    Success = true,
                    Session = cached
                };
            }

            // Get from database
            var session = await _databaseService.GetActiveCombatSessionAsync(request.ChannelId).ConfigureAwait(false);
            
            if (session == null)
            {
                return new GetActiveCombatResponse
                {
                    Success = true,
                    Session = null
                };
            }

            // Cache for a short duration since combat changes frequently
            await _cacheService.SetAsync(cacheKey, session, TimeSpan.FromMinutes(1)).ConfigureAwait(false);

            return new GetActiveCombatResponse
            {
                Success = true,
                Session = session
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active combat session for channel {ChannelId}", request.ChannelId);
            return new GetActiveCombatResponse
            {
                Success = false,
                Error = "An error occurred while retrieving combat session."
            };
        }
    }
}
