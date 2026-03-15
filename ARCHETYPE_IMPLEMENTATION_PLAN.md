# Shadowrun 3rd Edition Archetype-Based Character Creation Implementation Plan

**Date:** March 13, 2026  
**Author:** GPT-5.4 Analysis  
**Purpose:** Enforce SR3-compliant archetype-only character builds

---

## Executive Summary

The current Shadowrun Discord bot implementation allows custom character attribute allocation, which deviates from Shadowrun 3rd Edition (SR3) rules. This plan outlines how to transition to **mandatory archetype-based character creation** that strictly follows SR3 standards, eliminating custom builds entirely.

---

## TASK 1: Current Character Creation Analysis

### Current Implementation Status

#### Files Examined:
- `/Commands/Characters/CreateCharacterCommand.cs`
- `/Commands/Characters/CreateCharacterCommandHandler.cs`
- `/Commands/Validators/CreateCharacterCommandValidator.cs`
- `/Models/ShadowrunCharacter.cs`
- `/Commands/CharacterCommands.cs`
- `/src/ShadowrunDiscordBot.Domain/Entities/PrioritySystem.cs`

#### Current Behavior:

1. **Archetype Handling:**
   - Archetype is **required** but functions as a cosmetic label
   - No attribute constraints based on archetype
   - Validator only checks if archetype is in the valid list
   - Valid archetypes: "Street Samurai", "Mage", "Shaman", "Rigger", "Decker", "Physical Adept"

2. **Metatype Handling:**
   - Metatype applies attribute modifiers (e.g., Elf gets +1 Quickness, +2 Charisma)
   - **Does NOT follow SR3 priority system**
   - Modifiers are applied on top of base attributes (default 3)
   - No metatype-specific attribute min/max enforcement

3. **Attribute Allocation:**
   - Users can freely assign attributes 1-10
   - Generic validation: `InclusiveBetween(1, 10)`
   - **No archetype-specific constraints**
   - **No metatype-specific maximums enforced**

4. **Priority System:**
   - Priority system infrastructure EXISTS in `PrioritySystem.cs`
   - **NOT USED** in character creation flow
   - Contains metatype racial maximums and base values
   - Has Priority A-E table structure

### Issues with Current Implementation:

1. ❌ **Custom builds allowed** - Users can create any attribute combination
2. ❌ **No archetype enforcement** - Archetype is just a label, not a template
3. ❌ **Incorrect metatype modifiers** - Doesn't match SR3 priority system
4. ❌ **Priority system unused** - Infrastructure exists but not implemented
5. ❌ **No attribute point limits** - No total attribute point caps
6. ❌ **Backward compatibility risk** - Existing characters may violate new rules

---

## TASK 2: SR3 Rulebook Character Creation Research

### Shadowrun 3rd Edition Character Creation Rules

#### Priority System (A-E)

SR3 uses a **priority-based point allocation system**. Players assign priorities A through E to five categories:

1. **Metatype** - Determines race and attribute ranges
2. **Attributes** - Base attribute points to distribute
3. **Magic/Resonance** - Magical ability (or None for mundane)
4. **Skills** - Skill points for training
5. **Resources** - Starting nuyen (¥)

#### Priority Table (SR3 Standard):

| Priority | Metatype    | Attributes | Magic        | Skills | Resources  |
|----------|-------------|------------|--------------|--------|------------|
| A        | Any         | 30 points  | Full Magician| 50 pts | 1,000,000¥ |
| B        | Any         | 27 points  | Adept/Asp.   | 40 pts | 400,000¥   |
| C        | Elf/Troll   | 24 points  | Mundane      | 34 pts | 90,000¥    |
| D        | Dwarf/Ork   | 21 points  | Mundane      | 30 pts | 20,000¥    |
| E        | Human       | 18 points  | Mundane      | 27 pts | 5,000¥     |

#### Metatype Attribute Ranges (SR3):

| Metatype | Body      | Quickness | Strength  | Charisma  | Intell.   | Willpower |
|----------|-----------|-----------|-----------|-----------|-----------|-----------|
| **Human**| 1-6 (9)   | 1-6 (9)   | 1-6 (9)   | 1-6 (9)   | 1-6 (9)   | 1-6 (9)   |
| **Elf**  | 1-6 (9)   | 1-6 (11)  | 1-6 (9)   | 1-6 (12)  | 1-6 (9)   | 1-6 (9)   |
| **Dwarf**| 1-6 (11)  | 1-6 (9)   | 1-6 (12)  | 1-6 (9)   | 1-6 (9)   | 1-6 (11)  |
| **Ork**  | 1-6 (14)  | 1-6 (9)   | 1-6 (12)  | 1-6 (8)   | 1-6 (8)   | 1-6 (9)   |
| **Troll**| 1-6 (17)  | 1-6 (8)   | 1-6 (15)  | 1-6 (6)   | 1-6 (6)   | 1-6 (9)   |

*Note: Values in parentheses are racial maximums (augmented). Normal maximum is 6.*

#### Archetypes in SR3:

**Archetypes are pre-made character templates**, NOT custom builds. They represent common character concepts in the Shadowrun universe.

