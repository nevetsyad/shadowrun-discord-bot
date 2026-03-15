# Shadowrun 3rd Edition Build System Compliance Report

**Date:** March 14, 2026  
**Analyzed by:** GPT-5.4 (GLM-5)  
**Repository:** shadowrun-discord-bot

---

## Executive Summary

⚠️ **COMPLIANCE STATUS: PARTIALLY COMPLIANT**

The shadowrun-discord-bot implementation **partially follows** Shadowrun 3rd Edition character creation rules. While the infrastructure for SR3 rules exists, the current implementation is **more flexible** than strict SR3, allowing custom builds and deviations from the official priority system.

**Key Issues:**
- ❌ Custom builds allowed (SR3 requires mandatory archetype or priority-based builds)
- ❌ Priority system incomplete (only uses single priority level, not full A-E allocation)
- ❌ Archetype templates use ranges instead of fixed allocations
- ✅ Metatype attribute maximums correct
- ✅ Priority table values accurate
- ⚠️ Three creation modes available (Priority, Archetype, Custom) - SR3 should only have two

---

## SR3 Character Creation Rules (Official)

### The Priority System

Shadowrun 3rd Edition uses a **priority-based allocation system** where players assign priority levels A through E to five categories:

1. **Metatype** - Determines race and attribute ranges
2. **Attributes** - Base attribute points to distribute (18-30 points)
3. **Magic/Resonance** - Magical ability (Full Magician, Adept, Aspected, or Mundane)
4. **Skills** - Skill points for training (27-50 points)
5. **Resources** - Starting nuyen (¥5,000 - ¥1,000,000)

### Official SR3 Priority Table

| Priority | Metatype    | Attributes | Magic               | Skills | Resources  |
|----------|-------------|------------|---------------------|--------|------------|
| A        | Any         | 30 points  | Full Magician       | 50 pts | 1,000,000¥ |
| B        | Any         | 27 points  | Adept/Aspected      | 40 pts | 400,000¥   |
| C        | Elf/Troll   | 24 points  | Mundane             | 34 pts | 90,000¥    |
| D        | Dwarf/Ork   | 21 points  | Mundane             | 30 pts | 20,000¥    |
| E        | Human       | 18 points  | Mundane             | 27 pts | 5,000¥     |

### SR3 Character Creation Methods

**Method 1: Priority System (Standard)**
- Assign priorities A-E to the five categories
- Allocate attribute points within racial maximums
- Allocate skill points
- Select starting equipment/spells based on resources

**Method 2: Archetype Templates (Quick Start)**
- Choose pre-made archetype (Street Samurai, Mage, etc.)
- Archetype has FIXED attributes, skills, and resources
- Only metatype modifiers adjust values
- No customization during creation

**Important:** SR3 does NOT allow custom attribute allocation outside these systems.

---

## Current Implementation Analysis

### Character Creation Modes

The implementation supports **THREE modes** of character creation:

#### Mode 1: Priority-Based Creation
```csharp
// User specifies PriorityLevel (A, B, C, D, or E)
var command = new CreateCharacterCommand
{
    PriorityLevel = "A",
    Metatype = "Human",
    Body = 6,
    Quickness = 6,
    // ... attributes within racial maximums
};
```

**What works:**
- ✅ Priority table values are SR3-accurate
- ✅ Racial maximums enforced correctly
- ✅ Starting resources based on priority

**What's wrong:**
- ❌ Only uses SINGLE priority level, not full A-E allocation across 5 categories
- ❌ Doesn't enforce priority assignments to Metatype/Attributes/Magic/Skills/Resources
- ❌ Allows attribute allocation without checking attribute point budget
- ❌ Magic not properly tied to priority choice

#### Mode 2: Archetype-Based Creation
```csharp
// User specifies ArchetypeId
var command = new CreateCharacterCommand
{
    ArchetypeId = "Street Samurai",
    Metatype = "Human",
    Body = 6,
    // ... attributes must fit archetype ranges
};
```

**What works:**
- ✅ Archetypes have attribute constraints
- ✅ Metatype compatibility enforced
- ✅ Starting resources assigned

