using MediatR;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Commands.Combat;

/// <summary>
/// Handler for ending combat sessions
/// </summary>
public class EndCombatCommandHandler : IRequestHandler<EndCombatCommand, EndCombatResponse>
{
    private readonly DatabaseService _databaseService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<EndCombatCommandHandler> _logger;

    public EndCombatCommandHandler(
        DatabaseService databaseService,
        ICacheService cacheService,
        ILogger<EndCombatCommandHandler> logger)
    {
        _databaseService = databaseService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<EndCombatResponse> Handle(
        EndCombatCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await _databaseService.GetActiveCombatSessionAsync(request.ChannelId).ConfigureAwait(false);
            
            if (session == null)
            {
                return new EndCombatResponse
                {
                    Success = false,
                    Error = "No active combat session in this channel."
                };
            }

            await _databaseService.EndCombatSessionAsync(session.Id).ConfigureAwait(false);

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKeys.ActiveCombatSession(request.ChannelId)).ConfigureAwait(false);
            await _cacheService.RemoveAsync(CacheKeys.CombatSession(session.Id)).ConfigureAwait(false);

            _logger.LogInformation("Ended combat session {SessionId} in channel {ChannelId}",
                session.Id, request.ChannelId);

            return new EndCombatResponse
            {
                Success = true,
                Message = "⚔️ **Combat ended!** All participants have been removed from initiative order."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end combat session in channel {ChannelId}", request.ChannelId);
            return new EndCombatResponse
            {
                Success = false,
                Error = "An error occurred while ending combat."
            };
        }
    }
}
