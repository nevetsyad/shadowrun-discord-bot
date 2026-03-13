using MediatR;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Queries.Characters;

/// <summary>
/// Handler for getting all characters for a user
/// </summary>
public class GetUserCharactersQueryHandler : IRequestHandler<GetUserCharactersQuery, GetUserCharactersResponse>
{
    private readonly DatabaseService _databaseService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetUserCharactersQueryHandler> _logger;

    public GetUserCharactersQueryHandler(
        DatabaseService databaseService,
        ICacheService cacheService,
        ILogger<GetUserCharactersQueryHandler> logger)
    {
        _databaseService = databaseService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<GetUserCharactersResponse> Handle(
        GetUserCharactersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try cache first
            var cacheKey = CacheKeys.UserCharacters(request.DiscordUserId);
            var cached = await _cacheService.GetAsync<List<Models.ShadowrunCharacter>>(cacheKey).ConfigureAwait(false);
            
            if (cached != null && cached.Count > 0)
            {
                return new GetUserCharactersResponse
                {
                    Success = true,
                    Characters = cached
                };
            }

            // Get from database
            var characters = await _databaseService.GetUserCharactersAsync(request.DiscordUserId).ConfigureAwait(false);

            // Cache for future requests
            if (characters.Count > 0)
            {
                await _cacheService.SetAsync(cacheKey, characters, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
            }

            return new GetUserCharactersResponse
            {
                Success = true,
                Characters = characters
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters for user {UserId}", request.DiscordUserId);
            return new GetUserCharactersResponse
            {
                Success = false,
                Error = "An error occurred while retrieving characters."
            };
        }
    }
}
