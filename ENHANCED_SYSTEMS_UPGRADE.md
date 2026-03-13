# Shadowrun Discord Bot - Enhanced Systems Implementation

## Overview

This document details the enhancements made to the .NET Shadowrun Discord bot to bring it closer to feature parity with the original Node.js version while maintaining clean architecture and .NET best practices.

## Date
March 11, 2026

## Changes Summary

### 1. Priority System Implementation ✅

**New File:** `src/ShadowrunDiscordBot.Domain/Entities/PrioritySystem.cs`

**Features Added:**
- Full Shadowrun 3rd Edition priority table (A-E levels)
- Priority allocation tracking for character creation
- Racial maximums and base values for all metatypes
- Starting karma by race
- Priority validation against racial restrictions

**Priority Table Structure:**
```
Priority A: Full Magician (30 attribute pts, 50 skill pts, 1,000,000¥)
Priority B: Adept/Aspected Magician (27 attribute pts, 40 skill pts, 400,000¥)
Priority C: Elf/Troll (24 attribute pts, 34 skill pts, 90,000¥)
Priority D: Dwarf/Ork (21 attribute pts, 30 skill pts, 20,000¥)
Priority E: Human (18 attribute pts, 27 skill pts, 5,000¥)
```

**Database Table:** `PriorityAllocations`
- Tracks which priority level was assigned to which category
- Stores points allocated and maximum available
- JSON metadata field for flexibility

### 2. Enhanced Spell Tracking ✅

**New File:** `src/ShadowrunDiscordBot.Domain/Entities/ShadowrunSpell.cs`

**Improvements Over Legacy:**
- Detailed spell properties (category, subcategory, target type, range, duration)
- Drain calculation system with base and modifier values
- Force-based drain formulas
- Tradition restrictions for exclusive spells
- Source book references
- LOS maintenance tracking
- Minimum force requirements

**Spell Properties Added:**
- TargetType (Physical/Mana)
- Range (Touch/LOS/LOS(A)/Self)
- Duration (Instant/Sustained/Permanent)
- DamageType (Physical/Stun)
- DrainBase, DrainModifier, DrainFormula
- RequiredTradition for exclusive spells
- CausesDrain, RequiresLOSMaintenance
- MinForce, LearnedAtForce

**Helper Methods:**
- `CalculateDrain(int force)` - Calculate drain for a given force
- `GetDrainTargetNumber(int force)` - Get resistance target number

**Database Table:** `ShadowrunSpells`
- Indexed by CharacterId, Category, TargetType
- Unique constraint on CharacterId + Name

### 3. Enhanced Spirit System ✅

**New File:** `src/ShadowrunDiscordBot.Domain/Entities/ShadowrunSpirit.cs`

**Improvements Over Legacy:**
- Force-based attribute calculations (Body, Quickness, etc. = Force)
- Spirit armor and initiative tracking
- Materialization state tracking
- Damage and condition monitor
- Spirit powers and weaknesses
- Task tracking
- Expiration tracking (Force in hours)

**Spirit Properties Added:**
- CurrentTask
- Disposition (Friendly/Neutral/Hostile/Wild)
- IsMaterialized, MaterializedAt
- Damage, ConditionMonitor
- Powers, Weaknesses
- Armor, Initiative (calculated from Force)

**Helper Methods:**
- `UseService()` - Decrement services owed
- `IsActive()` - Check if spirit is still active
- `IsDisrupted()` - Check if damage exceeds condition monitor
- `TimeRemaining()` - Calculate time before expiration

**Spirit Types by Tradition:**
- Hermetic: Fire/Water/Air/Earth Elementals
- Shamanic: City/Country/Forest/Mountain/Prairie/Sea/Sky/Swamp Spirits, Totem Spirits
- Voudoun: Loa

**Database Table:** `ShadowrunSpirits`
- Indexed by CharacterId, Tradition, ServicesOwed, ExpiresAt
- Optimized for active spirit queries

### 4. Enhanced Cyberware System ✅

**New File:** `src/ShadowrunDiscordBot.Domain/Entities/ShadowrunCyberware.cs`

**Improvements Over Legacy:**
- Cyberware grade system (Standard, Alpha, Beta, Delta)
- Grade-adjusted essence and nuyen cost calculations
- Location tracking (Left Arm, Head, Torso, etc.)
- Maintenance tracking
- Cultured bioware flag
- Availability and legality codes
- Street index for black market costs

