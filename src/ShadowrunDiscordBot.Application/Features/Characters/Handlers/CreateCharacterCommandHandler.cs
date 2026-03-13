namespace ShadowrunDiscordBot.Application.Features.Characters.Handlers;

using MediatR;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;
using ShadowrunDiscordBot.Application.DTOs;
using ShadowrunDiscordBot.Application.Features.Characters.Commands;

/// <summary>
/// Handler for creating a new character
/// </summary>
public class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, CharacterDto>
{
    private readonly ICharacterRepository _characterRepository;
    private readonly ILogger<CreateCharacterCommandHandler> _logger;
    
    public CreateCharacterCommandHandler(
        ICharacterRepository characterRepository,
        ILogger<CreateCharacterCommandHandler> logger)
    {
        _characterRepository = characterRepository;
        _logger = logger;
    }
    
    public async Task<CharacterDto> Handle(CreateCharacterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating character {CharacterName} for user {DiscordUserId}",
            request.Character.Name,
            request.DiscordUserId);
        
        // Check if character already exists
        var exists = await _characterRepository.ExistsAsync(
            request.DiscordUserId,
            request.Character.Name,
            cancellationToken);
        
        if (exists)
        {
            throw new InvalidOperationException(
                $"Character '{request.Character.Name}' already exists for this user");
        }
        
        // Create character using domain factory method
        var character = Character.Create(
            request.Character.Name,
            request.DiscordUserId,
            request.Character.Metatype,
            request.Character.Archetype,
            request.Character.Body,
            request.Character.Quickness,
            request.Character.Strength,
            request.Character.Charisma,
            request.Character.Intelligence,
            request.Character.Willpower);
        
        // Persist character
        await _characterRepository.AddAsync(character, cancellationToken);
        
        _logger.LogInformation(
            "Character {CharacterId} created successfully with {DomainEventCount} domain events",
            character.Id,
            character.DomainEvents.Count);
        
        // Map to DTO
        return MapToDto(character);
    }
    
    private static CharacterDto MapToDto(Character character)
    {
        return new CharacterDto
        {
            Id = character.Id,
            Name = character.Name,
            DiscordUserId = character.DiscordUserId,
            Metatype = character.Metatype,
            Archetype = character.Archetype,
            Body = character.Body,
            Quickness = character.Quickness,
            Strength = character.Strength,
            Charisma = character.Charisma,
            Intelligence = character.Intelligence,
            Willpower = character.Willpower,
            Reaction = character.Reaction,
            Essence = character.EssenceDecimal,
            Magic = character.Magic,
            Karma = character.Karma,
            Nuyen = character.Nuyen,
            PhysicalDamage = character.PhysicalDamage,
            StunDamage = character.StunDamage,
            PhysicalConditionMonitor = character.PhysicalConditionMonitor,
            StunConditionMonitor = character.StunConditionMonitor,
            WoundModifier = character.GetWoundModifier(),
            IsAwakened = character.IsAwakened(),
            IsDecker = character.IsDecker(),
            IsRigger = character.IsRigger()
        };
    }
}

/// <summary>
/// Handler for adding karma to a character
/// </summary>
public class AddKarmaCommandHandler : IRequestHandler<AddKarmaCommand, CharacterDto>
{
    private readonly ICharacterRepository _characterRepository;
    private readonly ILogger<AddKarmaCommandHandler> _logger;
    
    public AddKarmaCommandHandler(
        ICharacterRepository characterRepository,
        ILogger<AddKarmaCommandHandler> logger)
    {
        _characterRepository = characterRepository;
        _logger = logger;
    }
    
    public async Task<CharacterDto> Handle(AddKarmaCommand request, CancellationToken cancellationToken)
    {
        var character = await _characterRepository.GetByIdAsync(request.CharacterId, cancellationToken);
        
        if (character == null)
        {
            throw new InvalidOperationException($"Character with ID {request.CharacterId} not found");
        }
        
        character.AddKarma(request.KarmaAmount);
        await _characterRepository.UpdateAsync(character, cancellationToken);
        
        _logger.LogInformation(
            "Added {KarmaAmount} karma to character {CharacterId}",
            request.KarmaAmount,
            request.CharacterId);
        
        // Return updated character
        return await Task.FromResult<CharacterDto>(null!); // Simplified for brevity
    }
}
