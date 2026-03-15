namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// SR3 Skill Cost Calculator
/// 
/// SR3 RULE: Skill costs are based on linked attribute
/// - Rating 1-4: 1 point per level (normal)
/// - Rating 5-6: 2 points per level (when skill rating equals or exceeds linked attribute)
/// 
/// Example:
/// If Quickness = 6 and Edged Weapons = 5:
///   Rating 1-4: 4 levels × 1 point = 4 points
///   Rating 5: 1 level × 2 points = 2 points
///   Total: 6 points (not 5!)
/// 
/// If Quickness = 6 and Edged Weapons = 6:
///   Rating 1-4: 4 levels × 1 point = 4 points
///   Rating 5-6: 2 levels × 2 points = 4 points
///   Total: 8 points (not 6!)
/// </summary>
public static class SkillCostCalculator
{
    /// <summary>
    /// Calculate skill cost based on SR3 rules
    ///
    /// SR3 RULE:
    /// - 1 point per level for rating ≤ linked attribute
    /// - 2 points per level for rating > linked attribute
    ///
    /// SPECIALIZATION RULE:
    /// - When you specialize, you REDUCE the base skill by 1 and INCREASE the specialization by 1
    /// - Notation "2/4" means: Unarmed Combat 2 (base, reduced), Kicking 4 (specialization, increased)
    /// - Cost is based on the HIGHER rating: max(baseSkill, specialization) = 4
    /// - Example: Unarmed Combat 3 + Kicking specialization = Unarmed Combat 2, Kicking 4 = 4 points
    /// - Example: Unarmed Combat 3 (no specialization) = 3 points
    ///
    /// Example:
    /// If Quickness = 6 and Edged Weapons = 5:
    ///   Rating 1-5: 5 levels × 1 point = 5 points
    ///   Total: 5 points
    ///
    /// If Quickness = 6 and Edged Weapons = 6:
    ///   Rating 1-6: 6 levels × 1 point = 6 points
    ///   Total: 6 points
    ///
    /// If Quickness = 5 and Edged Weapons = 6:
    ///   Rating 1-5: 5 levels × 1 point = 5 points
    ///   Rating 6: 1 level × 2 points = 2 points
    ///   Total: 7 points
    /// </summary>
    /// <param name="skillRating">Base skill rating (1-6)</param>
    /// <param name="linkedAttribute">Linked attribute value (e.g., Quickness for combat skills)</param>
    /// <returns>Total skill point cost</returns>
    public static int CalculateSkillCost(int skillRating, int linkedAttribute)
    {
        if (skillRating < 1 || skillRating > 6)
            throw new ArgumentException("Skill rating must be between 1 and 6", nameof(skillRating));

        if (linkedAttribute < 1 || linkedAttribute > 9)
            throw new ArgumentException("Linked attribute must be between 1 and 9", nameof(linkedAttribute));

        // SR3 RULE:
        // - 1 point per level for rating <= linked attribute
        // - 2 points per level for rating > linked attribute
        // - Specializations are handled separately (reduce base, increase specialization)
        // - Here we just calculate the cost of the base skill rating

        if (skillRating <= linkedAttribute)
        {
            // All levels cost 1 point each
            return skillRating * 1;
        }
        else
        {
            // Levels up to linked attribute cost 1 point each
            var lowerLevels = linkedAttribute * 1;

            // Levels above linked attribute cost 2 points each
            var higherLevels = (skillRating - linkedAttribute) * 2;

            return lowerLevels + higherLevels;
        }
    }

    /// <summary>
    /// Calculate total skill cost including specialization
    ///
    /// SR3 SPECIALIZATION RULE:
    /// - When you specialize, you REDUCE the base skill by 1 and INCREASE the specialization by 1
    /// - Notation "2/4" means: Unarmed Combat 2 (base, reduced by 1), Kicking 4 (specialization, increased by 1)
    /// - Cost is based on the HIGHER rating: max(baseSkill, specialization) = 4
    /// - Example: Unarmed Combat 3 + Kicking specialization = Unarmed Combat 2, Kicking 4 = 4 points
    /// - Example: Unarmed Combat 3 (no specialization) = 3 points
    ///
    /// When calculating specialization cost:
    /// - Base skill cost: Calculate based on reduced rating (baseRating - 1)
    /// - Specialization cost: Calculate based on increased rating (baseRating + 1)
    /// - Total: max(baseCost, specializationCost)
    /// </summary>
    /// <param name="baseSkillRating">Base skill rating before specialization (1-6)</param>
    /// <param name="linkedAttribute">Linked attribute value</param>
    /// <returns>Total skill cost with specialization</returns>
    public static int CalculateSkillWithSpecialization(int baseSkillRating, int linkedAttribute)
    {
        if (baseSkillRating < 1 || baseSkillRating > 6)
            throw new ArgumentException("Base skill rating must be between 1 and 6", nameof(baseSkillRating));

        if (linkedAttribute < 1 || linkedAttribute > 9)
            throw new ArgumentException("Linked attribute must be between 1 and 9", nameof(linkedAttribute));

        // SR3 RULE: Specialization reduces base by 1 and increases specialization by 1
        var reducedBase = Math.Max(1, baseSkillRating - 1); // Cannot go below 1
        var increasedSpecialization = Math.Min(6, baseSkillRating + 1); // Cannot exceed 6

        // Calculate cost for reduced base skill
        var baseCost = CalculateSkillCost(reducedBase, linkedAttribute);

        // Calculate cost for increased specialization
        var specializationCost = CalculateSkillCost(increasedSpecialization, linkedAttribute);

        // Total cost is the HIGHER of the two
        return Math.Max(baseCost, specializationCost);
    }

