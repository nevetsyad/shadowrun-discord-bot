# SR3 Racial Modifier System Fix

## Summary of Changes

### Problem
The previous implementation allowed users to input total attributes (including racial bonuses), not base attributes. This was not SR3-compliant.

### Solution
Implemented a proper **Base Attribute + Racial Modifier = Final Attribute** system.

---

## Changes Made

### 1. Updated `PrioritySystem.cs` and `PriorityAllocation.cs`
Added new `RacialModifiers` dictionary with correct SR3 racial modifiers:

| Metatype | Body | Quickness | Strength | Charisma | Intelligence | Willpower |
|----------|------|-----------|----------|----------|--------------|-----------|
| Human    | +0   | +0        | +0       | +0       | +0           | +0        |
| Elf      | +0   | +1        | +0       | +2       | +0           | +0        |
| Dwarf    | +1   | +0        | +2       | +0       | +0           | +1        |
| **Ork**  | **+3** | **+0**  | **+3**   | **-1**   | **+1**       | **+0**    |
| Troll    | +5   | -1        | +5       | -2       | -2           | +0        |

Added helper methods:
- `CalculateFinalAttribute()` - Calculate single final attribute
- `CalculateFinalAttributes()` - Calculate all final attributes
- `ValidateFinalAttributes()` - Validate against racial maximums

### 2. Updated `Character.cs` (Domain Entity)
Added new properties:
- `BaseBody`, `BaseQuickness`, `BaseStrength`, `BaseCharisma`, `BaseIntelligence`, `BaseWillpower`
- `AppliedRacialModifiers` - Dictionary storing which modifiers were applied

Updated factory method:
- Now accepts **base attributes** as input
- Automatically calculates **final attributes** using racial modifiers
- Validates final attributes against racial maximums

### 3. Updated `ShadowrunCharacter.cs` (Model)
Added same properties as Domain entity:
- Base attributes for storage
- Final attributes for gameplay
- Applied racial modifiers for display

### 4. Updated `CharacterDTOs.cs`
Updated DTOs to include:
- Base attributes (`BaseBody`, `BaseQuickness`, etc.)
- Final attributes (`Body`, `Quickness`, etc.)
- Applied racial modifiers for display

### 5. Updated `CreateCharacterCommandHandler.cs`
- Uses base attributes from request
- Calls `PriorityTable.CalculateFinalAttributes()` to get final values
- Logs both base and final attributes
- Validates final attributes

### 6. Updated `CharacterCommands.cs`
- Updated `ApplyRacialModifiers()` to use new system
- Updated `ApplyPriorityAllocations()` to work with base attributes
- Updated character sheet display to show base → final with modifiers

### 7. Updated Character Sheet Display
New display format shows:
```
💪 Body: 4 → 7 (+3)
🏃 Quickness: **6**
🏋️ Strength: 4 → 7 (+3)
```

---

## Viper Character (Created)

### Character Details
- **Name:** Viper
- **Metatype:** Ork
- **Archetype:** Street Samurai
- **Magic:** Mundane (Priority E)

### Priority Allocation
| Priority | Category | Value |
|----------|----------|-------|
| D | Metatype | Ork |
| A | Attributes | 30 points |
| E | Magic | Mundane |
| B | Skills | 40 points |
| C | Resources | 90,000¥ |

### Attributes

#### Base Attributes (User Input)
| Attribute | Value |
|-----------|-------|
| Body | 4 |
| Quickness | 6 |
| Strength | 4 |
| Charisma | 4 |
| Intelligence | 6 |
| Willpower | 6 |

#### Ork Racial Modifiers Applied
| Attribute | Modifier |
|-----------|----------|
| Body | +3 |
| Quickness | +0 |
| Strength | +3 |
| Charisma | -1 |
| Intelligence | +1 |
| Willpower | +0 |

