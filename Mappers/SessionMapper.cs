namespace ShadowrunDiscordBot.Mappers;

/// <summary>
/// Mapper for session-related types between Models and Domain.Entities
/// </summary>
public static class SessionMapper
{
    /// <summary>
    /// Convert from old SessionNote model to new SessionNote domain entity
    /// </summary>
    public static Domain.Entities.SessionNote ToDomain(Models.SessionNote model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new Domain.Entities.SessionNote
        {
            Id = model.Id,
            GameSessionId = model.GameSessionId,
            Content = model.Content,
            NoteType = model.NoteType,
            CreatedAt = model.CreatedAt,
            CreatedByUserId = model.CreatedByUserId,
            IsPinned = model.IsPinned
        };
    }

    /// <summary>
    /// Convert from new SessionNote domain entity to old SessionNote model
    /// </summary>
    public static Models.SessionNote ToModel(Domain.Entities.SessionNote entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new Models.SessionNote
        {
            Id = entity.Id,
            GameSessionId = entity.GameSessionId,
            Content = entity.Content,
            NoteType = entity.NoteType,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            IsPinned = entity.IsPinned
        };
    }

    /// <summary>
    /// Convert from old CompletedSession model to new CompletedSession domain entity
    /// </summary>
    public static Domain.Entities.CompletedSession ToDomain(Models.CompletedSession model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var entity = new Domain.Entities.CompletedSession
        {
            Id = model.Id,
            OriginalSessionId = model.OriginalSessionId,
            DiscordChannelId = model.DiscordChannelId,
            DiscordGuildId = model.DiscordGuildId,
            GameMasterUserId = model.GameMasterUserId,
            SessionName = model.SessionName,
            StartedAt = model.StartedAt,
            EndedAt = model.EndedAt,
            DurationMinutes = model.DurationMinutes,
            ParticipantCount = model.ParticipantCount,
            TotalKarmaAwarded = model.TotalKarmaAwarded,
            TotalNuyenAwarded = model.TotalNuyenAwarded,
            Summary = model.Summary,
            ArchivedAt = model.ArchivedAt
        };

        return entity;
    }

    /// <summary>
    /// Convert from new CompletedSession domain entity to old CompletedSession model
    /// </summary>
    public static Models.CompletedSession ToModel(Domain.Entities.CompletedSession entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var model = new Models.CompletedSession
        {
            Id = entity.Id,
            OriginalSessionId = entity.OriginalSessionId,
            DiscordChannelId = entity.DiscordChannelId,
            DiscordGuildId = entity.DiscordGuildId,
            GameMasterUserId = entity.GameMasterUserId,
            SessionName = entity.SessionName,
            StartedAt = entity.StartedAt,
            EndedAt = entity.EndedAt,
            DurationMinutes = entity.DurationMinutes,
            ParticipantCount = entity.ParticipantCount,
            TotalKarmaAwarded = entity.TotalKarmaAwarded,
            TotalNuyenAwarded = entity.TotalNuyenAwarded,
            Summary = entity.Summary,
            ArchivedAt = entity.ArchivedAt
        };

        return model;
    }

    /// <summary>
    /// Convert SessionParticipant model to domain entity
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
    /// Convert SessionParticipant domain entity to model
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
}
