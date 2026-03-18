using System.ComponentModel.DataAnnotations;

namespace ShadowrunDiscordBot.Domain.Entities;

#region Astral Space Rules

/// <summary>
/// Astral combat and projection tracking
/// </summary>
public class AstralCombatState
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    // Projection state
    public bool IsProjecting { get; set; } = false;
    public bool HasAstralPerception { get; set; } = false;
    public DateTime? ProjectionStartTime { get; set; }

    // Astral attributes (derived from mental attributes)
    public int AstralBody { get; set; } = 0; // Used in astral combat
    public int AstralStrength { get; set; } = 0; // Astral damage
    public int AstralQuickness { get; set; } = 0; // Astral initiative

    // Astral damage track
    public int AstralDamage { get; set; } = 0;
    public int AstralConditionMonitor => 10; // Fixed in astral space

    // Current location in astral
    [MaxLength(200)]
    public string? AstralLocation { get; set; }

    [Required]
    public virtual Character Character { get; set; } = null!;
}

/// <summary>
/// Foci - magical items that enhance sorcery
/// </summary>
public class CharacterFocus
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FocusType { get; set; } = "Sorcery"; // Sorcery, Spell, Spirit, Power, Weapon, Sustaining

    [Required]
    public int Force { get; set; } = 1;

    public bool IsBonded { get; set; } = false;
    public bool IsActive { get; set; } = false;

    // Karma cost to bond
    public int BondingCost { get; set; } = 0;

    // Essence cost when active
    public decimal EssenceCost { get; set; } = 0m;

    // Bonus provided
    [MaxLength(200)]
    public string? BonusDescription { get; set; }

    [Required]
    public virtual Character Character { get; set; } = null!;
}

/// <summary>
/// Astral signature left by magical effects
/// </summary>
public class AstralSignatureRecord
{
    [Key]
    public int Id { set; get; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string SignatureType { get; set; } = "Spell"; // Spell, Spirit, Emotional, Background

    public int Force { get; set; } = 0;

    // Who left it (if known)
    public int? CharacterId { get; set; }

    // How long until it fades
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public int HoursToFade { get; set; } = 24;

    [MaxLength(500)]
    public string? Description { get; set; }

    public virtual Character? Character { get; set; }
}

#endregion

#region Matrix Depth System

/// <summary>
/// Matrix host/system with full SR3 ratings
/// </summary>
public class MatrixHost
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // System ratings (SR3 uses separate ratings)
    [Required]
    public int AccessRating { get; set; } = 4; // Getting into the system
    [Required]
    public int ControlRating { get; set; } = 4; // Controlling system functions
    [Required]
    public int IndexRating { get; set; } = 4; // Finding files/data
    [Required]
    public int FilesRating { get; set; } = 4; // Manipulating data
    [Required]
    public int SlaveRating { get; set; } = 4; // Controlling connected devices

    // Security configuration
    [Required]
    public int SecurityCode { get; set; } = 4; // Base detection threshold

    // IC Loadout
    public virtual ICollection<HostICE> InstalledICE { get; set; } = new List<HostICE>();
}

/// <summary>
/// ICE installed in a host
/// </summary>
public class HostICE
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int HostId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ICEType { get; set; } = "Probe"; // Probe, Trace, Killer, Blaster, Black, Tar, Tar Baby

    [Required]
    [MaxLength(20)]
    public string ICEClass { get; set; } = "White"; // White, Gray, Black

    [Required]
    public int Rating { get; set; } = 4;

    // Activation threshold (security tally)
    public int ActivationTally { get; set; } = 5;

    public bool IsActive { get; set; } = false;

    [MaxLength(200)]
    public string? SpecialAbilities { get; set; }

    [Required]
    public virtual MatrixHost Host { get; set; } = null!;
}

/// <summary>
/// Active Matrix run tracking
/// </summary>
public class MatrixRun
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    public int HostId { get; set; }

    // Security tracking
    [Required]
    public int SecurityTally { get; set; } = 0;

    [Required]
    [MaxLength(20)]
    public string AlertStatus { get; set; } = "None"; // None, Passive, Active, Shutdown

    // Alert escalation thresholds
    public int PassiveThreshold { get; set; } = 10;
    public int ActiveThreshold { get; set; } = 20;
    public int ShutdownThreshold { get; set; } = 30;

    // Active IC encounters
    public virtual ICollection<ActiveICEncounter> ICEncounters { get; set; } = new List<ActiveICEncounter>();

    // Decker's deck
    public int CyberdeckId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    [Required]
    public virtual Character Character { get; set; } = null!;
    [Required]
    public virtual MatrixHost Host { get; set; } = null!;
    [Required]
    public virtual Cyberdeck Cyberdeck { get; set; } = null!;
}