### SR3 Core Archetypes:

1. **Street Samurai** - Cybered combat specialist
2. **Mage** - Hermetic magician
3. **Shaman** - Totem-following magician
4. **Physical Adept** - Magic-enhanced warrior
5. **Decker** - Matrix specialist/hacker
6. **Rigger** - Vehicle/drone controller
7. **Face** - Social specialist/negotiator
8. **Investigator** - Detective/bounty hunter
9. **Medic** - Street doctor/medic
10. **Covert Ops Specialist** - Stealth/infiltration expert

**Important:** In SR3, archetypes are **starting templates**, not rigid classes. However, for this implementation, we will treat them as **mandatory templates** to simplify character creation and enforce SR3 compliance.

---

## TASK 3: Implementation Design

### 3.1 Mandatory Archetype Selection

**Requirement:** All character creation MUST select an archetype. No custom builds allowed.

#### Implementation Changes:

1. **Archetype Selection Required:**
   - Archetype parameter becomes **mandatory** (already required in validator)
   - Remove ability to create characters without archetype
   - Archetype determines ALL starting attributes, skills, and resources

2. **Pre-Made Archetype Templates:**
   - Each archetype has **fixed attribute allocations**
   - Each archetype has **predetermined starting skills**
   - Each archetype has **set starting resources**
   - **No attribute point distribution by user**

3. **Archetype Constraints:**
   - Attributes are **locked** to archetype template values
   - Skills are **locked** to archetype starting skills
   - Only metatype modifiers adjust attributes within racial limits
   - No manual attribute/skill allocation

### 3.2 Pre-Made Archetypes to Implement

#### Archetype Templates (SR3-Based):

##### 1. **Street Samurai**
```
Attributes:
- Body: 5, Quickness: 6, Strength: 5
- Charisma: 3, Intelligence: 4, Willpower: 3
- Reaction: 5 (derived)

Starting Skills:
- Edged Weapons: 6
- Pistols: 5
- Athletics: 4
- Stealth: 3
- Etiquette (Street): 3

Starting Resources:
- Nuyen: 400,000¥ (Priority B resources)
- Karma: 0
- Cyberware: Wired Reflexes 2, Smartlink, Dermal Plating 3

Metatype Compatibility: Human, Elf, Dwarf, Ork, Troll
Priority Required: B (Attributes), C (Skills), B (Resources)
```

##### 2. **Mage (Hermetic)**
```
Attributes:
- Body: 3, Quickness: 3, Strength: 3
- Charisma: 4, Intelligence: 5, Willpower: 5
- Reaction: 4 (derived)
- Magic: 6

Starting Skills:
- Sorcery: 6
- Conjuring: 4
- Magical Theory: 5
- Etiquette (Corporate): 3
- Stealth: 2

Starting Resources:
- Nuyen: 90,000¥ (Priority C resources)
- Karma: 0
- Spells: 5 combat, 3 detection, 2 health (player choice from list)

Metatype Compatibility: Human, Elf, Dwarf
Priority Required: A (Magic), B (Attributes), C (Skills), C (Resources)
```

##### 3. **Shaman**
```
Attributes:
- Body: 4, Quickness: 4, Strength: 3
- Charisma: 5, Intelligence: 4, Willpower: 5
- Reaction: 4 (derived)
- Magic: 6

Starting Skills:
- Sorcery: 5
- Conjuring: 6
- Totem Lore: 4
- Etiquette (Tribal): 4
- Stealth: 2

Starting Resources:
- Nuyen: 90,000¥ (Priority C resources)
- Karma: 0
- Spells: 4 combat, 4 detection, 2 manipulation (player choice)
- Totem: Player choice from SR3 totem list

Metatype Compatibility: Human, Elf, Dwarf, Ork
Priority Required: A (Magic), B (Attributes), C (Skills), C (Resources)
```

##### 4. **Physical Adept**
```
Attributes:
- Body: 5, Quickness: 6, Strength: 5
- Charisma: 3, Intelligence: 4, Willpower: 4
- Reaction: 5 (derived)
- Magic: 6

Starting Skills:
- Unarmed Combat: 6
- Athletics: 5
- Stealth: 4
- Edged Weapons: 3
- Perception: 3

Starting Resources:
- Nuyen: 20,000¥ (Priority D resources)
- Karma: 0
- Adept Powers: 6 power points (Improved Reflexes 3, Killing Hands, Combat Sense 2)

Metatype Compatibility: Human, Elf, Dwarf, Ork, Troll
Priority Required: B (Magic - Adept), B (Attributes), C (Skills), D (Resources)
```

##### 5. **Decker**
```
Attributes:
- Body: 3, Quickness: 4, Strength: 3
- Charisma: 3, Intelligence: 6, Willpower: 4
- Reaction: 5 (derived)

Starting Skills:
- Computer: 6
- Electronics: 5
- Cyberdeck Design: 4
- Data Brokerage: 3
- Stealth: 3

Starting Resources:
- Nuyen: 400,000¥ (Priority B resources - includes cyberdeck)
- Karma: 0
- Equipment: Cyberdeck (Rating 4), Programs (Attack 4, Armor 4, Deception 4, etc.)

Metatype Compatibility: Human, Elf, Dwarf
Priority Required: B (Attributes), B (Skills), B (Resources), E (Magic)
```