**Cyberware Properties Added:**
- Subcategory (Headware, Bodyware, Cyberlimb, etc.)
- Grade (Standard/Alpha/Beta/Delta)
- BaseEssenceCost (before grade modifiers)
- Location, InstalledAt
- Bonuses, Drawbacks
- RequiresMaintenance, LastMaintenance
- IsCultured
- Availability, Legality, StreetIndex

**Grade Multipliers:**
- Standard: 1.0x essence, 1x cost
- Alpha: 0.8x essence, 2x cost
- Beta: 0.6x essence, 4x cost
- Delta: 0.5x essence, 10x cost

**Helper Methods:**
- `CalculateGradeAdjustedEssenceCost()` - Apply grade modifier
- `CalculateGradeAdjustedNuyenCost()` - Apply grade multiplier

**Database Table:** `ShadowrunCyberware`
- Indexed by CharacterId, Category, Grade, Location
- Optimized for essence cost queries

### 5. Character Origin & Background System ✅

**New File:** `src/ShadowrunDiscordBot.Domain/Entities/CharacterOrigin.cs`

**Features Added:**
- Comprehensive character background tracking
- Physical description (height, weight, appearance, distinguishing features)
- Personality and backstory
- Family, education, former occupation
- Motivations (reason for running, goals, fears)
- Lifestyle and SIN status
- Contacts and enemies
- Moral code, religion, voice description

**Origin Properties:**
- RealName, StreetName, Age, Gender, Ethnicity
- HeightCm, WeightKg, Appearance, DistinguishingFeatures
- Personality, Backstory, Family, Education
- FormerOccupation, ReasonForRunning, Goals, Fears, Hobbies
- Birthplace, Residence, SinStatus
- Lifestyle, LifestyleCost
- KnownContacts, Enemies, Affiliations
- CriminalRecord, MoralCode, Religion
- VoiceDescription, Quote

**Database Table:** `CharacterOrigins`
- One-to-one relationship with Characters
- Indexed by CharacterId (unique), Lifestyle, SinStatus

### 6. Character Contacts System ✅

**New File:** `src/ShadowrunDiscordBot.Domain/Entities/CharacterOrigin.cs`

**Features Added:**
- Contact name and type
- Connection rating (1-6)
- Loyalty rating (1-6)
- Backstory and services provided
- Location tracking
- Active/inactive status

**Contact Properties:**
- ContactName, ContactType
- ConnectionRating, LoyaltyRating
- Backstory, Services, Location, Notes
- IsActive

**Database Table:** `CharacterContacts`
- Indexed by CharacterId, ContactType, ConnectionRating, LoyaltyRating
- Optimized for high-connection contact queries

### 7. Database Schema Enhancements ✅

**Updated File:** `src/ShadowrunDiscordBot.Infrastructure/Data/ShadowrunDbContext.cs`

**New Tables:**
1. `PriorityAllocations` - Priority system tracking
2. `ShadowrunSpells` - Enhanced spell tracking
3. `ShadowrunSpirits` - Enhanced spirit tracking
4. `ShadowrunCyberware` - Enhanced cyberware tracking
5. `CharacterOrigins` - Character background details
6. `CharacterContacts` - Contact management

**New Indexes (High-Frequency Queries):**

**Characters Table:**
- `IX_Characters_Metatype` - Filter by metatype
- `IX_Characters_Archetype` - Filter by archetype
- `IX_Characters_Magic_Archetype` - Find awakened characters

**PriorityAllocations Table:**
- `IX_PriorityAllocations_CharacterId_Category` (unique) - Get character's priority allocations
- `IX_PriorityAllocations_Priority` - Filter by priority level

**ShadowrunSpells Table:**
- `IX_ShadowrunSpells_CharacterId_Name` (unique) - Get specific spell
- `IX_ShadowrunSpells_Category` - Filter by spell category
- `IX_ShadowrunSpells_Category_TargetType` - Combat spells by type

**ShadowrunSpirits Table:**
- `IX_ShadowrunSpirits_CharacterId_SpiritType` - Get spirits by type
- `IX_ShadowrunSpirits_Tradition` - Filter by tradition
- `IX_ShadowrunSpirits_IsBound_ServicesOwed` - Find bound spirits with services
- `IX_ShadowrunSpirits_Active` - Get active spirits (CharacterId + ServicesOwed + ExpiresAt)

