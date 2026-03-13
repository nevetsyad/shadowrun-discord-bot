namespace ShadowrunDiscordBot.Domain.ValueObjects;

/// <summary>
/// Value object representing Shadowrun attributes
/// </summary>
public class Attributes : IEquatable<Attributes>
{
    public int Body { get; }
    public int Quickness { get; }
    public int Strength { get; }
    public int Charisma { get; }
    public int Intelligence { get; }
    public int Willpower { get; }
    
    // Derived attributes
    public int Reaction => (Quickness + Intelligence) / 2;
    
    public Attributes(int body, int quickness, int strength, int charisma, int intelligence, int willpower)
    {
        ValidateAttribute(body, nameof(body));
        ValidateAttribute(quickness, nameof(quickness));
        ValidateAttribute(strength, nameof(strength));
        ValidateAttribute(charisma, nameof(charisma));
        ValidateAttribute(intelligence, nameof(intelligence));
        ValidateAttribute(willpower, nameof(willpower));
        
        Body = body;
        Quickness = quickness;
        Strength = strength;
        Charisma = charisma;
        Intelligence = intelligence;
        Willpower = willpower;
    }
    
    public static Attributes Default() => new(3, 3, 3, 3, 3, 3);
    
    public Attributes WithBody(int body) => new(body, Quickness, Strength, Charisma, Intelligence, Willpower);
    public Attributes WithQuickness(int quickness) => new(Body, quickness, Strength, Charisma, Intelligence, Willpower);
    public Attributes WithStrength(int strength) => new(Body, Quickness, strength, Charisma, Intelligence, Willpower);
    public Attributes WithCharisma(int charisma) => new(Body, Quickness, Strength, charisma, Intelligence, Willpower);
    public Attributes WithIntelligence(int intelligence) => new(Body, Quickness, Strength, Charisma, intelligence, Willpower);
    public Attributes WithWillpower(int willpower) => new(Body, Quickness, Strength, Charisma, Intelligence, willpower);
    
    public Attributes AddToBody(int bonus) => WithBody(Math.Min(10, Math.Max(1, Body + bonus)));
    public Attributes AddToQuickness(int bonus) => WithQuickness(Math.Min(10, Math.Max(1, Quickness + bonus)));
    public Attributes AddToStrength(int bonus) => WithStrength(Math.Min(10, Math.Max(1, Strength + bonus)));
    public Attributes AddToCharisma(int bonus) => WithCharisma(Math.Min(10, Math.Max(1, Charisma + bonus)));
    public Attributes AddToIntelligence(int bonus) => WithIntelligence(Math.Min(10, Math.Max(1, Intelligence + bonus)));
    public Attributes AddToWillpower(int bonus) => WithWillpower(Math.Min(10, Math.Max(1, Willpower + bonus)));
    
    private static void ValidateAttribute(int value, string name)
    {
        if (value < 1 || value > 10)
            throw new ArgumentOutOfRangeException(name, $"{name} must be between 1 and 10");
    }
    
    public bool Equals(Attributes? other)
    {
        if (other is null) return false;
        return Body == other.Body &&
               Quickness == other.Quickness &&
               Strength == other.Strength &&
               Charisma == other.Charisma &&
               Intelligence == other.Intelligence &&
               Willpower == other.Willpower;
    }
    
    public override bool Equals(object? obj) => Equals(obj as Attributes);
    
    public override int GetHashCode() => 
        HashCode.Combine(Body, Quickness, Strength, Charisma, Intelligence, Willpower);
    
    public static bool operator ==(Attributes left, Attributes right) => left.Equals(right);
    public static bool operator !=(Attributes left, Attributes right) => !left.Equals(right);
}