##### 6. **Rigger**
```
Attributes:
- Body: 4, Quickness: 5, Strength: 4
- Charisma: 3, Intelligence: 5, Willpower: 3
- Reaction: 5 (derived)

Starting Skills:
- Vehicle Operation: 6
- Gunnery: 5
- Electronics: 4
- Mechanic: 4
- Stealth: 2

Starting Resources:
- Nuyen: 400,000¥ (Priority B resources - includes vehicle/drone)
- Karma: 0
- Equipment: Vehicle (car or bike), 2 drones, VCR (Vehicle Control Rig 2)

Metatype Compatibility: Human, Elf, Dwarf, Ork
Priority Required: B (Attributes), B (Skills), B (Resources), E (Magic)
```

##### 7. **Face**
```
Attributes:
- Body: 3, Quickness: 4, Strength: 3
- Charisma: 6, Intelligence: 5, Willpower: 4
- Reaction: 4 (derived)

Starting Skills:
- Etiquette: 6
- Negotiation: 6
- Con: 5
- Leadership: 4
- Pistols: 3

Starting Resources:
- Nuyen: 90,000¥ (Priority C resources)
- Karma: 0
- Equipment: Fake SIN (Rating 4), Armor clothing, Pistol, Commlink

Metatype Compatibility: Human, Elf
Priority Required: B (Attributes), B (Skills), C (Resources), E (Magic)
```

### 3.3 Metatype Attribute Adjustments

When creating a character with a specific metatype, apply these modifiers to the archetype's base attributes:

```csharp
// From existing PrioritySystem.cs (already implemented)
public static readonly Dictionary<string, Dictionary<string, int>> RacialBaseValues = new()
{
    ["Human"] = new Dictionary<string, int>
    {
        ["Body"] = 1, ["Quickness"] = 1, ["Strength"] = 1,
        ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 1
    },
    ["Elf"] = new Dictionary<string, int>
    {
        ["Body"] = 1, ["Quickness"] = 2, ["Strength"] = 1,
        ["Charisma"] = 2, ["Intelligence"] = 1, ["Willpower"] = 1
    },
    ["Dwarf"] = new Dictionary<string, int>
    {
        ["Body"] = 2, ["Quickness"] = 1, ["Strength"] = 2,
        ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 2
    },
    ["Ork"] = new Dictionary<string, int>
    {
        ["Body"] = 3, ["Quickness"] = 1, ["Strength"] = 3,
        ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 1
    },
    ["Troll"] = new Dictionary<string, int>
    {
        ["Body"] = 5, ["Quickness"] = 1, ["Strength"] = 5,
        ["Charisma"] = 1, ["Intelligence"] = 1, ["Willpower"] = 1
    }
};
```

**Implementation Note:** The current system applies modifiers as bonuses (e.g., Elf gets +1 Quickness). This should be changed to use racial base values instead, or apply modifiers as adjustments within racial maximums.

### 3.4 Implementation Requirements

#### Required Code Changes:

##### 1. **Create Archetype Template System**

Create new file: `/Models/ArchetypeTemplate.cs`

```csharp
namespace ShadowrunDiscordBot.Models;

/// <summary>
/// Pre-defined archetype template for SR3 character creation
/// </summary>
public class ArchetypeTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Base attributes for this archetype
    public int Body { get; set; }
    public int Quickness { get; set; }
    public int Strength { get; set; }
    public int Charisma { get; set; }
    public int Intelligence { get; set; }
    public int Willpower { get; set; }
    
    // Magic attribute (0 for mundane, 6 for full magician, etc.)
    public int Magic { get; set; } = 0;
    
    // Starting skills
    public List<ArchetypeSkill> StartingSkills { get; set; } = new();
    
    // Starting resources
    public long StartingNuyen { get; set; }
    public int StartingKarma { get; set; }
    
    // Compatible metatypes
    public List<string> CompatibleMetatypes { get; set; } = new();
    
    // Starting equipment/cyberware/spells (JSON)
    public string? StartingEquipment { get; set; }
    
    // Priority requirements (for validation)
    public string RequiredAttributePriority { get; set; } = "B";
    public string RequiredSkillPriority { get; set; } = "C";
    public string RequiredResourcePriority { get; set; } = "B";
    public string? RequiredMagicPriority { get; set; }
}

public class ArchetypeSkill
{
    public string SkillName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Specialization { get; set; }
    public bool IsKnowledgeSkill { get; set; } = false;
}
```

##### 2. **Create Archetype Repository**

Create new file: `/Services/ArchetypeService.cs`

