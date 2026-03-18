namespace ShadowrunDiscordBot.Mappers;

/// <summary>
/// Mapper between old Models combat types and Domain.Entities combat types
/// </summary>
public static class CombatMapper
{
    /// <summary>
    /// Convert from old CombatSession model to new CombatSession domain entity
    /// </summary>
    public static Domain.Entities.CombatSession ToDomain(Models.CombatSession model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var entity = new Domain.Entities.CombatSession
        {
            Id = model.Id,
            DiscordChannelId = model.DiscordChannelId,
            IsActive = model.IsActive,
            StartedAt = model.StartedAt,
            EndedAt = model.EndedAt,
            CreatedAt = model.StartedAt,
            UpdatedAt = DateTime.UtcNow
        };

        // Map participants (simplified - Domain version uses Character objects)
        foreach (var modelParticipant in model.Participants)
        {
            if (modelParticipant.Character != null)
            {
                entity.Participants.Add(CharacterMapper.ToDomain(modelParticipant.Character));
            }
        }

        return entity;
    }

    /// <summary>
    /// Convert from new CombatSession domain entity to old CombatSession model
    /// </summary>
    public static Models.CombatSession ToModel(Domain.Entities.CombatSession entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var model = new Models.CombatSession
        {
            Id = entity.Id,
            DiscordChannelId = entity.DiscordChannelId,
            IsActive = entity.IsActive,
            StartedAt = entity.StartedAt,
            EndedAt = entity.EndedAt
        };

        // Map participants back
        foreach (var entityParticipant in entity.Participants)
        {
            model.Participants.Add(new Models.CombatParticipant
            {
                CombatSessionId = entity.Id,
                CharacterId = entityParticipant.Id,
                Name = entityParticipant.Name,
                Type = "PC",
                Initiative = 0, // Would need to be tracked separately
                InitiativePasses = 1,
                CombatSession = model
            });
        }

        return model;
    }

    /// <summary>
    /// Convert CombatParticipant model to domain representation
    /// </summary>
    public static Domain.Entities.CombatParticipant ToDomainParticipant(Models.CombatParticipant model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new Domain.Entities.CombatParticipant
        {
            Id = model.Id,
            CombatSessionId = model.CombatSessionId,
            // Map other properties as needed
        };
    }

    /// <summary>
    /// Convert DiceRollResult model to domain entity
    /// </summary>
    public static Domain.Entities.DiceRollResult ToDomain(Models.DiceRollResult model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new Domain.Entities.DiceRollResult
        {
            Id = model.Id,
            // Map properties - DiceRollResult structure may differ between model and entity
        };
    }

    /// <summary>
    /// Convert DiceRollResult domain entity to model
    /// </summary>
    public static Models.DiceRollResult ToModel(Domain.Entities.DiceRollResult entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new Models.DiceRollResult
        {
            Id = entity.Id,
            // Map properties back
        };
    }
}
