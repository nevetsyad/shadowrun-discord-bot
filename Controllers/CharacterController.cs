using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Controllers;

/// <summary>
/// API controller for character management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CharacterController : ControllerBase
{
    private readonly ShadowrunDbContext _dbContext;
    private readonly ILogger<CharacterController> _logger;

    public CharacterController(ShadowrunDbContext dbContext, ILogger<CharacterController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all characters for a user
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllCharacters([FromQuery] ulong? userId)
    {
        try
        {
            var query = _dbContext.Characters
                .Include(c => c.Skills)
                .Include(c => c.Cyberware)
                .Include(c => c.Spells)
                .Include(c => c.Gear)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(c => c.DiscordUserId == userId.Value);
            }

            var characters = await query
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                characters = characters.Select(c => new
                {
                    c.Id,
                    c.DiscordUserId,
                    c.Name,
                    c.Metatype,
                    c.CreatedAt,
                    c.UpdatedAt,
                    SkillsCount = c.Skills.Count,
                    CyberwareCount = c.Cyberware.Count,
                    SpellsCount = c.Spells.Count,
                    GearCount = c.Gear.Count,
                    // Attributes
                    c.Body,
                    c.Agility,
                    c.Reaction,
                    c.Strength,
                    c.Charisma,
                    c.Intuition,
                    c.Logic,
                    c.Willpower,
                    c.Edge,
                    c.Magic,
                    c.Resonance,
                    c.Essence
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific character by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCharacter(int id)
    {
        try
        {
            var character = await _dbContext.Characters
                .Include(c => c.Skills)
                .Include(c => c.Cyberware)
                .Include(c => c.Spells)
                .Include(c => c.Spirits)
                .Include(c => c.Gear)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (character == null)
            {
                return NotFound(new { success = false, error = "Character not found" });
            }

            return Ok(new { success = true, character });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character {CharacterId}", id);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get character skills
    /// </summary>
    [HttpGet("{id}/skills")]
    public async Task<IActionResult> GetCharacterSkills(int id)
    {
        try
        {
            var skills = await _dbContext.CharacterSkills
                .Where(s => s.CharacterId == id)
                .OrderBy(s => s.SkillName)
                .ToListAsync();

            return Ok(new { success = true, skills });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get skills for character {CharacterId}", id);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get character cyberware
    /// </summary>
    [HttpGet("{id}/cyberware")]
    public async Task<IActionResult> GetCharacterCyberware(int id)
    {
        try
        {
            var cyberware = await _dbContext.CharacterCyberware
                .Where(c => c.CharacterId == id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(new { success = true, cyberware });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cyberware for character {CharacterId}", id);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get character spells
    /// </summary>
    [HttpGet("{id}/spells")]
    public async Task<IActionResult> GetCharacterSpells(int id)
    {
        try
        {
            var spells = await _dbContext.CharacterSpells
                .Where(s => s.CharacterId == id)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Ok(new { success = true, spells });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get spells for character {CharacterId}", id);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new character
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCharacter([FromBody] CreateCharacterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { success = false, error = "Character name is required" });
            }

            // Check if user already has max characters
            var existingCount = await _dbContext.Characters
                .CountAsync(c => c.DiscordUserId == request.DiscordUserId);

            if (existingCount >= 5) // Default max
            {
                return BadRequest(new { success = false, error = "Maximum character limit reached" });
            }

            var character = new ShadowrunCharacter
            {
                DiscordUserId = request.DiscordUserId,
                Name = request.Name,
                Metatype = request.Metatype ?? "Human",
                Body = request.Body ?? 1,
                Agility = request.Agility ?? 1,
                Reaction = request.Reaction ?? 1,
                Strength = request.Strength ?? 1,
                Charisma = request.Charisma ?? 1,
                Intuition = request.Intuition ?? 1,
                Logic = request.Logic ?? 1,
                Willpower = request.Willpower ?? 1,
                Edge = request.Edge ?? 1,
                Magic = request.Magic ?? 0,
                Resonance = request.Resonance ?? 0,
                Essence = request.Essence ?? 6.0m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Characters.Add(character);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created character {CharacterName} via API", character.Name);

            return Ok(new { success = true, character });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create character");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Update a character
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCharacter(int id, [FromBody] UpdateCharacterRequest request)
    {
        try
        {
            var character = await _dbContext.Characters.FindAsync(id);
            if (character == null)
            {
                return NotFound(new { success = false, error = "Character not found" });
            }

            // Update fields if provided
            if (request.Name != null) character.Name = request.Name;
            if (request.Body.HasValue) character.Body = request.Body.Value;
            if (request.Agility.HasValue) character.Agility = request.Agility.Value;
            if (request.Reaction.HasValue) character.Reaction = request.Reaction.Value;
            if (request.Strength.HasValue) character.Strength = request.Strength.Value;
            if (request.Charisma.HasValue) character.Charisma = request.Charisma.Value;
            if (request.Intuition.HasValue) character.Intuition = request.Intuition.Value;
            if (request.Logic.HasValue) character.Logic = request.Logic.Value;
            if (request.Willpower.HasValue) character.Willpower = request.Willpower.Value;
            if (request.Edge.HasValue) character.Edge = request.Edge.Value;
            if (request.Magic.HasValue) character.Magic = request.Magic.Value;
            if (request.Resonance.HasValue) character.Resonance = request.Resonance.Value;
            if (request.Essence.HasValue) character.Essence = request.Essence.Value;

            character.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Updated character {CharacterId}", id);

            return Ok(new { success = true, character });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update character {CharacterId}", id);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a character
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCharacter(int id)
    {
        try
        {
            var character = await _dbContext.Characters.FindAsync(id);
            if (character == null)
            {
                return NotFound(new { success = false, error = "Character not found" });
            }

            _dbContext.Characters.Remove(character);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Deleted character {CharacterId} via API", id);

            return Ok(new { success = true, message = "Character deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete character {CharacterId}", id);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for creating a character
/// </summary>
public class CreateCharacterRequest
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
}

/// <summary>
/// Request model for updating a character
/// </summary>
public class UpdateCharacterRequest
{
    public string? Name { get; set; }
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
}
