using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Mappers;

/// <summary>
/// Mapper between Domain and Model for GameSession and related entities
/// </summary>
public static class GameSessionMapper
{
    #region GameSession Mappers

    /// <summary>
    /// Convert GameSession model to Domain entity
    /// </summary>
    public static Domain.Entities.GameSession ToDomain(Models.GameSession model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var entity = new Domain.Entities.GameSession
        {
            Id = model.Id,
            DiscordChannelId = model.DiscordChannelId,
            DiscordGuildId = model.DiscordGuildId,
            GameMasterUserId = model.GameMasterUserId,
            SessionName = model.SessionName,
            InGameDateTime = model.InGameDateTime,
            CurrentLocation = model.CurrentLocation,
            LocationDescription = model.LocationDescription,
            Status = model.Status,
            StartedAt = model.StartedAt,
            EndedAt = model.EndedAt,
            LastActivityAt = model.LastActivityAt,
            Notes = model.Notes,
            Metadata = model.Metadata
        };

        // Map collections
        if (model.Participants != null)
        {
            foreach (var participant in model.Participants)
            {
                entity.Participants.Add(ToDomain(participant));
            }
        }

        if (model.NarrativeEvents != null)
        {
            foreach (var narrativeEvent in model.NarrativeEvents)
            {
                entity.NarrativeEvents.Add(ToDomain(narrativeEvent));
            }
        }

        if (model.PlayerChoices != null)
        {
            foreach (var choice in model.PlayerChoices)
            {
                entity.PlayerChoices.Add(ToDomain(choice));
            }
        }

        if (model.NPCRelationships != null)
        {
            foreach (var relationship in model.NPCRelationships)
            {
                entity.NPCRelationships.Add(ToDomain(relationship));
            }
        }

        if (model.ActiveMissions != null)
        {
            foreach (var mission in model.ActiveMissions)
            {
                entity.ActiveMissions.Add(ToDomain(mission));
            }
        }

        return entity;
    }

    /// <summary>
    /// Convert GameSession Domain entity to Model
    /// </summary>
    public static Models.GameSession ToModel(Domain.Entities.GameSession entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var model = new Models.GameSession
        {
            Id = entity.Id,
            DiscordChannelId = entity.DiscordChannelId,
            DiscordGuildId = entity.DiscordGuildId,
            GameMasterUserId = entity.GameMasterUserId,
            SessionName = entity.SessionName,
            InGameDateTime = entity.InGameDateTime,
            CurrentLocation = entity.CurrentLocation,
            LocationDescription = entity.LocationDescription,
            Status = entity.Status,
            StartedAt = entity.StartedAt,
            EndedAt = entity.EndedAt,
            LastActivityAt = entity.LastActivityAt,
            Notes = entity.Notes,
            Metadata = entity.Metadata
        };

        // Map collections
        if (entity.Participants != null)
        {
            foreach (var participant in entity.Participants)
            {
                model.Participants.Add(ToModel(participant));
            }
        }

        if (entity.NarrativeEvents != null)
        {
            foreach (var narrativeEvent in entity.NarrativeEvents)
            {
                model.NarrativeEvents.Add(ToModel(narrativeEvent));
            }
        }

        if (entity.PlayerChoices != null)
        {
            foreach (var choice in entity.PlayerChoices)
            {
                model.PlayerChoices.Add(ToModel(choice));
            }
        }

        if (entity.NPCRelationships != null)
        {
            foreach (var relationship in entity.NPCRelationships)
            {
                model.NPCRelationships.Add(ToModel(relationship));
            }
        }

        if (entity.ActiveMissions != null)
        {
            foreach (var mission in entity.ActiveMissions)
            {
                model.ActiveMissions.Add(ToModel(mission));
            }
        }

        return model;
    }

    #endregion

    #region SessionParticipant Mappers

    /// <summary>
    /// Convert SessionParticipant model to Domain entity
    /// </summary>
    public static Domain.Entities.SessionParticipant ToDomain(Models.SessionParticipant model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new Domain.Entities.SessionParticipant
        {
            Id = model.Id,
            GameSessionId = model.GameSessionId,
            DiscordUserId = model.DiscordUserId,
            CharacterId = model.CharacterId,
            JoinedAt = model.JoinedAt,
            SessionKarma = model.SessionKarma,
            SessionNuyen = model.SessionNuyen,
            Notes = model.Notes,
            IsActive = model.IsActive
        };
    }

    /// <summary>
    /// Convert SessionParticipant Domain entity to Model
    /// </summary>
    public static Models.SessionParticipant ToModel(Domain.Entities.SessionParticipant entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new Models.SessionParticipant
        {
            Id = entity.Id,
            GameSessionId = entity.GameSessionId,
            DiscordUserId = entity.DiscordUserId,
            CharacterId = entity.CharacterId,
            JoinedAt = entity.JoinedAt,
            SessionKarma = entity.SessionKarma,
            SessionNuyen = entity.SessionNuyen,
            Notes = entity.Notes,
            IsActive = entity.IsActive
        };
    }

    #endregion

