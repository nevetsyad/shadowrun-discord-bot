using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// High-performance dice rolling service with Friedman dice algorithm and object pooling
/// </summary>
public sealed class DiceService : IDisposable
{
    private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
    private readonly object _rngLock = new();
    private readonly ILogger<DiceService>? _logger;
    private bool _disposed;

    // FIX: HIGH-003 - Add maximum pool size to prevent DoS attacks
    /// <summary>
    /// Maximum number of dice that can be rolled in a single pool to prevent resource exhaustion
    /// </summary>
    private const int MaxPoolSize = 100;

    // FIX: HIGH-004 - Reduce max iterations for exploding dice to prevent infinite loops
    /// <summary>
    /// Maximum iterations for exploding dice to prevent excessive computation
    /// </summary>
    private const int MaxExplodingIterations = 20;

    public DiceService(ILogger<DiceService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Roll a single die with specified sides using cryptographically secure RNG
    /// </summary>
    public int RollDie(int sides)
    {
        if (sides < 1)
            throw new ArgumentException("Die must have at least 1 side", nameof(sides));

        if (sides == 1)
            return 1;

        // Use rejection sampling for uniform distribution
        int bytesNeeded = (int)Math.Ceiling(Math.Log2(sides) / 8.0);
        int mask = (1 << (int)Math.Ceiling(Math.Log2(sides))) - 1;
        
        lock (_rngLock)
        {
            Span<byte> buffer = stackalloc byte[4];
            int result;
            
            do
            {
                _rng.GetBytes(buffer.Slice(0, bytesNeeded));
                result = BitConverter.ToInt32(buffer) & mask;
            } while (result >= sides);

            return result + 1;
        }
    }

    /// <summary>
    /// Roll multiple dice efficiently using pooled arrays
    /// </summary>
    public int[] RollDice(int count, int sides)
    {
        if (count < 0)
            throw new ArgumentException("Dice count must be non-negative", nameof(count));

        if (count == 0)
            return Array.Empty<int>();

        var results = new int[count];
        
        for (int i = 0; i < count; i++)
        {
            results[i] = RollDie(sides);
        }

        return results;
    }

    /// <summary>
    /// Parse standard dice notation (e.g., "2d6+3", "1d20", "4d6k3")
    /// </summary>
    public DiceResult ParseAndRoll(string notation)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(notation))
            {
                var ex = new ArgumentException("Dice notation cannot be empty", nameof(notation));
                _logger?.LogError(ex, "Empty dice notation provided");
                throw ex;
            }

            // Parse notation using Span for efficiency
            var span = notation.AsSpan().Trim();
            
