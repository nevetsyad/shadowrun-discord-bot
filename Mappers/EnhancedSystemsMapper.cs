using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Mappers;

/// <summary>
/// Mappers for Enhanced Systems (CombatPoolState, Vehicle, Drone, etc.)
/// </summary>
public static class EnhancedSystemsMapper
{
    #region CombatPoolState Mappers

    /// <summary>
    /// Convert CombatPoolState model to Domain entity
    /// </summary>
    public static Domain.Entities.CombatPoolState ToDomain(Models.CombatPoolState model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new Domain.Entities.CombatPoolState
        {
            Id = model.Id,
            CharacterId = model.CharacterId,
            CombatSessionId = model.CombatSessionId,
            TotalPool = model.TotalPool,
            AllocatedToAttack = model.AllocatedToAttack,
            AllocatedToDefense = model.AllocatedToDefense,
            AllocatedToDamage = model.AllocatedToDamage,
            AllocatedToOther = model.AllocatedToOther,
            CurrentTurn = model.CurrentTurn
        };
    }

    /// <summary>
    /// Convert CombatPoolState Domain entity to Model
    /// </summary>
    public static Models.CombatPoolState ToModel(Domain.Entities.CombatPoolState entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new Models.CombatPoolState
        {
            Id = entity.Id,
            CharacterId = entity.CharacterId,
            CombatSessionId = entity.CombatSessionId,
            TotalPool = entity.TotalPool,
            AllocatedToAttack = entity.AllocatedToAttack,
            AllocatedToDefense = entity.AllocatedToDefense,
            AllocatedToDamage = entity.AllocatedToDamage,
            AllocatedToOther = entity.AllocatedToOther,
            CurrentTurn = entity.CurrentTurn
        };
    }

    #endregion

    #region Vehicle Mappers

    /// <summary>
    /// Convert Vehicle model to Domain entity
    /// </summary>
    public static Domain.Entities.Vehicle ToDomain(Models.Vehicle model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var entity = new Domain.Entities.Vehicle
        {
            Id = model.Id,
            CharacterId = model.CharacterId,
            Name = model.Name,
            VehicleType = model.VehicleType,
            Body = model.Body,
            Armor = model.Armor,
            Speed = model.Speed,
            Acceleration = model.Acceleration,
            Handling = model.Handling,
            ManeuverScore = model.ManeuverScore,
            SensorRating = model.SensorRating,
            Signature = model.Signature,
            AutopilotRating = model.AutopilotRating,
            Seats = model.Seats,
            CargoCapacity = model.CargoCapacity,
            Cost = model.Cost,
            Availability = model.Availability,
            StreetIndex = model.StreetIndex,
            Concealability = model.Concealability
        };

        // Map weapons
        if (model.Weapons != null)
        {
            foreach (var weapon in model.Weapons)
            {
                entity.Weapons.Add(new Domain.Entities.VehicleWeapon
                {
                    VehicleId = entity.Id,
                    Name = weapon.Name,
                    Type = weapon.Type,
                    Damage = weapon.Damage,
                    AmmoCapacity = weapon.AmmoCapacity,
                    AmmoRemaining = weapon.AmmoRemaining,
                    FireModes = weapon.FireModes,
                    Skill = weapon.Skill,
                    Range = weapon.Range,
                    RecoilCompensation = weapon.RecoilCompensation,
                    MountLocation = weapon.MountLocation
                });
            }
        }

        return entity;
    }

    /// <summary>
    /// Convert Vehicle Domain entity to Model
    /// </summary>
    public static Models.Vehicle ToModel(Domain.Entities.Vehicle entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var model = new Models.Vehicle
        {
            Id = entity.Id,
            CharacterId = entity.CharacterId,
            Name = entity.Name,
            VehicleType = entity.VehicleType,
            Body = entity.Body,
            Armor = entity.Armor,
            Speed = entity.Speed,
            Acceleration = entity.Acceleration,
            Handling = entity.Handling,
            ManeuverScore = entity.ManeuverScore,
            SensorRating = entity.SensorRating,
            Signature = entity.Signature,
            AutopilotRating = entity.AutopilotRating,
            Seats = entity.Seats,
            CargoCapacity = entity.CargoCapacity,
            Cost = entity.Cost,
            Availability = entity.Availability,
            StreetIndex = entity.StreetIndex,
            Concealability = entity.Concealability
        };

        // Map weapons
        if (entity.Weapons != null)
        {
            foreach (var weapon in entity.Weapons)
            {
                model.Weapons.Add(new Models.VehicleWeapon
                {
                    VehicleId = model.Id,
                    Name = weapon.Name,
                    Type = weapon.Type,
                    Damage = weapon.Damage,
                    AmmoCapacity = weapon.AmmoCapacity,
                    AmmoRemaining = weapon.AmmoRemaining,
                    FireModes = weapon.FireModes,
                    Skill = weapon.Skill,
                    Range = weapon.Range,
                    RecoilCompensation = weapon.RecoilCompensation,
                    MountLocation = weapon.MountLocation
                });
            }
        }

        return model;
    }

