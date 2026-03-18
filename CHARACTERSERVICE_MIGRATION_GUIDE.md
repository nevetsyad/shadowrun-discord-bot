# CharacterService Migration Guide

**Target File:** `Services/CharacterService.cs`  
**Current State:** Uses `ShadowrunCharacter` (old anemic model)  
**Target State:** Uses `Character` entity and `ICharacterRepository`  
**Priority:** 🔴 Critical (blocks other migrations)  
**Estimated Effort:** 4-6 hours

---

## Current Implementation Analysis

### Dependencies (Current)
```csharp
using ShadowrunDiscordBot.Models;  // ❌ Old namespace
```

### Constructor (Current)
```csharp
public class CharacterService
{
    private readonly DatabaseService _databaseService;  // ❌ Direct DB access
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(
        DatabaseService databaseService,
        ILogger<CharacterService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }
}
```

### Key Methods to Migrate
1. `GetAllCharactersAsync()` - Get all characters
2. `GetCharactersPageAsync()` - Paginated character list
3. `GetCharacterByIdAsync()` - Get single character
4. `CreateCharacterAsync()` - Create new character
5. `UpdateCharacterAsync()` - Update existing character
6. `DeleteCharacterAsync()` - Delete character
7. `AddSkillAsync()` - Add skill to character
8. `UpdateSkillAsync()` - Update character skill
9. `RemoveSkillAsync()` - Remove skill from character
10. Similar methods for cyberware, spells, spirits, gear

---

## Migration Strategy

### Step 1: Update Dependencies

**Before:**
```csharp
using ShadowrunDiscordBot.Models;
```

**After:**
```csharp
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;
using ShadowrunDiscordBot.Application.DTOs;
```

### Step 2: Update Constructor

**Before:**
```csharp
private readonly DatabaseService _databaseService;

public CharacterService(
    DatabaseService databaseService,
    ILogger<CharacterService> logger)
{
    _databaseService = databaseService;
    _logger = logger;
}
```

**After:**
```csharp
private readonly ICharacterRepository _characterRepository;
private readonly ILogger<CharacterService> _logger;

public CharacterService(
    ICharacterRepository characterRepository,
    ILogger<CharacterService> logger)
{
    _characterRepository = characterRepository;
    _logger = logger;
}
```

### Step 3: Update Method Signatures

Most method signatures should remain the same (they already use DTOs), but internal implementations need to change.

---

## Method Migration Examples

### 1. GetAllCharactersAsync()

**Before:**
```csharp
public async Task<List<CharacterSummaryDto>> GetAllCharactersAsync(ulong? userId = null)
{
    try
    {
        var characters = userId.HasValue
            ? await _databaseService.GetUserCharactersAsync(userId.Value)
            : await _databaseService.GetAllCharactersAsync();

        return characters.Select(MapToSummaryDto).ToList();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get characters");
        throw;
    }
}
```

**After:**
```csharp
public async Task<List<CharacterSummaryDto>> GetAllCharactersAsync(ulong? userId = null)
{
    try
    {
        var characters = userId.HasValue
            ? await _characterRepository.GetByUserIdAsync(userId.Value)
            : await _characterRepository.GetAllAsync();

        return characters.Select(MapToSummaryDto).ToList();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get characters for user {UserId}", userId);
        throw;
    }
}
```

**Changes:**
- ✅ Use `_characterRepository` instead of `_databaseService`
- ✅ Repository methods return domain entities
- ✅ Better error logging with context

---

### 2. GetCharacterByIdAsync()

**Before:**
```csharp
public async Task<CharacterDto?> GetCharacterByIdAsync(int id)
{
    try
    {
        var character = await _databaseService.GetCharacterByIdAsync(id);
        if (character == null)
        {
            return null;
        }

        return MapToDto(character);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get character {CharacterId}", id);
        throw;
    }
}
```

**After:**
```csharp
public async Task<CharacterDto?> GetCharacterByIdAsync(int id)
{
    try
    {
        var character = await _characterRepository.GetByIdAsync(id);
        if (character == null)
        {
            _logger.LogWarning("Character {CharacterId} not found", id);
            return null;
        }

        return MapToDto(character);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get character {CharacterId}", id);
        throw;
    }
}
```

**Changes:**
- ✅ Use repository instead of database service
- ✅ Added warning log for not found case
- ✅ No other changes needed

---

### 3. CreateCharacterAsync() - CRITICAL

