using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Vehicle and Drone combat service for SR3
/// </summary>
public class VehicleCombatService
{
    private readonly DiceService _diceService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<VehicleCombatService> _logger;

    public VehicleCombatService(
        DiceService diceService,
        DatabaseService databaseService,
        ILogger<VehicleCombatService> logger)
    {
        _diceService = diceService;
        _databaseService = databaseService;
        _logger = logger;
    }

    #region Vehicle Management

    /// <summary>
    /// Create a vehicle for a character
    /// </summary>
    public async Task<Vehicle> CreateVehicleAsync(
        int characterId,
        string name,
        string vehicleType,
        int body,
        int armor,
        int speed,
        int handling,
        int sensor)
    {
        var vehicle = new Vehicle
        {
            CharacterId = characterId,
            Name = name,
            VehicleType = vehicleType,
            Body = body,
            Armor = armor,
            Speed = speed,
            Handling = handling,
            SensorRating = sensor,
            ManeuverScore = handling // Base maneuver = handling
        };

        await _databaseService.AddVehicleAsync(vehicle);

        _logger.LogInformation("Created vehicle {Name} for character {CharId}",
            name, characterId);

        return vehicle;
    }

    /// <summary>
    /// Calculate maneuver score with driver skill
    /// </summary>
    public int CalculateManeuverScore(Vehicle vehicle, int pilotSkill)
    {
        // SR3: Maneuver Score = Handling + Pilot Skill
        return vehicle.Handling + pilotSkill;
    }

    /// <summary>
    /// Install rigger adaptation on vehicle
    /// </summary>
    public async Task<Vehicle> InstallRiggerAdaptationAsync(int vehicleId, int controlRigRating)
    {
        var vehicle = await _databaseService.GetVehicleAsync(vehicleId);
        if (vehicle == null)
            throw new InvalidOperationException("Vehicle not found.");

        vehicle.HasRiggerAdaptation = true;
        vehicle.RiggerControlRating = controlRigRating;

        await _databaseService.UpdateVehicleAsync(vehicle);

        _logger.LogInformation("Installed Control Rig {Rating} on vehicle {Name}",
            controlRigRating, vehicle.Name);

        return vehicle;
    }

    #endregion

    #region Sensor Tests

    /// <summary>
    /// Perform sensor test to detect targets
    /// </summary>
    public async Task<SensorTestResult> SensorTestAsync(
        int vehicleId,
        string targetSignature,
        int range,
        int environmentalModifier = 0)
    {
        var vehicle = await _databaseService.GetVehicleAsync(vehicleId);
        if (vehicle == null)
            return SensorTestResult.Fail("Vehicle not found.");

        // Sensor test pool = Sensor Rating
        var pool = vehicle.SensorRating;
        var targetNumber = 4 + environmentalModifier;

        // Range modifiers
        targetNumber += GetRangeModifier(range, vehicle.SensorRating);

        var result = _diceService.RollShadowrun(pool, targetNumber);

        var detected = result.Successes > 0;
        var detailedInfo = result.Successes >= 3;

        return new SensorTestResult
        {
            Success = true,
            TargetSignature = targetSignature,
            SensorRating = vehicle.SensorRating,
            TargetNumber = targetNumber,
            Successes = result.Successes,
            Detected = detected,
            DetailedInfo = detailedInfo,
            Details = detected
                ? $"Target detected! ({result.Successes} successes)"
                : "Target not detected."
        };
    }

    private int GetRangeModifier(int range, int sensorRating)
    {
        // Sensor range categories in km
        var shortRange = sensorRating * 10;
        var mediumRange = sensorRating * 50;
        var longRange = sensorRating * 100;

        if (range <= shortRange) return 0;
        if (range <= mediumRange) return 2;
        if (range <= longRange) return 4;
        return 8; // Extreme range
    }

    #endregion

    #region Vehicle Combat