            // Find 'd' or 'D'
            int dIndex = -1;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == 'd' || span[i] == 'D')
                {
                    dIndex = i;
                    break;
                }
            }

            if (dIndex == -1)
            {
                var ex = new ArgumentException("Invalid dice notation. Must contain 'd' (e.g., 2d6)", nameof(notation));
                _logger?.LogError(ex, "Invalid dice notation: {Notation}", notation);
                throw ex;
            }

            // Parse dice count
            var countSpan = span.Slice(0, dIndex);
            if (!int.TryParse(countSpan, out int diceCount))
                diceCount = 1; // Support "d6" notation

            // Parse remaining (sides + modifier)
            var remaining = span.Slice(dIndex + 1);
            int modifier = 0;
            int sides;
            bool keepHighest = false;
            int keepCount = 0;

            // Check for modifier (+ or -)
            int modIndex = -1;
            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] == '+' || remaining[i] == '-')
                {
                    modIndex = i;
                    break;
                }
            }

            // Check for keep notation (k3 = keep highest 3)
            int keepIndex = -1;
            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] == 'k' || remaining[i] == 'K')
                {
                    keepIndex = i;
                    break;
                }
            }

            // Parse sides
            ReadOnlySpan<char> sidesSpan;
            if (modIndex > 0)
                sidesSpan = remaining.Slice(0, modIndex);
            else if (keepIndex > 0)
                sidesSpan = remaining.Slice(0, keepIndex);
            else
                sidesSpan = remaining;

            if (!int.TryParse(sidesSpan, out sides))
            {
                var ex = new ArgumentException($"Invalid dice sides: {sidesSpan.ToString()}", nameof(notation));
                _logger?.LogError(ex, "Invalid dice sides in notation: {Notation}", notation);
                throw ex;
            }

            // Parse modifier
            if (modIndex > 0)
            {
                var modSpan = remaining.Slice(modIndex);
                if (!int.TryParse(modSpan, out modifier))
                {
                    var ex = new ArgumentException($"Invalid modifier: {modSpan.ToString()}", nameof(notation));
                    _logger?.LogError(ex, "Invalid modifier in notation: {Notation}", notation);
                    throw ex;
                }
            }

            // Parse keep count
            if (keepIndex > 0)
            {
                keepHighest = true;
                var keepSpan = remaining.Slice(keepIndex + 1);
                if (!int.TryParse(keepSpan, out keepCount))
                    keepCount = 1;
            }

            // Roll dice
            var rolls = RollDice(diceCount, sides);

            // Apply keep highest if specified
            int[] usedRolls = rolls;
            if (keepHighest && keepCount > 0 && keepCount < rolls.Length)
            {
                var sorted = rolls.OrderByDescending(r => r).ToArray();
                usedRolls = sorted.Take(keepCount).ToArray();
            }

            var total = usedRolls.Sum() + modifier;

            _logger?.LogDebug("Dice roll: {Notation} = {Total} (rolls: [{Rolls}])", 
                notation, total, string.Join(", ", rolls));

            return new DiceResult
            {
                Notation = notation,
                Rolls = rolls,
                UsedRolls = usedRolls,
                Modifier = modifier,
                Total = total,
                Details = FormatResult(rolls, usedRolls, modifier, keepHighest, keepCount)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error parsing and rolling dice: {Notation}", notation);
            throw;
        }
    }

    /// <summary>
    /// Shadowrun-specific dice rolling with success counting (5+ = success)
    /// </summary>
    public ShadowrunDiceResult RollShadowrun(int poolSize, int targetNumber = 4)
    {
        try
        {
            // FIX: HIGH-003 - Validate pool size bounds
            if (poolSize < 0)
            {
                var ex = new ArgumentException("Pool size must be non-negative", nameof(poolSize));
                _logger?.LogError(ex, "Invalid pool size: {PoolSize}", poolSize);
                throw ex;
            }

            if (poolSize > MaxPoolSize)
            {
                var ex = new ArgumentException($"Pool size cannot exceed {MaxPoolSize}", nameof(poolSize));
                _logger?.LogError(ex, "Pool size too large: {PoolSize}", poolSize);
                throw ex;
            }

            if (targetNumber < 2 || targetNumber > 6)
            {
                _logger?.LogWarning("Unusual target number: {TargetNumber}. Standard Shadowrun uses 2-6.", targetNumber);
            }

            if (poolSize == 0)
                return new ShadowrunDiceResult
                {
                    PoolSize = 0,
                    TargetNumber = targetNumber,
                    Successes = 0,
                    Rolls = Array.Empty<int>(),
                    Glitch = false,
                    CriticalGlitch = false,
                    Details = "No dice rolled"
                };

            var rolls = RollDice(poolSize, 6);
            var successes = rolls.Count(r => r >= targetNumber);
            var ones = rolls.Count(r => r == 1);

            // Glitch detection: more than half the dice show 1s
            var isGlitch = ones > (poolSize / 2.0);
            var isCriticalGlitch = isGlitch && successes == 0;

            _logger?.LogDebug("Shadowrun roll: pool={Pool}, target={Target}, successes={Successes}, glitch={Glitch}", 
                poolSize, targetNumber, successes, isGlitch);

            return new ShadowrunDiceResult
            {
                PoolSize = poolSize,
                TargetNumber = targetNumber,
                Successes = successes,
                Rolls = rolls,
                Glitch = isGlitch,
                CriticalGlitch = isCriticalGlitch,
                Details = FormatShadowrunResult(rolls, successes, targetNumber, isGlitch, isCriticalGlitch)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rolling Shadowrun dice: pool={Pool}, target={Target}", poolSize, targetNumber);
            throw;
        }
    }

    /// <summary>
    /// Roll initiative (Reaction + Initiative Dice)
    /// </summary>
    public InitiativeResult RollInitiative(int reaction, int initiativeDice = 1)
    {
        if (initiativeDice < 1)
            initiativeDice = 1;

        var rolls = RollDice(initiativeDice, 6);
        var diceTotal = rolls.Sum();
        var total = reaction + diceTotal;

        // Calculate initiative passes (based on total)
        int passes = total switch
        {
            >= 40 => 4,
            >= 30 => 3,
            >= 20 => 2,
            _ => 1
        };

        return new InitiativeResult
        {
            Reaction = reaction,
            DiceRolls = rolls,
            DiceTotal = diceTotal,
            Total = total,
            Passes = passes,
            Details = $"{reaction} + [{string.Join(" + ", rolls)}] = **{total}** ({passes} pass{(passes > 1 ? "es" : "")})"
        };
    }

    /// <summary>
    /// Roll with Edge (exploding sixes) - counts successes and rerolls sixes
    /// </summary>
    public ShadowrunDiceResult RollEdge(int poolSize, int targetNumber = 5)
    {
        // FIX: HIGH-003 - Validate pool size
        if (poolSize < 0)
            throw new ArgumentException("Pool size must be non-negative", nameof(poolSize));

        if (poolSize > MaxPoolSize)
            throw new ArgumentException($"Pool size cannot exceed {MaxPoolSize}", nameof(poolSize));

        if (poolSize == 0)
            return new ShadowrunDiceResult
            {
                PoolSize = 0,
                TargetNumber = targetNumber,
                Successes = 0,
                Rolls = Array.Empty<int>(),
                Glitch = false,
                CriticalGlitch = false,
                Sixes = 0,
                Details = "No dice rolled"
            };

        var allRolls = new List<int>();
        var currentPool = poolSize;
        var iterations = 0;
        // FIX: HIGH-004 - Use reduced max iterations constant

        while (currentPool > 0 && iterations < MaxExplodingIterations)
        {
            iterations++;
            var rolls = RollDice(currentPool, 6);
            allRolls.AddRange(rolls);

            var sixes = rolls.Count(r => r == 6);
            if (sixes == 0)
                break;
            
            currentPool = sixes; // Roll again for each 6
        }

        var successes = allRolls.Count(r => r >= targetNumber);
        var ones = allRolls.Take(poolSize).Count(r => r == 1); // Only count ones from initial roll for glitch
        var isGlitch = ones > (poolSize / 2.0);
        var isCriticalGlitch = isGlitch && successes == 0;
        var sixCount = allRolls.Count(r => r == 6);

        return new ShadowrunDiceResult
        {
            PoolSize = poolSize,
            TargetNumber = targetNumber,
            Successes = successes,
            Rolls = allRolls.ToArray(),
            Glitch = isGlitch,
            CriticalGlitch = isCriticalGlitch,
            Sixes = sixCount,
            Details = FormatEdgeResult(allRolls, successes, targetNumber, sixCount)
        };
    }

    /// <summary>
    /// Roll Friedman dice (exploding dice for Shadowrun)
    /// </summary>
    public FriedmanDiceResult RollFriedmanDice(int poolSize, int targetNumber = 4)
    {
        // FIX: HIGH-003 - Validate pool size
        if (poolSize < 0)
            throw new ArgumentException("Pool size must be non-negative", nameof(poolSize));

        if (poolSize > MaxPoolSize)
            throw new ArgumentException($"Pool size cannot exceed {MaxPoolSize}", nameof(poolSize));

        if (poolSize == 0)
            return new FriedmanDiceResult
            {
                PoolSize = 0,
                TargetNumber = targetNumber,
                Successes = 0,
                Rolls = Array.Empty<int>(),
                Details = "No dice rolled"
            };

        var allRolls = new List<int>();
        var explodingRolls = new List<List<int>>();
        var currentPool = poolSize;
        var totalSuccesses = 0;
        var iterations = 0;
        // FIX: HIGH-004 - Use reduced max iterations constant

        while (currentPool > 0 && iterations < MaxExplodingIterations)
        {
            iterations++;
            var rolls = RollDice(currentPool, 6);
            allRolls.AddRange(rolls);

            var sixes = rolls.Count(r => r == 6);
            var successes = rolls.Count(r => r >= targetNumber);
            totalSuccesses += successes;

            if (sixes > 0)
            {
                explodingRolls.Add(rolls.ToList());
                currentPool = sixes; // Roll again for each 6
            }
            else
            {
                explodingRolls.Add(rolls.ToList());
                break;
            }
        }

        return new FriedmanDiceResult
        {
            PoolSize = poolSize,
            TargetNumber = targetNumber,
            Successes = totalSuccesses,
            Rolls = allRolls.ToArray(),
            ExplodingRolls = explodingRolls,
            Details = FormatFriedmanResult(explodingRolls, totalSuccesses, targetNumber)
        };
    }

    private static string FormatResult(int[] allRolls, int[] usedRolls, int modifier, bool keepHighest, int keepCount)
    {
        var sb = new StringBuilder();

        if (keepHighest && keepCount > 0)
        {
            sb.Append($"[{string.Join(", ", allRolls)}] → Keep highest {keepCount}: [{string.Join(", ", usedRolls)}]");
        }
        else
        {
            sb.Append($"[{string.Join(", ", usedRolls)}]");
        }

        if (modifier != 0)
        {
            sb.Append(modifier > 0 ? $" + {modifier}" : $" - {Math.Abs(modifier)}");
        }

        return sb.ToString();
    }

    private static string FormatShadowrunResult(int[] rolls, int successes, int targetNumber, bool glitch, bool criticalGlitch)
    {
        var sb = new StringBuilder();
        sb.Append($"[{string.Join(", ", rolls)}] → {successes} success{(successes != 1 ? "es" : "")} (≥{targetNumber})");

        if (criticalGlitch)
            sb.Append(" **CRITICAL GLITCH!**");
        else if (glitch)
            sb.Append(" ⚠️ **Glitch**");

        return sb.ToString();
    }

    private static string FormatFriedmanResult(List<List<int>> explodingRolls, int totalSuccesses, int targetNumber)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < explodingRolls.Count; i++)
        {
            if (i > 0)
                sb.Append(" → ");
            
            sb.Append($"[{string.Join(", ", explodingRolls[i])}]");
        }

        sb.Append($" → {totalSuccesses} success{(totalSuccesses != 1 ? "es" : "")} (≥{targetNumber})");

        return sb.ToString();
    }

    private static string FormatEdgeResult(List<int> allRolls, int successes, int targetNumber, int sixes)
    {
        var sb = new StringBuilder();
        sb.Append($"[{string.Join(", ", allRolls)}] → {successes} success{(successes != 1 ? "es" : "")} (≥{targetNumber})");
        sb.Append($" | {sixes} six{(sixes != 1 ? "es" : "")} exploded");

        return sb.ToString();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _rng.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Standard dice roll result
/// </summary>
public record DiceResult
{
    public string Notation { get; init; } = string.Empty;
    public int[] Rolls { get; init; } = Array.Empty<int>();
    public int[] UsedRolls { get; init; } = Array.Empty<int>();
    public int Modifier { get; init; }
    public int Total { get; init; }
    public string Details { get; init; } = string.Empty;
}

/// <summary>
/// Shadowrun dice roll result
/// </summary>
public record ShadowrunDiceResult
{
    public int PoolSize { get; init; }
    public int TargetNumber { get; init; }
    public int Successes { get; init; }
    public int[] Rolls { get; init; } = Array.Empty<int>();
    public bool Glitch { get; init; }
    public bool CriticalGlitch { get; init; }
    public int Sixes { get; init; }
    public string Details { get; init; } = string.Empty;
}

/// <summary>
/// Initiative roll result
/// </summary>
public record InitiativeResult
{
    public int Reaction { get; init; }
    public int[] DiceRolls { get; init; } = Array.Empty<int>();
    public int DiceTotal { get; init; }
    public int Total { get; init; }
    public int Passes { get; init; }
    public string Details { get; init; } = string.Empty;
}

/// <summary>
/// Friedman (exploding) dice result
/// </summary>
public record FriedmanDiceResult
{
    public int PoolSize { get; init; }
    public int TargetNumber { get; init; }
    public int Successes { get; init; }
    public int[] Rolls { get; init; } = Array.Empty<int>();
    public List<List<int>> ExplodingRolls { get; init; } = new();
    public string Details { get; init; } = string.Empty;
}
