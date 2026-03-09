using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Service for Astral Space operations in SR3
/// </summary>
public class AstralService
{
    private readonly DiceService _diceService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<AstralService> _logger;

    public AstralService(
        DiceService diceService,
        DatabaseService databaseService,
        ILogger<AstralService> logger)
    {
        _diceService = diceService;
        _databaseService = databaseService;
        _logger = logger;
    }

    #region Astral Projection

    /// <summary>
    /// Initiate astral projection for a magician
    /// </summary>
    public async Task<AstralResult> BeginProjectionAsync(int characterId)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return AstralResult.Fail("Character not found.");

        if (!character.IsAwakened())
            return AstralResult.Fail("Only awakened characters can project astrally.");

        if (character.Magic <= 0)
            return AstralResult.Fail("Character has no Magic rating.");

        // Calculate astral attributes
        var astralState = new AstralCombatState
        {
            CharacterId = characterId,
            IsProjecting = true,
            ProjectionStartTime = DateTime.UtcNow,
            HasAstralPerception = true,
            // Astral attributes in SR3
            AstralBody = (character.Charisma + character.Intelligence + character.Willpower) / 3,
            AstralStrength = character.Charisma,
            AstralQuickness = character.Intelligence
        };

        await _databaseService.AddAstralStateAsync(astralState);

        _logger.LogInformation("Character {CharId} began astral projection", characterId);