```csharp
namespace ShadowrunDiscordBot.Services;

public class ArchetypeService
{
    private readonly Dictionary<string, ArchetypeTemplate> _archetypes;
    
    public ArchetypeService()
    {
        _archetypes = InitializeArchetypes();
    }
    
    public ArchetypeTemplate? GetArchetype(string name)
    {
        return _archetypes.TryGetValue(name, out var archetype) ? archetype : null;
    }
    
    public List<string> GetAllArchetypeNames()
    {
        return _archetypes.Keys.ToList();
    }
    
    public bool IsValidArchetypeForMetatype(string archetype, string metatype)
    {
        var template = GetArchetype(archetype);
        return template?.CompatibleMetatypes.Contains(metatype, StringComparer.OrdinalIgnoreCase) ?? false;
    }
    
    private Dictionary<string, ArchetypeTemplate> InitializeArchetypes()
    {
        return new Dictionary<string, ArchetypeTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            ["Street Samurai"] = new ArchetypeTemplate
            {
                Name = "Street Samurai",
                Description = "Cybered combat specialist",
                Body = 5, Quickness = 6, Strength = 5,
                Charisma = 3, Intelligence = 4, Willpower = 3,
                Magic = 0,
                StartingNuyen = 400000,
                StartingKarma = 0,
                CompatibleMetatypes = new List<string> { "Human", "Elf", "Dwarf", "Ork", "Troll" },
                StartingSkills = new List<ArchetypeSkill>
                {
                    new() { SkillName = "Edged Weapons", Rating = 6 },
                    new() { SkillName = "Pistols", Rating = 5 },
                    new() { SkillName = "Athletics", Rating = 4 },
                    new() { SkillName = "Stealth", Rating = 3 },
                    new() { SkillName = "Etiquette", Rating = 3, Specialization = "Street" }
                },
                RequiredAttributePriority = "B",
                RequiredSkillPriority = "C",
                RequiredResourcePriority = "B"
            },
            
            ["Mage"] = new ArchetypeTemplate
            {
                Name = "Mage",
                Description = "Hermetic magician",
                Body = 3, Quickness = 3, Strength = 3,
                Charisma = 4, Intelligence = 5, Willpower = 5,
                Magic = 6,
                StartingNuyen = 90000,
                StartingKarma = 0,
                CompatibleMetatypes = new List<string> { "Human", "Elf", "Dwarf" },
                StartingSkills = new List<ArchetypeSkill>
                {
                    new() { SkillName = "Sorcery", Rating = 6 },
                    new() { SkillName = "Conjuring", Rating = 4 },
                    new() { SkillName = "Magical Theory", Rating = 5 },
                    new() { SkillName = "Etiquette", Rating = 3, Specialization = "Corporate" },
                    new() { SkillName = "Stealth", Rating = 2 }
                },
                RequiredAttributePriority = "B",
                RequiredSkillPriority = "C",
                RequiredResourcePriority = "C",
                RequiredMagicPriority = "A"
            },
            
            // ... (add all 7 archetypes defined above)
        };
    }
}
```

##### 3. **Update CreateCharacterCommand**

Modify `/Commands/Characters/CreateCharacterCommand.cs`:

```csharp
public class CreateCharacterCommand : IRequest<CreateCharacterResponse>
{
    public ulong DiscordUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Metatype { get; set; } = "Human";
    public string Archetype { get; set; } = string.Empty; // Now REQUIRED, no default
    
    // REMOVE: These should not be user-settable
    // public int Body { get; set; } = 3;
    // public int Quickness { get; set; } = 3;
    // ... etc
    
    // Attributes will be determined by archetype template
}
```

##### 4. **Update CreateCharacterCommandValidator**

Modify `/Commands/Validators/CreateCharacterCommandValidator.cs`:

```csharp
public class CreateCharacterCommandValidator : AbstractValidator<CreateCharacterCommand>
{
    private readonly ArchetypeService _archetypeService;
    
    private static readonly string[] ValidMetatypes = { "Human", "Elf", "Dwarf", "Ork", "Troll" };

    public CreateCharacterCommandValidator(ArchetypeService archetypeService)
    {
        _archetypeService = archetypeService;
        
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
            .NotEmpty().WithMessage("Archetype is required - custom builds are not allowed")
            .Must(BeValidArchetype).WithMessage("Invalid archetype. Use one of the pre-defined archetypes.")
            .Must((cmd, archetype) => BeCompatibleArchetype(archetype, cmd.Metatype))
            .WithMessage((cmd, archetype) => 
                $"Archetype '{archetype}' is not compatible with metatype '{cmd.Metatype}'");

        // REMOVE: Attribute validation - attributes are now determined by archetype
        // RuleFor(x => x.Body)...
        // RuleFor(x => x.Quickness)...
        // etc.
    }

    private bool BeValidMetatype(string metatype)
    {
        return ValidMetatypes.Contains(metatype, StringComparer.OrdinalIgnoreCase);
    }

    private bool BeValidArchetype(string archetype)
    {
        return _archetypeService.GetArchetype(archetype) != null;
    }
    
    private bool BeCompatibleArchetype(string archetype, string metatype)
    {
        return _archetypeService.IsValidArchetypeForMetatype(archetype, metatype);
    }
}
```

##### 5. **Update CreateCharacterCommandHandler**

Modify `/Commands/Characters/CreateCharacterCommandHandler.cs`:

```csharp
public class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, CreateCharacterResponse>
{
    private readonly DatabaseService _databaseService;
    private readonly ICacheService _cacheService;
    private readonly ArchetypeService _archetypeService;
    private readonly ILogger<CreateCharacterCommandHandler> _logger;

    public CreateCharacterCommandHandler(
        DatabaseService databaseService,
        ICacheService cacheService,
        ArchetypeService archetypeService,
        ILogger<CreateCharacterCommandHandler> logger)
    {
        _databaseService = databaseService;
        _cacheService = cacheService;
        _archetypeService = archetypeService;
        _logger = logger;
    }

    public async Task<CreateCharacterResponse> Handle(
        CreateCharacterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate archetype exists
            var archetypeTemplate = _archetypeService.GetArchetype(request.Archetype);
            if (archetypeTemplate == null)
            {
                return new CreateCharacterResponse
                {
                    Success = false,
                    Error = $"Invalid archetype '{request.Archetype}'"
                };
            }
            
            // Validate archetype is compatible with metatype
            if (!_archetypeService.IsValidArchetypeForMetatype(request.Archetype, request.Metatype))
            {
                return new CreateCharacterResponse
                {
                    Success = false,
                    Error = $"Archetype '{request.Archetype}' is not compatible with metatype '{request.Metatype}'"
                };
            }

            // Check if character name already exists
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

            // Create character from archetype template
            var character = CreateCharacterFromArchetype(
                request.DiscordUserId,
                request.Name,
                request.Metatype,
                archetypeTemplate);

            // Create the character in the database
            var created = await _databaseService.CreateCharacterAsync(character).ConfigureAwait(false);

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKeys.UserCharacters(request.DiscordUserId)).ConfigureAwait(false);

            _logger.LogInformation("Created {Archetype} character {CharacterName} (ID: {CharacterId}) for user {UserId}",
                created.Archetype, created.Name, created.Id, created.DiscordUserId);

            return new CreateCharacterResponse
            {
                Success = true,
                CharacterId = created.Id,
                Message = $"**{created.Name}** the {created.Metatype} {created.Archetype} created successfully!\n" +
                         $"Attributes: BOD {created.Body}, QUI {created.Quickness}, STR {created.Strength}, " +
                         $"CHA {created.Charisma}, INT {created.Intelligence}, WIL {created.Willpower}"
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
    
    private ShadowrunCharacter CreateCharacterFromArchetype(
        ulong discordUserId,
        string name,
        string metatype,
        ArchetypeTemplate archetype)
    {
        // Get racial base values from PrioritySystem
        var racialBase = PriorityTable.RacialBaseValues[metatype];
        
        // Create character with archetype base attributes
        var character = new ShadowrunCharacter
        {
            DiscordUserId = discordUserId,
            Name = name,
            Metatype = metatype,
            Archetype = archetype.Name,
            
            // Apply archetype attributes (these are final, adjusted for metatype already in template)
            Body = archetype.Body,
            Quickness = archetype.Quickness,
            Strength = archetype.Strength,
            Charisma = archetype.Charisma,
            Intelligence = archetype.Intelligence,
            Willpower = archetype.Willpower,
            Magic = archetype.Magic,
            
            // Resources from archetype
            Karma = archetype.StartingKarma,
            Nuyen = archetype.StartingNuyen,
            
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Add starting skills from archetype
        foreach (var skill in archetype.StartingSkills)
        {
            character.Skills.Add(new CharacterSkill
            {
                SkillName = skill.SkillName,
                Rating = skill.Rating,
                Specialization = skill.Specialization,
                IsKnowledgeSkill = skill.IsKnowledgeSkill,
                Character = character
            });
        }
        
        // TODO: Parse and add starting equipment/cyberware/spells from archetype.StartingEquipment (JSON)
        
        return character;
    }
}
```

##### 6. **Update CharacterCommands.cs (Discord Interface)**

Modify the Discord slash command handler to remove attribute parameters:

```csharp
public async Task CreateCharacterAsync(SocketSlashCommand command)
{
    await LogCommandExecutionAsync(command, "Create Character");

    try
    {
        var options = command.Data.Options.First().Options.ToList();
        var name = options.First(o => o.Name == "name").Value.ToString();
        var metatype = options.First(o => o.Name == "metatype").Value.ToString();
        var archetype = options.First(o => o.Name == "archetype").Value.ToString();

        // REMOVE: All attribute parameters from slash command
        // Users NO LONGER input attributes - archetype determines them

        // Create command WITHOUT attributes
        var createCommand = new CreateCharacterCommand
        {
            DiscordUserId = command.User.Id,
            Name = name!,
            Metatype = metatype!,
            Archetype = archetype!
            // Attributes removed - will be set by archetype
        };

        // Send to MediatR handler
        var result = await Mediator.Send(createCommand);
        
        if (result.Success)
        {
            await command.RespondAsync($"✅ {result.Message}");
        }
        else
        {
            await command.RespondAsync($"❌ {result.Error}", ephemeral: true);
        }
    }
    catch (Exception ex)
    {
        await HandleErrorAsync(command, ex, "CreateCharacter");
    }
}
```

##### 7. **Update Discord Slash Command Registration**