    #endregion

    #region Drone Mappers

    /// <summary>
    /// Convert Drone model to Domain entity
    /// </summary>
    public static Domain.Entities.Drone ToDomain(Models.Drone model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var entity = new Domain.Entities.Drone
        {
            Id = model.Id,
            CharacterId = model.CharacterId,
            Name = model.Name,
            VehicleType = model.VehicleType,
            Body = model.Body,
            Armor = model.Armor,
            Speed = model.Speed,
            Acceleration = model.Acceleration,
            Handling = model.Handling,
            ManeuverScore = model.ManeuverScore,
            SensorRating = model.SensorRating,
            Signature = model.Signature,
            AutopilotRating = model.AutopilotRating,
            Seats = model.Seats,
            CargoCapacity = model.CargoCapacity,
            Cost = model.Cost,
            Availability = model.Availability,
            StreetIndex = model.StreetIndex,
            Concealability = model.Concealability,
            // Drone-specific properties
            DroneBody = model.DroneBody,
            DroneArmor = model.DroneArmor,
            SpeedRating = model.SpeedRating,
            HandlingRating = model.HandlingRating,
            Autopilot = model.Autopilot,
            SensorRatingDrone = model.SensorRatingDrone,
            SignatureDrone = model.SignatureDrone,
            LoadRating = model.LoadRating,
            SignalRating = model.SignalRating
        };

        // Map weapons
        if (model.Weapons != null)
        {
            foreach (var weapon in model.Weapons)
            {
                entity.Weapons.Add(new Domain.Entities.VehicleWeapon
                {
                    VehicleId = entity.Id,
                    Name = weapon.Name,
                    Type = weapon.Type,
                    Damage = weapon.Damage,
                    AmmoCapacity = weapon.AmmoCapacity,
                    AmmoRemaining = weapon.AmmoRemaining,
                    FireModes = weapon.FireModes,
                    Skill = weapon.Skill,
                    Range = weapon.Range,
                    RecoilCompensation = weapon.RecoilCompensation,
                    MountLocation = weapon.MountLocation
                });
            }

            // Map autosofts
            foreach (var autosoft in model.Autosofts)
            {
                entity.Autosofts.Add(new Domain.Entities.DroneAutosoft
                {
                    DroneId = entity.Id,
                    Name = autosoft.Name,
                    Type = autosoft.Type,
                    Rating = autosoft.Rating
                });
            }
        }

        return entity;
    }

    /// <summary>
    /// Convert Drone Domain entity to Model
    /// </summary>
    public static Models.Drone ToModel(Domain.Entities.Drone entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var model = new Models.Drone
        {
            Id = entity.Id,
            CharacterId = entity.CharacterId,
            Name = entity.Name,
            VehicleType = entity.VehicleType,
            Body = entity.Body,
            Armor = entity.Armor,
            Speed = entity.Speed,
            Acceleration = entity.Acceleration,
            Handling = entity.Handling,
            ManeuverScore = entity.ManeuverScore,
            SensorRating = entity.SensorRating,
            Signature = entity.Signature,
            AutopilotRating = entity.AutopilotRating,
            Seats = entity.Seats,
            CargoCapacity = entity.CargoCapacity,
            Cost = entity.Cost,
            Availability = entity.Availability,
            StreetIndex = entity.StreetIndex,
            Concealability = entity.Concealability,
            // Drone-specific properties
            DroneBody = entity.DroneBody,
            DroneArmor = entity.DroneArmor,
            SpeedRating = entity.SpeedRating,
            HandlingRating = entity.HandlingRating,
            Autopilot = entity.Autopilot,
            SensorRatingDrone = entity.SensorRatingDrone,
            SignatureDrone = entity.SignatureDrone,
            LoadRating = entity.LoadRating,
            SignalRating = entity.SignalRating
        };

        // Map weapons
        if (entity.Weapons != null)
        {
            foreach (var weapon in entity.Weapons)
            {
                model.Weapons.Add(new Models.VehicleWeapon
                {
                    VehicleId = model.Id,
                    Name = weapon.Name,
                    Type = weapon.Type,
                    Damage = weapon.Damage,
                    AmmoCapacity = weapon.AmmoCapacity,
                    AmmoRemaining = weapon.AmmoRemaining,
                    FireModes = weapon.FireModes,
                    Skill = weapon.Skill,
                    Range = weapon.Range,
                    RecoilCompensation = weapon.RecoilCompensation,
                    MountLocation = weapon.MountLocation
                });
            }

            // Map autosofts
            foreach (var autosoft in entity.Autosofts)
            {
                model.Autosofts.Add(new Models.DroneAutosoft
                {
                    DroneId = model.Id,
                    Name = autosoft.Name,
                    Type = autosoft.Type,
                    Rating = autosoft.Rating
                });
            }
        }

        return model;
    }

    #endregion
}
