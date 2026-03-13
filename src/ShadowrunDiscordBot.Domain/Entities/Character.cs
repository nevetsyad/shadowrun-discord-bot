namespace ShadowrunDiscordBot.Domain.Entities;

using ShadowrunDiscordBot.Domain.Common;
using ShadowrunDiscordBot.Domain.Events.Characters;
using ShadowrunDiscordBot.Domain.ValueObjects;

/// <summary>
/// Shadowrun 3rd Edition Character entity with domain logic and invariants
/// </summary>
public class Character : BaseEntity
{
    // Identity
    public string Name { get; private set; }
    public ulong DiscordUserId { get; private set; }
    
    // Character Details
    public string Metatype { get; private set; }
    public string Archetype { get; private set; }
    
    // Attributes (1-10 scale)
    public int Body { get; private set; }
    public int Quickness { get; private set; }
    public int Strength { get; private set; }
    public int Charisma { get; private set; }
    public int Intelligence { get; private set; }
    public int Willpower { get; private set; }
    
    // Derived Attributes
    public int Reaction => (Quickness + Intelligence) / 2;
    
    // Essence (stored as 100x, so 6.00 = 600)
    public int Essence { get; private set; }
    public decimal EssenceDecimal => Essence / 100m;
    
    // Magic and Initiation
    public int Magic { get; private set; }
    public int InitiationGrade { get; private set; }
    public int BioIndex { get; private set; }
    
    // Resources
    public int Karma { get; private set; }
    public long Nuyen { get; private set; }
    
    // Damage Tracks
    public int PhysicalDamage { get; private set; }
    public int StunDamage { get; private set; }
    
    // Calculated values
    public int PhysicalConditionMonitor => (Body + 8) / 2;
    public int StunConditionMonitor => (Willpower + 8) / 2;
    
    // Collections
    private readonly List<CharacterSkill> _skills = new();
    public IReadOnlyCollection<CharacterSkill> Skills => _skills.AsReadOnly();
    
    private readonly List<CharacterCyberware> _cyberware = new();
    public IReadOnlyCollection<CharacterCyberware> Cyberware => _cyberware.AsReadOnly();
    
    private readonly List<CharacterSpell> _spells = new();
    public IReadOnlyCollection<CharacterSpell> Spells => _spells.AsReadOnly();
    
    private readonly List<CharacterSpirit> _spirits = new();
    public IReadOnlyCollection<CharacterSpirit> Spirits => _spirits.AsReadOnly();
    
    private readonly List<CharacterGear> _gear = new();
    public IReadOnlyCollection<CharacterGear> Gear => _gear.AsReadOnly();
    
    // Domain Events
    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Priority System
    public string? Priorities { get; private set; }
    
    // Private constructor for EF Core
    private Character()
    {
        Name = string.Empty;
        Metatype = "Human";
        Archetype = "Street Samurai";
    }
    
    // Factory method for creating a new character
    public static Character Create(
        string name,
        ulong discordUserId,
        string metatype,
        string archetype,
        int body,
        int quickness,
        int strength,
        int charisma,
        int intelligence,
        int willpower)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Character name cannot be empty", nameof(name));
        
        if (body < 1 || body > 10)
            throw new ArgumentOutOfRangeException(nameof(body), "Body must be between 1 and 10");
        
        // Create character
        var character = new Character
        {
            Name = name,
            DiscordUserId = discordUserId,
            Metatype = metatype,
            Archetype = archetype,
            Body = body,
            Quickness = quickness,
            Strength = strength,
            Charisma = charisma,
            Intelligence = intelligence,
            Willpower = willpower,
            Essence = 600, // 6.00
            Magic = 0,
            InitiationGrade = 0,
            BioIndex = 0,
            Karma = 0,
            Nuyen = 0,
            PhysicalDamage = 0,
            StunDamage = 0
        };
        
        // Add domain event
        character.AddDomainEvent(new CharacterCreatedEvent(character.Id, character.Name, character.DiscordUserId));
        
