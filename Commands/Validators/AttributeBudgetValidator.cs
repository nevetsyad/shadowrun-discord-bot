using FluentValidation;
using ShadowrunDiscordBot.Domain.Entities;

namespace ShadowrunDiscordBot.Commands.Validators;

/// <summary>
/// Validator ensuring attribute total doesn't exceed priority point budget
/// Validates SR3 attribute point allocation
/// </summary>
public class AttributeBudgetValidator : AbstractValidator<PriorityAllocation>
{
    public AttributeBudgetValidator()
    {
        // Calculate total attribute points from priority allocation
        RuleFor(x => x)
            .Must(x => BeWithinAttributeBudget(x.AttributesPriority, x.MetatypePriority, x.SkillsPriority))
            .WithMessage("Attribute point allocation exceeds the budget for the assigned priority")
            .When(x => !string.IsNullOrEmpty(x.AttributesPriority) && !string.IsNullOrEmpty(x.MetatypePriority));

        // Magic budget validation
        RuleFor(x => x)
            .Must(x => BeWithinMagicBudget(x.MagicPriority, x.MetatypePriority, x.SkillsPriority))
            .WithMessage("Magic allocation exceeds the budget for the assigned priority")
            .When(x => !string.IsNullOrEmpty(x.MagicPriority) && !string.IsNullOrEmpty(x.MetatypePriority));

        // Skills budget validation
        RuleFor(x => x)
            .Must(x => BeWithinSkillsBudget(x.SkillsPriority, x.AttributesPriority))
            .WithMessage("Skill point allocation exceeds the budget for the assigned priority")
            .When(x => !string.IsNullOrEmpty(x.SkillsPriority) && !string.IsNullOrEmpty(x.AttributesPriority));
    }

    /// <summary>
    /// Validate that attributes are within the priority point budget
    /// Priority A: 30 points
    /// Priority B: 27 points
    /// Priority C: 24 points
    /// Priority D: 21 points
    /// Priority E: 18 points
    /// </summary>
    private bool BeWithinAttributeBudget(string attributesPriority, string metatypePriority, string skillsPriority)
    {
        if (!PriorityTable.Table.ContainsKey(attributesPriority))
            return false;

        var availablePoints = PriorityTable.Table[attributesPriority].AttributePoints;

        // Calculate total attribute points from priority levels
        // Each priority level grants that many attribute points (base attributes don't count)
        var totalPoints = 0;

        // Metatype priority determines racial base values and maximums
        var metatypeMax = PriorityTable.RacialMaximums.TryGetValue(metatypePriority, out var maxValues)
            ? maxValues
            : null;

        if (metatypeMax != null)
        {
            totalPoints = metatypeMax["Body"] + metatypeMax["Quickness"] +
                          metatypeMax["Strength"] + metatypeMax["Charisma"] +
                          metatypeMax["Intelligence"] + metatypeMax["Willpower"];
        }

        // Skills priority also affects attribute maximums (Magic priority for Adept characters)
        if (!string.IsNullOrEmpty(skillsPriority))
        {
            var skillPriorityData = PriorityTable.Table[skillsPriority];
            if (skillPriorityData.RacialRestrictions.Contains("Adept") || skillPriorityData.RacialRestrictions.Contains("Aspected"))
            {
                // Adept characters have additional attribute bonuses
                totalPoints += 3; // +3 additional attribute points
            }
        }

        return totalPoints <= availablePoints;
    }

    /// <summary>
    /// Validate that magic is within the priority budget
    /// Priority A: Full Magician (Magic 6)
    /// Priority B: Adept (Magic 5)
    /// Priority C-D-E: Mundane (Magic 0)
    /// </summary>
    private bool BeWithinMagicBudget(string magicPriority, string metatypePriority, string skillsPriority)
    {
        if (!PriorityTable.Table.ContainsKey(magicPriority))
            return false;

        var magicType = PriorityTable.Table[magicPriority].Name;

        // Determine if character can have magic
        var canHaveMagic = false;

        if (magicType == "Full Magician" || magicType == "Adept/Aspected Magician")
        {
            canHaveMagic = true;
        }

        if (!canHaveMagic)
            return true; // Can have magic (0) is always valid

        // Check metatype compatibility
        var metatypeMax = PriorityTable.RacialMaximums.TryGetValue(metatypePriority, out var maxValues)
            ? maxValues
            : null;

        if (metatypeMax != null && metatypeMax["Charisma"] < 1)
            return false; // Required attributes for magic (Charisma 1)

        // Check skills priority compatibility
        if (!string.IsNullOrEmpty(skillsPriority))
        {
            var skillPriorityData = PriorityTable.Table[skillsPriority];
            if (skillPriorityData.RacialRestrictions.Contains("Adept") || skillPriorityData.RacialRestrictions.Contains("Aspected"))
            {
                // Adept/Aspected characters must have appropriate skills
                if (!IsAdeptWithAppropriateSkills(magicPriority))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validate that skills are within the priority point budget
    /// Priority A: 50 points
    /// Priority B: 40 points
    /// Priority C: 34 points
    /// Priority D: 30 points
    /// Priority E: 27 points
    /// </summary>
    private bool BeWithinSkillsBudget(string skillsPriority, string attributesPriority)
    {
        if (!PriorityTable.Table.ContainsKey(skillsPriority))
            return false;

        var availablePoints = PriorityTable.Table[skillsPriority].SkillPoints;

        // Magic priority affects skill points for awakened characters
        var magicPriority = magicPriorityFromAllocation(attributesPriority);
        if (!string.IsNullOrEmpty(magicPriority))
        {
            var magicType = PriorityTable.Table[magicPriority].Name;
            if (magicType == "Full Magician" || magicType == "Adept/Aspected Magician")
            {
                availablePoints -= 3; // -3 skill points for Awakened
            }
        }

        return availablePoints >= 0;
    }

    private string magicPriorityFromAllocation(string attributesPriority)
    {
        // Priority C-D: Mundane only
        if (attributesPriority == "C" || attributesPriority == "D")
            return "E";

        // Priority A: Full Magician
        if (attributesPriority == "A")
            return "A";

        // Priority B: Adept/Aspected
        if (attributesPriority == "B")
            return "B";

        return "E";
    }

    private bool IsAdeptWithAppropriateSkills(string magicPriority)
    {
        var magicType = PriorityTable.Table[magicPriority].Name;
        return magicType == "Adept/Aspected Magician";
    }
}