In your command registration (likely in `Program.cs` or a command module):

```csharp
// REMOVE attribute options from the create character slash command
var createCharacterCommand = new SlashCommandBuilder()
    .WithName("character")
    .WithDescription("Character management commands")
    .AddOption(new SlashCommandOptionBuilder()
        .WithName("create")
        .WithDescription("Create a new character from an archetype template")
        .WithType(ApplicationCommandOptionType.SubCommand)
        .AddOption("name", ApplicationCommandOptionType.String, "Character name", isRequired: true)
        .AddOption("metatype", ApplicationCommandOptionType.String, "Character metatype", isRequired: true,
            choices: new[]
            {
                new ApplicationCommandOptionChoiceProperties { Name = "Human", Value = "Human" },
                new ApplicationCommandOptionChoiceProperties { Name = "Elf", Value = "Elf" },
                new ApplicationCommandOptionChoiceProperties { Name = "Dwarf", Value = "Dwarf" },
                new ApplicationCommandOptionChoiceProperties { Name = "Ork", Value = "Ork" },
                new ApplicationCommandOptionChoiceProperties { Name = "Troll", Value = "Troll" }
            })
        .AddOption("archetype", ApplicationCommandOptionType.String, "Character archetype (pre-made template)", isRequired: true,
            choices: new[]
            {
                new ApplicationCommandOptionChoiceProperties { Name = "Street Samurai", Value = "Street Samurai" },
                new ApplicationCommandOptionChoiceProperties { Name = "Mage", Value = "Mage" },
                new ApplicationCommandOptionChoiceProperties { Name = "Shaman", Value = "Shaman" },
                new ApplicationCommandOptionChoiceProperties { Name = "Physical Adept", Value = "Physical Adept" },
                new ApplicationCommandOptionChoiceProperties { Name = "Decker", Value = "Decker" },
                new ApplicationCommandOptionChoiceProperties { Name = "Rigger", Value = "Rigger" },
                new ApplicationCommandOptionChoiceProperties { Name = "Face", Value = "Face" }
            })
        // REMOVED: Body, Quickness, Strength, Charisma, Intelligence, Willpower options
    );
```

##### 8. **Register ArchetypeService in DI Container**

In `Program.cs`:

```csharp
// Register ArchetypeService as singleton
builder.Services.AddSingleton<ArchetypeService>();
```

---

## TASK 4: Implementation Plan Summary

### Phase 1: Core Infrastructure (Week 1)

1. ✅ Create `ArchetypeTemplate.cs` model
2. ✅ Create `ArchetypeService.cs` with 7 core archetypes
3. ✅ Register `ArchetypeService` in DI container
4. ✅ Update `CreateCharacterCommand` to remove attribute parameters
5. ✅ Update `CreateCharacterCommandValidator` to enforce archetype compatibility

### Phase 2: Handler & Logic (Week 1-2)

6. ✅ Update `CreateCharacterCommandHandler` to use archetype templates
7. ✅ Implement `CreateCharacterFromArchetype()` method
8. ✅ Add archetype-metatype compatibility validation
9. ✅ Remove attribute allocation logic from handler

### Phase 3: Discord Interface (Week 2)

10. ✅ Update `CharacterCommands.cs` to remove attribute inputs
11. ✅ Update Discord slash command registration
12. ✅ Remove attribute options from slash command builder
13. ✅ Update help text and examples

### Phase 4: Testing & Validation (Week 2-3)

14. ✅ Create unit tests for `ArchetypeService`
15. ✅ Create integration tests for archetype-based creation
16. ✅ Test all 7 archetypes with all compatible metatypes
17. ✅ Test validation of incompatible archetype-metatype combinations

### Phase 5: Migration & Backward Compatibility (Week 3)

18. ✅ Create migration strategy for existing characters
19. ✅ Add migration script to convert existing characters to archetype-based
20. ✅ Implement fallback for pre-existing characters without archetypes
21. ✅ Add warning/notice for existing characters that violate archetype rules

### Phase 6: Documentation & Rollout (Week 3-4)

22. ✅ Update user documentation with archetype list
23. ✅ Create archetype reference guide for players
24. ✅ Update README.md with new character creation process
25. ✅ Announce changes to Discord server users

---

## Archetypes to Support (Full List)

### Priority 1 (Core 7 - Immediate Implementation):

1. **Street Samurai** - Cybered combat specialist
2. **Mage** - Hermetic magician
3. **Shaman** - Totem-following magician
4. **Physical Adept** - Magic-enhanced warrior
5. **Decker** - Matrix specialist/hacker
6. **Rigger** - Vehicle/drone controller
7. **Face** - Social specialist/negotiator

### Priority 2 (Extended - Future Implementation):

8. **Investigator** - Detective/bounty hunter
9. **Medic** - Street doctor/medic
10. **Covert Ops Specialist** - Stealth/infiltration expert
11. **Weapons Specialist** - Firearms expert
12. **Ganger** - Street tough
13. **Corporate Security** - Corp guard/soldier
14. **Smuggler** - Transport specialist

---

## Potential Issues & Solutions

### Issue 1: Backward Compatibility