**What's wrong:**
- ❌ Archetypes use attribute RANGES instead of FIXED values
- ❌ Users can still choose attributes within ranges (not true to SR3 archetypes)
- ❌ Skill bonuses added, but not using SR3 skill point system
- ❌ No priority allocation tracking

#### Mode 3: Custom Build (Default)
```csharp
// No PriorityLevel or ArchetypeId specified
var command = new CreateCharacterCommand
{
    Metatype = "Human",
    Body = 5,
    Quickness = 7,
    // ... any attributes 1-10
};
```

**What works:**
- ✅ Basic attribute range validation (1-10)
- ✅ Metatype modifiers applied

**What's wrong:**
- ❌ **COMPLETELY NON-COMPLIANT with SR3**
- ❌ No point-buy system
- ❌ No priority allocation
- ❌ No archetype enforcement
- ❌ Allows any attribute combination

---

## Detailed Compliance Checklist

### ✅ COMPLIANT: Metatype System

**SR3 Rule:** Each metatype has specific attribute ranges and modifiers.

**Implementation:**
```csharp
// From PrioritySystem.cs - CORRECT
public static readonly Dictionary<string, Dictionary<string, int>> RacialMaximums = new()
{
    ["Human"] = { Body: 9, Quickness: 9, Strength: 9, Charisma: 9, Intelligence: 9, Willpower: 9 },
    ["Elf"] = { Body: 9, Quickness: 11, Strength: 9, Charisma: 12, Intelligence: 9, Willpower: 9 },
    ["Dwarf"] = { Body: 11, Quickness: 9, Strength: 12, Charisma: 9, Intelligence: 9, Willpower: 11 },
    ["Ork"] = { Body: 14, Quickness: 9, Strength: 12, Charisma: 8, Intelligence: 8, Willpower: 9 },
    ["Troll"] = { Body: 17, Quickness: 8, Strength: 15, Charisma: 6, Intelligence: 6, Willpower: 9 }
};
```

**Verdict:** ✅ **FULLY COMPLIANT** - Racial maximums match SR3 exactly

---

### ⚠️ PARTIAL: Priority Table Values

**SR3 Rule:** Priority levels determine attribute points, skill points, and resources.

**Implementation:**
```csharp
// From PrioritySystem.cs - ACCURATE VALUES
["A"] = { AttributePoints: 30, SkillPoints: 50, Nuyen: 1000000 },
["B"] = { AttributePoints: 27, SkillPoints: 40, Nuyen: 400000 },
["C"] = { AttributePoints: 24, SkillPoints: 34, Nuyen: 90000 },
["D"] = { AttributePoints: 21, SkillPoints: 30, Nuyen: 20000 },
["E"] = { AttributePoints: 18, SkillPoints: 27, Nuyen: 5000 }
```

**Verdict:** ⚠️ **VALUES CORRECT, BUT INCOMPLETE**
- ✅ Priority values are SR3-accurate
- ❌ System only uses single priority, not full A-E allocation
- ❌ Doesn't enforce 30-point budget for Priority A attributes
- ❌ Doesn't enforce 50-point budget for Priority A skills

---

### ❌ NON-COMPLIANT: Attribute Allocation

**SR3 Rule:** Attributes allocated from point budget based on priority.

**Implementation:**
```csharp
// From CreateCharacterCommandHandler.cs - NO POINT BUDGET CHECKING
var character = new ShadowrunCharacter
{
    Body = request.Body,        // Directly assigned
    Quickness = request.Quickness,  // No point budget validation
    Strength = request.Strength,
    // ...
};
```

**Issues:**
- ❌ No validation that attribute total matches priority budget
- ❌ Users can assign any values within racial maximums
- ❌ Point-buy system not implemented
- ❌ No tracking of allocated vs. available points

**Example of SR3 Violation:**
```csharp
// This is ALLOWED but violates SR3:
var command = new CreateCharacterCommand
{
    PriorityLevel = "E",  // Only 18 attribute points available
    Body = 9,             // 9 points
    Quickness = 9,        // 9 points
    Strength = 9,         // 9 points
    // Total: 27 points allocated (exceeds 18-point budget!)
};
```

