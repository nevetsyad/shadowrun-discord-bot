using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Mappers;

/// <summary>
/// Mapper between ShadowrunCharacter (old model) and Character (domain entity)
/// </summary>
public static class CharacterMapper
{
    /// <summary>
    /// Convert domain entity to old model (for backward compatibility)
    /// </summary>
    public static ShadowrunCharacter ToModel(Character entity)
    {
        if (entity == null) return null!;

        var model = new ShadowrunCharacter
        {
            Id = entity.Id,
            Name = entity.Name,
            DiscordUserId = entity.DiscordUserId,
            Metatype = entity.Metatype,
            Archetype = entity.Archetype,
            BaseBody = entity.BaseBody,
            BaseQuickness = entity.BaseQuickness,
            BaseStrength = entity.BaseStrength,
            BaseCharisma = entity.BaseCharisma,
            BaseIntelligence = entity.BaseIntelligence,
            BaseWillpower = entity.BaseWillpower,
            Body = entity.Body,
            Quickness = entity.Quickness,
            Strength = entity.Strength,
            Charisma = entity.Charisma,
            Intelligence = entity.Intelligence,
            Willpower = entity.Willpower,
            Essence = entity.Essence,
            EssenceDecimal = entity.EssenceDecimal,
            BioIndex = entity.BioIndex,
            Magic = entity.Magic,
            InitiationGrade = entity.InitiationGrade,
            Karma = entity.Karma,
            Nuyen = entity.Nuyen,
            PhysicalDamage = entity.PhysicalDamage,
            StunDamage = entity.StunDamage,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        // Copy collections
        model.Skills = entity.Skills.Select(s => new CharacterSkill
        {
            CharacterId = entity.Id,
            Name = s.Name,
            Rating = s.Rating,
            Specialization = s.Specialization
        }).ToList();

        model.Cyberware = entity.Cyberware.Select(c => new CharacterCyberware
        {
            CharacterId = entity.Id,
            Name = c.Name,
            Type = c.Type,
            Rating = c.Rating,
            Source = c.Source
        }).ToList();

        model.Spells = entity.Spells.Select(s => new CharacterSpell
        {
            CharacterId = entity.Id,
            Name = s.Name,
            Type = s.Type,
            Rating = s.Rating,
            Force = s.Force,
            TrickSpell = s.TrickSpell
        }).ToList();

        model.Spirits = entity.Spirits.Select(s => new CharacterSpirit
        {
            CharacterId = entity.Id,
            Name = s.Name,
            Type = s.Type,
            Force = s.Force,
            Service = s.Service
        }).ToList();

        model.Gear = entity.Gear.Select(g => new CharacterGear
        {
            CharacterId = entity.Id,
            Name = g.Name,
            Quantity = g.Quantity,
            Comment = g.Comment
        }).ToList();

        return model;
    }

    /// <summary>
    /// Convert old model to domain entity
    /// </summary>
    public static Character ToEntity(ShadowrunCharacter model)
    {
        if (model == null) return null!;

        var entity = Character.Create(
            name: model.Name,
            discordUserId: model.DiscordUserId,
            metatype: model.Metatype,
            archetype: model.Archetype ?? "Custom",
            archetypeId: 0, // Not available from old model
            baseBody: model.BaseBody,
            baseQuickness: model.BaseQuickness,
            baseStrength: model.BaseStrength,
            baseCharisma: model.BaseCharisma,
            baseIntelligence: model.BaseIntelligence,
            baseWillpower: model.BaseWillpower
        );

        // Update calculated properties
        entity.Essence = model.Essence;
        entity.BioIndex = model.BioIndex;
        entity.Magic = model.Magic;
        entity.InitiationGrade = model.InitiationGrade;
        entity.Karma = model.Karma;
        entity.Nuyen = model.Nuyen;
        entity.PhysicalDamage = model.PhysicalDamage;
        entity.StunDamage = model.StunDamage;
        entity.UpdatedAt = model.UpdatedAt;

        // Copy collections
        entity._skills.Clear();
        entity._skills.AddRange(model.Skills.Select(s => new CharacterSkill
        {
            Name = s.Name,
            Rating = s.Rating,
            Specialization = s.Specialization
        }));

        entity._cyberware.Clear();
        entity._cyberware.AddRange(model.Cyberware.Select(c => new CharacterCyberware
        {
            Name = c.Name,
            Type = c.Type,
            Rating = c.Rating,
            Source = c.Source
        }));

        entity._spells.Clear();
        entity._spells.AddRange(model.Spells.Select(s => new CharacterSpell
        {
            Name = s.Name,
            Type = s.Type,
            Rating = s.Rating,
            Force = s.Force,
            TrickSpell = s.TrickSpell
        }));

        entity._spirits.Clear();
        entity._spirits.AddRange(model.Spirits.Select(s => new CharacterSpirit
        {
            Name = s.Name,
            Type = s.Type,
            Force = s.Force,
            Service = s.Service
        }));

        entity._gear.Clear();
        entity._gear.AddRange(model.Gear.Select(g => new CharacterGear
        {
            Name = g.Name,
            Quantity = g.Quantity,
            Comment = g.Comment
        }));

        return entity;
    }
}