        return character;
    }
    
    // Business logic methods
    public void TakePhysicalDamage(int damage)
    {
        if (damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damage), "Damage cannot be negative");
        
        PhysicalDamage += damage;
        AddDomainEvent(new PhysicalDamageTakenEvent(Id, damage, PhysicalDamage));
    }
    
    public void TakeStunDamage(int damage)
    {
        if (damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damage), "Damage cannot be negative");
        
        StunDamage += damage;
        AddDomainEvent(new StunDamageTakenEvent(Id, damage, StunDamage));
    }
    
    public void HealPhysicalDamage(int healing)
    {
        if (healing < 0)
            throw new ArgumentOutOfRangeException(nameof(healing), "Healing cannot be negative");
        
        PhysicalDamage = Math.Max(0, PhysicalDamage - healing);
        AddDomainEvent(new PhysicalDamageHealedEvent(Id, healing, PhysicalDamage));
    }
    
    public void HealStunDamage(int healing)
    {
        if (healing < 0)
            throw new ArgumentOutOfRangeException(nameof(healing), "Healing cannot be negative");
        
        StunDamage = Math.Max(0, StunDamage - healing);
        AddDomainEvent(new StunDamageHealedEvent(Id, healing, StunDamage));
    }
    
    public void AddKarma(int karma)
    {
        if (karma < 0)
            throw new ArgumentOutOfRangeException(nameof(karma), "Karma cannot be negative");
        
        Karma += karma;
        AddDomainEvent(new KarmaAwardedEvent(Id, karma, Karma));
    }
    
    public void SpendKarma(int karma)
    {
        if (karma < 0)
            throw new ArgumentOutOfRangeException(nameof(karma), "Karma cannot be negative");
        
        if (Karma < karma)
            throw new InvalidOperationException($"Character only has {Karma} karma, cannot spend {karma}");
        
        Karma -= karma;
        AddDomainEvent(new KarmaSpentEvent(Id, karma, Karma));
    }
    
    public void AddNuyen(long nuyen)
    {
        if (nuyen < 0)
            throw new ArgumentOutOfRangeException(nameof(nuyen), "Nuyen cannot be negative");
        
        Nuyen += nuyen;
        AddDomainEvent(new NuyenEarnedEvent(Id, nuyen, Nuyen));
    }
    
    public void SpendNuyen(long nuyen)
    {
        if (nuyen < 0)
            throw new ArgumentOutOfRangeException(nameof(nuyen), "Nuyen cannot be negative");
        
        if (Nuyen < nuyen)
            throw new InvalidOperationException($"Character only has {Nuyen}¥, cannot spend {nuyen}¥");
        
        Nuyen -= nuyen;
        AddDomainEvent(new NuyenSpentEvent(Id, nuyen, Nuyen));
    }
    
    public void InstallCyberware(string name, string category, decimal essenceCost, long nuyenCost, int rating = 0)
    {
        if (Essence - (int)(essenceCost * 100) < 0)
            throw new InvalidOperationException($"Installing {name} would reduce essence below 0");
        
        var cyberware = new CharacterCyberware(Id, name, category, essenceCost, nuyenCost, rating);
        _cyberware.Add(cyberware);
        
        Essence -= (int)(essenceCost * 100);
        
        AddDomainEvent(new CyberwareInstalledEvent(Id, name, essenceCost, Essence / 100m));
    }
    
    public void LearnSpell(string name, string category, int drainModifier)
    {
        if (!IsAwakened())
            throw new InvalidOperationException("Only awakened characters can learn spells");
        
        var spell = new CharacterSpell(Id, name, category, drainModifier);
        _spells.Add(spell);
        
        AddDomainEvent(new SpellLearnedEvent(Id, name, category));
    }
    
    public void AddSkill(string skillName, int rating, string? specialization = null, bool isKnowledgeSkill = false)
    {
        if (rating < 0 || rating > 10)
            throw new ArgumentOutOfRangeException(nameof(rating), "Skill rating must be between 0 and 10");
        
        var skill = new CharacterSkill(Id, skillName, rating, specialization, isKnowledgeSkill);
        _skills.Add(skill);
        
        AddDomainEvent(new SkillAddedEvent(Id, skillName, rating));
    }
    
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Character name cannot be empty", nameof(newName));
        
        var oldName = Name;
        Name = newName;
        
        AddDomainEvent(new CharacterNameChangedEvent(Id, oldName, newName));
    }
    
    // Helper methods
    public decimal CalculateEssenceLoss()
    {
        return _cyberware.Sum(c => c.EssenceCost);
    }
    
    public decimal GetCurrentEssence()
    {
        return Math.Max(0, 6.0m - CalculateEssenceLoss());
    }
    
    public bool IsAwakened()
    {
        return Magic > 0 || Archetype.Contains("Mage") || Archetype.Contains("Shaman") || Archetype.Contains("Adept");
    }
    
    public bool IsDecker()
    {
        return Archetype.Contains("Decker");
    }
    
    public bool IsRigger()
    {
        return Archetype.Contains("Rigger");
    }
    
    public int GetWoundModifier()
    {
        var physicalWounds = PhysicalDamage / 3;
        var stunWounds = StunDamage / 3;
        return -(physicalWounds + stunWounds);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}

