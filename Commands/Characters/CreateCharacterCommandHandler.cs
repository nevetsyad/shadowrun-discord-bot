using MediatR;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Exceptions;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Commands.Characters;

/// <summary>
/// Handler for creating new characters with validation and caching support
/// FIX: Added comprehensive XML documentation for all public methods
/// </summary>
public class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, CreateCharacterResponse>
{
    private readonly DatabaseService _databaseService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreateCharacterCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the CreateCharacterCommandHandler
    /// </summary>
    /// <param name="databaseService">Database service for character persistence</param>
    /// <param name="cacheService">Cache service for invalidating character lists</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public CreateCharacterCommandHandler(
        DatabaseService databaseService,
        ICacheService cacheService,
        ILogger<CreateCharacterCommandHandler> logger)
    {
        _databaseService = databaseService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the creation of a new Shadowrun character with validation
    /// </summary>
    /// <param name="request">The command containing character creation data</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Response indicating success/failure and the created character ID</returns>
    public async Task<CreateCharacterResponse> Handle(
        CreateCharacterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // GPT-5.4 FIX: Removed duplicate validation - FluentValidation (CreateCharacterCommandValidator)
            // is now the single source of truth for validation rules.
            // Name validation: max 50 characters (as defined in CreateCharacterCommandValidator)
            // Metatype validation: Human, Elf, Dwarf, Ork, Troll
            // Attribute validation: 1-10 for all attributes

            // Check if character name already exists for this user
            var existing = await _databaseService.GetCharacterByNameAsync(
                request.DiscordUserId,
                request.Name).ConfigureAwait(false);

            if (existing != null)
            {
                return new CreateCharacterResponse
                {
                    Success = false,
                    Error = $"Character '{request.Name}' already exists for this user"
                };
            }

            // GPT-5.4 FIX: Removed metatype and attribute validation - handled by FluentValidation
            // Metatype validation: Human, Elf, Dwarf, Ork, Troll (in CreateCharacterCommandValidator)
            // Attribute validation: 1-10 for all attributes (in CreateCharacterCommandValidator)

            var character = new ShadowrunCharacter
            {
                DiscordUserId = request.DiscordUserId,
                Name = request.Name,
                Metatype = request.Metatype,
                Archetype = request.Archetype,
                Body = request.Body,
                Quickness = request.Quickness,
                Strength = request.Strength,
                Charisma = request.Charisma,
                Intelligence = request.Intelligence,
                Willpower = request.Willpower,
                Karma = request.Karma,
                Nuyen = request.Nuyen,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Create the character in the database (wrapped in transaction)
            var created = await _databaseService.CreateCharacterAsync(character).ConfigureAwait(false);

            // Invalidate user characters cache
            await _cacheService.RemoveAsync(CacheKeys.UserCharacters(request.DiscordUserId)).ConfigureAwait(false);

            _logger.LogInformation("Created character {CharacterName} (ID: {CharacterId}) for user {UserId}",
                created.Name, created.Id, created.DiscordUserId);

            return new CreateCharacterResponse
            {
                Success = true,
                CharacterId = created.Id,
                Message = $"Character **{created.Name}** created successfully!"
            };
        }
        catch (CharacterAlreadyExistsException ex)
        {
            // GPT-5.4 FIX: Surface duplicate-name domain exception as a user-friendly response.
            _logger.LogWarning(ex, "Character already exists: {CharacterName}", request.Name);
            return new CreateCharacterResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create character {CharacterName}", request.Name);
            return new CreateCharacterResponse
            {
                Success = false,
                Error = "An error occurred while creating the character. Please try again later."
            };
        }
    }
}
