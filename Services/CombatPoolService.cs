using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Combat Pool management for SR3
/// Combat Pool = (Quickness + Intelligence + Willpower) / 2
/// </summary>
public class CombatPoolService
{
    private readonly DiceService _diceService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<CombatPoolService> _logger;

    public CombatPoolService(
        DiceService diceService,
        DatabaseService databaseService,
        ILogger<CombatPoolService> logger)
    {
        _diceService = diceService;
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate total combat pool for a character
    /// </summary>
    public int CalculateCombatPool(ShadowrunCharacter character)
    {
        // SR3: Combat Pool = (Quickness + Intelligence + Willpower) / 2
        return (character.Quickness + character.Intelligence + character.Willpower) / 2;
    }

    /// <summary>
    /// Initialize combat pool for a combat session
    /// </summary>
    public async Task<CombatPoolState> InitializePoolAsync(int characterId, int combatSessionId)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            throw new InvalidOperationException("Character not found.");

        var totalPool = CalculateCombatPool(character);

        var poolState = new CombatPoolState
        {
            CharacterId = characterId,
            CombatSessionId = combatSessionId,
            TotalPool = totalPool,
            AllocatedToAttack = 0,
            AllocatedToDefense = 0,
            AllocatedToDamage = 0,
            AllocatedToOther = 0,
            CurrentTurn = 1
        };

        await _databaseService.AddCombatPoolStateAsync(poolState);

        _logger.LogInformation("Initialized combat pool for {CharName}: {Pool} dice",
            character.Name, totalPool);

        return poolState;
    }

    /// <summary>
    /// Allocate combat pool dice
    /// </summary>
    public async Task<PoolAllocationResult> AllocatePoolAsync(
        int poolStateId,
        int attackDice = 0,
        int defenseDice = 0,
        int damageDice = 0,
        int otherDice = 0)
    {
        var poolState = await _databaseService.GetCombatPoolStateAsync(poolStateId);
        if (poolState == null)
            return PoolAllocationResult.Fail("Combat pool state not found.");

        var totalAllocated = attackDice + defenseDice + damageDice + otherDice;

        // Check if allocation exceeds available pool
        if (totalAllocated > poolState.TotalPool)
        {
            return PoolAllocationResult.Fail(
                $"Cannot allocate {totalAllocated} dice. Maximum pool: {poolState.TotalPool}");
        }

        poolState.AllocatedToAttack = attackDice;
        poolState.AllocatedToDefense = defenseDice;
        poolState.AllocatedToDamage = damageDice;
        poolState.AllocatedToOther = otherDice;

        await _databaseService.UpdateCombatPoolStateAsync(poolState);

        return new PoolAllocationResult
        {
            Success = true,
            TotalPool = poolState.TotalPool,
            AllocatedAttack = attackDice,
            AllocatedDefense = defenseDice,
            AllocatedDamage = damageDice,
            AllocatedOther = otherDice,
            RemainingPool = poolState.TotalPool - totalAllocated,
            Details = FormatAllocation(poolState)
        };
    }

    /// <summary>
    /// Use combat pool dice for an action
    /// </summary>
    public async Task<PoolUsageResult> UsePoolDiceAsync(
        int poolStateId,
        string usageType,
        int diceRequested,
        int skillRating = 0)
    {
        var poolState = await _databaseService.GetCombatPoolStateAsync(poolStateId);
        if (poolState == null)
            return PoolUsageResult.Fail("Combat pool state not found.");

        // Get available dice based on usage type
        var availableDice = usageType.ToLower() switch
        {
            "attack" => poolState.AllocatedToAttack,
            "defense" => poolState.AllocatedToDefense,
            "damage" => poolState.AllocatedToDamage,
            "other" => poolState.AllocatedToOther,
            _ => 0
        };

        if (diceRequested > availableDice)
            return PoolUsageResult.Fail($"Only {availableDice} dice allocated for {usageType}");

        // Roll the dice
        var totalDice = skillRating + diceRequested;
        var result = _diceService.RollShadowrun(totalDice, 4);

        // Log usage
        var usage = new CombatPoolUsage
        {
            CombatPoolStateId = poolStateId,
            UsageType = usageType,
            DiceUsed = diceRequested,
            Successes = result.Successes,
            Description = $"Used {diceRequested} combat pool for {usageType}"
        };
        await _databaseService.AddCombatPoolUsageAsync(usage);

        // Reduce allocated pool
        switch (usageType.ToLower())
        {
            case "attack":
                poolState.AllocatedToAttack -= diceRequested;
                break;
            case "defense":
                poolState.AllocatedToDefense -= diceRequested;
                break;
            case "damage":
                poolState.AllocatedToDamage -= diceRequested;
                break;
            case "other":
                poolState.AllocatedToOther -= diceRequested;
                break;
        }

        await _databaseService.UpdateCombatPoolStateAsync(poolState);

        _logger.LogDebug("Combat pool used: {Dice} for {Type}, got {Successes} successes",
            diceRequested, usageType, result.Successes);

        return new PoolUsageResult
        {
            Success = true,
            UsageType = usageType,
            DiceUsed = diceRequested,
            SkillRating = skillRating,
            TotalDice = totalDice,
            Successes = result.Successes,
            Details = result.Details
        };
    }