    /// <summary>
    /// Add vehicle to combat session
    /// </summary>
    public async Task<VehicleCombatant> AddVehicleToCombatAsync(
        int combatSessionId,
        int vehicleId,
        int driverReaction,
        int pilotSkill = 0)
    {
        var vehicle = await _databaseService.GetVehicleAsync(vehicleId);
        if (vehicle == null)
            throw new InvalidOperationException("Vehicle not found.");

        // Create vehicle combat session if needed
        var vehicleCombatSession = await GetOrCreateVehicleCombatSessionAsync(combatSessionId);

        // Roll vehicle initiative
        // SR3: Vehicle Initiative = Driver Reaction + 1D6 (2D6 for riggers)
        var initDice = vehicle.HasRiggerAdaptation ? 2 : 1;
        var initResult = _diceService.RollInitiative(driverReaction, initDice);

        var combatant = new VehicleCombatant
        {
            VehicleCombatSessionId = vehicleCombatSession.Id,
            VehicleId = vehicleId,
            Initiative = initResult.Total,
            InitiativePasses = initResult.Passes,
            HasActed = false
        };

        await _databaseService.AddVehicleCombatantAsync(combatant);

        return combatant;
    }

    private async Task<VehicleCombatSession> GetOrCreateVehicleCombatSessionAsync(int combatSessionId)
    {
        var existing = await _databaseService.GetVehicleCombatSessionAsync(combatSessionId);
        if (existing != null) return existing;

        var newSession = new VehicleCombatSession
        {
            CombatSessionId = combatSessionId,
            CurrentRange = 100,
            CurrentSpeed = 0
        };

        await _databaseService.AddVehicleCombatSessionAsync(newSession);
        return newSession;
    }

    /// <summary>
    /// Perform vehicle attack
    /// </summary>
    public async Task<VehicleAttackResult> VehicleAttackAsync(
        int vehicleId,
        string targetName,
        int weaponIndex,
        int gunnerySkill,
        int sensorBonus = 0,
        bool isSensorEnhanced = true)
    {
        var vehicle = await _databaseService.GetVehicleAsync(vehicleId);
        if (vehicle == null)
            return VehicleAttackResult.Fail("Vehicle not found.");

        var weapon = vehicle.Weapons?.ElementAtOrDefault(weaponIndex);
        if (weapon == null)
            return VehicleAttackResult.Fail("Weapon not found.");

        // Calculate attack pool
        int attackPool;
        if (isSensorEnhanced)
        {
            // Sensor-enhanced gunnery: Gunnery Skill + Sensor Rating
            attackPool = gunnerySkill + vehicle.SensorRating;
        }
        else
        {
            // Manual gunnery: Gunnery Skill only (or reduced sensor)
            attackPool = gunnerySkill + sensorBonus;
        }

        var result = _diceService.RollShadowrun(attackPool, 4);

        // Calculate damage
        var baseDamage = weapon.DamageCode;
        var netSuccesses = result.Successes;
        var finalDamage = baseDamage + netSuccesses;

        _logger.LogInformation("Vehicle attack: {Vehicle} -> {Target}, Successes: {Successes}, Damage: {Damage}",
            vehicle.Name, targetName, result.Successes, finalDamage);

        return new VehicleAttackResult
        {
            Success = true,
            VehicleName = vehicle.Name,
            WeaponName = weapon.Name,
            TargetName = targetName,
            AttackPool = attackPool,
            Successes = result.Successes,
            BaseDamage = baseDamage,
            FinalDamage = finalDamage,
            DamageType = weapon.DamageType,
            Details = $"{vehicle.Name} attacks {targetName} with {weapon.Name}: {result.Details}"
        };
    }