**Before:**
```csharp
public async Task<CharacterDto> CreateCharacterAsync(CreateCharacterDto createDto)
{
    try
    {
        // Check if user already has max characters
        var existingCharacters = await _databaseService.GetUserCharactersAsync(createDto.DiscordUserId);
        if (existingCharacters.Count >= 5)
        {
            throw new InvalidOperationException("Maximum character limit reached (5 characters per user)");
        }

        var character = new ShadowrunCharacter
        {
            Name = createDto.Name,
            DiscordUserId = createDto.DiscordUserId,
            Metatype = createDto.Metatype,
            Archetype = createDto.Archetype,
            // ... set all properties
        };

        var created = await _databaseService.CreateCharacterAsync(character);
        return MapToDto(created);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create character");
        throw;
    }
}
```

**After:**
```csharp
public async Task<CharacterDto> CreateCharacterAsync(CreateCharacterDto createDto)
{
    try
    {
        // Check if user already has max characters
        var existingCharacters = await _characterRepository.GetByUserIdAsync(createDto.DiscordUserId);
        if (existingCharacters.Count() >= 5)
        {
            throw new InvalidOperationException("Maximum character limit reached (5 characters per user)");
        }

        // Use domain factory method - it validates and applies business rules
        var character = Character.Create(
            name: createDto.Name,
            discordUserId: createDto.DiscordUserId,
            metatype: createDto.Metatype,
            archetype: createDto.Archetype,
            archetypeId: createDto.ArchetypeId,
            baseBody: createDto.BaseBody,
            baseQuickness: createDto.BaseQuickness,
            baseStrength: createDto.BaseStrength,
            baseCharisma: createDto.BaseCharisma,
            baseIntelligence: createDto.BaseIntelligence,
            baseWillpower: createDto.BaseWillpower
        );

        var created = await _characterRepository.AddAsync(character);
        
        _logger.LogInformation(
            "Created character {CharacterName} (ID: {CharacterId}) for user {UserId}",
            created.Name, created.Id, createDto.DiscordUserId);

        return MapToDto(created);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create character for user {UserId}", createDto.DiscordUserId);
        throw;
    }
}
```

**Changes:**
- ✅ Use repository to check existing characters
- ✅ Use domain factory method `Character.Create()` instead of `new ShadowrunCharacter()`
- ✅ Factory method handles validation and racial modifiers
- ✅ Use repository to persist
- ✅ Added success logging with context
- ✅ Better error logging with user ID

---

### 4. UpdateCharacterAsync()

**Before:**
```csharp
public async Task<CharacterDto> UpdateCharacterAsync(int id, UpdateCharacterDto updateDto)
{
    try
    {
        var character = await _databaseService.GetCharacterByIdAsync(id);
        if (character == null)
        {
            throw new NotFoundException($"Character with ID {id} not found");
        }

        // Update properties
        character.Name = updateDto.Name ?? character.Name;
        character.Karma = updateDto.Karma ?? character.Karma;
        character.Nuyen = updateDto.Nuyen ?? character.Nuyen;
        // ... etc

        character.UpdatedAt = DateTime.UtcNow;

        var updated = await _databaseService.UpdateCharacterAsync(character);
        return MapToDto(updated);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to update character {CharacterId}", id);
        throw;
    }
}
```

**After:**
```csharp
public async Task<CharacterDto> UpdateCharacterAsync(int id, UpdateCharacterDto updateDto)
{
    try
    {
        var character = await _characterRepository.GetByIdAsync(id);
        if (character == null)
        {
            throw new NotFoundException($"Character with ID {id} not found");
        }

        // Use domain methods to update - they enforce invariants
        if (updateDto.Name != null)
        {
            character.UpdateName(updateDto.Name);
        }

        if (updateDto.Karma.HasValue)
        {
            character.UpdateKarma(updateDto.Karma.Value);
        }

        if (updateDto.Nuyen.HasValue)
        {
            character.UpdateNuyen(updateDto.Nuyen.Value);
        }

        // ... other updates using domain methods

        var updated = await _characterRepository.UpdateAsync(character);
        
        _logger.LogInformation(
            "Updated character {CharacterId}: {CharacterName}",
            updated.Id, updated.Name);

        return MapToDto(updated);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to update character {CharacterId}", id);
        throw;
    }
}
```

**Changes:**
- ✅ Use repository to get entity
- ✅ Use domain methods instead of direct property setters
- ✅ Domain methods enforce business rules and invariants
- ✅ Use repository to persist
- ✅ Added success logging

---

### 5. DeleteCharacterAsync()

**Before:**
```csharp
public async Task<bool> DeleteCharacterAsync(int id)
{
    try
    {
        return await _databaseService.DeleteCharacterAsync(id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete character {CharacterId}", id);
        throw;
    }
}
```