/// <summary>
/// Active IC encounter during a Matrix run
/// </summary>
public class ActiveICEncounter
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MatrixRunId { get; set; }

    [Required]
    public int HostICEId { get; set; }

    public bool IsDefeated { get; set; } = false;
    public int DamageToDeck { get; set; } = 0;
    public int DamageToCharacter { get; set; } = 0; // For black IC

    [MaxLength(500)]
    public string? EncounterLog { get; set; }

    [Required]
    public virtual MatrixRun MatrixRun { get; set; } = null!;
    [Required]
    public virtual HostICE HostICE { get; set; } = null!;
}

#endregion

#region Combat Pool System

/// <summary>
/// Combat pool management for SR3
/// </summary>
public class CombatPoolState
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    public int CombatSessionId { get; set; }

    // Total combat pool = (Quickness + Intelligence + Willpower) / 2
    public int TotalPool { get; set; } = 0;

    // Allocation for current combat turn
    public int AllocatedToAttack { get; set; } = 0;
    public int AllocatedToDefense { get; set; } = 0;
    public int AllocatedToDamage { get; set; } = 0;
    public int AllocatedToOther { get; set; } = 0; // Called shots, etc.

    // Pool refreshes each Combat Turn
    public int CurrentTurn { get; set; } = 1;

    [Required]
    public virtual Character Character { get; set; } = null!;
    [Required]
    public virtual CombatSession CombatSession { get; set; } = null!;
    public virtual ICollection<CombatPoolUsage> PoolUsages { get; set; } = new List<CombatPoolUsage>();
}

/// <summary>
/// Combat pool usage log
/// </summary>
public class CombatPoolUsage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CombatPoolStateId { get; set; }

    [Required]
    [MaxLength(50)]
    public string UsageType { get; set; } = "Attack"; // Attack, Defense, Damage, Called Shot

    [Required]
    public int DiceUsed { get; set; }

    [Required]
    public int Successes { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public virtual CombatPoolState CombatPoolState { get; set; } = null!;
}

#endregion

#region Vehicle Combat

/// <summary>
/// Vehicle for riggers and drivers
/// </summary>
public class Vehicle
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string VehicleType { get; set; } = "Ground"; // Ground, Water, Aircraft, Drone

    // Vehicle stats
    [Required]
    public int Body { get; set; } = 10;
    [Required]
    public int Armor { get; set; } = 3;
    [Required]
    public int Speed { get; set; } = 100; // km/h
    [Required]
    public int Acceleration { get; set; } = 10;

    // Handling
    [Required]
    public int Handling { get; set; } = 3;
    [Required]
    public int ManeuverScore { get; set; } = 0; // Derived from Handling + Pilot Skill

    // Sensor rating
    [Required]
    public int SensorRating { get; set; } = 3;

    // Damage
    public int CurrentDamage { get; set; } = 0;
    public int ConditionMonitor => (Body + 8) / 2;

    // Rigger adaptation
    public bool HasRiggerAdaptation { get; set; } = false;
    public int RiggerControlRating { get; set; } = 0; // Control Rig rating

    // Weapons
    public virtual ICollection<VehicleWeapon> Weapons { get; set; } = new List<VehicleWeapon>();

    [Required]
    public virtual Character Character { get; set; } = null!;
}

/// <summary>
/// Weapon mounted on a vehicle
/// </summary>
public class VehicleWeapon
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int VehicleId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int DamageCode { get; set; } = 6; // Base damage
    [Required]
    [MaxLength(20)]
    public string DamageType { get; set; } = "Physical";

    public int RangeShort { get; set; } = 50;
    public int RangeMedium { get; set; } = 150;
    public int RangeLong { get; set; } = 350;
    public int RangeExtreme { get; set; } = 800;

    // Fire modes
    public bool CanSingleFire { get; set; } = true;
    public bool CanBurstFire { get; set; } = false;
    public bool CanFullAuto { get; set; } = false;

    [Required]
    public virtual Vehicle Vehicle { get; set; } = null!;
}

/// <summary>
/// Drone (autonomous or remote-controlled)
/// </summary>
public class Drone : Vehicle
{
    [Required]
    [MaxLength(50)]
    public string DroneModel { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ControlMode { get; set; } = "Autonomous"; // Autonomous, Remote, Rigged

    // Pilot program rating
    [Required]
    public int PilotRating { get; set; } = 3;

    // Autosoft programs
    public virtual ICollection<DroneAutosoft> Autosofts { get; set; } = new List<DroneAutosoft>();
}

/// <summary>
/// Autosoft program for drones
/// </summary>
public class DroneAutosoft
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DroneId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty; // Targeting, Clearsight, Stealth, etc.