#### Final Attributes (Calculated)
| Attribute | Base | Modifier | Final |
|-----------|------|----------|-------|
| Body | 4 | +3 | **7** |
| Quickness | 6 | +0 | **6** |
| Strength | 4 | +3 | **7** |
| Charisma | 4 | -1 | **3** |
| Intelligence | 6 | +1 | **7** |
| Willpower | 6 | +0 | **6** |

#### Derived Attributes
- **Reaction:** 6 (Quickness + Intelligence / 2)
- **Essence:** 6.0
- **Magic:** 0

### Resources
- **Nuyen:** 90,000¥ (Priority C)
- **Karma:** 0 (Ork starting karma)

---

## Character Sheet Preview

```
📜 Viper
Ork Street Samurai

PHYSICAL ATTRIBUTES
💪 Body: 4 → 7 (+3)
🏃 Quickness: **6**
🏋️ Strength: 4 → 7 (+3)

MENTAL ATTRIBUTES
💬 Charisma: 4 → 3 (-1)
🧠 Intelligence: 6 → 7 (+1)
🎯 Willpower: **6**

DERIVED
⚡ Reaction: 6
✨ Essence: 6.00

CONDITION MONITORS
❤️ Physical: □□□□□□□□ (0/8)
💫 Stun: □□□□□□□ (0/7)

RESOURCES
🌟 Karma: 0
💰 Nuyen: 90,000¥
```

---

## SR3 Compliance Fixes Implemented

1. ✅ **Base Attribute System**: User inputs base attributes, not total
2. ✅ **Automatic Racial Modifiers**: System applies modifiers automatically
3. ✅ **Correct Ork Modifiers**: Body +3, Strength +3, Charisma -1, Intelligence +1
4. ✅ **Racial Maximum Validation**: Final attributes validated against maximums
5. ✅ **Character Sheet Display**: Shows base → final with modifier indicators
6. ✅ **Priority System Integration**: Works with priority-based character creation
7. ✅ **Archetype System Integration**: Works with archetype-based character creation
8. ✅ **Backward Compatibility**: Legacy methods preserved for existing characters

---

## Files Modified

1. `/src/ShadowrunDiscordBot.Domain/Entities/PrioritySystem.cs`
2. `/src/ShadowrunDiscordBot.Domain/Entities/PriorityAllocation.cs`
3. `/src/ShadowrunDiscordBot.Domain/Entities/Character.cs`
4. `/src/ShadowrunDiscordBot.Application/DTOs/CharacterDTOs.cs`
5. `/src/ShadowrunDiscordBot.Application/Features/Characters/Handlers/CreateCharacterCommandHandler.cs`
6. `/Models/ShadowrunCharacter.cs`
7. `/Commands/CharacterCommands.cs`

---

## Testing

### Test Case: Viper (Ork)
```
Input Base Attributes: Body 4, Quickness 6, Strength 4, Charisma 4, Intelligence 6, Willpower 6
Expected Racial Modifiers: Body +3, Strength +3, Charisma -1, Intelligence +1
Expected Final Attributes: Body 7, Quickness 6, Strength 7, Charisma 3, Intelligence 7, Willpower 6
Result: ✅ PASS
```

### Test Case: Human (No Modifiers)
```
Input Base Attributes: Body 5, Quickness 5, Strength 5, Charisma 5, Intelligence 5, Willpower 5
Expected Racial Modifiers: All +0
Expected Final Attributes: Same as base
Result: ✅ PASS
```

### Test Case: Elf (Quickness +1, Charisma +2)
```
Input Base Attributes: Body 3, Quickness 5, Strength 3, Charisma 4, Intelligence 4, Willpower 4
Expected Racial Modifiers: Quickness +1, Charisma +2
Expected Final Attributes: Body 3, Quickness 6, Strength 3, Charisma 6, Intelligence 4, Willpower 4
Result: ✅ PASS
```

---

## Next Steps

1. Add interactive character creation with attribute point allocation
2. Add skill selection for priority-based characters
3. Add gear selection interface
4. Add cyberware/bioware essence tracking
5. Add spell selection for awakened characters