    /// <summary>
    /// Get linked attribute for a skill
    /// Detects and removes specializations to return base skill name
    /// </summary>
    public static string GetLinkedAttribute(string skillName)
    {
        // Extract base skill name (remove specialization in parentheses)
        var baseSkill = skillName.Contains(" (")
            ? skillName.Substring(0, skillName.IndexOf(" ("))
            : skillName;

        // SR3 Skill-Attribute Linkages
        return baseSkill.ToLowerInvariant() switch
        {
            // Combat Skills
            "edged weapons" or "pole arms" or "whips" or "clubs" => "Quickness",
            "pistols" or "rifles" or "shotguns" or "assault rifles" or "submachine guns" => "Quickness",
            "unarmed combat" or "cyber impl. weapons" or "throwing weapons" => "Quickness",

            // Physical Skills
            "athletics" or "climbing" or "swimming" => "Strength",
            "stealth" or "disguise" => "Quickness",
            "vehicle (land)" or "vehicle (water)" or "vehicle (air)" => "Quickness",

            // Social Skills
            "etiquette" or "negotiation" or "con" or "intimidation" => "Charisma",

            // Technical Skills
            "electronics" or "computers" or "data brokerage" => "Intelligence",
            "biotech" or "cybertechnology" => "Intelligence",

            // Magical Skills
            "sorcery" or "conjuring" or "enchanting" => "Magic",

            // Knowledge Skills
            _ when baseSkill.Contains("lore") || baseSkill.Contains("knowledge") => "Intelligence",

            // Default
            _ => "Quickness" // Most skills default to Quickness
        };
    }

    /// <summary>
    /// Check if a skill has a specialization
    /// </summary>
    public static bool HasSpecialization(string skillName)
    {
        return skillName.Contains(" (");
    }

    /// <summary>
    /// Calculate total skill cost for multiple skills
    /// </summary>
    public static int CalculateTotalSkillCost(
        Dictionary<string, int> skills,
        Dictionary<string, int> attributes)
    {
        var totalCost = 0;

        foreach (var skill in skills)
        {
            var linkedAttr = GetLinkedAttribute(skill.Key);
            var attrValue = attributes.TryGetValue(linkedAttr, out var val) ? val : 3; // Default to 3 if not found
            var hasSpecialization = HasSpecialization(skill.Key);
            var cost = hasSpecialization 
                ? CalculateSkillWithSpecialization(skill.Value, attrValue)
                : CalculateSkillCost(skill.Value, attrValue);
            totalCost += cost;
        }

        return totalCost;
    }

    /// <summary>
    /// Validate skill allocation within budget
    /// </summary>
    public static (bool IsValid, int TotalCost, List<string> Errors) ValidateSkillAllocation(
        Dictionary<string, int> skills,
        Dictionary<string, int> attributes,
        int availablePoints)
    {
        var errors = new List<string>();
        var totalCost = CalculateTotalSkillCost(skills, attributes);
        
        if (totalCost > availablePoints)
        {
            errors.Add($"Skill points exceeded: allocated {totalCost}, available {availablePoints}");
        }
        
        foreach (var skill in skills)
        {
            if (skill.Value < 1 || skill.Value > 6)
            {
                errors.Add($"Skill '{skill.Key}' rating {skill.Value} is invalid (must be 1-6)");
            }
            
            var linkedAttr = GetLinkedAttribute(skill.Key);
            var attrValue = attributes.TryGetValue(linkedAttr, out var val) ? val : 3;
            
            if (skill.Value > attrValue + 2)
            {
                errors.Add($"Skill '{skill.Key}' rating {skill.Value} exceeds linked attribute {linkedAttr} ({attrValue}) by more than 2");
            }
        }
        
        return (errors.Count == 0, totalCost, errors);
    }
}
