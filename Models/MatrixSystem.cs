using System.ComponentModel.DataAnnotations;

namespace ShadowrunDiscordBot.Models;

/// <summary>
/// Matrix/cyberdeck system for deckers
/// </summary>
public class Cyberdeck
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
    public string DeckType { get; set; } = "Standard"; // Micro, Standard, High, Elite

    // MPCP Rating (Master Persona Control Program) - 3 to 12
    [Required]
    public int MPCP { get; set; } = 3;

    // Active Memory (programs running)
    public int ActiveMemory { get; set; } = 100; // Mp

    // Storage Memory
    public int StorageMemory { get; set; } = 500; // Mp

    // Load rating (how many programs can run)
    public int LoadRating { get; set; } = 10;

    // Response rating (speed)
    public int ResponseRating { get; set; } = 1;

    // Hardening (damage resistance)
    public int Hardening { get; set; } = 0;

    // Firewall (defense rating)
    public int Firewall { get; set; } = 0;

    public long Value { get; set; } = 0;

    [Required]
    public virtual ShadowrunCharacter Character { get; set; } = null!;

    public virtual ICollection<DeckProgram> InstalledPrograms { get; set; } = new List<DeckProgram>();
}

/// <summary>
/// Programs installed on a cyberdeck
/// </summary>
public class DeckProgram
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CyberdeckId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = "Utility"; // Utility, Attack, Defense, Special

    [Required]
    public int Rating { get; set; } = 1;

    public int MemoryCost { get; set; } = 1; // Mp

    public bool IsLoaded { get; set; } = false;

    [Required]
    public virtual Cyberdeck Cyberdeck { get; set; } = null!;
}

/// <summary>
/// Matrix combat session
/// </summary>
public class MatrixSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CharacterId { get; set; }

    public bool IsInVR { get; set; } = false;

    public int SecurityTally { get; set; } = 0;

    [MaxLength(50)]
    public string AlertLevel { get; set; } = "None"; // None, Passive, Active, Shutdown

    public int CurrentInitiative { get; set; } = 0;

    public int InitiativePasses { get; set; } = 1;

    public virtual ICollection<ActiveICE> ActiveICE { get; set; } = new List<ActiveICE>();

    [Required]
    public virtual ShadowrunCharacter Character { get; set; } = null!;
}

/// <summary>
/// Active ICE (Intrusion Countermeasure Electronics)
/// </summary>
public class ActiveICE
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MatrixSessionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ICEType { get; set; } = "Probe"; // Probe, Killer, Black, Tar

    [Required]
    public int Rating { get; set; } = 1;

    public bool IsActivated { get; set; } = false;

    public int SecurityTallyThreshold { get; set; } = 0;

    [Required]
    public virtual MatrixSession MatrixSession { get; set; } = null!;
}
