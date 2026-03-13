using Microsoft.AspNetCore.Mvc;
using ShadowrunDiscordBot.Services;

namespace ShadowrunDiscordBot.Controllers;

/// <summary>
/// API controller for character management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CharacterController : ControllerBase
{
    private readonly CharacterService _characterService;
    private readonly ILogger<CharacterController> _logger;

    public CharacterController(
        CharacterService characterService,
        ILogger<CharacterController> logger)
    {
        _characterService = characterService;
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
            var characters = await _characterService.GetAllCharactersAsync(userId);
            return Ok(new { success = true, characters });
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
            var character = await _characterService.GetCharacterByIdAsync(id);

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
            var skills = await _characterService.GetCharacterSkillsAsync(id);
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
            var cyberware = await _characterService.GetCharacterCyberwareAsync(id);
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
            var spells = await _characterService.GetCharacterSpellsAsync(id);
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

            var createDto = new CreateCharacterDto
            {
                DiscordUserId = request.DiscordUserId,
                Name = request.Name,
                Metatype = request.Metatype,
                Body = request.Body,
                Agility = request.Agility,
                Reaction = request.Reaction,
                Strength = request.Strength,
                Charisma = request.Charisma,
                Intuition = request.Intuition,
                Logic = request.Logic,
                Willpower = request.Willpower,
                Edge = request.Edge,
                Magic = request.Magic,
                Resonance = request.Resonance,
                Essence = request.Essence
            };

            var character = await _characterService.CreateCharacterAsync(createDto);

            _logger.LogInformation("Created character {CharacterName} via API", character.Name);

            return Ok(new { success = true, character });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
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
            var updateDto = new UpdateCharacterDto
            {
                Name = request.Name,
                Body = request.Body,
                Agility = request.Agility,
                Reaction = request.Reaction,
                Strength = request.Strength,
                Charisma = request.Charisma,
                Intuition = request.Intuition,
                Logic = request.Logic,
                Willpower = request.Willpower,
                Edge = request.Edge,
                Magic = request.Magic,
                Resonance = request.Resonance,
                Essence = request.Essence
            };

            var character = await _characterService.UpdateCharacterAsync(id, updateDto);

            if (character == null)
            {
                return NotFound(new { success = false, error = "Character not found" });
            }

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
            var deleted = await _characterService.DeleteCharacterAsync(id);

            if (!deleted)
            {
                return NotFound(new { success = false, error = "Character not found" });
            }

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
