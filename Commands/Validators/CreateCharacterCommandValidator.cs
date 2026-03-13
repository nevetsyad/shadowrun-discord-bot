using FluentValidation;
using ShadowrunDiscordBot.Commands.Characters;

namespace ShadowrunDiscordBot.Commands.Validators;

/// <summary>
/// Validator for CreateCharacterCommand using FluentValidation
/// FIX: Added comprehensive validation rules matching input validation requirements
/// </summary>
public class CreateCharacterCommandValidator : AbstractValidator<CreateCharacterCommand>
{
    private static readonly string[] ValidMetatypes = { "Human", "Elf", "Dwarf", "Ork", "Troll" };
    private static readonly string[] ValidArchetypes = 
    { 
        "Street Samurai", "Mage", "Shaman", "Rigger", "Decker", "Physical Adept" 
    };

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

        RuleFor(x => x.Archetype)
            .NotEmpty().WithMessage("Archetype is required")
            .Must(BeValidArchetype).WithMessage($"Archetype must be one of: {string.Join(", ", ValidArchetypes)}");

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

        // Resources validation
        RuleFor(x => x.Karma)
            .GreaterThanOrEqualTo(0).WithMessage("Karma cannot be negative");

        RuleFor(x => x.Nuyen)
            .GreaterThanOrEqualTo(0).WithMessage("Nuyen cannot be negative")
            .LessThanOrEqualTo(1000000000).WithMessage("Nuyen exceeds maximum allowed value");
    }

    private bool BeValidMetatype(string metatype)
    {
        return ValidMetatypes.Contains(metatype, StringComparer.OrdinalIgnoreCase);
    }

    private bool BeValidArchetype(string archetype)
    {
        return ValidArchetypes.Contains(archetype, StringComparer.OrdinalIgnoreCase);
    }
}
