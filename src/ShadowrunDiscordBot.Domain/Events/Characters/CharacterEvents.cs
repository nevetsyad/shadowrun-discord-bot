namespace ShadowrunDiscordBot.Domain.Events.Characters;

using ShadowrunDiscordBot.Domain.Common;

/// <summary>
/// Event raised when a new character is created
/// </summary>
public class CharacterCreatedEvent : DomainEvent
{
    public string CharacterName { get; }
    public ulong DiscordUserId { get; }
    
    public CharacterCreatedEvent(int characterId, string characterName, ulong discordUserId)
    {
        AggregateId = Guid.NewGuid();
        CharacterName = characterName;
        DiscordUserId = discordUserId;
        EventType = nameof(CharacterCreatedEvent);
    }
}

/// <summary>
/// Event raised when a character's name is changed
/// </summary>
public class CharacterNameChangedEvent : DomainEvent
{
    public string OldName { get; }
    public string NewName { get; }
    
    public CharacterNameChangedEvent(int characterId, string oldName, string newName)
    {
        AggregateId = Guid.NewGuid();
        OldName = oldName;
        NewName = newName;
        EventType = nameof(CharacterNameChangedEvent);
    }
}

/// <summary>
/// Event raised when a character takes physical damage
/// </summary>
public class PhysicalDamageTakenEvent : DomainEvent
{
    public int DamageAmount { get; }
    public int TotalPhysicalDamage { get; }
    
    public PhysicalDamageTakenEvent(int characterId, int damageAmount, int totalPhysicalDamage)
    {
        AggregateId = Guid.NewGuid();
        DamageAmount = damageAmount;
        TotalPhysicalDamage = totalPhysicalDamage;
        EventType = nameof(PhysicalDamageTakenEvent);
    }
}

/// <summary>
/// Event raised when a character takes stun damage
/// </summary>
public class StunDamageTakenEvent : DomainEvent
{
    public int DamageAmount { get; }
    public int TotalStunDamage { get; }
    
    public StunDamageTakenEvent(int characterId, int damageAmount, int totalStunDamage)
    {
        AggregateId = Guid.NewGuid();
        DamageAmount = damageAmount;
        TotalStunDamage = totalStunDamage;
        EventType = nameof(StunDamageTakenEvent);
    }
}

/// <summary>
/// Event raised when a character heals physical damage
/// </summary>
public class PhysicalDamageHealedEvent : DomainEvent
{
    public int HealingAmount { get; }
    public int RemainingPhysicalDamage { get; }
    
    public PhysicalDamageHealedEvent(int characterId, int healingAmount, int remainingPhysicalDamage)
    {
        AggregateId = Guid.NewGuid();
        HealingAmount = healingAmount;
        RemainingPhysicalDamage = remainingPhysicalDamage;
        EventType = nameof(PhysicalDamageHealedEvent);
    }
}

/// <summary>
/// Event raised when a character heals stun damage
/// </summary>
public class StunDamageHealedEvent : DomainEvent
{
    public int HealingAmount { get; }
    public int RemainingStunDamage { get; }
    
    public StunDamageHealedEvent(int characterId, int healingAmount, int remainingStunDamage)
    {
        AggregateId = Guid.NewGuid();
        HealingAmount = healingAmount;
        RemainingStunDamage = remainingStunDamage;
        EventType = nameof(StunDamageHealedEvent);
    }
}

/// <summary>
/// Event raised when a character is awarded karma
/// </summary>
public class KarmaAwardedEvent : DomainEvent
{
    public int KarmaAmount { get; }
    public int TotalKarma { get; }
    
    public KarmaAwardedEvent(int characterId, int karmaAmount, int totalKarma)
    {
        AggregateId = Guid.NewGuid();
        KarmaAmount = karmaAmount;
        TotalKarma = totalKarma;
        EventType = nameof(KarmaAwardedEvent);
    }
}

/// <summary>
/// Event raised when a character spends karma
/// </summary>
public class KarmaSpentEvent : DomainEvent
{
    public int KarmaAmount { get; }
    public int RemainingKarma { get; }
    
    public KarmaSpentEvent(int characterId, int karmaAmount, int remainingKarma)
    {
        AggregateId = Guid.NewGuid();
        KarmaAmount = karmaAmount;
        RemainingKarma = remainingKarma;
        EventType = nameof(KarmaSpentEvent);
    }
}

/// <summary>
/// Event raised when a character earns nuyen
/// </summary>
public class NuyenEarnedEvent : DomainEvent
{
    public long NuyenAmount { get; }
    public long TotalNuyen { get; }
    
    public NuyenEarnedEvent(int characterId, long nuyenAmount, long totalNuyen)
    {
        AggregateId = Guid.NewGuid();
        NuyenAmount = nuyenAmount;
        TotalNuyen = totalNuyen;
        EventType = nameof(NuyenEarnedEvent);
    }
}

/// <summary>
/// Event raised when a character spends nuyen
/// </summary>
public class NuyenSpentEvent : DomainEvent
{
    public long NuyenAmount { get; }
    public long RemainingNuyen { get; }
    
    public NuyenSpentEvent(int characterId, long nuyenAmount, long remainingNuyen)
    {
        AggregateId = Guid.NewGuid();
        NuyenAmount = nuyenAmount;
        RemainingNuyen = remainingNuyen;
        EventType = nameof(NuyenSpentEvent);
    }
}

/// <summary>
/// Event raised when cyberware is installed
/// </summary>
public class CyberwareInstalledEvent : DomainEvent
{
    public string CyberwareName { get; }
    public decimal EssenceCost { get; }
    public decimal RemainingEssence { get; }
    
    public CyberwareInstalledEvent(int characterId, string cyberwareName, decimal essenceCost, decimal remainingEssence)
    {
        AggregateId = Guid.NewGuid();
        CyberwareName = cyberwareName;
        EssenceCost = essenceCost;
        RemainingEssence = remainingEssence;
        EventType = nameof(CyberwareInstalledEvent);
    }
}

/// <summary>
/// Event raised when a character learns a spell
/// </summary>
public class SpellLearnedEvent : DomainEvent
{
    public string SpellName { get; }
    public string Category { get; }
    
    public SpellLearnedEvent(int characterId, string spellName, string category)
    {
        AggregateId = Guid.NewGuid();
        SpellName = spellName;
        Category = category;
        EventType = nameof(SpellLearnedEvent);
    }
}

/// <summary>
/// Event raised when a character learns a new skill
/// </summary>
public class SkillAddedEvent : DomainEvent
{
    public string SkillName { get; }
    public int Rating { get; }
    
    public SkillAddedEvent(int characterId, string skillName, int rating)
    {
        AggregateId = Guid.NewGuid();
        SkillName = skillName;
        Rating = rating;
        EventType = nameof(SkillAddedEvent);
    }
}
