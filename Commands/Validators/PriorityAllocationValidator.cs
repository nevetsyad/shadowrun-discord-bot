using FluentValidation;
using ShadowrunDiscordBot.Domain.Entities;

namespace ShadowrunDiscordBot.Commands.Validators;

/// <summary>
/// Validator ensuring all 5 priorities A-E are assigned exactly once
/// Validates complete SR3 priority allocation system
/// </summary>
public class PriorityAllocationValidator : AbstractValidator<PriorityAllocation>
{
    private static readonly string[] ValidPriorities = { "A", "B", "C", "D", "E" };

    public PriorityAllocationValidator()
    {
        // Each priority field must be valid (A-E)
        RuleFor(x => x.MetatypePriority)
            .NotEmpty().WithMessage("Metatype priority is required")
            .Must(BeValidPriority).WithMessage("Metatype priority must be A, B, C, D, or E")
            .MaximumLength(1).WithMessage("Metatype priority must be a single character");

        RuleFor(x => x.AttributesPriority)
            .NotEmpty().WithMessage("Attributes priority is required")
            .Must(BeValidPriority).WithMessage("Attributes priority must be A, B, C, D, or E")
            .MaximumLength(1).WithMessage("Attributes priority must be a single character");

        RuleFor(x => x.MagicPriority)
            .NotEmpty().WithMessage("Magic priority is required")
            .Must(BeValidPriority).WithMessage("Magic priority must be A, B, C, D, or E")
            .MaximumLength(1).WithMessage("Magic priority must be a single character");

        RuleFor(x => x.SkillsPriority)
            .NotEmpty().WithMessage("Skills priority is required")
            .Must(BeValidPriority).WithMessage("Skills priority must be A, B, C, D, or E")
            .MaximumLength(1).WithMessage("Skills priority must be a single character");

        RuleFor(x => x.ResourcesPriority)
            .NotEmpty().WithMessage("Resources priority is required")
            .Must(BeValidPriority).WithMessage("Resources priority must be A, B, C, D, or E")
            .MaximumLength(1).WithMessage("Resources priority must be a single character");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required")
            .Must(BeValidPriority).WithMessage("Priority must be A, B, C, D, or E")
            .MaximumLength(1).WithMessage("Priority must be a single character");

        // Validate that all 5 priorities are assigned exactly once
        RuleFor(x => x)
            .Must(x => x.MetatypePriority != x.AttributesPriority)
            .WithMessage("Metatype and Attributes cannot share the same priority")
            .When(x => !string.IsNullOrEmpty(x.MetatypePriority) && !string.IsNullOrEmpty(x.AttributesPriority));

        RuleFor(x => x)
            .Must(x => x.AttributesPriority != x.MagicPriority)
            .WithMessage("Attributes and Magic cannot share the same priority")
            .When(x => !string.IsNullOrEmpty(x.AttributesPriority) && !string.IsNullOrEmpty(x.MagicPriority));

        RuleFor(x => x)
            .Must(x => x.MagicPriority != x.SkillsPriority)
            .WithMessage("Magic and Skills cannot share the same priority")
            .When(x => !string.IsNullOrEmpty(x.MagicPriority) && !string.IsNullOrEmpty(x.SkillsPriority));

        RuleFor(x => x)
            .Must(x => x.SkillsPriority != x.ResourcesPriority)
            .WithMessage("Skills and Resources cannot share the same priority")
            .When(x => !string.IsNullOrEmpty(x.SkillsPriority) && !string.IsNullOrEmpty(x.ResourcesPriority));

        RuleFor(x => x)
            .Must(x => x.ResourcesPriority != x.MetatypePriority)
            .WithMessage("Resources and Metatype cannot share the same priority")
            .When(x => !string.IsNullOrEmpty(x.ResourcesPriority) && !string.IsNullOrEmpty(x.MetatypePriority));

        // Ensure all 5 priorities are used (complete A-E allocation)
        RuleFor(x => x)
            .Must(x => AreAllPrioritiesUnique(x.MetatypePriority, x.AttributesPriority, x.MagicPriority, x.SkillsPriority, x.ResourcesPriority))
            .WithMessage("All 5 priorities (A-E) must be assigned exactly once to each category")
            .When(x => !string.IsNullOrEmpty(x.MetatypePriority) && !string.IsNullOrEmpty(x.AttributesPriority) &&
                       !string.IsNullOrEmpty(x.MagicPriority) && !string.IsNullOrEmpty(x.SkillsPriority) &&
                       !string.IsNullOrEmpty(x.ResourcesPriority));
    }

    private bool BeValidPriority(string priority)
    {
        return !string.IsNullOrEmpty(priority) && ValidPriorities.Contains(priority);
    }

    private bool AreAllPrioritiesUnique(string p1, string p2, string p3, string p4, string p5)
    {
        return new[] { p1, p2, p3, p4, p5 }.Distinct().Count() == 5;
    }
}