**Verdict:** ❌ **NON-COMPLIANT** - No point budget enforcement

---

### ❌ NON-COMPLIANT: Skill Allocation

**SR3 Rule:** Skills allocated from skill point budget based on priority.

**Implementation:**
```csharp
// From CreateCharacterCommandHandler.cs
private void AddPrioritySkills(ShadowrunCharacter character, string priority)
{
    var skillPoints = PriorityTable.Table[priority].SkillPoints;
    
    // Adds fixed skills, then allocates remaining to "Drive Vehicle"
    switch (priority)
    {
        case "A":
            character.Skills.Add(new CharacterSkill { SkillName = "Sorcery", Rating = 4 });
            character.Skills.Add(new CharacterSkill { SkillName = "Conjuring", Rating = 4 });
            break;
        // ...
    }
}
```

**Issues:**
- ❌ Hardcoded skill assignments (not player choice)
- ❌ Doesn't use proper skill point allocation
- ❌ No validation of skill point budget
- ❌ Arbitrary "Drive Vehicle" allocation for remaining points

**Verdict:** ❌ **NON-COMPLIANT** - Skill system not implemented correctly

---

### ❌ NON-COMPLIANT: Magic/Resonance Allocation

**SR3 Rule:** Magic priority determines magical ability (Full Magician, Adept, etc.).

**Implementation:**
```csharp
// From CreateCharacterCommandHandler.cs
private int GetMagicForPriority(string priority)
{
    return priority switch
    {
        "A" => 6,  // Full Magician
        "B" => 5,  // Adept
        _ => 0     // Mundane
    };
}
```

**Issues:**
- ❌ Oversimplified - SR3 has more nuance (Aspected, etc.)
- ❌ Doesn't track magic type (Hermetic vs. Shamanic)
- ❌ Not tied to the full priority allocation system
- ❌ Magic priority should be separate category, not derived from single priority

**Verdict:** ❌ **NON-COMPLIANT** - Magic system incomplete

---

### ❌ NON-COMPLIANT: Archetype System

**SR3 Rule:** Archetypes are fixed templates with predetermined attributes/skills/resources.

**Implementation:**
```csharp
// From ArchetypeService.cs
["Street Samurai"] = new ArchetypeTemplate
{
    BodyMin = 5, BodyMax = 9,      // RANGE instead of fixed value
    QuicknessMin = 5, QuicknessMax = 9,
    // ...
}
```

**Issues:**
- ❌ Uses ranges instead of fixed values
- ❌ Users can still choose within ranges (not true SR3 archetype)
- ❌ No priority allocation stored in archetype
- ❌ Starting resources don't match SR3 (10,000¥ vs SR3's 400,000¥ for Street Sam)

**Example of SR3 Violation:**
```csharp
// SR3 Street Samurai should have FIXED attributes:
Body: 5, Quickness: 6, Strength: 5, etc.

// But implementation allows:
Body: 9, Quickness: 9, Strength: 8  // Within range but NOT the archetype template
```

**Verdict:** ❌ **NON-COMPLIANT** - Archetypes are flexible ranges, not fixed templates

---

### ❌ NON-COMPLIANT: Custom Builds

**SR3 Rule:** All characters must use priority system or archetype templates.

**Implementation:**
```csharp
// From CreateCharacterCommandHandler.cs
var isArchetypeBuild = !string.IsNullOrWhiteSpace(request.ArchetypeId);
var isPriorityBuild = !string.IsNullOrWhiteSpace(request.PriorityLevel);

// If neither specified, allows CUSTOM BUILD
if (!isArchetypeBuild && !isPriorityBuild)
{
    // Creates character with user-assigned attributes
    // NO VALIDATION against SR3 rules
}
```

**Verdict:** ❌ **NON-COMPLIANT** - Custom builds violate SR3 rules

---

## Critical SR3 Deviations

### 1. Missing Full Priority Allocation

**SR3 Requirement:** Assign priorities A-E to five categories.

