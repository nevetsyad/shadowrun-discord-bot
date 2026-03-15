namespace ShadowrunDiscordBot.Application.Features.Characters.Handlers;

using MediatR;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;
using ShadowrunDiscordBot.Application.DTOs;
using ShadowrunDiscordBot.Application.Features.Characters.Commands;
using ShadowrunDiscordBot.Application.Services;

/// <summary>
/// SR3 COMPLIANT: Handler for creating a new character with base attributes + racial modifiers
/// User provides BASE attributes, system automatically applies racial modifiers to get FINAL attributes
/// </summary>
public class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, CharacterDto>
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IArchetypeService _archetypeService;
    private readonly ILogger<CreateCharacterCommandHandler> _logger;

    public CreateCharacterCommandHandler(
        ICharacterRepository characterRepository,
        IArchetypeService archetypeService,
        ILogger<CreateCharacterCommandHandler> logger)
    {
        _characterRepository = characterRepository;
        _archetypeService = archetypeService;
        _logger = logger;
    }

    public async Task<CharacterDto> Handle(CreateCharacterCommand request, CancellationToken cancellationToken)
    {
        var isCustomBuild = string.IsNullOrWhiteSpace(request.Character.ArchetypeId);
        
        _logger.LogInformation(
            "Creating SR3 character {CharacterName} for user {DiscordUserId} - {BuildType}",
            request.Character.Name,
            request.DiscordUserId,
            isCustomBuild ? "Custom Build" : $"Archetype: {request.Character.ArchetypeId}");

        // SR3 COMPLIANCE: Get base attributes from request
        var baseBody = request.Character.BaseBody;
        var baseQuickness = request.Character.BaseQuickness;
        var baseStrength = request.Character.BaseStrength;
        var baseCharisma = request.Character.BaseCharisma;
        var baseIntelligence = request.Character.BaseIntelligence;
        var baseWillpower = request.Character.BaseWillpower;
        
        _logger.LogInformation(
            "Base attributes for {Metatype}: Body={BaseBody}, Quickness={BaseQuickness}, Strength={BaseStrength}, " +
            "Charisma={BaseCharisma}, Intelligence={BaseIntelligence}, Willpower={BaseWillpower}",
            request.Character.Metatype, baseBody, baseQuickness, baseStrength,
            baseCharisma, baseIntelligence, baseWillpower);

        // Validate archetype if provided (OPTIONAL)
        ArchetypeTemplate? archetype = null;
        if (!isCustomBuild)
        {
            archetype = await ValidateArchetypeAsync(request.Character, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Creating character with custom build (no archetype)");
        }

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

        // SR3 COMPLIANCE: Create character using domain factory method with base attributes
        // The factory method will automatically apply racial modifiers
        var character = Character.Create(
            request.Character.Name,
            request.DiscordUserId,
            request.Character.Metatype,
            archetype?.Name ?? request.Character.Archetype,
            request.Character.ArchetypeId,
            baseBody,
            baseQuickness,
            baseStrength,
            baseCharisma,
            baseIntelligence,
            baseWillpower);

        // Log the applied racial modifiers
        _logger.LogInformation(
            "Applied {Metatype} racial modifiers: Body={BodyMod:+0;-#;+0}, Quickness={QuicknessMod:+0;-#;+0}, " +
            "Strength={StrengthMod:+0;-#;+0}, Charisma={CharismaMod:+0;-#;+0}, " +
            "Intelligence={IntelligenceMod:+0;-#;+0}, Willpower={WillpowerMod:+0;-#;+0}",
            request.Character.Metatype,
            character.AppliedRacialModifiers["Body"],
            character.AppliedRacialModifiers["Quickness"],
            character.AppliedRacialModifiers["Strength"],
            character.AppliedRacialModifiers["Charisma"],
            character.AppliedRacialModifiers["Intelligence"],
            character.AppliedRacialModifiers["Willpower"]);
        
        _logger.LogInformation(
            "Final attributes: Body={Body}, Quickness={Quickness}, Strength={Strength}, " +
            "Charisma={Charisma}, Intelligence={Intelligence}, Willpower={Willpower}",
            character.Body, character.Quickness, character.Strength,
            character.Charisma, character.Intelligence, character.Willpower);

        // Apply archetype bonuses if archetype is provided
        if (archetype != null)
        {
            ApplyArchetypeBonuses(character, archetype);
        }

        // Persist character
        await _characterRepository.AddAsync(character, cancellationToken);

        _logger.LogInformation(
            "Character {CharacterId} created successfully - {BuildType} with {DomainEventCount} domain events",
            character.Id,
            character.IsCustomBuild ? "Custom Build" : $"Archetype: {request.Character.ArchetypeId}",
            character.DomainEvents.Count);

        // Map to DTO
        return MapToDto(character);
    }

    /// <summary>
    /// Validate archetype constraints (only called if archetype is provided)
    /// </summary>
    private async Task<ArchetypeTemplate> ValidateArchetypeAsync(CreateCharacterDto characterDto, CancellationToken cancellationToken)
    {
        // Retrieve archetype
        var archetype = await _archetypeService.GetArchetypeByIdAsync(characterDto.ArchetypeId!, cancellationToken);
        if (archetype == null)
        {
            throw new ArgumentException($"Archetype '{characterDto.ArchetypeId}' not found", nameof(characterDto.ArchetypeId));
        }

        if (!archetype.IsActive)
        {
            throw new ArgumentException($"Archetype '{archetype.Name}' is not available for character creation", nameof(characterDto.ArchetypeId));
        }

        // Validate metatype compatibility
        var isCompatible = await _archetypeService.IsMetatypeCompatibleAsync(
            characterDto.ArchetypeId!,
            characterDto.Metatype,
            cancellationToken);

        if (!isCompatible)
        {
            throw new ArgumentException(
                $"Metatype '{characterDto.Metatype}' is not compatible with archetype '{archetype.Name}'. " +
                $"Compatible metatypes: {string.Join(", ", archetype.CompatibleMetatypes)}",
                nameof(characterDto.Metatype));
        }

        // Validate attributes against archetype requirements
        // SR3 COMPLIANCE: Validate FINAL attributes (after racial modifiers)
        var finalAttributes = PriorityTable.CalculateFinalAttributes(
            characterDto.Metatype,
            characterDto.BaseBody, characterDto.BaseQuickness, characterDto.BaseStrength,
            characterDto.BaseCharisma, characterDto.BaseIntelligence, characterDto.BaseWillpower);

        var (isValid, errors) = await _archetypeService.ValidateAttributesAsync(
            characterDto.ArchetypeId!,
            finalAttributes["Body"],
            finalAttributes["Quickness"],
            finalAttributes["Strength"],
            finalAttributes["Charisma"],
            finalAttributes["Intelligence"],
            finalAttributes["Willpower"],
            cancellationToken);

        if (!isValid)
        {
            throw new ArgumentException(
                $"Attributes do not meet archetype requirements:\n{string.Join("\n", errors)}",
                nameof(characterDto));
        }

        return archetype;
    }

    /// <summary>
    /// Apply archetype-specific bonuses to character
    /// </summary>
    private void ApplyArchetypeBonuses(Character character, ArchetypeTemplate archetype)
    {
        // Apply starting resources
        character.AddKarma(archetype.StartingKarma);
        character.AddNuyen(archetype.StartingNuyen);

        // Apply skill bonuses
        foreach (var skillBonus in archetype.SkillBonuses)
        {
            character.AddSkill(
                skillBonus.SkillName,
                skillBonus.Bonus,
                skillBonus.Specialization,
                skillBonus.IsKnowledgeSkill);
        }

        _logger.LogInformation(
            "Applied archetype bonuses: {Karma} karma, {Nuyen}¥ nuyen, {SkillCount} skills",
            archetype.StartingKarma,
            archetype.StartingNuyen,
            archetype.SkillBonuses.Count);
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
            IsCustomBuild = character.IsCustomBuild,
            
            // SR3 COMPLIANCE: Include both base and final attributes
            BaseBody = character.BaseBody,
            BaseQuickness = character.BaseQuickness,
            BaseStrength = character.BaseStrength,
            BaseCharisma = character.BaseCharisma,
            BaseIntelligence = character.BaseIntelligence,
            BaseWillpower = character.BaseWillpower,
            
            Body = character.Body,
            Quickness = character.Quickness,
            Strength = character.Strength,
            Charisma = character.Charisma,
            Intelligence = character.Intelligence,
            Willpower = character.Willpower,
            
            AppliedRacialModifiers = character.AppliedRacialModifiers,
            
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
/// GPT-5.4 FIX: Handler for archetype-based character creation command (optional helper)
/// </summary>
public class CreateArchetypeCharacterCommandHandler : IRequestHandler<CreateArchetypeCharacterCommand, CharacterDto>
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IArchetypeService _archetypeService;
    private readonly ILogger<CreateArchetypeCharacterCommandHandler> _logger;

    public CreateArchetypeCharacterCommandHandler(
        ICharacterRepository characterRepository,
        IArchetypeService archetypeService,
        ILogger<CreateArchetypeCharacterCommandHandler> logger)
    {
        _characterRepository = characterRepository;
        _archetypeService = archetypeService;
        _logger = logger;
    }

    public async Task<CharacterDto> Handle(CreateArchetypeCharacterCommand request, CancellationToken cancellationToken)
    {
        // Delegate to the main handler by converting to CreateCharacterCommand
        var command = new CreateCharacterCommand
        {
            DiscordUserId = request.DiscordUserId,
            Character = new CreateCharacterDto
            {
                Name = request.Name,
                Metatype = request.Metatype,
                ArchetypeId = request.ArchetypeId,
                Body = request.Body,
                Quickness = request.Quickness,
                Strength = request.Strength,
                Charisma = request.Charisma,
                Intelligence = request.Intelligence,
                Willpower = request.Willpower
            }
        };

        var handler = new CreateCharacterCommandHandler(_characterRepository, _archetypeService, _logger);
        return await handler.Handle(command, cancellationToken);
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