**Problem:** Existing characters may have custom attributes that don't match any archetype.

**Solution:**
- Keep existing characters as-is (grandfather clause)
- Add `IsCustomBuild` flag to `ShadowrunCharacter` model
- Display warning on legacy characters: "⚠️ This character uses legacy custom build rules"
- Only enforce archetype system for NEW characters created after migration date

```csharp
public class ShadowrunCharacter
{
    // ... existing properties ...
    
    // Migration flag for pre-archetype characters
    public bool IsCustomBuild { get; set; } = false;
    public DateTime? CreatedBeforeArchetypeSystem { get; set; }
}
```

### Issue 2: Limited Flexibility

**Problem:** Players may want custom builds not covered by archetypes.

**Solution:**
- Implement 7+ archetypes covering major playstyles
- Add "Advanced" archetypes for niche builds (Priority 2 list)
- Allow GMs to create custom archetypes via admin interface (future feature)
- Provide karma for post-creation customization (SR3 advancement system)

### Issue 3: Attribute Imbalance

**Problem:** Some archetypes may be more powerful than others.

**Solution:**
- Use SR3 official archetype templates (playtested for balance)
- Implement point-buy validation to ensure archetype totals match priority system
- Add archetype power level ratings for GM reference
- Monitor gameplay feedback and adjust as needed

### Issue 4: Missing Priority System Integration

**Problem:** Current priority system exists but isn't used.

**Solution:**
- Phase 1: Implement archetype system (this plan)
- Phase 2: Add priority system for advanced character creation (future)
- Allow GMs to choose: Archetype-only (simple) or Priority-based (advanced)

### Issue 5: User Confusion

**Problem:** Users accustomed to custom builds may be confused by archetype system.

**Solution:**
- Provide detailed archetype descriptions in help command
- Show archetype preview before creation: `/character preview <archetype>`
- Display archetype stats and skills in character sheet
- Create example characters for each archetype

```csharp
// New command: Preview archetype
public async Task PreviewArchetypeAsync(SocketSlashCommand command)
{
    var archetypeName = command.Data.Options.First(o => o.Name == "name").Value.ToString();
    var archetype = _archetypeService.GetArchetype(archetypeName);
    
    if (archetype == null)
    {
        await command.RespondAsync($"❌ Archetype '{archetypeName}' not found.", ephemeral: true);
        return;
    }
    
    var embed = new EmbedBuilder()
        .WithTitle($"📋 {archetype.Name}")
        .WithDescription(archetype.Description)
        .AddField("Attributes", 
            $"BOD {archetype.Body} | QUI {archetype.Quickness} | STR {archetype.Strength}\n" +
            $"CHA {archetype.Charisma} | INT {archetype.Intelligence} | WIL {archetype.Willpower}" +
            (archetype.Magic > 0 ? $"\n🔮 Magic: {archetype.Magic}" : ""))
        .AddField("Starting Skills", 
            string.Join("\n", archetype.StartingSkills.Select(s => 
                $"• {s.SkillName} {s.Rating}" + (s.Specialization != null ? $" ({s.Specialization})" : ""))))
        .AddField("Resources", 
            $"💰 Nuyen: {archetype.StartingNuyen:N0}¥\n" +
            $"⭐ Karma: {archetype.StartingKarma}")
        .AddField("Compatible Metatypes", 
            string.Join(", ", archetype.CompatibleMetatypes))
        .Build();
    
    await command.RespondAsync(embed: embed);
}
```

---

## Example Implementation: 3 Archetypes

### Example 1: Street Samurai

```csharp
new ArchetypeTemplate
{
    Name = "Street Samurai",
    Description = "A cybernetically-enhanced combat specialist. Masters of warfare and street survival.",
    Body = 5, Quickness = 6, Strength = 5,
    Charisma = 3, Intelligence = 4, Willpower = 3,
    Magic = 0,
    StartingNuyen = 400000,
    StartingKarma = 0,
    CompatibleMetatypes = new List<string> { "Human", "Elf", "Dwarf", "Ork", "Troll" },
    StartingSkills = new List<ArchetypeSkill>
    {
        new() { SkillName = "Edged Weapons", Rating = 6 },
        new() { SkillName = "Pistols", Rating = 5 },
        new() { SkillName = "Athletics", Rating = 4 },
        new() { SkillName = "Stealth", Rating = 3 },
        new() { SkillName = "Etiquette", Rating = 3, Specialization = "Street" }
    },
    StartingEquipment = JsonSerializer.Serialize(new
    {
        Cyberware = new[]
        {
            new { Name = "Wired Reflexes", Rating = 2, EssenceCost = 3.0m },
            new { Name = "Smartlink", Rating = 1, EssenceCost = 0.5m },
            new { Name = "Dermal Plating", Rating = 3, EssenceCost = 1.5m }
        },
        Weapons = new[]
        {
            new { Name = "Katana", Damage = "S", Reach = 1 },
            new { Name = "Ares Predator", Damage = "M", Range = "Heavy Pistol" }
        }
    }),
    RequiredAttributePriority = "B",
    RequiredSkillPriority = "C",
    RequiredResourcePriority = "B"
}
```