**After:**
```csharp
public async Task<bool> DeleteCharacterAsync(int id)
{
    try
    {
        var character = await _characterRepository.GetByIdAsync(id);
        if (character == null)
        {
            _logger.LogWarning("Attempted to delete non-existent character {CharacterId}", id);
            return false;
        }

        await _characterRepository.DeleteAsync(character);
        
        _logger.LogInformation(
            "Deleted character {CharacterId}: {CharacterName}",
            id, character.Name);

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete character {CharacterId}", id);
        throw;
    }
}
```

**Changes:**
- ✅ Get entity first for logging purposes
- ✅ Use repository to delete
- ✅ Better logging with character name
- ✅ Warning log for non-existent character

---

### 6. AddSkillAsync() - Collection Management

**Before:**
```csharp
public async Task<CharacterSkillDto> AddSkillAsync(int characterId, CreateSkillDto skillDto)
{
    try
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
        {
            throw new NotFoundException($"Character with ID {characterId} not found");
        }

        var skill = new CharacterSkill
        {
            CharacterId = characterId,
            Name = skillDto.Name,
            Rating = skillDto.Rating,
            Specialization = skillDto.Specialization
        };

        character.Skills.Add(skill);
        await _databaseService.UpdateCharacterAsync(character);

        return MapToSkillDto(skill);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to add skill to character {CharacterId}", characterId);
        throw;
    }
}
```

**After:**
```csharp
public async Task<CharacterSkillDto> AddSkillAsync(int characterId, CreateSkillDto skillDto)
{
    try
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null)
        {
            throw new NotFoundException($"Character with ID {characterId} not found");
        }

        // Use domain method - it validates and enforces business rules
        character.AddSkill(
            name: skillDto.Name,
            rating: skillDto.Rating,
            specialization: skillDto.Specialization
        );

        await _characterRepository.UpdateAsync(character);

        // Get the newly added skill
        var skill = character.Skills.LastOrDefault(s => s.Name == skillDto.Name);
        
        _logger.LogInformation(
            "Added skill {SkillName} (Rating: {Rating}) to character {CharacterId}",
            skillDto.Name, skillDto.Rating, characterId);

        return MapToSkillDto(skill!);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to add skill to character {CharacterId}", characterId);
        throw;
    }
}
```

**Changes:**
- ✅ Use repository to get entity
- ✅ Use domain method `AddSkill()` which validates and enforces rules
- ✅ Use repository to persist
- ✅ Better logging

---

## Mapping Functions

The mapping functions (MapToDto, MapToSummaryDto, etc.) need to be updated to work with domain entities.

### MapToDto()

**Before:**
```csharp
private static CharacterDto MapToDto(ShadowrunCharacter character)
{
    return new CharacterDto
    {
        Id = character.Id,
        Name = character.Name,
        DiscordUserId = character.DiscordUserId,
        Metatype = character.Metatype,
        Archetype = character.Archetype,
        // ... map all properties
    };
}
```

**After:**
```csharp
private static CharacterDto MapToDto(Character character)
{
    return new CharacterDto
    {
        Id = character.Id,
        Name = character.Name,
        DiscordUserId = character.DiscordUserId,
        Metatype = character.Metatype,
        Archetype = character.Archetype,
        // ... map all properties from domain entity
        // Note: Some properties may be calculated or accessed differently
        Reaction = character.Reaction,  // Calculated property
        PhysicalConditionMonitor = character.PhysicalConditionMonitor,  // Calculated
        StunConditionMonitor = character.StunConditionMonitor,  // Calculated
        EssenceDecimal = character.EssenceDecimal,  // Different property name
        // ... etc
    };
}
```

---

## Testing Strategy

### Unit Tests Needed

1. **GetAllCharactersAsync Tests**
   - Returns all characters when no user filter
   - Returns only user's characters when user filter applied
   - Returns empty list when no characters exist
   - Handles repository exceptions

2. **GetCharacterByIdAsync Tests**
   - Returns character when found
   - Returns null when not found
   - Handles repository exceptions

3. **CreateCharacterAsync Tests**
   - Creates character successfully
   - Throws when user has max characters
   - Validates input through domain entity
   - Handles repository exceptions

4. **UpdateCharacterAsync Tests**
   - Updates character successfully
   - Throws when character not found
   - Uses domain methods for updates
   - Handles repository exceptions

5. **DeleteCharacterAsync Tests**
   - Deletes character successfully
   - Returns false when character not found
   - Handles repository exceptions

6. **AddSkillAsync Tests**
   - Adds skill successfully
   - Throws when character not found
   - Uses domain method for validation
   - Handles repository exceptions

### Test Example

