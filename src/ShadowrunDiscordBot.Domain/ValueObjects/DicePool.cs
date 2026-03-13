namespace ShadowrunDiscordBot.Domain.ValueObjects;

/// <summary>
/// Value object representing a dice pool for Shadowrun tests
/// </summary>
public class DicePool : IEquatable<DicePool>
{
    public int Count { get; }
    public int TargetNumber { get; }
    public int Threshold { get; }
    public bool ExplodingDice { get; }
    
    public DicePool(int count, int targetNumber = 4, int threshold = 1, bool explodingDice = false)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Dice count cannot be negative");
        
        if (targetNumber < 2 || targetNumber > 12)
            throw new ArgumentOutOfRangeException(nameof(targetNumber), "Target number must be between 2 and 12");
        
        if (threshold < 0)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold cannot be negative");
        
        Count = count;
        TargetNumber = targetNumber;
        Threshold = threshold;
        ExplodingDice = explodingDice;
    }
    
    public static DicePool Default(int count) => new(count);
    
    public static DicePool WithTarget(int count, int targetNumber) => new(count, targetNumber);
    
    public static DicePool WithThreshold(int count, int threshold) => new(count, threshold: threshold);
    
    public static DicePool Exploding(int count) => new(count, explodingDice: true);
    
    public DicePool AddModifier(int modifier)
    {
        return new DicePool(Math.Max(0, Count + modifier), TargetNumber, Threshold, ExplodingDice);
    }
    
    public DicePool WithNewTarget(int newTarget)
    {
        return new DicePool(Count, newTarget, Threshold, ExplodingDice);
    }
    
    public bool Equals(DicePool? other)
    {
        if (other is null) return false;
        return Count == other.Count &&
               TargetNumber == other.TargetNumber &&
               Threshold == other.Threshold &&
               ExplodingDice == other.ExplodingDice;
    }
    
    public override bool Equals(object? obj) => Equals(obj as DicePool);
    
    public override int GetHashCode() => HashCode.Combine(Count, TargetNumber, Threshold, ExplodingDice);
    
    public override string ToString() => 
        $"{Count}D6{(ExplodingDice ? "!" : "")} vs {TargetNumber} (threshold {Threshold})";
    
    public static bool operator ==(DicePool left, DicePool right) => left.Equals(right);
    public static bool operator !=(DicePool left, DicePool right) => !left.Equals(right);
}