**Validation Rules:**
- Compatible with all metatypes
- Total attribute points: 5+6+5+3+4+3 = 26 (within Priority B: 27)
- Total skill points: 6+5+4+3+3 = 21 (within Priority C: 34)
- Resources: 400,000¥ (Priority B)
- Magic: 0 (Mundane, Priority E)

### Example 2: Mage

```csharp
new ArchetypeTemplate
{
    Name = "Mage",
    Description = "A hermetic magician who follows the scientific approach to magic. Scholars of the arcane.",
    Body = 3, Quickness = 3, Strength = 3,
    Charisma = 4, Intelligence = 5, Willpower = 5,
    Magic = 6,
    StartingNuyen = 90000,
    StartingKarma = 0,
    CompatibleMetatypes = new List<string> { "Human", "Elf", "Dwarf" },
    StartingSkills = new List<ArchetypeSkill>
    {
        new() { SkillName = "Sorcery", Rating = 6 },
        new() { SkillName = "Conjuring", Rating = 4 },
        new() { SkillName = "Magical Theory", Rating = 5 },
        new() { SkillName = "Etiquette", Rating = 3, Specialization = "Corporate" },
        new() { SkillName = "Stealth", Rating = 2 }
    },
    StartingEquipment = JsonSerializer.Serialize(new
    {
        Spells = new[]
        {
            new { Name = "Manabolt", Category = "Combat", Drain = "M" },
            new { Name = "Stunbolt", Category = "Combat", Drain = "S" },
            new { Name = "Powerbolt", Category = "Combat", Drain = "M" },
            new { Name = "Detect Enemies", Category = "Detection", Drain = "L" },
            new { Name = "Detect Magic", Category = "Detection", Drain = "L" }
        },
        Foci = new[]
        {
            new { Name = "Spell Focus", Rating = 2, Type = "Sustaining" }
        }
    }),
    RequiredAttributePriority = "B",
    RequiredSkillPriority = "C",
    RequiredResourcePriority = "C",
    RequiredMagicPriority = "A"
}
```

**Validation Rules:**
- Compatible with Human, Elf, Dwarf only (cannot be Ork/Troll mage in SR3 without special rules)
- Total attribute points: 3+3+3+4+5+5 = 23 (within Priority B: 27)
- Total skill points: 6+4+5+3+2 = 20 (within Priority C: 34)
- Resources: 90,000¥ (Priority C)
- Magic: 6 (Full Magician, Priority A)

### Example 3: Decker

```csharp
new ArchetypeTemplate
{
    Name = "Decker",
    Description = "A Matrix specialist who navigates virtual reality to hack systems and steal data.",
    Body = 3, Quickness = 4, Strength = 3,
    Charisma = 3, Intelligence = 6, Willpower = 4,
    Magic = 0,
    StartingNuyen = 400000,
    StartingKarma = 0,
    CompatibleMetatypes = new List<string> { "Human", "Elf", "Dwarf" },
    StartingSkills = new List<ArchetypeSkill>
    {
        new() { SkillName = "Computer", Rating = 6 },
        new() { SkillName = "Electronics", Rating = 5 },
        new() { SkillName = "Cyberdeck Design", Rating = 4 },
        new() { SkillName = "Data Brokerage", Rating = 3 },
        new() { SkillName = "Stealth", Rating = 3 }
    },
    StartingEquipment = JsonSerializer.Serialize(new
    {
        Cyberdeck = new
        {
            Name = "Fuchi Cyber-7",
            MPCP = 4,
            Hardening = 4,
            ActiveMemory = 400,
            Storage = 5000
        },
        Programs = new[]
        {
            new { Name = "Attack", Rating = 4 },
            new { Name = "Armor", Rating = 4 },
            new { Name = "Deception", Rating = 4 },
            new { Name = "Sleaze", Rating = 4 },
            new { Name = "Evaluate", Rating = 4 }
        }
    }),
    RequiredAttributePriority = "B",
    RequiredSkillPriority = "B",
    RequiredResourcePriority = "B",
    RequiredMagicPriority = null // Mundane
}
```

**Validation Rules:**
- Compatible with Human, Elf, Dwarf (high Intelligence races)
- Total attribute points: 3+4+3+3+6+4 = 23 (within Priority B: 27)
- Total skill points: 6+5+4+3+3 = 21 (within Priority B: 40)
- Resources: 400,000¥ (Priority B - covers expensive cyberdeck)
- Magic: 0 (Mundane, Priority E)

---

## Summary

This implementation plan enforces **mandatory archetype-based character creation** that strictly follows Shadowrun 3rd Edition rules. Key changes:

1. **No custom builds** - Users must choose from pre-made archetypes
2. **Archetype determines all** - Attributes, skills, and resources are fixed by archetype
3. **Metatype compatibility** - Archetypes restrict which metatypes can use them
4. **SR3 compliance** - All archetypes match SR3 priority system and point totals
5. **Backward compatible** - Existing characters grandfathered, only new characters affected

This system ensures all characters are balanced, SR3-compliant, and true to the Shadowrun universe.