```csharp
[Fact]
public async Task CreateCharacterAsync_ValidInput_ReturnsCreatedCharacter()
{
    // Arrange
    var mockRepo = new Mock<ICharacterRepository>();
    mockRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<ulong>()))
        .ReturnsAsync(Enumerable.Empty<Character>());
    mockRepo.Setup(r => r.AddAsync(It.IsAny<Character>()))
        .ReturnsAsync((Character c) => { c.Id = 1; return c; });

    var mockLogger = new Mock<ILogger<CharacterService>>();
    var service = new CharacterService(mockRepo.Object, mockLogger.Object);

    var createDto = new CreateCharacterDto
    {
        Name = "Test Character",
        DiscordUserId = 123456789,
        Metatype = "Human",
        Archetype = "Street Samurai",
        BaseBody = 5,
        BaseQuickness = 4,
        BaseStrength = 5,
        BaseCharisma = 3,
        BaseIntelligence = 4,
        BaseWillpower = 3
    };

    // Act
    var result = await service.CreateCharacterAsync(createDto);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Character", result.Name);
    Assert.Equal(123456789ul, result.DiscordUserId);
    mockRepo.Verify(r => r.AddAsync(It.IsAny<Character>()), Times.Once);
}
```

---

## Common Pitfalls to Avoid

### 1. ❌ Direct Property Setter
```csharp
character.Name = newName;  // ❌ Bypasses validation
```

### ✅ Use Domain Method
```csharp
character.UpdateName(newName);  // ✅ Enforces validation
```

---

### 2. ❌ Creating Entities with `new`
```csharp
var character = new Character
{
    Name = "Test",
    // ... set properties
};  // ❌ Bypasses factory validation
```

### ✅ Use Factory Method
```csharp
var character = Character.Create(
    name: "Test",
    // ... all required parameters
);  // ✅ Validates and enforces invariants
```

---

### 3. ❌ Bypassing Repository
```csharp
await _databaseService.UpdateCharacterAsync(character);  // ❌ Old pattern
```

### ✅ Use Repository
```csharp
await _characterRepository.UpdateAsync(character);  // ✅ New pattern
```

---

### 4. ❌ Ignoring Domain Events
```csharp
await _characterRepository.AddAsync(character);
// ❌ Domain events not dispatched
```

### ✅ Handle Domain Events
```csharp
await _characterRepository.AddAsync(character);

// Dispatch domain events
foreach (var domainEvent in character.DomainEvents)
{
    // Publish to event handlers
    await _mediator.Publish(domainEvent);
}

character.ClearDomainEvents();
```

---

## Migration Checklist

- [ ] Update using statements
- [ ] Update constructor to use `ICharacterRepository`
- [ ] Update `GetAllCharactersAsync()`
- [ ] Update `GetCharactersPageAsync()`
- [ ] Update `GetCharacterByIdAsync()`
- [ ] Update `CreateCharacterAsync()`
- [ ] Update `UpdateCharacterAsync()`
- [ ] Update `DeleteCharacterAsync()`
- [ ] Update `AddSkillAsync()`
- [ ] Update `UpdateSkillAsync()`
- [ ] Update `RemoveSkillAsync()`
- [ ] Update similar methods for cyberware
- [ ] Update similar methods for spells
- [ ] Update similar methods for spirits
- [ ] Update similar methods for gear
- [ ] Update all mapping functions
- [ ] Add unit tests
- [ ] Run all tests
- [ ] Update Program.cs registration (if needed)
- [ ] Test in development environment
- [ ] Code review
- [ ] Commit changes

---

## After Migration

### 1. Update Program.cs
```csharp
// CharacterService should already be registered correctly
services.AddScoped<CharacterService>();
```

### 2. Update Tests
All existing tests for CharacterService need to be updated to:
- Mock `ICharacterRepository` instead of `DatabaseService`
- Use domain entities instead of old models
- Verify domain behavior

### 3. Integration Testing
- Test character creation end-to-end
- Test character update end-to-end
- Test skill/cyberware/spell management
- Verify database persistence

### 4. Delete Old Code
After successful migration and testing:
```bash
# Remove old model file
git rm Models/ShadowrunCharacter.cs

# Update any remaining references
# Search for: ShadowrunDiscordBot.Models.ShadowrunCharacter
```

---

## Success Criteria

- ✅ All methods use `ICharacterRepository`
- ✅ All methods use domain entities
- ✅ No references to `ShadowrunCharacter` (old model)
- ✅ All unit tests passing
- ✅ All integration tests passing
- ✅ Code coverage > 80%
- ✅ No compiler warnings
- ✅ Successfully tested in development
- ✅ Code reviewed and approved
- ✅ Documentation updated

---

**Guide Created By:** Claude (OpenClaw)  
**Date:** 2026-03-17 22:55 EDT  
**Status:** Ready for implementation (requires .NET SDK)