        return AstralResult.Ok($"Began astral projection. Astral Body: {astralState.AstralBody}, " +
            $"Astral Strength: {astralState.AstralStrength}, Astral Quickness: {astralState.AstralQuickness}");
    }

    /// <summary>
    /// End astral projection
    /// </summary>
    public async Task<AstralResult> EndProjectionAsync(int characterId)
    {
        var astralState = await _databaseService.GetAstralStateAsync(characterId);
        if (astralState == null || !astralState.IsProjecting)
            return AstralResult.Fail("Character is not projecting.");

        // Calculate time projected
        var hoursProjected = (DateTime.UtcNow - astralState.ProjectionStartTime)?.TotalHours ?? 0;

        // In SR3, projecting too long can cause stun damage
        if (hoursProjected > character.Magic)
        {
            var excessHours = (int)(hoursProjected - character.Magic);
            var stunDamage = excessHours * 2; // 2 boxes per hour over Magic rating
            return AstralResult.Fail($"Projection exceeded safe duration. Character takes {stunDamage} stun damage upon return.");
        }

        astralState.IsProjecting = false;
        await _databaseService.UpdateAstralStateAsync(astralState);

        return AstralResult.Ok($"Ended astral projection after {hoursProjected:F1} hours.");
    }

    /// <summary>
    /// Activate astral perception (for astral sight without projection)
    /// </summary>
    public async Task<AstralResult> ActivatePerceptionAsync(int characterId)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return AstralResult.Fail("Character not found.");

        if (!character.IsAwakened())
            return AstralResult.Fail("Only awakened characters can use astral perception.");

        var astralState = await _databaseService.GetAstralStateAsync(characterId);
        if (astralState == null)
        {
            astralState = new AstralCombatState
            {
                CharacterId = characterId,
                HasAstralPerception = true
            };
            await _databaseService.AddAstralStateAsync(astralState);
        }
        else
        {
            astralState.HasAstralPerception = true;
            await _databaseService.UpdateAstralStateAsync(astralState);
        }

        return AstralResult.Ok("Astral perception activated. You can now see the astral plane.");
    }

    #endregion

    #region Astral Combat

    /// <summary>
    /// Execute astral combat attack
    /// </summary>
    public async Task<AstralCombatResult> AstralAttackAsync(
        int attackerId,
        string targetName,
        int poolDice,
        int defensePool = 0)
    {
        var attacker = await _databaseService.GetCharacterByIdAsync(attackerId);
        if (attacker == null)
            return AstralCombatResult.Fail("Attacker not found.");

        var attackerAstral = await _databaseService.GetAstralStateAsync(attackerId);
        if (attackerAstral == null || (!attackerAstral.IsProjecting && !attackerAstral.HasAstralPerception))
            return AstralCombatResult.Fail("Attacker is not in astral space.");

        // Astral combat pool = Astral Combat Skill + Astral Combat Pool
        var attackResult = _diceService.RollShadowrun(poolDice, 4);

        int defenseSuccesses = 0;
        if (defensePool > 0)
        {
            var defenseResult = _diceService.RollShadowrun(defensePool, 4);
            defenseSuccesses = defenseResult.Successes;
        }

        var netSuccesses = Math.Max(0, attackResult.Successes - defenseSuccesses);

        // Astral damage = Astral Strength + Net Successes
        var damage = attackerAstral.AstralStrength + netSuccesses;

        // Staging (every 2 net successes stages damage up one level)
        var damageLevel = CalculateDamageLevel(netSuccesses);

        _logger.LogInformation("Astral attack: {Attacker} -> {Target}, Net: {Net}, Damage: {Damage} ({Level})",
            attacker.Name, targetName, netSuccesses, damage, damageLevel);

        return new AstralCombatResult
        {
            Success = true,
            AttackSuccesses = attackResult.Successes,
            DefenseSuccesses = defenseSuccesses,
            NetSuccesses = netSuccesses,
            Damage = damage,
            DamageLevel = damageLevel,
            Details = $"Astral attack on {targetName}: {attackResult.Details}\n" +
                $"Damage: {damage} {damageLevel}"
        };
    }

    /// <summary>
    /// Resist astral damage
    /// </summary>
    public async Task<DamageResult> ResistAstralDamageAsync(int characterId, int damage, string damageLevel)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return DamageResult.Fail("Character not found.");

        var astralState = await _databaseService.GetAstralStateAsync(characterId);
        if (astralState == null)
            return DamageResult.Fail("Character has no astral state.");

        // Astral damage resistance = Willpower
        var resistancePool = character.Willpower;
        var resistanceResult = _diceService.RollShadowrun(resistancePool, 4);

        var actualDamage = Math.Max(0, damage - resistanceResult.Successes);

        // Apply damage to astral track
        astralState.AstralDamage += actualDamage;

        // Check for knockback/unconsciousness
        if (astralState.AstralDamage >= astralState.AstralConditionMonitor)
        {
            // Character forced back to body
            astralState.IsProjecting = false;
            astralState.AstralDamage = astralState.AstralConditionMonitor;
        }

        await _databaseService.UpdateAstralStateAsync(astralState);

        return new DamageResult
        {
            Success = true,
            DamageType = "Astral",
            DamageInflicted = actualDamage,
            ResistedDamage = resistanceResult.Successes,
            CurrentDamage = astralState.AstralDamage,
            MaxDamage = astralState.AstralConditionMonitor,
            Details = $"Resisted {resistanceResult.Successes} damage. Took {actualDamage} astral damage."
        };
    }

    private string CalculateDamageLevel(int netSuccesses)
    {
        return netSuccesses switch
        {
            0 => "Light",
            1 => "Light",
            2 => "Moderate",
            3 => "Moderate",
            4 => "Serious",
            5 => "Serious",
            _ => "Deadly"
        };
    }

    #endregion

    #region Astral Signatures

    /// <summary>
    /// Detect astral signatures in an area
    /// </summary>
    public async Task<AstralSignatureResult> DetectSignaturesAsync(int characterId, string location)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return AstralSignatureResult.Fail("Character not found.");

        var astralState = await _databaseService.GetAstralStateAsync(characterId);
        if (astralState == null || (!astralState.IsProjecting && !astralState.HasAstralPerception))
            return AstralSignatureResult.Fail("Astral perception required.");

        // Assensing test = Assensing Skill + Intelligence
        var pool = character.Intelligence; // Simplified - should include Assensing skill
        var result = _diceService.RollShadowrun(pool, 4);

        var signatures = await _databaseService.GetAstralSignaturesAtLocationAsync(location);

        // Only detect signatures with Force <= successes
        var detectedSignatures = signatures.Where(s => s.Force <= result.Successes).ToList();

        return new AstralSignatureResult
        {
            Success = true,
            Successes = result.Successes,
            SignaturesDetected = detectedSignatures.Count,
            Signatures = detectedSignatures,
            Details = $"Detected {detectedSignatures.Count} astral signatures."
        };
    }

    /// <summary>
    /// Leave an astral signature (from spell casting, summoning, etc.)
    /// </summary>
    public async Task LeaveSignatureAsync(int characterId, string location, string signatureType, int force)
    {
        var signature = new AstralSignatureRecord
        {
            Location = location,
            SignatureType = signatureType,
            Force = force,
            CharacterId = characterId,
            DetectedAt = DateTime.UtcNow,
            HoursToFade = force * 6 // Higher force = lasts longer
        };

        await _databaseService.AddAstralSignatureAsync(signature);
    }

    #endregion

    #region Foci

    /// <summary>
    /// Bond a focus to a character
    /// </summary>
    public async Task<FocusResult> BondFocusAsync(int characterId, string focusName, string focusType, int force)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return FocusResult.Fail("Character not found.");

        if (!character.IsAwakened())
            return FocusResult.Fail("Only awakened characters can bond foci.");

        // Karma cost = Force
        var karmaCost = KarmaCosts.BindFocus(force);
        if (character.Karma < karmaCost)
            return FocusResult.Fail($"Insufficient karma. Need {karmaCost}, have {character.Karma}.");

        var focus = new CharacterFocus
        {
            CharacterId = characterId,
            Name = focusName,
            FocusType = focusType,
            Force = force,
            IsBonded = true,
            IsActive = false,
            BondingCost = karmaCost,
            EssenceCost = force * 0.1m // Foci cost essence when active
        };

        character.Karma -= karmaCost;

        await _databaseService.AddFocusAsync(focus);
        await _databaseService.UpdateCharacterAsync(character);

        return FocusResult.Ok($"Bonded {focusName} (Force {force}). Spent {karmaCost} karma.");
    }

    /// <summary>
    /// Activate a focus
    /// </summary>
    public async Task<FocusResult> ActivateFocusAsync(int characterId, int focusId)
    {
        var focus = await _databaseService.GetFocusAsync(focusId);
        if (focus == null || focus.CharacterId != characterId)
            return FocusResult.Fail("Focus not found or not owned.");

        if (!focus.IsBonded)
            return FocusResult.Fail("Focus must be bonded before activation.");

        focus.IsActive = true;
        await _databaseService.UpdateFocusAsync(focus);

        return FocusResult.Ok($"Activated {focus.Name}. Essence cost: {focus.EssenceCost}");
    }

    /// <summary>
    /// Deactivate a focus
    /// </summary>
    public async Task<FocusResult> DeactivateFocusAsync(int characterId, int focusId)
    {
        var focus = await _databaseService.GetFocusAsync(focusId);
        if (focus == null || focus.CharacterId != characterId)
            return FocusResult.Fail("Focus not found or not owned.");

        focus.IsActive = false;
        await _databaseService.UpdateFocusAsync(focus);

        return FocusResult.Ok($"Deactivated {focus.Name}.");
    }

    #endregion

    #region Spirit Forms

    /// <summary>
    /// Check if a spirit can materialize
    /// </summary>
    public bool CanMaterialize(CharacterSpirit spirit)
    {
        // All spirits can materialize in SR3
        return spirit.ServicesOwed > 0;
    }

    /// <summary>
    /// Handle spirit in astral vs materialized form
    /// </summary>
    public string GetSpiritFormState(CharacterSpirit spirit)
    {
        // Spirits exist on the astral plane by default
        // Materialized spirits can interact with physical world
        return $"Spirit: {spirit.SpiritType} (Force {spirit.Force})\n" +
               $"Form: Astral (can materialize)\n" +
               $"Services: {spirit.ServicesOwed}";
    }

    #endregion
}

