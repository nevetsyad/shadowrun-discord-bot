using MediatR;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Exceptions;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Services;
using ShadowrunDiscordBot.Application.Services;
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Commands.Validators;
using CharacterSkill = ShadowrunDiscordBot.Models.CharacterSkill;
using CharacterCyberware = ShadowrunDiscordBot.Models.CharacterCyberware;
using CharacterGear = ShadowrunDiscordBot.Models.CharacterGear;

namespace ShadowrunDiscordBot.Commands.Characters;

/// <summary>
/// SR3 COMPLIANT: Handler for creating new characters
/// REMOVES: Custom build logic - requires priority allocation OR archetype
/// VALIDATES: Attribute budgets, skill budgets, priority assignments
/// APPLIES: Fixed archetype values (no customization)
/// </summary>
public class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, CreateCharacterResponse>
{
    private readonly DatabaseService _databaseService;
    private readonly ICacheService _cacheService;
    private readonly IArchetypeService _archetypeService;
    private readonly ILogger<CreateCharacterCommandHandler> _logger;
    private readonly PriorityAllocationValidator _priorityValidator;
    private readonly AttributeBudgetValidator _budgetValidator;

    public CreateCharacterCommandHandler(
        DatabaseService databaseService,
        ICacheService cacheService,
        IArchetypeService archetypeService,
        ILogger<CreateCharacterCommandHandler> logger)
    {
        _databaseService = databaseService;
        _cacheService = cacheService;
        _archetypeService = archetypeService;
        _logger = logger;
        _priorityValidator = new PriorityAllocationValidator();
        _budgetValidator = new AttributeBudgetValidator();
    }

