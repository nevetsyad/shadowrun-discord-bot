using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Mappers;

/// <summary>
/// Mapper between Domain.MatrixSession and old Models.MatrixRun namespace.
/// </summary>
public static class MatrixRunMapper
{
    /// <summary>
    /// Convert from MatrixRun model to MatrixSession domain entity
    /// </summary>
    public static MatrixSession ToDomain(MatrixRun model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var entity = new MatrixSession
        {
            Id = model.Id,
            CharacterId = model.CharacterId,
            IsInVR = true, // Default to VR for active runs
            SecurityTally = model.SecurityTally,
            AlertLevel = model.AlertStatus
        };

        return entity;
    }

    /// <summary>
    /// Convert from MatrixSession domain entity to MatrixRun model
    /// </summary>
    public static MatrixRun ToModel(MatrixSession entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var model = new MatrixRun
        {
            Id = entity.Id,
            CharacterId = entity.CharacterId,
            SecurityTally = entity.SecurityTally,
            AlertStatus = entity.AlertLevel,
            StartedAt = DateTime.UtcNow
        };

        return model;
    }
}
