using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Enhanced Matrix service with full SR3 system ratings, security tally, and IC
/// </summary>
public class EnhancedMatrixService
{
    private readonly DiceService _diceService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<EnhancedMatrixService> _logger;

    public EnhancedMatrixService(
        DiceService diceService,
        DatabaseService databaseService,
        ILogger<EnhancedMatrixService> logger)
    {
        _diceService = diceService;
        _databaseService = databaseService;
        _logger = logger;
    }

    #region System Ratings

    /// <summary>
    /// Create a Matrix host with full SR3 ratings
    /// </summary>
    public async Task<MatrixHost> CreateHostAsync(
        string name,
        int access,
        int control,
        int index,
        int files,
        int slave,
        int securityCode = 4)
    {
        var host = new MatrixHost
        {
            Name = name,
            AccessRating = access,
            ControlRating = control,
            IndexRating = index,
            FilesRating = files,
            SlaveRating = slave,
            SecurityCode = securityCode
        };

        await _databaseService.AddMatrixHostAsync(host);

        _logger.LogInformation("Created Matrix host {Name} with ratings A:{Access} C:{Control} I:{Index} F:{Files} S:{Slave}",
            name, access, control, index, files, slave);

        return host;
    }

    /// <summary>
    /// Get effective system rating for a specific subsystem
    /// </summary>
    public int GetEffectiveRating(MatrixHost host, string subsystem)
    {
        return subsystem.ToLower() switch
        {
            "access" => host.AccessRating,
            "control" => host.ControlRating,
            "index" => host.IndexRating,
            "files" => host.FilesRating,
            "slave" => host.SlaveRating,
            _ => 4
        };
    }

    #endregion

    #region Security Tally

    /// <summary>
    /// Begin a Matrix run against a host
    /// </summary>
    public async Task<MatrixRunResult> BeginRunAsync(int characterId, int hostId, int cyberdeckId)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        var host = await _databaseService.GetMatrixHostAsync(hostId);
        var deck = await _databaseService.GetCyberdeckAsync(cyberdeckId);

        if (character == null || host == null || deck == null)
            return MatrixRunResult.Fail("Invalid character, host, or cyberdeck.");

        var run = new MatrixRun
        {
            CharacterId = characterId,
            HostId = hostId,
            CyberdeckId = cyberdeckId,
            SecurityTally = 0,
            AlertStatus = "None",
            PassiveThreshold = 10,
            ActiveThreshold = 20,
            ShutdownThreshold = 30,
            StartedAt = DateTime.UtcNow
        };

        await _databaseService.AddMatrixRunAsync(run);

        _logger.LogInformation("Matrix run started: {Character} vs {Host}", character.Name, host.Name);