    [Required]
    public int Rating { get; set; } = 1;

    [Required]
    public virtual Drone Drone { get; set; } = null!;
}

/// <summary>
/// Vehicle combat session tracking
/// </summary>
public class VehicleCombatSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CombatSessionId { get; set; }

    public int CurrentRange { get; set; } = 100; // meters

    public int CurrentSpeed { get; set; } = 0;

    [MaxLength(200)]
    public string? TerrainType { get; set; } = "Open";

    public virtual ICollection<VehicleCombatant> VehicleCombatants { get; set; } = new List<VehicleCombatant>();

    [Required]
    public virtual CombatSession CombatSession { get; set; } = null!;
}

/// <summary>
/// Vehicle in combat
/// </summary>
public class VehicleCombatant
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int VehicleCombatSessionId { get; set; }

    [Required]
    public int VehicleId { get; set; }

    public int Initiative { get; set; } = 0;
    public int InitiativePasses { get; set; } = 1;
    public bool HasActed { get; set; } = false;

    [Required]
    public virtual VehicleCombatSession VehicleCombatSession { get; set; } = null!;
    [Required]
    public virtual Vehicle Vehicle { get; set; } = null!;
}

#endregion

#region Contacts and Legwork

/// <summary>
/// Legwork attempt tracking
/// </summary>
public class LegworkAttempt
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    public int? ContactId { get; set; }

    [Required]
    [MaxLength(200)]
    public string InformationSought { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LegworkType { get; set; } = "Street"; // Street, Matrix, Corporate, etc.

    // Skill used
    [MaxLength(50)]
    public string? SkillUsed { get; set; } // Etiquette, Negotiation, etc.

    // Results
    public int DiceRolled { get; set; } = 0;
    public int Successes { get; set; } = 0;
    public int TargetNumber { get; set; } = 4;

    [MaxLength(500)]
    public string? InformationGained { get; set; }

    // Cost
    public int NuyenCost { get; set; } = 0;
    public int TimeHours { get; set; } = 0;

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public virtual Character Character { get; set; } = null!;
    public virtual CharacterContact? Contact { get; set; }
}

/// <summary>
/// Johnson meeting record
/// </summary>
public class JohnsonMeeting
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameSessionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string JohnsonName { get; set; } = "Mr. Johnson";

    [Required]
    [MaxLength(100)]
    public string Corporation { get; set; } = "Unknown";

    // Negotiation
    public int InitialOffer { get; set; } = 0;
    public int FinalOffer { get; set; } = 0;
    public int NegotiationSuccesses { get; set; } = 0;

    // Mission details
    [MaxLength(500)]
    public string? MissionBriefing { get; set; }

    // Meeting outcome
    public bool Accepted { get; set; } = false;

    public DateTime MeetingDate { get; set; } = DateTime.UtcNow;

    [Required]
    public virtual GameSession GameSession { get; set; } = null!;
}

#endregion

#region Karma System

/// <summary>
/// Karma tracking for character advancement
/// </summary>
public class KarmaRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    // Karma earned/spent
    public int KarmaChange { get; set; } = 0;

    // Running totals
    public int TotalEarned { get; set; } = 0;
    public int TotalSpent { get; set; } = 0;
    public int CurrentKarma { get; set; } = 0;

    // Karma Pool (for rerolls)
    public int KarmaPool { get; set; } = 0;
    public int KarmaPoolMax { get; set; } = 0; // Max = (TotalEarned / 10)

    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Source { get; set; } // Mission, Roleplay, Good Karma, etc.

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public virtual Character Character { get; set; } = null!;
    public virtual ICollection<KarmaExpenditure> Expenditures { get; set; } = new List<KarmaExpenditure>();
}

/// <summary>
/// Karma expenditure for advancement
/// </summary>
public class KarmaExpenditure
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int KarmaRecordId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ExpenditureType { get; set; } = "Skill"; // Skill, Attribute, Spell, etc.

    [MaxLength(100)]
    public string? TargetName { get; set; } // Skill name, attribute, spell name

    public int PreviousRating { get; set; } = 0;
    public int NewRating { get; set; } = 0;
    public int KarmaCost { get; set; } = 0;

    public DateTime SpentAt { get; set; } = DateTime.UtcNow;

    [Required]
    public virtual KarmaRecord KarmaRecord { get; set; } = null!;
}