    /// <summary>
    /// Vehicle damage resistance
    /// </summary>
    public async Task<VehicleDamageResult> ResistVehicleDamageAsync(
        int vehicleId,
        int incomingDamage,
        string damageType)
    {
        var vehicle = await _databaseService.GetVehicleAsync(vehicleId);
        if (vehicle == null)
            return VehicleDamageResult.Fail("Vehicle not found.");

        // Vehicle resistance = Body + Armor
        var pool = vehicle.Body + vehicle.Armor;
        var result = _diceService.RollShadowrun(pool, 4);

        var actualDamage = Math.Max(0, incomingDamage - result.Successes);
        vehicle.CurrentDamage += actualDamage;

        await _databaseService.UpdateVehicleAsync(vehicle);

        var destroyed = vehicle.CurrentDamage >= vehicle.ConditionMonitor;

        return new VehicleDamageResult
        {
            Success = true,
            VehicleName = vehicle.Name,
            IncomingDamage = incomingDamage,
            ResistedDamage = result.Successes,
            ActualDamage = actualDamage,
            CurrentDamage = vehicle.CurrentDamage,
            MaxDamage = vehicle.ConditionMonitor,
            Destroyed = destroyed,
            Details = destroyed
                ? $"{vehicle.Name} DESTROYED!"
                : $"{vehicle.Name} took {actualDamage} damage. ({vehicle.CurrentDamage}/{vehicle.ConditionMonitor})"
        };
    }

    /// <summary>
    /// Maneuver test for vehicle actions
    /// </summary>
    public async Task<ManeuverResult> ManeuverTestAsync(
        int vehicleId,
        string maneuverType,
        int pilotSkill,
        int modifier = 0)
    {
        var vehicle = await _databaseService.GetVehicleAsync(vehicleId);
        if (vehicle == null)
            return ManeuverResult.Fail("Vehicle not found.");

        var maneuverScore = CalculateManeuverScore(vehicle, pilotSkill);
        var targetNumber = 4 + modifier;

        var result = _diceService.RollShadowrun(maneuverScore, targetNumber);

        return new ManeuverResult
        {
            Success = result.Successes > 0,
            ManeuverType = maneuverType,
            ManeuverScore = maneuverScore,
            TargetNumber = targetNumber,
            Successes = result.Successes,
            Details = result.Successes > 0
                ? $"{maneuverType} successful! ({result.Successes} successes)"
                : $"{maneuverType} failed."
        };
    }

    #endregion

    #region Drone Control

    /// <summary>
    /// Create a drone
    /// </summary>
    public async Task<Drone> CreateDroneAsync(
        int characterId,
        string name,
        string model,
        int body,
        int armor,
        int pilot,
        int sensor)
    {
        var drone = new Drone
        {
            CharacterId = characterId,
            Name = name,
            DroneModel = model,
            VehicleType = "Drone",
            Body = body,
            Armor = armor,
            PilotRating = pilot,
            SensorRating = sensor,
            ControlMode = "Autonomous",
            Handling = pilot, // Drones use Pilot for handling
            ManeuverScore = pilot
        };

        await _databaseService.AddDroneAsync(drone);

        _logger.LogInformation("Created drone {Name} for character {CharId}",
            name, characterId);

        return drone;
    }

    /// <summary>
    /// Set drone control mode
    /// </summary>
    public async Task<DroneControlResult> SetDroneControlModeAsync(
        int droneId,
        string controlMode,
        int riggerReaction = 0)
    {
        var drone = await _databaseService.GetDroneAsync(droneId);
        if (drone == null)
            return DroneControlResult.Fail("Drone not found.");

        drone.ControlMode = controlMode;

        int initiative = 0;
        int initiativePasses = 1;

        switch (controlMode.ToLower())
        {
            case "autonomous":
                // Autonomous: Pilot Rating + 1D6
                var autoInit = _diceService.RollInitiative(drone.PilotRating, 1);
                initiative = autoInit.Total;
                initiativePasses = autoInit.Passes;
                break;

            case "remote":
                // Remote: Reaction + 1D6
                var remoteInit = _diceService.RollInitiative(riggerReaction, 1);
                initiative = remoteInit.Total;
                initiativePasses = remoteInit.Passes;
                break;

            case "rigged":
                // Rigged: Reaction + 2D6 (VR mode)
                var riggedInit = _diceService.RollInitiative(riggerReaction, 2);
                initiative = riggedInit.Total;
                initiativePasses = riggedInit.Passes;
                break;
        }

        await _databaseService.UpdateDroneAsync(drone);

        return new DroneControlResult
        {
            Success = true,
            DroneName = drone.Name,
            ControlMode = controlMode,
            Initiative = initiative,
            InitiativePasses = initiativePasses,
            Details = $"Drone {drone.Name} set to {controlMode} mode. Initiative: {initiative}"
        };
    }