        return MatrixRunResult.Ok($"Started run against {host.Name}. Security tally: 0");
    }

    /// <summary>
    /// Add to security tally and check for alert escalation
    /// </summary>
    public async Task<SecurityResult> AddToSecurityTallyAsync(int runId, int points)
    {
        var run = await _databaseService.GetMatrixRunAsync(runId);
        if (run == null)
            return SecurityResult.Fail("Matrix run not found.");

        run.SecurityTally += points;
        var previousAlert = run.AlertStatus;

        // Check alert escalation
        if (run.SecurityTally >= run.ShutdownThreshold && run.AlertStatus != "Shutdown")
        {
            run.AlertStatus = "Shutdown";
            _logger.LogWarning("Matrix run {RunId}: SHUTDOWN ALERT triggered", runId);
        }
        else if (run.SecurityTally >= run.ActiveThreshold && run.AlertStatus == "Passive")
        {
            run.AlertStatus = "Active";
            _logger.LogWarning("Matrix run {RunId}: ACTIVE ALERT triggered", runId);
        }
        else if (run.SecurityTally >= run.PassiveThreshold && run.AlertStatus == "None")
        {
            run.AlertStatus = "Passive";
            _logger.LogInformation("Matrix run {RunId}: PASSIVE ALERT triggered", runId);
        }

        await _databaseService.UpdateMatrixRunAsync(run);

        return new SecurityResult
        {
            Success = true,
            SecurityTally = run.SecurityTally,
            PreviousAlert = previousAlert,
            CurrentAlert = run.AlertStatus,
            AlertEscalated = previousAlert != run.AlertStatus,
            Details = $"Security tally: {run.SecurityTally}. Alert: {run.AlertStatus}"
        };
    }

    /// <summary>
    /// Check alert status effects
    /// </summary>
    public Dictionary<string, object> GetAlertEffects(string alertStatus)
    {
        return alertStatus switch
        {
            "None" => new Dictionary<string, object>
            {
                ["ICActivation"] = "Normal",
                ["TraceModifier"] = 0,
                ["ResponseModifier"] = 0
            },
            "Passive" => new Dictionary<string, object>
            {
                ["ICActivation"] = "All Probe IC active",
                ["TraceModifier"] = -2,
                ["ResponseModifier"] = 0
            },
            "Active" => new Dictionary<string, object>
            {
                ["ICActivation"] = "All IC active",
                ["TraceModifier"] = -4,
                ["ResponseModifier"] = +2,
                ["SpawnIC"] = true
            },
            "Shutdown" => new Dictionary<string, object>
            {
                ["ICActivation"] = "All IC active + spawned IC",
                ["TraceModifier"] = -6,
                ["ResponseModifier"] = +4,
                ["ForceLogout"] = "In 3 turns"
            },
            _ => new Dictionary<string, object>()
        };
    }

    #endregion

    #region IC Types

    /// <summary>
    /// Install IC on a host
    /// </summary>
    public async Task<ICResult> InstallICEAsync(int hostId, string iceType, string iceClass, int rating, int activationTally)
    {
        var host = await _databaseService.GetMatrixHostAsync(hostId);
        if (host == null)
            return ICResult.Fail("Host not found.");

        var ice = new HostICE
        {
            HostId = hostId,
            ICEType = iceType,
            ICEClass = iceClass,
            Rating = rating,
            ActivationTally = activationTally,
            IsActive = false
        };

        await _databaseService.AddHostICEAsync(ice);

        return ICResult.Ok($"Installed {iceClass} {iceType} IC (Rating {rating}) on {host.Name}.");
    }

    /// <summary>
    /// Check if IC activates based on security tally
    /// </summary>
    public async Task<List<HostICE>> CheckICEActivationAsync(int runId)
    {
        var run = await _databaseService.GetMatrixRunAsync(runId);
        if (run == null) return new List<HostICE>();

        var hostICE = await _databaseService.GetHostICEAsync(run.HostId);
        var activatingICE = new List<HostICE>();

        foreach (var ice in hostICE)
        {
            if (!ice.IsActive && run.SecurityTally >= ice.ActivationTally)
            {
                ice.IsActive = true;
                activatingICE.Add(ice);
                _logger.LogWarning("IC activated: {Type} ({Class}) Rating {Rating}",
                    ice.ICEType, ice.ICEClass, ice.Rating);
            }
        }

        if (activatingICE.Any())
        {
            await _databaseService.UpdateHostICEAsync(activatingICE);
        }

        return activatingICE;
    }

    /// <summary>
    /// Execute IC attack based on type
    /// </summary>
    public async Task<ICAttackResult> ExecuteICAttackAsync(int runId, int iceId, int deckerDefensePool)
    {
        var run = await _databaseService.GetMatrixRunAsync(runId);
        var ice = await _databaseService.GetHostICEByIdAsync(iceId);

        if (run == null || ice == null)
            return ICAttackResult.Fail("Invalid run or IC.");

        // IC attack pool = Rating
        var attackPool = ice.Rating;
        var attackResult = _diceService.RollShadowrun(attackPool, 4);

        // Decker defense
        var defenseResult = _diceService.RollShadowrun(deckerDefensePool, 4);
        var netSuccesses = Math.Max(0, attackResult.Successes - defenseResult.Successes);

        var result = new ICAttackResult
        {
            Success = true,
            ICEType = ice.ICEType,
            ICEClass = ice.ICEClass,
            Rating = ice.Rating,
            AttackSuccesses = attackResult.Successes,
            DefenseSuccesses = defenseResult.Successes,
            NetSuccesses = netSuccesses
        };

        // Apply effects based on IC type
        result.Effects = GetICEffects(ice, netSuccesses);
        result.Details = FormatICResult(ice, attackResult, defenseResult, netSuccesses);

        // Log encounter
        var encounter = new ActiveICEncounter
        {
            MatrixRunId = runId,
            HostICEId = iceId,
            EncounterLog = result.Details
        };
        await _databaseService.AddICEncounterAsync(encounter);

        return result;
    }

    /// <summary>
    /// Get effects based on IC type
    /// </summary>
    private Dictionary<string, object> GetICEffects(HostICE ice, int netSuccesses)
    {
        return (ice.ICEClass, ice.ICEType) switch
        {
            // White IC - Non-lethal
            ("White", "Probe") => new Dictionary<string, object>
            {
                ["Effect"] = "Reconnaissance",
                ["SecurityIncrease"] = netSuccesses,
                ["Details"] = "Probes decker's system for information"
            },
            ("White", "Trace") => new Dictionary<string, object>
            {
                ["Effect"] = "Trace",
                ["TraceSuccess"] = netSuccesses > 0,
                ["LocationRevealed"] = netSuccesses >= 2,
                ["Details"] = netSuccesses > 0 ? "Physical location traced!" : "Trace failed"
            },
            ("White", "Killer") => new Dictionary<string, object>
            {
                ["Effect"] = "DeckDamage",
                ["Damage"] = netSuccesses * 2,
                ["Details"] = $"Deals {netSuccesses * 2} damage to cyberdeck"
            },

            // Gray IC - Lethal to deck, potentially harmful
            ("Gray", "Blaster") => new Dictionary<string, object>
            {
                ["Effect"] = "DeckDamage",
                ["Damage"] = netSuccesses * 3,
                ["ProgramCorruption"] = netSuccesses >= 3,
                ["Details"] = $"Deals {netSuccesses * 3} deck damage"
            },
            ("Gray", "Tar") => new Dictionary<string, object>
            {
                ["Effect"] = "ProgramDestroy",
                ["ProgramsDestroyed"] = netSuccesses,
                ["Details"] = $"Destroys {netSuccesses} loaded program(s)"
            },
            ("Gray", "Tar Baby") => new Dictionary<string, object>
            {
                ["Effect"] = "Trap",
                ["Trapped"] = netSuccesses > 0,
                ["EscapeModifier"] = netSuccesses * -2,
                ["Details"] = "Traps decker in the system"
            },

            // Black IC - Lethal to decker
            ("Black", "Black IC") => new Dictionary<string, object>
            {
                ["Effect"] = "Biofeedback",
                ["StunDamage"] = netSuccesses * 2,
                ["PhysicalDamage"] = netSuccesses >= 3 ? netSuccesses : 0,
                ["Details"] = $"Biofeedback: {netSuccesses * 2} Stun" + 
                    (netSuccesses >= 3 ? $", {netSuccesses} Physical" : "")
            },
            ("Black", "Black Hammer") => new Dictionary<string, object>
            {
                ["Effect"] = "LethalBiofeedback",
                ["PhysicalDamage"] = netSuccesses * 2,
                ["Details"] = $"LETHAL biofeedback: {netSuccesses * 2} Physical damage"
            },
            _ => new Dictionary<string, object>
            {
                ["Effect"] = "Unknown",
                ["Details"] = "Unknown IC type"
            }
        };
    }

    private string FormatICResult(HostICE ice, ShadowrunDiceResult attack, ShadowrunDiceResult defense, int net)
    {
        return $"**{ice.ICEClass} {ice.ICEType} IC Attack**\n" +
               $"IC Rating: {ice.Rating}\n" +
               $"Attack: {attack.Successes} successes\n" +
               $"Defense: {defense.Successes} successes\n" +
               $"Net: {net}\n";
    }

    #endregion

    #region Matrix Actions

    /// <summary>
    /// Attempt to access a subsystem
    /// </summary>
    public async Task<MatrixActionResult> AccessSubsystemAsync(int runId, string subsystem, int hackingPool)
    {
        var run = await _databaseService.GetMatrixRunAsync(runId);
        if (run == null)
            return MatrixActionResult.Fail("Matrix run not found.");

        var host = await _databaseService.GetMatrixHostAsync(run.HostId);
        var rating = GetEffectiveRating(host, subsystem);

        // Target number = System Rating
        var result = _diceService.RollShadowrun(hackingPool, rating);

        if (result.Successes > 0)
        {
            // Success - reduce security tally
            run.SecurityTally = Math.Max(0, run.SecurityTally - result.Successes);
            await _databaseService.UpdateMatrixRunAsync(run);
        }
        else
        {
            // Failure - increase security tally
            var securityResult = await AddToSecurityTallyAsync(runId, 2);
        }

        return new MatrixActionResult
        {
            Success = result.Successes > 0,
            Action = $"Access {subsystem}",
            Rating = rating,
            DiceRolled = hackingPool,
            Successes = result.Successes,
            Details = result.Details
        };
    }

    /// <summary>
    /// Attempt to crash/defeat IC
    /// </summary>
    public async Task<MatrixActionResult> CrashICEAsync(int runId, int iceId, int attackPool)
    {
        var ice = await _databaseService.GetHostICEByIdAsync(iceId);
        if (ice == null)
            return MatrixActionResult.Fail("IC not found.");

        // Target = IC Rating
        var result = _diceService.RollShadowrun(attackPool, ice.Rating);

        if (result.Successes >= ice.Rating)
        {
            ice.IsActive = false;
            await _databaseService.UpdateHostICEAsync(ice);

            // Reduce security on success
            var run = await _databaseService.GetMatrixRunAsync(runId);
            if (run != null)
            {
                run.SecurityTally = Math.Max(0, run.SecurityTally - ice.Rating);
                await _databaseService.UpdateMatrixRunAsync(run);
            }
        }
        else
        {
            // Failed - increase security
            await AddToSecurityTallyAsync(runId, 3);
        }

        return new MatrixActionResult
        {
            Success = result.Successes >= ice.Rating,
            Action = $"Crash {ice.ICEType} IC",
            Rating = ice.Rating,
            DiceRolled = attackPool,
            Successes = result.Successes,
            Details = result.Successes >= ice.Rating
                ? $"Crashed {ice.ICEType} IC!"
                : $"Failed to crash IC. Security increased."
        };
    }

    #endregion
}