    #region NarrativeEvent Mappers

    /// <summary>
    /// Convert NarrativeEvent model to Domain entity
    /// </summary>
    public static Domain.Entities.NarrativeEvent ToDomain(Models.NarrativeEvent model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new Domain.Entities.NarrativeEvent
        {
            Id = model.Id,
            GameSessionId = model.GameSessionId,
            Title = model.Title,
            Description = model.Description,
            EventType = model.EventType,
            RecordedAt = model.RecordedAt,
            RelatedCharacterId = model.RelatedCharacterId,
            RelatedNPC = model.RelatedNPC
        };
    }

    /// <summary>
    /// Convert NarrativeEvent Domain entity to Model
    /// </summary>
    public static Models.NarrativeEvent ToModel(Domain.Entities.NarrativeEvent entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new Models.NarrativeEvent
        {
            Id = entity.Id,
            GameSessionId = entity.GameSessionId,
            Title = entity.Title,
            Description = entity.Description,
            EventType = entity.EventType,
            RecordedAt = entity.RecordedAt,
            RelatedCharacterId = entity.RelatedCharacterId,
            RelatedNPC = entity.RelatedNPC
        };
    }

    #endregion

    #region PlayerChoice Mappers

    /// <summary>
    /// Convert PlayerChoice model to Domain entity
    /// </summary>
    public static Domain.Entities.PlayerChoice ToDomain(Models.PlayerChoice model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new Domain.Entities.PlayerChoice
        {
            Id = model.Id,
            GameSessionId = model.GameSessionId,
            DiscordUserId = model.DiscordUserId,
            ChoiceDescription = model.ChoiceDescription,
            Outcome = model.Outcome,
            MadeAt = model.MadeAt,
            ConsequenceSummary = model.ConsequenceSummary
        };
    }

    /// <summary>
    /// Convert PlayerChoice Domain entity to Model
    /// </summary>
    public static Models.PlayerChoice ToModel(Domain.Entities.PlayerChoice entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new Models.PlayerChoice
        {
            Id = entity.Id,
            GameSessionId = entity.GameSessionId,
            DiscordUserId = entity.DiscordUserId,
            ChoiceDescription = entity.ChoiceDescription,
            Outcome = entity.Outcome,
            MadeAt = entity.MadeAt,
            ConsequenceSummary = entity.ConsequenceSummary
        };
    }

    #endregion

    #region NPCRelationship Mappers

    /// <summary>
    /// Convert NPCRelationship model to Domain entity
    /// </summary>
    public static Domain.Entities.NPCRelationship ToDomain(Models.NPCRelationship model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new Domain.Entities.NPCRelationship
        {
            Id = model.Id,
            GameSessionId = model.GameSessionId,
            NPCName = model.NPCName,
            Attitude = model.Attitude,
            TrustLevel = model.TrustLevel,
            Notes = model.Notes,
            LastInteraction = model.LastInteraction
        };
    }

    /// <summary>
    /// Convert NPCRelationship Domain entity to Model
    /// </summary>
    public static Models.NPCRelationship ToModel(Domain.Entities.NPCRelationship entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new Models.NPCRelationship
        {
            Id = entity.Id,
            GameSessionId = entity.GameSessionId,
            NPCName = entity.NPCName,
            Attitude = entity.Attitude,
            TrustLevel = entity.TrustLevel,
            Notes = entity.Notes,
            LastInteraction = entity.LastInteraction
        };
    }

    #endregion

    #region ActiveMission Mappers

    /// <summary>
    /// Convert ActiveMission model to Domain entity
    /// </summary>
    public static Domain.Entities.ActiveMission ToDomain(Models.ActiveMission model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new Domain.Entities.ActiveMission
        {
            Id = model.Id,
            GameSessionId = model.GameSessionId,
            MissionName = model.MissionName,
            MissionType = model.MissionType,
            Difficulty = model.Difficulty,
            Status = model.Status,
            AcceptedAt = model.AcceptedAt,
            CompletedAt = model.CompletedAt,
            Objectives = model.Objectives,
            CurrentObjective = model.CurrentObjective,
            KarmaAwarded = model.KarmaAwarded,
            NuyenAwarded = model.NuyenAwarded,
            Notes = model.Notes
        };
    }

    /// <summary>
    /// Convert ActiveMission Domain entity to Model
    /// </summary>
    public static Models.ActiveMission ToModel(Domain.Entities.ActiveMission entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new Models.ActiveMission
        {
            Id = entity.Id,
            GameSessionId = entity.GameSessionId,
            MissionName = entity.MissionName,
            MissionType = entity.MissionType,
            Difficulty = entity.Difficulty,
            Status = entity.Status,
            AcceptedAt = entity.AcceptedAt,
            CompletedAt = entity.CompletedAt,
            Objectives = entity.Objectives,
            CurrentObjective = entity.CurrentObjective,
            KarmaAwarded = entity.KarmaAwarded,
            NuyenAwarded = entity.NuyenAwarded,
            Notes = entity.Notes
        };
    }

    #endregion
}