    public async Task<CreateCharacterResponse> Handle(
        CreateCharacterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // SR3 COMPLIANCE: Must have either priority allocation OR archetype (not both)
            var hasPriorityAllocation = request.PriorityAllocation != null;
            var hasArchetype = !string.IsNullOrWhiteSpace(request.ArchetypeId);

            if (!hasPriorityAllocation && !hasArchetype)
            {
                return new CreateCharacterResponse
                {
                    Success = false,
                    Error = "SR3 Compliance Error: Character creation requires either PriorityAllocation or ArchetypeId"
                };
            }

            if (hasPriorityAllocation && hasArchetype)
            {
                return new CreateCharacterResponse
                {
                    Success = false,
                    Error = "SR3 Compliance Error: Cannot specify both PriorityAllocation and ArchetypeId - choose one"
                };
            }

            _logger.LogInformation(
                "Creating SR3-compliant character {CharacterName} for user {UserId} - BuildType: {BuildType}",
                request.Name,
                request.DiscordUserId,
                hasArchetype ? "Archetype" : "Priority");

            // Validate character name uniqueness
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

            ShadowrunCharacter character;

            if (hasArchetype)
            {
                // ARCHETYPE BUILD: Use fixed archetype values
                character = await CreateFromArchetypeAsync(request, cancellationToken);
            }
            else
            {
                // PRIORITY BUILD: Use priority allocation with budget validation
                character = await CreateFromPriorityAsync(request, cancellationToken);
            }

            // Create the character in the database
            var created = await _databaseService.CreateCharacterAsync(character).ConfigureAwait(false);

            // Invalidate user characters cache
            await _cacheService.RemoveAsync(CacheKeys.UserCharacters(request.DiscordUserId)).ConfigureAwait(false);

            var buildType = hasArchetype ? "Archetype-based" : "Priority-based";
            _logger.LogInformation("Created SR3-compliant character {CharacterName} (ID: {CharacterId}) for user {UserId} - {BuildType}",
                created.Name, created.Id, created.DiscordUserId, buildType);

            return new CreateCharacterResponse
            {
                Success = true,
                CharacterId = created.Id,
                Message = $"SR3-compliant character **{created.Name}** created successfully! ({buildType})",
                BuildType = buildType,
                AttributePointsUsed = hasPriorityAllocation ? CalculateAttributePointsUsed(created) : null,
                SkillPointsUsed = hasPriorityAllocation ? CalculateSkillPointsUsed(created) : null
            };
        }
        catch (CharacterAlreadyExistsException ex)
        {
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

    /// <summary>
    /// SR3 COMPLIANT: Create character from fixed archetype template
    /// NO customization - attributes come from archetype definition
    /// </summary>
    private async Task<ShadowrunCharacter> CreateFromArchetypeAsync(
        CreateCharacterCommand request,
        CancellationToken cancellationToken)
    {
        var archetype = await _archetypeService.GetArchetypeByIdAsync(request.ArchetypeId!, cancellationToken);

        if (archetype == null)
        {
            throw new ArgumentException($"Archetype '{request.ArchetypeId}' not found");
        }

        // Validate metatype compatibility
        var isMetatypeCompatible = await _archetypeService.IsMetatypeCompatibleAsync(
            request.ArchetypeId!,
            request.Metatype,
            cancellationToken);

        if (!isMetatypeCompatible)
        {
            throw new ArgumentException(
                $"Metatype '{request.Metatype}' is not compatible with archetype '{archetype.Name}'. " +
                $"Compatible metatypes: {string.Join(", ", archetype.CompatibleMetatypes)}");
        }

        var character = new ShadowrunCharacter
        {
            DiscordUserId = request.DiscordUserId,
            Name = request.Name,
            Metatype = request.Metatype,
            Archetype = archetype.Name,
            ArchetypeId = request.ArchetypeId,
            IsCustomBuild = false, // SR3 COMPLIANCE: Not a custom build

            // SR3 COMPLIANCE: Apply FIXED archetype attributes
            Body = archetype.Body,
            Quickness = archetype.Quickness,
            Strength = archetype.Strength,
            Charisma = archetype.Charisma,
            Intelligence = archetype.Intelligence,
            Willpower = archetype.Willpower,

            // SR3 COMPLIANCE: Apply metatype modifiers to fixed attributes
            // (This adjusts for racial bonuses while keeping the archetype concept)
            // (Implementation would call archetype.ApplyMetatypeModifiers)

            // SR3 COMPLIANCE: Use archetype starting resources
            Nuyen = archetype.StartingNuyen,
            Karma = archetype.StartingKarma,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Apply archetype skill bonuses
        foreach (var skillBonus in archetype.SkillBonuses)
        {
            character.Skills.Add(new CharacterSkill
            {
                SkillName = skillBonus.Key,
                Rating = skillBonus.Value,
                IsKnowledgeSkill = false
            });
        }

        // Set magic if awakened archetype
        if (archetype.IsAwakened)
        {
            character.Magic = 6; // Standard starting magic for awakened
        }

        _logger.LogInformation(
            "Applied archetype {Archetype}: Fixed attributes, {Nuyen}¥, {Karma} karma, {SkillCount} skills",
            archetype.Name,
            archetype.StartingNuyen,
            archetype.StartingKarma,
            archetype.SkillBonuses.Count);

        return character;
    }

    /// <summary>
    /// SR3 COMPLIANT: Create character from priority allocation
    /// VALIDATES: All 5 priorities assigned, attribute budget, skill budget
    /// </summary>
    private async Task<ShadowrunCharacter> CreateFromPriorityAsync(
        CreateCharacterCommand request,
        CancellationToken cancellationToken)
    {
        var allocation = request.PriorityAllocation!;

        // Validate priority allocation (all A-E assigned exactly once)
        var priorityValidation = await _priorityValidator.ValidateAsync(allocation, cancellationToken);
        if (!priorityValidation.IsValid)
        {
            throw new ArgumentException($"Invalid priority allocation: {string.Join(", ", priorityValidation.Errors)}");
        }

        // Validate attribute budget
        var budgetValidation = await _budgetValidator.ValidateAsync(allocation, cancellationToken);
        if (!budgetValidation.IsValid)
        {
            throw new ArgumentException($"Budget validation failed: {string.Join(", ", budgetValidation.Errors)}");
        }

        var character = new ShadowrunCharacter
        {
            DiscordUserId = request.DiscordUserId,
            Name = request.Name,
            Metatype = request.Metatype,
            Archetype = "Priority Build",
            IsCustomBuild = false, // SR3 COMPLIANCE: Not a custom build

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Apply priority-based allocations
        await ApplyPriorityAllocationsAsync(character, allocation, request.Metatype, cancellationToken);

        return character;
    }

    /// <summary>
    /// SR3 COMPLIANT: Apply priority allocations with budget validation
    /// </summary>
    private async Task ApplyPriorityAllocationsAsync(
        ShadowrunCharacter character,
        PriorityAllocation allocation,
        string metatype,
        CancellationToken cancellationToken)
    {
        var attributePriority = allocation.AttributesPriority;
        var skillPriority = allocation.SkillsPriority;
        var resourcePriority = allocation.ResourcesPriority;
        var magicPriority = allocation.MagicPriority;

        // Get priority data
        var attributeData = PriorityTable.Table[attributePriority];
        var skillData = PriorityTable.Table[skillPriority];
        var resourceData = PriorityTable.Table[resourcePriority];

        // Apply racial base values
        var baseValues = PriorityTable.RacialBaseValues[metatype];
        character.Body = baseValues["Body"];
        character.Quickness = baseValues["Quickness"];
        character.Strength = baseValues["Strength"];
        character.Charisma = baseValues["Charisma"];
        character.Intelligence = baseValues["Intelligence"];
        character.Willpower = baseValues["Willpower"];

        // SR3 COMPLIANCE: Allocate attribute points within budget
        // (In a full implementation, this would be interactive or use defaults)
        var availableAttributePoints = attributeData.AttributePoints;
        var attributes = new List<(string Name, int Value)>
        {
            ("Body", character.Body),
            ("Quickness", character.Quickness),
            ("Strength", character.Strength),
            ("Charisma", character.Charisma),
            ("Intelligence", character.Intelligence),
            ("Willpower", character.Willpower)
        };

        // Simple allocation: distribute points evenly (would be interactive in full version)
        var remainingPoints = availableAttributePoints;
        while (remainingPoints > 0)
        {
            var minAttr = attributes.OrderBy(a => a.Value).First();
            var newValue = minAttr.Value + 1;
            var maxValues = PriorityTable.RacialMaximums[metatype];

            if (newValue <= maxValues[minAttr.Name])
            {
                minAttr.Value++;
                remainingPoints--;
            }
            else
            {
                break;
            }
        }

        // Apply calculated values
        character.Body = attributes.First(a => a.Name == "Body").Value;
        character.Quickness = attributes.First(a => a.Name == "Quickness").Value;
        character.Strength = attributes.First(a => a.Name == "Strength").Value;
        character.Charisma = attributes.First(a => a.Name == "Charisma").Value;
        character.Intelligence = attributes.First(a => a.Name == "Intelligence").Value;
        character.Willpower = attributes.First(a => a.Name == "Willpower").Value;

        // Set resources based on priority
        character.Nuyen = resourceData.Nuyen;
        character.Karma = PriorityTable.StartingKarma[metatype];

        // Set magic based on magic priority
        character.Magic = GetMagicForPriority(magicPriority);

        // SR3 COMPLIANCE: Allocate skills within budget using proper cost calculation
        // Skills cost 1 point per level for ratings 1-4
        // Skills cost 2 points per level for ratings 5-6 when skill rating >= linked attribute
        // Specializations cost 1 point per level for the specialization itself
        var availableSkillPoints = skillData.SkillPoints;
        var defaultSkills = GetDefaultSkillsForPriority(magicPriority);

        // Build attribute dictionary for skill cost calculation
        var attributes = new Dictionary<string, int>
        {
            ["Body"] = character.Body,
            ["Quickness"] = character.Quickness,
            ["Strength"] = character.Strength,
            ["Charisma"] = character.Charisma,
            ["Intelligence"] = character.Intelligence,
            ["Willpower"] = character.Willpower,
            ["Magic"] = character.Magic
        };

        foreach (var skill in defaultSkills)
        {
            // Get linked attribute for this skill
            var linkedAttr = SkillCostCalculator.GetLinkedAttribute(skill.Name);
            var attrValue = attributes.TryGetValue(linkedAttr, out var val) ? val : 3;

            // Check if skill has a specialization
            var hasSpecialization = SkillCostCalculator.HasSpecialization(skill.Name);

            // Calculate SR3-compliant skill cost
            int skillCost;
            if (hasSpecialization)
            {
                // SR3 RULE: Specialization reduces base by 1, increases specialization by 1
                // Use CalculateSkillWithSpecialization for specialization
                skillCost = SkillCostCalculator.CalculateSkillWithSpecialization(skill.Rating, attrValue);

                _logger.LogDebug(
                    "Added skill {Skill} {Rating} (cost {Cost}pts, specialization: base {Base} → spec {Spec}, linked to {Attr} {AttrVal})",
                    skill.Name, skill.Rating, skillCost, skill.Rating - 1, skill.Rating + 1, linkedAttr, attrValue);
            }
            else
            {
                // No specialization - use regular cost calculation
                skillCost = SkillCostCalculator.CalculateSkillCost(skill.Rating, attrValue);

                _logger.LogDebug(
                    "Added skill {Skill} {Rating} (cost {Cost}pts, linked to {Attr} {AttrVal})",
                    skill.Name, skill.Rating, skillCost, linkedAttr, attrValue);
            }

            if (availableSkillPoints >= skillCost)
            {
                character.Skills.Add(new CharacterSkill
                {
                    SkillName = skill.Name,
                    Rating = skill.Rating,
                    IsKnowledgeSkill = skill.IsKnowledge
                });
                availableSkillPoints -= skillCost;
            }
            else
            {
                _logger.LogWarning(
                    "Cannot afford skill {Skill} {Rating} (cost {Cost}pts, only {Available}pts available)",
                    skill.Name, skill.Rating, skillCost, availableSkillPoints);
            }
        }

        // Store priority allocation
        character.PriorityLevel = attributePriority;
        character.AttributeModifiers = new Dictionary<string, int>
        {
            ["Body"] = character.Body - baseValues["Body"],
            ["Quickness"] = character.Quickness - baseValues["Quickness"],
            ["Strength"] = character.Strength - baseValues["Strength"],
            ["Charisma"] = character.Charisma - baseValues["Charisma"],
            ["Intelligence"] = character.Intelligence - baseValues["Intelligence"],
            ["Willpower"] = character.Willpower - baseValues["Willpower"]
        };

        _logger.LogInformation(
            "Applied priority allocation: Attr={AttrPriority}({AttrPts}pts), " +
            "Skills={SkillPriority}({SkillPts}pts), Resources={ResourcePriority}({Nuyen}¥), " +
            "Magic={MagicPriority}(Magic {Magic})",
            attributePriority, attributeData.AttributePoints,
            skillPriority, skillData.SkillPoints,
            resourcePriority, resourceData.Nuyen,
            magicPriority, character.Magic);
    }

    private int GetMagicForPriority(string magicPriority)
    {
        return magicPriority switch
        {
            "A" => 6, // Full Magician
            "B" => 5, // Adept
            _ => 0   // Mundane (C, D, E)
        };
    }

    private List<(string Name, int Rating, bool IsKnowledge)> GetDefaultSkillsForPriority(string magicPriority)
    {
        var skills = new List<(string, int, bool)>();

        switch (magicPriority)
        {
            case "A": // Full Magician
                skills.Add(("Sorcery", 4, false));
                skills.Add(("Conjuring", 4, false));
                break;
            case "B": // Adept
                skills.Add(("Unarmed Combat", 5, false));
                skills.Add(("Athletics", 4, false));
                break;
            default: // C, D, E - Mundane
                skills.Add(("Athletics", 3, false));
                skills.Add(("Stealth", 2, false));
                break;
        }

        // Add common skills
        skills.Add(("Pistols", 2, false));
        skills.Add(("Edged Weapons", 2, false));

        return skills;
    }

    private int CalculateAttributePointsUsed(ShadowrunCharacter character)
    {
        return character.Body + character.Quickness + character.Strength +
               character.Charisma + character.Intelligence + character.Willpower;
    }

    private int CalculateSkillPointsUsed(ShadowrunCharacter character)
    {
        return character.Skills?.Sum(s => s.Rating) ?? 0;
    }
}