#region Result Types

public record MatrixRunResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? RunId { get; init; }

    public static MatrixRunResult Ok(string message, int? runId = null) => new() { Success = true, Message = message, RunId = runId };
    public static MatrixRunResult Fail(string message) => new() { Success = false, Message = message };
}

public record SecurityResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public int SecurityTally { get; init; }
    public string PreviousAlert { get; init; } = "None";
    public string CurrentAlert { get; init; } = "None";
    public bool AlertEscalated { get; init; }

    public static SecurityResult Fail(string details) => new() { Success = false, Details = details };
}

public record ICResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ICResult Ok(string message) => new() { Success = true, Message = message };
    public static ICResult Fail(string message) => new() { Success = false, Message = message };
}

public record ICAttackResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string ICEType { get; init; } = string.Empty;
    public string ICEClass { get; init; } = "White";
    public int Rating { get; init; }
    public int AttackSuccesses { get; init; }
    public int DefenseSuccesses { get; init; }
    public int NetSuccesses { get; init; }
    public Dictionary<string, object> Effects { get; init; } = new();

    public static ICAttackResult Fail(string details) => new() { Success = false, Details = details };
}

public record MatrixActionResult
{
    public bool Success { get; init; }
    public string Action { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public int Rating { get; init; }
    public int DiceRolled { get; init; }
    public int Successes { get; init; }
}

#endregion