    /// <summary>
    /// Refresh combat pool for new turn
    /// </summary>
    public async Task RefreshPoolAsync(int poolStateId, int newTurn)
    {
        var poolState = await _databaseService.GetCombatPoolStateAsync(poolStateId);
        if (poolState == null) return;

        // Pool refreshes each Combat Turn
        poolState.AllocatedToAttack = 0;
        poolState.AllocatedToDefense = 0;
        poolState.AllocatedToDamage = 0;
        poolState.AllocatedToOther = 0;
        poolState.CurrentTurn = newTurn;

        await _databaseService.UpdateCombatPoolStateAsync(poolState);

        _logger.LogDebug("Combat pool refreshed for turn {Turn}", newTurn);
    }

    /// <summary>
    /// Get combat pool status
    /// </summary>
    public async Task<string> GetPoolStatusAsync(int characterId, int combatSessionId)
    {
        var poolState = await _databaseService.GetCombatPoolStateForCharacterAsync(characterId, combatSessionId);
        if (poolState == null)
            return "No combat pool initialized.";

        return FormatAllocation(poolState);
    }

    private string FormatAllocation(CombatPoolState state)
    {
        var remaining = state.TotalPool - state.AllocatedToAttack - state.AllocatedToDefense -
                        state.AllocatedToDamage - state.AllocatedToOther;

        return $"**Combat Pool (Turn {state.CurrentTurn})**\n" +
               $"Total: {state.TotalPool} dice\n" +
               $"Allocated:\n" +
               $"  ⚔️ Attack: {state.AllocatedToAttack}\n" +
               $"  🛡️ Defense: {state.AllocatedToDefense}\n" +
               $"  💥 Damage: {state.AllocatedToDamage}\n" +
               $"  📌 Other: {state.AllocatedToOther}\n" +
               $"Remaining: {remaining}";
    }
}

/// <summary>
/// Hacking pool for deckers
/// </summary>
public class HackingPoolService
{
    private readonly DiceService _diceService;

    public HackingPoolService(DiceService diceService)
    {
        _diceService = diceService;
    }

    /// <summary>
    /// Calculate hacking pool for a decker
    /// </summary>
    public int CalculateHackingPool(ShadowrunCharacter character, Cyberdeck deck)
    {
        // SR3: Hacking Pool = MPCP / 2 (rounded up)
        return (deck.MPCP + 1) / 2;
    }
}

/// <summary>
/// Magic pool for magicians
/// </summary>
public class MagicPoolService
{
    private readonly DiceService _diceService;

    public MagicPoolService(DiceService diceService)
    {
        _diceService = diceService;
    }

    /// <summary>
    /// Calculate magic pool for a magician
    /// </summary>
    public int CalculateMagicPool(ShadowrunCharacter character)
    {
        if (!character.IsAwakened()) return 0;

        // SR3: Magic Pool = Magic rating
        return character.Magic;
    }

    /// <summary>
    /// Calculate astral combat pool
    /// </summary>
    public int CalculateAstralCombatPool(ShadowrunCharacter character)
    {
        if (!character.IsAwakened()) return 0;

        // SR3: Astral Combat Pool = (Charisma + Intelligence + Willpower) / 2
        return (character.Charisma + character.Intelligence + character.Willpower) / 2;
    }
}

/// <summary>
/// Task pool for riggers
/// </summary>
public class TaskPoolService
{
    /// <summary>
    /// Calculate task pool for a rigger controlling drones
    /// </summary>
    public int CalculateTaskPool(ShadowrunCharacter character, int controlRigRating)
    {
        // SR3: Task Pool = Control Rig Rating x 2
        return controlRigRating * 2;
    }
}

#region Result Types

public record PoolAllocationResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public int TotalPool { get; init; }
    public int AllocatedAttack { get; init; }
    public int AllocatedDefense { get; init; }
    public int AllocatedDamage { get; init; }
    public int AllocatedOther { get; init; }
    public int RemainingPool { get; init; }

    public static PoolAllocationResult Fail(string details) => new() { Success = false, Details = details };
}

public record PoolUsageResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string UsageType { get; init; } = string.Empty;
    public int DiceUsed { get; init; }
    public int SkillRating { get; init; }
    public int TotalDice { get; init; }
    public int Successes { get; init; }

    public static PoolUsageResult Fail(string details) => new() { Success = false, Details = details };
}

#endregion
