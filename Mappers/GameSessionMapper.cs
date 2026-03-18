using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Models;
using DomainGameSession = ShadowrunDiscordBot.Domain.Entities.GameSession;
using ModelGameSession = ShadowrunDiscordBot.Models.GameSession;

namespace ShadowrunDiscordBot.Mappers;

/// <summary>
/// Mapper between GameSession (old model) and GameSession (domain entity)
/// </summary>
public static class GameSessionMapper
{
    /// <summary>
    /// Convert domain entity to old model (for backward compatibility)
    /// </summary>
    public static ModelGameSession ToModel(DomainGameSession entity)
    {
        if (entity == null) return null!;

        var model = new Models.GameSession
        {
            Id = entity.Id,
            DiscordGuildId = entity.DiscordGuildId,
            DiscordChannelId = entity.DiscordChannelId,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            StartedAt = entity.StartedAt,
            EndedAt = entity.EndedAt,
            CreatedBy = entity.CreatedBy
        };

        // Copy navigation properties
        model.SessionParticipants = entity.SessionParticipants?.Select(sp => new SessionParticipant
        {
            GameSessionId = entity.Id,
            UserId = sp.UserId,
            Role = sp.Role,
            JoinedAt = sp.JoinedAt
        }).ToList() ?? new List<SessionParticipant>();

        model.NarrativeEvents = entity.NarrativeEvents?.Select(ne => new NarrativeEvent
        {
            GameSessionId = entity.Id,
            Type = ne.Type,
            Description = ne.Description,
            Timestamp = ne.Timestamp
        }).ToList() ?? new List<NarrativeEvent>();

        model.PlayerChoices = entity.PlayerChoices?.Select(pc => new PlayerChoice
        {
            GameSessionId = entity.Id,
            PlayerId = pc.PlayerId,
            Choice = pc.Choice,
            Timestamp = pc.Timestamp
        }).ToList() ?? new List<PlayerChoice>();

        return model;
    }

    /// <summary>
    /// Convert old model to domain entity
    /// </summary>
    public static DomainGameSession ToEntity(ModelGameSession model)
    {
        if (model == null) return null!;

        var entity = new GameSession
        {
            DiscordGuildId = model.DiscordGuildId,
            DiscordChannelId = model.DiscordChannelId,
            Name = model.Name,
            Description = model.Description,
            IsActive = model.IsActive,
            StartedAt = model.StartedAt,
            EndedAt = model.EndedAt,
            CreatedBy = model.CreatedBy
        };

        if (model.Id > 0)
        {
            entity.Id = model.Id;
        }

        // Copy navigation properties
        if (model.SessionParticipants != null)
        {
            entity.SessionParticipants.Clear();
            entity.SessionParticipants.AddRange(model.SessionParticipants.Select(sp => new SessionParticipant
            {
                UserId = sp.UserId,
                Role = sp.Role,
                JoinedAt = sp.JoinedAt
            }));
        }

        if (model.NarrativeEvents != null)
        {
            entity.NarrativeEvents.Clear();
            entity.NarrativeEvents.AddRange(model.NarrativeEvents.Select(ne => new NarrativeEvent
            {
                Type = ne.Type,
                Description = ne.Description,
                Timestamp = ne.Timestamp
            }));
        }

        if (model.PlayerChoices != null)
        {
            entity.PlayerChoices.Clear();
            entity.PlayerChoices.AddRange(model.PlayerChoices.Select(pc => new PlayerChoice
            {
                PlayerId = pc.PlayerId,
                Choice = pc.Choice,
                Timestamp = pc.Timestamp
            }));
        }

        return entity;
    }
}
