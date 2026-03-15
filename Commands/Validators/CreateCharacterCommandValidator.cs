using FluentValidation;
using ShadowrunDiscordBot.Commands.Characters;

namespace ShadowrunDiscordBot.Commands.Validators;

/// <summary>
/// GPT-5.4 FIX: Validator for CreateCharacterCommand with OPTIONAL archetype support
/// If archetypeId is provided, validates against archetype constraints
/// If archetypeId is null, validates basic SR3 rules
/// Priority System: Validates priority-based character creation
/// </summary>
public class CreateCharacterCommandValidator : AbstractValidator<CreateCharacterCommand>
{
    private static readonly string[] ValidMetatypes = { "Human", "Elf", "Dwarf", "Ork", "Troll" };

    public CreateCharacterCommandValidator()
    {
        RuleFor(x => x.DiscordUserId)
            .GreaterThan(0).WithMessage("Discord User ID must be valid");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Character name is required")
            .Length(1, 50).WithMessage("Character name must be between 1 and 50 characters")
            .Matches(@"^[a-zA-Z0-9_\-\s]+$").WithMessage("Character name can only contain letters, numbers, spaces, underscores, and hyphens");

        RuleFor(x => x.Metatype)
            .NotEmpty().WithMessage("Metatype is required")
            .Must(BeValidMetatype).WithMessage($"Metatype must be one of: {string.Join(", ", ValidMetatypes)}");

        // GPT-5.4 FIX: Archetype is now OPTIONAL - only validate if provided
        // If ArchetypeId is null, it's a custom build and we don't validate the Archetype field
        RuleFor(x => x.Archetype)
            .NotEmpty().WithMessage("Archetype display name is required when ArchetypeId is provided")
            .When(x => !string.IsNullOrWhiteSpace(x.ArchetypeId));

        // GPT-5.4 FIX: ArchetypeId is optional - if provided, it must be valid
        // No validation needed when null (custom build)

        // Attribute validation (Shadowrun 3e attributes range: 1-10 for most metatypes)
        RuleFor(x => x.Body)
            .InclusiveBetween(1, 10).WithMessage("Body must be between 1 and 10");

        RuleFor(x => x.Quickness)
            .InclusiveBetween(1, 10).WithMessage("Quickness must be between 1 and 10");

        RuleFor(x => x.Strength)
            .InclusiveBetween(1, 10).WithMessage("Strength must be between 1 and 10");

        RuleFor(x => x.Charisma)
            .InclusiveBetween(1, 10).WithMessage("Charisma must be between 1 and 10");

        RuleFor(x => x.Intelligence)
            .InclusiveBetween(1, 10).WithMessage("Intelligence must be between 1 and 10");

        RuleFor(x => x.Willpower)
            .InclusiveBetween(1, 10).WithMessage("Willpower must be between 1 and 10");

        // Priority System Validation
        RuleFor(x => x.PriorityLevel)
            .NotEmpty().WithMessage("Priority level is required for priority-based characters")
            .Must(BeValidPriority).WithMessage("Priority must be one of: A, B, C, D, E");

        // Priority-specific validations
        RuleFor(x => x.Body)
            .Must((command, body) => BeWithinRacialMaximum(command.Metatype, body))
            .WithMessage("Body exceeds racial maximum for this metatype")
            .When(x => x.PriorityLevel != null && x.PriorityLevel != "E");

        RuleFor(x => x.Quickness)
            .Must((command, quickness) => BeWithinRacialMaximum(command.Metatype, quickness))
            .WithMessage("Quickness exceeds racial maximum for this metatype")
            .When(x => x.PriorityLevel != null && x.PriorityLevel != "E");

        RuleFor(x => x.Strength)
            .Must((command, strength) => BeWithinRacialMaximum(command.Metatype, strength))
            .WithMessage("Strength exceeds racial maximum for this metatype")
            .When(x => x.PriorityLevel != null && x.PriorityLevel != "E");

        RuleFor(x => x.Charisma)
            .Must((command, charisma) => BeWithinRacialMaximum(command.Metatype, charisma))
            .WithMessage("Charisma exceeds racial maximum for this metatype")
            .When(x => x.PriorityLevel != null && x.PriorityLevel != "E");

        RuleFor(x => x.Intelligence)
            .Must((command, intelligence) => BeWithinRacialMaximum(command.Metatype, intelligence))
            .WithMessage("Intelligence exceeds racial maximum for this metatype")
            .When(x => x.PriorityLevel != null && x.PriorityLevel != "E");

        RuleFor(x => x.Willpower)
            .Must((command, willpower) => BeWithinRacialMaximum(command.Metatype, willpower))
            .WithMessage("Willpower exceeds racial maximum for this metatype")
            .When(x => x.PriorityLevel != null && x.PriorityLevel != "E");

        // Resources validation
        RuleFor(x => x.Karma)
            .GreaterThanOrEqualTo(0).WithMessage("Karma cannot be negative");

        RuleFor(x => x.Nuyen)
            .GreaterThanOrEqualTo(0).WithMessage("Nuyen cannot be negative")
            .LessThanOrEqualTo(1000000000).WithMessage("Nuyen exceeds maximum allowed value");

        // Magic validation for priority system
        RuleFor(x => x.Magic)
            .Must((command, magic) => BeAppropriateMagic(command.PriorityLevel, command.Metatype, magic))
            .WithMessage("Magic is not appropriate for this priority level and metatype")
            .When(x => x.PriorityLevel != null);
    }

    private bool BeValidMetatype(string metatype)
    {
        return ValidMetatypes.Contains(metatype, StringComparer.OrdinalIgnoreCase);
    }

    private bool BeValidPriority(string priority)
    {
        return priority != null && (priority == "A" || priority == "B" || priority == "C" || priority == "D" || priority == "E");
    }

    private bool BeWithinRacialMaximum(string metatype, int value)
    {
        if (!PriorityTable.RacialMaximums.TryGetValue(metatype, out var maxValues))
        {
            return true;
        }

        var attributes = new List<(string Name, int Value)>
        {
            ("Body", value),
            ("Quickness", value),
            ("Strength", value),
            ("Charisma", value),
            ("Intelligence", value),
            ("Willpower", value)
        };

        return attributes.All(a => maxValues.ContainsKey(a.Name) && a.Value <= maxValues[a.Name]);
    }

    private bool BeAppropriateMagic(string priority, string metatype, int magic)
    {
        // Priority E: Mundane only (magic must be 0)
        if (priority == "E")
        {
            return magic == 0;
        }

        // Priority C-D: Mundane only (magic must be 0)
        if (priority == "C" || priority == "D")
        {
            return magic == 0;
        }

        // Priority A-B: Awakened possible (magic can be 5-6)
        if (priority == "A")
        {
            return magic >= 6;
        }

        if (priority == "B")
        {
            return magic >= 5 && magic <= 6;
        }

        return true;
    }
}