/// <summary>
/// Character skill value object
/// </summary>
public class CharacterSkill
{
    public int Id { get; }
    public int CharacterId { get; }
    public string SkillName { get; }
    public int Rating { get; private set; }
    public string? Specialization { get; }
    public bool IsKnowledgeSkill { get; }
    
    public CharacterSkill(int characterId, string skillName, int rating, string? specialization, bool isKnowledgeSkill)
    {
        CharacterId = characterId;
        SkillName = skillName;
        Rating = rating;
        Specialization = specialization;
        IsKnowledgeSkill = isKnowledgeSkill;
    }
    
    public void ImproveSkill(int improvement)
    {
        if (improvement <= 0)
            throw new ArgumentOutOfRangeException(nameof(improvement), "Improvement must be positive");
        
        Rating = Math.Min(10, Rating + improvement);
    }
}

/// <summary>
/// Character cyberware value object
/// </summary>
public class CharacterCyberware
{
    public int Id { get; }
    public int CharacterId { get; }
    public string Name { get; }
    public string Category { get; }
    public decimal EssenceCost { get; }
    public long NuyenCost { get; }
    public int Rating { get; }
    public bool IsInstalled { get; private set; }
    
    public CharacterCyberware(int characterId, string name, string category, decimal essenceCost, long nuyenCost, int rating)
    {
        CharacterId = characterId;
        Name = name;
        Category = category;
        EssenceCost = essenceCost;
        NuyenCost = nuyenCost;
        Rating = rating;
        IsInstalled = true;
    }
}

/// <summary>
/// Character spell value object
/// </summary>
public class CharacterSpell
{
    public int Id { get; }
    public int CharacterId { get; }
    public string Name { get; }
    public string Category { get; }
    public int DrainModifier { get; }
    public bool IsExclusive { get; }
    
    public CharacterSpell(int characterId, string name, string category, int drainModifier)
    {
        CharacterId = characterId;
        Name = name;
        Category = category;
        DrainModifier = drainModifier;
        IsExclusive = false;
    }
}

/// <summary>
/// Character spirit value object
/// </summary>
public class CharacterSpirit
{
    public int Id { get; }
    public int CharacterId { get; }
    public string SpiritType { get; }
    public int Force { get; }
    public string Tradition { get; }
    public int ServicesOwed { get; private set; }
    public bool IsBound { get; private set; }
    public DateTime? SummonedAt { get; }
    public DateTime? ExpiresAt { get; }
    
    public CharacterSpirit(int characterId, string spiritType, int force, string tradition, int servicesOwed)
    {
        CharacterId = characterId;
        SpiritType = spiritType;
        Force = force;
        Tradition = tradition;
        ServicesOwed = servicesOwed;
        IsBound = false;
        SummonedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddHours(force); // Spirits last for Force hours
    }
    
    public void UseService()
    {
        if (ServicesOwed <= 0)
            throw new InvalidOperationException("Spirit has no services owed");
        
        ServicesOwed--;
    }
}

/// <summary>
/// Character gear value object
/// </summary>
public class CharacterGear
{
    public int Id { get; }
    public int CharacterId { get; }
    public string Name { get; }
    public string Category { get; }
    public long Value { get; }
    public int Quantity { get; private set; }
    public bool IsEquipped { get; private set; }
    
    public CharacterGear(int characterId, string name, string category, long value, int quantity)
    {
        CharacterId = characterId;
        Name = name;
        Category = category;
        Value = value;
        Quantity = quantity;
        IsEquipped = false;
    }
    
    public void Equip()
    {
        IsEquipped = true;
    }
    
    public void Unequip()
    {
        IsEquipped = false;
    }
    
    public void ChangeQuantity(int delta)
    {
        var newQuantity = Quantity + delta;
        if (newQuantity < 0)
            throw new InvalidOperationException("Cannot have negative quantity");
        
        Quantity = newQuantity;
    }
}