#region Result Types

public record AstralResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static AstralResult Ok(string message) => new() { Success = true, Message = message };
    public static AstralResult Fail(string message) => new() { Success = false, Message = message };
}

public record AstralCombatResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public int AttackSuccesses { get; init; }
    public int DefenseSuccesses { get; init; }
    public int NetSuccesses { get; init; }
    public int Damage { get; init; }
    public string DamageLevel { get; init; } = "Light";

    public static AstralCombatResult Fail(string details) => new() { Success = false, Details = details };
}

public record DamageResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string DamageType { get; init; } = "Physical";
    public int DamageInflicted { get; init; }
    public int ResistedDamage { get; init; }
    public int CurrentDamage { get; init; }
    public int MaxDamage { get; init; }

    public static DamageResult Fail(string details) => new() { Success = false, Details = details };
}

public record AstralSignatureResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public int Successes { get; init; }
    public int SignaturesDetected { get; init; }
    public List<AstralSignatureRecord> Signatures { get; init; } = new();

    public static AstralSignatureResult Fail(string details) => new() { Success = false, Details = details };
}

public record FocusResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static FocusResult Ok(string message) => new() { Success = true, Message = message };
    public static FocusResult Fail(string message) => new() { Success = false, Message = message };
}

#endregion