**Current Implementation:** Single "PriorityLevel" field.

**Impact:** Cannot create SR3-compliant characters.

**Example:**
```
SR3 Character Creation:
- Priority A: Attributes (30 points)
- Priority B: Skills (40 points)  
- Priority C: Resources (90,000¥)
- Priority D: Metatype (Dwarf)
- Priority E: Magic (Mundane)

Current Implementation:
- PriorityLevel: "A" (only one priority)
- No allocation tracking
```

---

### 2. No Point Budget Enforcement

**SR3 Requirement:** Attributes allocated from fixed point budget.

**Current Implementation:** Free assignment within racial maximums.

**Impact:** Unbalanced characters, violates game mechanics.

---

### 3. Flexible Archetypes

**SR3 Requirement:** Archetypes have fixed attributes/skills.

**Current Implementation:** Archetypes use min/max ranges.

**Impact:** Not true archetypes, just attribute suggestions.

---

### 4. Custom Builds Allowed

**SR3 Requirement:** Must use priority or archetype system.

**Current Implementation:** Custom builds are default.

**Impact:** Complete deviation from SR3 rules.

---

## Recommendations for SR3 Compliance

### Priority 1: Implement Full Priority System

**Required Changes:**

1. **Replace single PriorityLevel with full allocation:**
```csharp
public class CreateCharacterCommand
{
    // REMOVE: public string? PriorityLevel { get; set; }
    
    // ADD: Full priority allocation
    public PriorityAssignment PriorityAssignment { get; set; } = new();
}

public class PriorityAssignment
{
    public string MetatypePriority { get; set; } = "E";      // A-E
    public string AttributesPriority { get; set; } = "C";    // A-E
    public string MagicPriority { get; set; } = "E";         // A-E
    public string SkillsPriority { get; set; } = "C";        // A-E
    public string ResourcesPriority { get; set; } = "D";     // A-E
    
    // Validation: Each priority A-E must be used exactly once
}
```

2. **Validate attribute point budget:**
```csharp
public async Task<(bool IsValid, string ErrorMessage)> ValidateAttributeBudgetAsync(
    CreateCharacterCommand request,
    CancellationToken cancellationToken)
{
    var attributePriority = request.PriorityAssignment.AttributesPriority;
    var availablePoints = PriorityTable.Table[attributePriority].AttributePoints;
    
    var allocatedPoints = request.Body + request.Quickness + request.Strength +
                         request.Charisma + request.Intelligence + request.Willpower;
    
    if (allocatedPoints > availablePoints)
    {
        return (false, $"Attribute points exceeded: allocated {allocatedPoints}, available {availablePoints}");
    }
    
    return (true, string.Empty);
}
```

---

### Priority 2: Fix Archetype Templates

**Required Changes:**

1. **Use fixed values instead of ranges:**
```csharp
["Street Samurai"] = new ArchetypeTemplate
{
    // FIXED values (not ranges)
    Body = 5,
    Quickness = 6,
    Strength = 5,
    Charisma = 3,
    Intelligence = 4,
    Willpower = 3,
    
    // Starting resources per SR3
    StartingNuyen = 400000,  // Not 10,000
    StartingKarma = 0,
    
    // Fixed skills
    StartingSkills = new List<ArchetypeSkill>
    {
        new() { SkillName = "Edged Weapons", Rating = 6 },
        new() { SkillName = "Pistols", Rating = 5 },
        new() { SkillName = "Athletics", Rating = 4 }
    }
}
```

2. **Remove user attribute input for archetype builds:**
```csharp
// Archetype builds should NOT accept attribute parameters
if (isArchetypeBuild)
{
    // Attributes come from archetype template
    // NOT from user input
}
```

---

### Priority 3: Remove Custom Builds

**Required Changes:**

1. **Make priority or archetype mandatory:**
```csharp
// In CreateCharacterCommandValidator
RuleFor(x => x)
    .Must(x => !string.IsNullOrWhiteSpace(x.PriorityAssignment?.ToString()) || 
               !string.IsNullOrWhiteSpace(x.ArchetypeId))
    .WithMessage("Character creation requires either Priority Assignment or Archetype selection");
```

