using MediatR;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Commands.Combat;

/// <summary>
/// Handler for starting combat sessions
/// </summary>
public class StartCombatCommandHandler : IRequestHandler<StartCombatCommand, StartCombatResponse>
{
    private readonly DatabaseService _databaseService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<StartCombatCommandHandler> _logger;

    public StartCombatCommandHandler(
        DatabaseService databaseService,
        ICacheService cacheService,
        ILogger<StartCombatCommandHandler> logger)
    {
        _databaseService = databaseService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<StartCombatResponse> Handle(
        StartCombatCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if there's already an active combat session
            var existing = await _databaseService.GetActiveCombatSessionAsync(request.ChannelId).ConfigureAwait(false);
            
            if (existing != null)
            {
                return new StartCombatResponse
                {
                    Success = false,
                    Error = "There is already an active combat session in this channel. End it first with `/combat end`."
                };
            }

            var session = await _databaseService.CreateCombatSessionAsync(
                request.ChannelId,
                request.GuildId).ConfigureAwait(false);

            _logger.LogInformation("Started combat session {SessionId} in channel {ChannelId}",
                session.Id, request.ChannelId);

            return new StartCombatResponse
            {
                Success = true,
                SessionId = session.Id,
                Message = "⚔️ **Combat initiated!** Use `/combat add` to add participants."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start combat session in channel {ChannelId}", request.ChannelId);
            return new StartCombatResponse
            {
                Success = false,
                Error = "An error occurred while starting combat."
            };
        }
    }
}