/// <summary>
/// Karma costs for advancement (SR3 rules)
/// </summary>
public static class KarmaCosts
{
    // Skill improvement: Rating x 2
    public static int SkillImprovement(int currentRating)
        => currentRating * 2;

    // Attribute improvement: Rating x 3
    public static int AttributeImprovement(int currentRating)
        => currentRating * 3;

    // New skill: 2 karma
    public const int NewSkill = 2;

    // New specialization: 1 karma
    public const int Specialization = 1;

    // New spell: 1 karma
    public const int NewSpell = 1;

    // Initiation: Grade x 10
    public static int Initiation(int grade)
        => grade * 10;

    // Binding a focus: Force x 1
    public static int BindFocus(int force)
        => force;

    // Ally spirit: Complex formula, simplified
    public static int AllySpirit(int force)
        => force * 5;
}

#endregion

#region Damage and Healing

/// <summary>
/// Detailed damage tracking with staging
/// </summary>
public class DamageRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DamageType { get; set; } = "Physical"; // Physical, Stun, Astral

    // Damage staging (SR3)
    public int BaseDamage { get; set; } = 0; // Before staging
    public int NetSuccesses { get; set; } = 0; // Net hits on attack
    public int FinalDamage { get; set; } = 0; // After staging

    // Damage level
    [MaxLength(20)]
    public string DamageLevel { get; set; } = "Light"; // Light, Moderate, Serious, Deadly

    // Damage code (e.g., 6M, 9S)
    [MaxLength(10)]
    public string? DamageCode { get; set; }

    // Source
    [MaxLength(100)]
    public string? Source { get; set; } // Weapon, Spell, Fall, etc.

    // Armor
    public int ArmorRating { get; set; } = 0;
    public int ArmorSuccesses { get; set; } = 0;

    // Body resistance
    public int BodySuccesses { get; set; } = 0;

    public DateTime InflictedAt { get; set; } = DateTime.UtcNow;
    public bool IsHealed { get; set; } = false;

    [Required]
    public virtual Character Character { get; set; } = null!;
}

/// <summary>
/// Healing attempt tracking
/// </summary>
public class HealingAttempt
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    public int? HealerId { get; set; } // If someone else is healing

    [Required]
    [MaxLength(50)]
    public string HealingType { get; set; } = "Natural"; // Natural, Biotech, FirstAid, Magical

    // Skill/attribute used
    [MaxLength(50)]
    public string? SkillUsed { get; set; }
    public int DiceRolled { get; set; } = 0;
    public int Successes { get; set; } = 0;
    public int TargetNumber { get; set; } = 4;

    // Results
    public int DamageHealed { get; set; } = 0;
    public int StagingReduction { get; set; } = 0; // Reduced damage level

    // Time
    public int TimeMinutes { get; set; } = 0;

    // Medical supplies used
    public bool UsedMedkit { get; set; } = false;
    public bool UsedMedicalFacility { get; set; } = false;

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public virtual Character Character { get; set; } = null!;
    public virtual Character? Healer { get; set; }
}

/// <summary>
/// Healing time tracking
/// </summary>
public class HealingTimeRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    [Required]
    [MaxLength(20)]
    public string DamageType { get; set; } = "Physical";

    [Required]
    public int DamageAmount { get; set; }

    // Healing times (SR3 rules)
    public int BaseHours { get; set; } = 0;
    public int ModifiedHours { get; set; } = 0; // After Body successes

    public DateTime? HealingStarted { get; set; }
    public DateTime? HealingComplete { get; set; }

    // Modifiers
    public int BodySuccesses { get; set; } = 0;
    public bool HasMedicalCare { get; set; } = false;
    public bool HasAwakenedHealer { get; set; } = false;

    [Required]
    public virtual Character Character { get; set; } = null!;
}

/// <summary>
/// SR3 Healing time calculations
/// </summary>
public static class HealingTimes
{
    // Natural healing: Body test
    // Physical: 1 day per box (Body test reduces)
    // Stun: 1 hour per box (Body test reduces)

    public static int PhysicalHealingBase(int damage)
        => damage * 24; // Hours

    public static int StunHealingBase(int damage)
        => damage * 1; // Hours

    public static int ApplyBodySuccesses(int baseHours, int bodySuccesses)
        => Math.Max(1, baseHours - (bodySuccesses * 2)); // Each success reduces by 2 hours

    // First Aid: Can heal up to Biotech skill rating in boxes
    public static int FirstAidMax(int biotechRating)
        => biotechRating;

    // Magical healing: Heal spell restores (Force) boxes
    public static int MagicalHealingMax(int force)
        => force;
}

#endregion