    /// <summary>
    /// Install autosoft on drone
    /// </summary>
    public async Task<AutosoftResult> InstallAutosoftAsync(int droneId, string name, int rating)
    {
        var drone = await _databaseService.GetDroneAsync(droneId);
        if (drone == null)
            return AutosoftResult.Fail("Drone not found.");

        var autosoft = new DroneAutosoft
        {
            DroneId = droneId,
            Name = name,
            Rating = rating
        };

        await _databaseService.AddDroneAutosoftAsync(autosoft);

        return AutosoftResult.Ok($"Installed {name} autosoft (Rating {rating}) on {drone.Name}");
    }

    /// <summary>
    /// Drone attack using autosofts
    /// </summary>
    public async Task<DroneAttackResult> DroneAttackAsync(
        int droneId,
        string targetName,
        int targetingAutosoftRating,
        int weaponDamage)
    {
        var drone = await _databaseService.GetDroneAsync(droneId);
        if (drone == null)
            return DroneAttackResult.Fail("Drone not found.");

        // Attack pool = Pilot + Targeting Autosoft
        var attackPool = drone.PilotRating + targetingAutosoftRating;
        var result = _diceService.RollShadowrun(attackPool, 4);

        var finalDamage = weaponDamage + result.Successes;

        return new DroneAttackResult
        {
            Success = result.Successes > 0,
            DroneName = drone.Name,
            TargetName = targetName,
            AttackPool = attackPool,
            Successes = result.Successes,
            Damage = finalDamage,
            Details = $"{drone.Name} attacks {targetName}: {result.Details}"
        };
    }

    #endregion
}

#region Result Types

public record SensorTestResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string TargetSignature { get; init; } = string.Empty;
    public int SensorRating { get; init; }
    public int TargetNumber { get; init; }
    public int Successes { get; init; }
    public bool Detected { get; init; }
    public bool DetailedInfo { get; init; }

    public static SensorTestResult Fail(string details) => new() { Success = false, Details = details };
}

public record VehicleAttackResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string VehicleName { get; init; } = string.Empty;
    public string WeaponName { get; init; } = string.Empty;
    public string TargetName { get; init; } = string.Empty;
    public int AttackPool { get; init; }
    public int Successes { get; init; }
    public int BaseDamage { get; init; }
    public int FinalDamage { get; init; }
    public string DamageType { get; init; } = "Physical";

    public static VehicleAttackResult Fail(string details) => new() { Success = false, Details = details };
}

public record VehicleDamageResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string VehicleName { get; init; } = string.Empty;
    public int IncomingDamage { get; init; }
    public int ResistedDamage { get; init; }
    public int ActualDamage { get; init; }
    public int CurrentDamage { get; init; }
    public int MaxDamage { get; init; }
    public bool Destroyed { get; init; }

    public static VehicleDamageResult Fail(string details) => new() { Success = false, Details = details };
}

public record ManeuverResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string ManeuverType { get; init; } = string.Empty;
    public int ManeuverScore { get; init; }
    public int TargetNumber { get; init; }
    public int Successes { get; init; }

    public static ManeuverResult Fail(string details) => new() { Success = false, Details = details };
}

public record DroneControlResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string DroneName { get; init; } = string.Empty;
    public string ControlMode { get; init; } = "Autonomous";
    public int Initiative { get; init; }
    public int InitiativePasses { get; init; }

    public static DroneControlResult Fail(string details) => new() { Success = false, Details = details };
}

public record AutosoftResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static AutosoftResult Ok(string message) => new() { Success = true, Message = message };
    public static AutosoftResult Fail(string message) => new() { Success = false, Message = message };
}

public record DroneAttackResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string DroneName { get; init; } = string.Empty;
    public string TargetName { get; init; } = string.Empty;
    public int AttackPool { get; init; }
    public int Successes { get; init; }
    public int Damage { get; init; }

    public static DroneAttackResult Fail(string details) => new() { Success = false, Details = details };
}

#endregion