**ShadowrunCyberware Table:**
- `IX_ShadowrunCyberware_CharacterId_Name` - Get specific cyberware
- `IX_ShadowrunCyberware_Category` - Filter by cyberware/bioware
- `IX_ShadowrunCyberware_Category_Grade` - Find cyberware by grade
- `IX_ShadowrunCyberware_Location` - Get cyberware by body location

**CharacterContacts Table:**
- `IX_CharacterContacts_CharacterId_ContactName` - Get specific contact
- `IX_CharacterContacts_ContactType` - Filter by contact type
- `IX_CharacterContacts_Ratings` - High-connection contacts (CharacterId + ConnectionRating + LoyaltyRating)
- `IX_CharacterContacts_Active` - Get active contacts

**Skills Table:**
- `IX_Skills_CharacterId_SkillName` (unique) - Get specific skill
- `IX_Skills_SkillName` - Filter by skill name
- `IX_Skills_IsKnowledgeSkill` - Separate active vs knowledge skills

**Gear Table:**
- `IX_Gear_CharacterId_Name` - Get specific gear
- `IX_Gear_Category` - Filter by category
- `IX_Gear_Equipped` - Get equipped items

**CombatSessions Table:**
- `IX_CombatSessions_IsActive` - Find active combats
- `IX_CombatSessions_StartedAt` - Order by start time

### 8. Migration File ✅

**New File:** `src/ShadowrunDiscordBot.Infrastructure/Data/Migrations/EnhancedSystemsMigration.cs`

**Migration Name:** `EnhancedCharacterSystems`

**What It Does:**
- Creates 6 new tables with proper foreign keys
- Adds 25+ strategic indexes for query optimization
- Maintains backward compatibility with legacy tables
- Documents all changes inline

**Rollback Support:**
- Includes `Down()` method to remove all new tables
- Safe rollback without affecting existing data

## Benefits

### Feature Parity
- ✅ Full priority system from Node.js version
- ✅ Detailed spell tracking with categories and drain
- ✅ Enhanced spirit system with force-based attributes
- ✅ Cyberware grade system
- ✅ Character background and contacts

### Performance
- ✅ 25+ strategic indexes for common queries
- ✅ Composite indexes for complex filters
- ✅ Optimized for awakened character queries
- ✅ Fast active spirit/contact lookups

### Maintainability
- ✅ Clean separation of concerns (Domain entities)
- ✅ Proper EF Core patterns
- ✅ Validation attributes on all properties
- ✅ Comprehensive documentation
- ✅ Backward compatible with existing code

### Data Integrity
- ✅ Foreign key relationships
- ✅ Cascade delete on character deletion
- ✅ Unique constraints prevent duplicates
- ✅ Default values for optional fields

## Backward Compatibility

The migration maintains full backward compatibility:
- Legacy tables (CharacterCyberware, CharacterSpell, CharacterSpirit) remain
- Existing Character entity unchanged
- No breaking changes to existing functionality
- New entities are additive, not replacements

## Usage Examples

### Creating a Character with Priority System

```csharp
// Create character
var character = Character.Create(
    name: "Ghost",
    discordUserId: 123456789,
    metatype: "Elf",
    archetype: "Mage",
    body: 3, quickness: 4, strength: 2,
    charisma: 5, intelligence: 5, willpower: 4
);

// Set priority allocations
var priorityA = new PriorityAllocation
{
    CharacterId = character.Id,
    Priority = "A",
    Category = "Magic",
    PointsAllocated = 1,
    MaxPoints = PriorityTable.Table["A"].AttributePoints
};

var priorityB = new PriorityAllocation
{
    CharacterId = character.Id,
    Priority = "B",
    Category = "Attributes",
    PointsAllocated = 27,
    MaxPoints = PriorityTable.Table["B"].AttributePoints
};
```

### Learning an Enhanced Spell

```csharp
var spell = new ShadowrunSpell
{
    CharacterId = character.Id,
    Name = "Manabolt",
    Category = "Combat",
    Subcategory = "Direct",
    TargetType = "Mana",
    Range = "LOS",
    Duration = "Instant",
    DrainBase = 0,
    DrainModifier = 3,
    DrainFormula = "(F/2) + 3",
    DamageType = "Physical",
    Description = "Direct damage spell targeting living beings",
    Source = "SR3 p.195"
};

var drainTarget = spell.GetDrainTargetNumber(force: 6); // Returns 6
```

### Summoning a Spirit

