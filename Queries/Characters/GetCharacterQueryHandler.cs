using MediatR;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Queries.Characters;

/// <summary>
/// Handler for getting a character by ID
/// </summary>
public class GetCharacterQueryHandler : IRequestHandler<GetCharacterQuery, GetCharacterResponse>
{
    private readonly DatabaseService _databaseService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetCharacterQueryHandler> _logger;

    public GetCharacterQueryHandler(
        DatabaseService databaseService,
        ICacheService cacheService,
        ILogger<GetCharacterQueryHandler> logger)
    {
        _databaseService = databaseService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<GetCharacterResponse> Handle(
        GetCharacterQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try cache first
            var cacheKey = CacheKeys.Character(request.CharacterId);
            var cached = await _cacheService.GetAsync<Models.ShadowrunCharacter>(cacheKey).ConfigureAwait(false);
            
            if (cached != null)
            {
                return new GetCharacterResponse
                {
                    Success = true,
                    Character = cached
                };
            }

            // Get from database
            var character = await _databaseService.GetCharacterAsync(request.CharacterId).ConfigureAwait(false);
            
            if (character == null)
            {
                return new GetCharacterResponse
                {
                    Success = false,
                    Error = "Character not found."
                };
            }

            // Cache for future requests
            await _cacheService.SetAsync(cacheKey, character, TimeSpan.FromMinutes(30)).ConfigureAwait(false);

            return new GetCharacterResponse
            {
                Success = true,
                Character = character
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character {CharacterId}", request.CharacterId);
            return new GetCharacterResponse
            {
                Success = false,
                Error = "An error occurred while retrieving the character."
            };
        }
    }
}