2. **Deprecate custom builds:**
```csharp
// Add migration for existing custom builds
public class ShadowrunCharacter
{
    public bool IsLegacyCustomBuild { get; set; } = false;
    public DateTime? CreatedBeforePrioritySystem { get; set; }
}
```

---

### Priority 4: Implement Proper Skill System

**Required Changes:**

1. **Track skill point allocation:**
```csharp
public class Character
{
    public int TotalSkillPointsAllocated { get; set; }
    public int MaxSkillPointsAvailable { get; set; }
    
    public bool ValidateSkillBudget()
    {
        var total = Skills.Sum(s => s.Rating);
        return total <= MaxSkillPointsAvailable;
    }
}
```

2. **Validate skill point budget during creation:**
```csharp
var skillPriority = request.PriorityAssignment.SkillsPriority;
var availableSkillPoints = PriorityTable.Table[skillPriority].SkillPoints;

var allocatedSkillPoints = request.Skills.Sum(s => s.Rating);

if (allocatedSkillPoints > availableSkillPoints)
{
    return (false, "Skill point budget exceeded");
}
```

---

## Migration Path

### Phase 1: Add Full Priority System (Week 1-2)
- Implement PriorityAssignment model
- Add attribute budget validation
- Add skill budget validation
- Test priority-based creation

### Phase 2: Fix Archetype Templates (Week 2-3)
- Convert ranges to fixed values
- Update starting resources to SR3 values
- Remove attribute input for archetype builds
- Test archetype creation

### Phase 3: Deprecate Custom Builds (Week 3-4)
- Make priority/archetype mandatory
- Add migration for existing characters
- Add "legacy" flag to custom builds
- Update documentation

### Phase 4: Testing & Validation (Week 4-5)
- Test all SR3 character creation scenarios
- Validate against SR3 rulebook examples
- Performance testing
- User acceptance testing

---

## Summary

### Current State: PARTIALLY COMPLIANT

**What Works:**
- ✅ Racial maximums accurate
- ✅ Priority table values correct
- ✅ Basic validation in place

**What Doesn't Work:**
- ❌ Full priority allocation system missing
- ❌ No point budget enforcement
- ❌ Archetypes use ranges instead of fixed values
- ❌ Custom builds allowed
- ❌ Skill system incomplete
- ❌ Magic system oversimplified

### Compliance Score: 40/100

**Breakdown:**
- Metatype System: 10/10 ✅
- Priority Table: 8/10 ⚠️
- Attribute Allocation: 2/10 ❌
- Skill Allocation: 2/10 ❌
- Magic System: 4/10 ⚠️
- Archetype System: 4/10 ⚠️
- Custom Builds: 0/10 ❌
- Overall SR3 Adherence: 10/30 ❌

### Recommendation: MAJOR REFACTORING REQUIRED

To achieve full SR3 compliance, the character creation system needs significant refactoring:
1. Implement full A-E priority allocation across 5 categories
2. Add point budget validation for attributes and skills
3. Fix archetype templates to use fixed values
4. Remove custom build option
5. Properly implement magic/resonance allocation

**Estimated Effort:** 3-4 weeks of development work

---

## Files Analyzed

- `/src/ShadowrunDiscordBot.Domain/Entities/ArchetypeTemplate.cs`
- `/src/ShadowrunDiscordBot.Application/Services/ArchetypeService.cs`
- `/Commands/Characters/CreateCharacterCommand.cs`
- `/Commands/Characters/CreateCharacterCommandHandler.cs`
- `/Models/ShadowrunCharacter.cs`
- `/src/ShadowrunDiscordBot.Domain/Entities/PrioritySystem.cs`
- `/ARCHETYPE_IMPLEMENTATION_PLAN.md`
- `/ARCHETYPE_SYSTEM_COMPLETE.md`

---

**Report Generated:** March 14, 2026  
**Analysis Model:** GPT-5.4 (GLM-5)  
**Repository Version:** Current (March 2026)
