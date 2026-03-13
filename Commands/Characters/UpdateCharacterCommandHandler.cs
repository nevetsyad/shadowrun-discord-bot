using MediatR;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Commands.Characters;

/// <summary>
/// Handler for updating existing characters
/// </summary>
public class UpdateCharacterCommandHandler : IRequestHandler<UpdateCharacterCommand, UpdateCharacterResponse>
{
    private readonly DatabaseService _databaseService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UpdateCharacterCommandHandler> _logger;

    public UpdateCharacterCommandHandler(
        DatabaseService databaseService,
        ICacheService cacheService,
        ILogger<UpdateCharacterCommandHandler> logger)
    {
        _databaseService = databaseService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<UpdateCharacterResponse> Handle(
        UpdateCharacterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var character = await _databaseService.GetCharacterAsync(request.CharacterId).ConfigureAwait(false);
            
            if (character == null)
            {
                return new UpdateCharacterResponse
                {
                    Success = false,
                    Error = "Character not found."
                };
            }

            // Verify ownership
            if (character.DiscordUserId != request.DiscordUserId)
            {
                return new UpdateCharacterResponse
                {
                    Success = false,
                    Error = "You don't have permission to update this character."
                };
            }

            // Apply updates
            if (request.Name != null) character.Name = request.Name;
            if (request.Body.HasValue) character.Body = request.Body.Value;
            if (request.Quickness.HasValue) character.Quickness = request.Quickness.Value;
            if (request.Strength.HasValue) character.Strength = request.Strength.Value;
            if (request.Charisma.HasValue) character.Charisma = request.Charisma.Value;
            if (request.Intelligence.HasValue) character.Intelligence = request.Intelligence.Value;
            if (request.Willpower.HasValue) character.Willpower = request.Willpower.Value;
            if (request.Karma.HasValue) character.Karma = request.Karma.Value;
            if (request.Nuyen.HasValue) character.Nuyen = request.Nuyen.Value;
            if (request.PhysicalDamage.HasValue) character.PhysicalDamage = request.PhysicalDamage.Value;
            if (request.StunDamage.HasValue) character.StunDamage = request.StunDamage.Value;

            character.UpdatedAt = DateTime.UtcNow;

            await _databaseService.UpdateCharacterAsync(character).ConfigureAwait(false);

            // Invalidate caches
            await _cacheService.RemoveAsync(CacheKeys.Character(character.Id)).ConfigureAwait(false);
            await _cacheService.RemoveAsync(CacheKeys.UserCharacters(request.DiscordUserId)).ConfigureAwait(false);

            _logger.LogInformation("Updated character {CharacterName} (ID: {CharacterId})",
                character.Name, character.Id);

            return new UpdateCharacterResponse
            {
                Success = true,
                Message = $"Character **{character.Name}** updated successfully!"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update character {CharacterId}", request.CharacterId);
            return new UpdateCharacterResponse
            {
                Success = false,
                Error = "An error occurred while updating the character."
            };
        }
    }
}
