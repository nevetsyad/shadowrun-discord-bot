using FluentValidation;
using ShadowrunDiscordBot.Commands.Characters;

namespace ShadowrunDiscordBot.Commands.Validators;

/// <summary>
/// Validator for UpdateCharacterCommand using FluentValidation
/// FIX: Added comprehensive validation rules for character updates
/// </summary>
public class UpdateCharacterCommandValidator : AbstractValidator<UpdateCharacterCommand>
{
    public UpdateCharacterCommandValidator()
    {
        RuleFor(x => x.CharacterId)
            .GreaterThan(0).WithMessage("Character ID must be valid");

        RuleFor(x => x.DiscordUserId)
            .GreaterThan(0).WithMessage("Discord User ID must be valid");

        // Name validation (only if provided)
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Character name cannot be empty when provided")
            .Length(1, 50).WithMessage("Character name must be between 1 and 50 characters")
            .Matches(@"^[a-zA-Z0-9_\-\s]+$").WithMessage("Character name can only contain letters, numbers, spaces, underscores, and hyphens")
            .When(x => x.Name != null);

        // Attribute validation (only if provided)
        RuleFor(x => x.Body)
            .InclusiveBetween(1, 10).WithMessage("Body must be between 1 and 10")
            .When(x => x.Body.HasValue);

        RuleFor(x => x.Quickness)
            .InclusiveBetween(1, 10).WithMessage("Quickness must be between 1 and 10")
            .When(x => x.Quickness.HasValue);

        RuleFor(x => x.Strength)
            .InclusiveBetween(1, 10).WithMessage("Strength must be between 1 and 10")
            .When(x => x.Strength.HasValue);

        RuleFor(x => x.Charisma)
            .InclusiveBetween(1, 10).WithMessage("Charisma must be between 1 and 10")
            .When(x => x.Charisma.HasValue);

        RuleFor(x => x.Intelligence)
            .InclusiveBetween(1, 10).WithMessage("Intelligence must be between 1 and 10")
            .When(x => x.Intelligence.HasValue);

        RuleFor(x => x.Willpower)
            .InclusiveBetween(1, 10).WithMessage("Willpower must be between 1 and 10")
            .When(x => x.Willpower.HasValue);

        // Resources validation (only if provided)
        RuleFor(x => x.Karma)
            .GreaterThanOrEqualTo(0).WithMessage("Karma cannot be negative")
            .When(x => x.Karma.HasValue);

        RuleFor(x => x.Nuyen)
            .GreaterThanOrEqualTo(0).WithMessage("Nuyen cannot be negative")
            .LessThanOrEqualTo(1000000000).WithMessage("Nuyen exceeds maximum allowed value")
            .When(x => x.Nuyen.HasValue);

        // Damage validation (only if provided)
        RuleFor(x => x.PhysicalDamage)
            .GreaterThanOrEqualTo(0).WithMessage("Physical damage cannot be negative")
            .LessThanOrEqualTo(20).WithMessage("Physical damage cannot exceed 20 (beyond death)")
            .When(x => x.PhysicalDamage.HasValue);

        RuleFor(x => x.StunDamage)
            .GreaterThanOrEqualTo(0).WithMessage("Stun damage cannot be negative")
            .LessThanOrEqualTo(10).WithMessage("Stun damage cannot exceed 10 (unconscious)")
            .When(x => x.StunDamage.HasValue);

        // Ensure at least one field is being updated
        RuleFor(x => x)
            .Must(HaveAtLeastOneUpdate).WithMessage("At least one field must be provided for update")
            .OverridePropertyName("UpdateFields");
    }

    private bool HaveAtLeastOneUpdate(UpdateCharacterCommand command)
    {
        return command.Name != null ||
               command.Body.HasValue ||
               command.Quickness.HasValue ||
               command.Strength.HasValue ||
               command.Charisma.HasValue ||
               command.Intelligence.HasValue ||
               command.Willpower.HasValue ||
               command.Karma.HasValue ||
               command.Nuyen.HasValue ||
               command.PhysicalDamage.HasValue ||
               command.StunDamage.HasValue;
    }
}
