using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Service for character management operations
/// </summary>
public class CharacterService
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(
        DatabaseService databaseService,
        ILogger<CharacterService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    #region Character CRUD

    /// <summary>
    /// Get all characters, optionally filtered by user.
    /// GPT-5.4 FIX: Preserve legacy non-paginated behavior.
    /// </summary>
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

    /// <summary>
    /// Get a page of characters.
    /// GPT-5.4 FIX: Explicit pagination entry point so callers do not rely on GetAllCharactersAsync().
    /// </summary>
    public async Task<List<CharacterSummaryDto>> GetCharactersPageAsync(int skip, int take)
    {
        try
        {
            var characters = await _databaseService.GetCharactersPageAsync(skip, take);
            return characters.Select(MapToSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character page: skip={Skip}, take={Take}", skip, take);
            throw;
        }
    }

    /// <summary>
    /// Get a character by ID with all related data
    /// </summary>
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

    /// <summary>
    /// Create a new character
    /// </summary>
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
                DiscordUserId = createDto.DiscordUserId,
                Name = createDto.Name,
                Metatype = createDto.Metatype ?? "Human",
                Body = createDto.Body ?? 1,
                // SR3 FIX: Agility maps to Quickness in SR3
                Quickness = createDto.Agility ?? createDto.Quickness ?? 1,
                // SR3 FIX: Reaction is calculated (Quickness + Intelligence) / 2, can't be set directly
                Strength = createDto.Strength ?? 1,
                Charisma = createDto.Charisma ?? 1,
                // SR3 FIX: Intuition/Logic map to Intelligence in SR3
                Intelligence = createDto.Intuition ?? createDto.Logic ?? createDto.Intelligence ?? 1,
                Willpower = createDto.Willpower ?? 1,
                // SR3 FIX: Edge doesn't exist in SR3, ignoring
                Magic = createDto.Magic ?? 0,
                // SR3 FIX: Resonance doesn't exist in SR3, ignoring
                Essence = (int)((createDto.Essence ?? 6.0m) * 100), // Convert to internal format
                Nuyen = createDto.Nuyen ?? 0,
                Karma = createDto.Karma ?? 0
            };

            var created = await _databaseService.CreateCharacterAsync(character);
            _logger.LogInformation("Created character {CharacterName} via service", created.Name);

            return MapToDto(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create character");
            throw;
        }
    }

    /// <summary>
    /// Update an existing character
    /// </summary>
    public async Task<CharacterDto?> UpdateCharacterAsync(int id, UpdateCharacterDto updateDto)
    {
        try
        {
            var character = await _databaseService.GetCharacterByIdAsync(id);
            if (character == null)
            {
                return null;
            }

            // Update fields if provided
            if (updateDto.Name != null) character.Name = updateDto.Name;
            if (updateDto.Metatype != null) character.Metatype = updateDto.Metatype;
            if (updateDto.Body.HasValue) character.Body = updateDto.Body.Value;
            // SR3 FIX: Agility maps to Quickness
            if (updateDto.Agility.HasValue) character.Quickness = updateDto.Agility.Value;
            // SR3 FIX: Reaction is calculated, can't be set directly - ignoring
            if (updateDto.Strength.HasValue) character.Strength = updateDto.Strength.Value;
            if (updateDto.Charisma.HasValue) character.Charisma = updateDto.Charisma.Value;
            // SR3 FIX: Intuition/Logic map to Intelligence
            if (updateDto.Intuition.HasValue) character.Intelligence = updateDto.Intuition.Value;
            if (updateDto.Logic.HasValue) character.Intelligence = updateDto.Logic.Value;
            if (updateDto.Willpower.HasValue) character.Willpower = updateDto.Willpower.Value;
            // SR3 FIX: Edge doesn't exist in SR3 - ignoring
            if (updateDto.Magic.HasValue) character.Magic = updateDto.Magic.Value;
            // SR3 FIX: Resonance doesn't exist in SR3 - ignoring
            if (updateDto.Essence.HasValue) character.Essence = (int)(updateDto.Essence.Value * 100);
            if (updateDto.Nuyen.HasValue) character.Nuyen = updateDto.Nuyen.Value;
            if (updateDto.Karma.HasValue) character.Karma = updateDto.Karma.Value;

            var updated = await _databaseService.UpdateCharacterAsync(character);
            _logger.LogInformation("Updated character {CharacterId}", id);

            return MapToDto(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update character {CharacterId}", id);
            throw;
        }
    }

    /// <summary>
    /// Delete a character
    /// </summary>
    public async Task<bool> DeleteCharacterAsync(int id)
    {
        try
        {
            var deleted = await _databaseService.DeleteCharacterAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Deleted character {CharacterId} via service", id);
            }
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete character {CharacterId}", id);
            throw;
        }
    }

    #endregion

    #region Character Relationships

    /// <summary>
    /// Get character skills
    /// </summary>
    public async Task<List<CharacterSkillDto>> GetCharacterSkillsAsync(int characterId)
    {
        try
        {
            var character = await _databaseService.GetCharacterByIdAsync(characterId);
            if (character == null)
            {
                throw new InvalidOperationException($"Character {characterId} not found");
            }

            return character.Skills?.Select(s => new CharacterSkillDto
            {
                Id = s.Id,
                SkillName = s.SkillName,
                Rating = s.Rating,
                Specialization = s.Specialization,
                Attribute = s.Attribute
            }).ToList() ?? new List<CharacterSkillDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get skills for character {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Get character cyberware
    /// </summary>
    public async Task<List<CharacterCyberwareDto>> GetCharacterCyberwareAsync(int characterId)
    {
        try
        {
            var character = await _databaseService.GetCharacterByIdAsync(characterId);
            if (character == null)
            {
                throw new InvalidOperationException($"Character {characterId} not found");
            }

            return character.Cyberware?.Select(c => new CharacterCyberwareDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                Rating = c.Rating,
                EssenceCost = c.EssenceCost,
                Grade = c.Grade,
                Description = c.Description
            }).ToList() ?? new List<CharacterCyberwareDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cyberware for character {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Get character spells
    /// </summary>
    public async Task<List<CharacterSpellDto>> GetCharacterSpellsAsync(int characterId)
    {
        try
        {
            var character = await _databaseService.GetCharacterByIdAsync(characterId);
            if (character == null)
            {
                throw new InvalidOperationException($"Character {characterId} not found");
            }

            return character.Spells?.Select(s => new CharacterSpellDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Type = s.Type,
                Range = s.Range,
                Damage = s.Damage,
                Duration = s.Duration,
                DrainValue = s.DrainValue,
                Description = s.Description
            }).ToList() ?? new List<CharacterSpellDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get spells for character {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Get character gear
    /// </summary>
    public async Task<List<CharacterGearDto>> GetCharacterGearAsync(int characterId)
    {
        try
        {
            var character = await _databaseService.GetCharacterByIdAsync(characterId);
            if (character == null)
            {
                throw new InvalidOperationException($"Character {characterId} not found");
            }

            return character.Gear?.Select(g => new CharacterGearDto
            {
                Id = g.Id,
                Name = g.Name,
                Category = g.Category,
                Quantity = g.Quantity,
                Rating = g.Rating,
                Cost = g.Cost,
                Description = g.Description
            }).ToList() ?? new List<CharacterGearDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get gear for character {CharacterId}", characterId);
            throw;
        }
    }

    #endregion

    #region Private Helpers

    private CharacterSummaryDto MapToSummaryDto(ShadowrunCharacter c)
    {
        return new CharacterSummaryDto
        {
            Id = c.Id,
            DiscordUserId = c.DiscordUserId,
            Name = c.Name,
            Metatype = c.Metatype,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            SkillsCount = c.Skills?.Count ?? 0,
            CyberwareCount = c.Cyberware?.Count ?? 0,
            SpellsCount = c.Spells?.Count ?? 0,
            GearCount = c.Gear?.Count ?? 0,
            Body = c.Body,
            // SR3 FIX: Map Quickness to Agility for SR4/5 compatibility
            Agility = c.Quickness,
            Reaction = c.Reaction,
            Strength = c.Strength,
            Charisma = c.Charisma,
            // SR3 FIX: SR3 has Intelligence, not Intuition/Logic
            Intuition = c.Intelligence,
            Logic = c.Intelligence,
            Willpower = c.Willpower,
            // SR3 FIX: Edge doesn't exist in SR3, default to 0
            Edge = 0,
            Magic = c.Magic,
            // SR3 FIX: Resonance doesn't exist in SR3, default to 0
            Resonance = 0,
            Essence = c.EssenceDecimal
        };
    }

    private CharacterDto MapToDto(ShadowrunCharacter c)
    {
        return new CharacterDto
        {
            Id = c.Id,
            DiscordUserId = c.DiscordUserId,
            Name = c.Name,
            Metatype = c.Metatype,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Body = c.Body,
            // SR3 FIX: Map Quickness to Agility for SR4/5 compatibility
            Agility = c.Quickness,
            Reaction = c.Reaction,
            Strength = c.Strength,
            Charisma = c.Charisma,
            // SR3 FIX: SR3 has Intelligence, not Intuition/Logic
            Intuition = c.Intelligence,
            Logic = c.Intelligence,
            Willpower = c.Willpower,
            // SR3 FIX: Edge doesn't exist in SR3, default to 0
            Edge = 0,
            Magic = c.Magic,
            // SR3 FIX: Resonance doesn't exist in SR3, default to 0
            Resonance = 0,
            Essence = c.EssenceDecimal,
            Nuyen = c.Nuyen,
            Karma = c.Karma,
            Skills = c.Skills?.Select(s => new CharacterSkillDto
            {
                Id = s.Id,
                SkillName = s.SkillName,
                Rating = s.Rating,
                Specialization = s.Specialization,
                Attribute = s.Attribute
            }).ToList(),
            Cyberware = c.Cyberware?.Select(cy => new CharacterCyberwareDto
            {
                Id = cy.Id,
                Name = cy.Name,
                Type = cy.Type,
                Rating = cy.Rating,
                EssenceCost = cy.EssenceCost,
                Grade = cy.Grade,
                Description = cy.Description
            }).ToList(),
            Spells = c.Spells?.Select(sp => new CharacterSpellDto
            {
                Id = sp.Id,
                Name = sp.Name,
                Category = sp.Category,
                Type = sp.Type,
                Range = sp.Range,
                Damage = sp.Damage,
                Duration = sp.Duration,
                DrainValue = sp.DrainValue,
                Description = sp.Description
            }).ToList(),
            Spirits = c.Spirits?.Select(sp => new CharacterSpiritDto
            {
                Id = sp.Id,
                Name = sp.Name,
                Type = sp.Type,
                Force = sp.Force,
                Services = sp.Services,
                Bound = sp.Bound
            }).ToList(),
            Gear = c.Gear?.Select(g => new CharacterGearDto
            {
                Id = g.Id,
                Name = g.Name,
                Category = g.Category,
                Quantity = g.Quantity,
                Rating = g.Rating,
                Cost = g.Cost,
                Description = g.Description
            }).ToList()
        };
    }

    #endregion
}

#region DTOs

/// <summary>
/// Summary DTO for character list views
/// </summary>
public class CharacterSummaryDto
{
    public int Id { get; set; }
    public ulong DiscordUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Metatype { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int SkillsCount { get; set; }
    public int CyberwareCount { get; set; }
    public int SpellsCount { get; set; }
    public int GearCount { get; set; }
    public int Body { get; set; }
    public int Agility { get; set; }
    public int Reaction { get; set; }
    public int Strength { get; set; }
    public int Charisma { get; set; }
    public int Intuition { get; set; }
    public int Logic { get; set; }
    public int Willpower { get; set; }
    public int Edge { get; set; }
    public int Magic { get; set; }
    public int Resonance { get; set; }
    public decimal Essence { get; set; }
}

/// <summary>
/// Full character DTO with all relationships
/// </summary>
public class CharacterDto
{
    public int Id { get; set; }
    public ulong DiscordUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Metatype { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Body { get; set; }
    public int Agility { get; set; }
    public int Reaction { get; set; }
    public int Strength { get; set; }
    public int Charisma { get; set; }
    public int Intuition { get; set; }
    public int Logic { get; set; }
    public int Willpower { get; set; }
    public int Edge { get; set; }
    public int Magic { get; set; }
    public int Resonance { get; set; }
    public decimal Essence { get; set; }
    public int Nuyen { get; set; }
    public int Karma { get; set; }
    public List<CharacterSkillDto>? Skills { get; set; }
    public List<CharacterCyberwareDto>? Cyberware { get; set; }
    public List<CharacterSpellDto>? Spells { get; set; }
    public List<CharacterSpiritDto>? Spirits { get; set; }
    public List<CharacterGearDto>? Gear { get; set; }
}

/// <summary>
/// DTO for creating a new character
/// </summary>
public class CreateCharacterDto
{
    public ulong DiscordUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Metatype { get; set; }
    public int? Body { get; set; }
    public int? Agility { get; set; }
    public int? Reaction { get; set; }
    public int? Strength { get; set; }
    public int? Charisma { get; set; }
    public int? Intuition { get; set; }
    public int? Logic { get; set; }
    public int? Willpower { get; set; }
    public int? Edge { get; set; }
    public int? Magic { get; set; }
    public int? Resonance { get; set; }
    public decimal? Essence { get; set; }
    public int? Nuyen { get; set; }
    public int? Karma { get; set; }
}

/// <summary>
/// DTO for updating an existing character
/// </summary>
public class UpdateCharacterDto
{
    public string? Name { get; set; }
    public string? Metatype { get; set; }
    public int? Body { get; set; }
    public int? Agility { get; set; }
    public int? Reaction { get; set; }
    public int? Strength { get; set; }
    public int? Charisma { get; set; }
    public int? Intuition { get; set; }
    public int? Logic { get; set; }
    public int? Willpower { get; set; }
    public int? Edge { get; set; }
    public int? Magic { get; set; }
    public int? Resonance { get; set; }
    public decimal? Essence { get; set; }
    public int? Nuyen { get; set; }
    public int? Karma { get; set; }
}

/// <summary>
/// DTO for character skill
/// </summary>
public class CharacterSkillDto
{
    public int Id { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Specialization { get; set; }
    public string? Attribute { get; set; }
}

/// <summary>
/// DTO for character cyberware
/// </summary>
public class CharacterCyberwareDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int Rating { get; set; }
    public decimal EssenceCost { get; set; }
    public string? Grade { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// DTO for character spell
/// </summary>
public class CharacterSpellDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Type { get; set; }
    public string? Range { get; set; }
    public string? Damage { get; set; }
    public string? Duration { get; set; }
    public int DrainValue { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// DTO for character spirit
/// </summary>
public class CharacterSpiritDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public int Force { get; set; }
    public int Services { get; set; }
    public bool Bound { get; set; }
}

/// <summary>
/// DTO for character gear
/// </summary>
public class CharacterGearDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int Quantity { get; set; }
    public int Rating { get; set; }
    public int Cost { get; set; }
    public string? Description { get; set; }
}

#endregion