```csharp
var spirit = new ShadowrunSpirit
{
    CharacterId = character.Id,
    SpiritType = SpiritTypes.FireElemental,
    Force = 4,
    Tradition = "Hermetic",
    ServicesOwed = 2,
    SummonedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddHours(4), // Force in hours
    Disposition = "Neutral"
};

// Force-based attributes
var body = spirit.Body; // 4
var armor = spirit.Armor; // 8 (Force * 2)
var initiative = spirit.Initiative; // "8 + 2D6"
```

### Installing Cyberware

```csharp
var cyberware = new ShadowrunCyberware
{
    CharacterId = character.Id,
    Name = "Wired Reflexes",
    Category = "Cyberware",
    Subcategory = "Bodyware",
    Rating = 2,
    BaseEssenceCost = 2.0m,
    Grade = "Beta",
    Location = "Torso",
    NuyenCost = 100000
};

// Grade-adjusted costs
var adjustedEssence = cyberware.CalculateGradeAdjustedEssenceCost(); // 1.2 (2.0 * 0.6)
var adjustedNuyen = cyberware.CalculateGradeAdjustedNuyenCost(); // 400000 (100000 * 4)
```

### Adding Character Background

```csharp
var origin = new CharacterOrigin
{
    CharacterId = character.Id,
    RealName = "Unknown",
    StreetName = "Ghost",
    Age = 28,
    Gender = "Male",
    Metatype = "Elf",
    HeightCm = 185,
    WeightKg = 75,
    Appearance = "Tall, slender elf with silver hair and cybernetic eyes",
    Personality = "Professional, cold, efficient",
    Backstory = "Former corporate security specialist who was betrayed by his employer...",
    Lifestyle = Lifestyles.Middle,
    LifestyleCost = Lifestyles.MonthlyCosts[Lifestyles.Middle],
    SinStatus = SinStatuses.CriminalSIN
};
```

### Adding a Contact

```csharp
var contact = new CharacterContact
{
    CharacterId = character.Id,
    ContactName = "Mr. Johnson",
    ContactType = "Fixer",
    ConnectionRating = 4,
    LoyaltyRating = 2,
    Services = "Finds shadowrun work, provides intel",
    Location = "Downtown Seattle",
    Backstory = "Saved his life during a run gone bad"
};
```

## Next Steps

### Recommended Future Enhancements

1. **Vehicle Tracking** - Add detailed vehicle system with modifications
2. **Edge/Flaw System** - Track positive/negative qualities
3. **Reputation System** - Street cred and notoriety tracking
4. **Adept Powers** - Detailed power tracking for physical adepts
5. **Focus Management** - Magical foci and bonding costs
6. **Drone/Rigger System** - Drone control and vehicle rigging

### Performance Monitoring

Monitor these queries for optimization:
- Active spirits by character
- Spells by category
- High-connection contacts
- Cyberware by location
- Priority allocations by character

## Files Created

1. `src/ShadowrunDiscordBot.Domain/Entities/PrioritySystem.cs` (5.4 KB)
2. `src/ShadowrunDiscordBot.Domain/Entities/ShadowrunSpell.cs` (5.3 KB)
3. `src/ShadowrunDiscordBot.Domain/Entities/ShadowrunSpirit.cs` (6.5 KB)
4. `src/ShadowrunDiscordBot.Domain/Entities/ShadowrunCyberware.cs` (5.4 KB)
5. `src/ShadowrunDiscordBot.Domain/Entities/CharacterOrigin.cs` (7.1 KB)
6. `src/ShadowrunDiscordBot.Infrastructure/Data/ShadowrunDbContextEnhanced.cs` (17.5 KB)
7. `src/ShadowrunDiscordBot.Infrastructure/Data/Migrations/EnhancedSystemsMigration.cs` (23.9 KB)
8. `ENHANCED_SYSTEMS_IMPLEMENTATION.md` (this file)

**Total:** ~71 KB of new code

## Conclusion

These enhancements bring the .NET Shadowrun Discord bot to feature parity with the Node.js version while:
- Maintaining clean architecture principles
- Using proper EF Core patterns
- Optimizing for performance with strategic indexes
- Ensuring backward compatibility
- Providing comprehensive documentation

The bot now supports the full Shadowrun 3rd Edition character creation system with priority-based allocation, detailed spell tracking, enhanced spirit management, cyberware grades, and comprehensive character backgrounds.
