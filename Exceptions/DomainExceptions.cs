using System;

namespace ShadowrunDiscordBot.Exceptions;

/// <summary>
/// Base exception for all Shadowrun domain-specific exceptions
/// FIX: Created specific exception types for better error handling and actionable guidance
/// </summary>
public abstract class ShadowrunException : Exception
{
    protected ShadowrunException(string message) : base(message) { }
    protected ShadowrunException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a character is not found
/// </summary>
public class CharacterNotFoundException : ShadowrunException
{
    public int? CharacterId { get; }
    public string? CharacterName { get; }
    public ulong? DiscordUserId { get; }

    public CharacterNotFoundException(int characterId) 
        : base($"Character with ID {characterId} was not found. Verify the character ID and ensure it exists.")
    {
        CharacterId = characterId;
    }

    public CharacterNotFoundException(ulong discordUserId, string characterName) 
        : base($"Character '{characterName}' was not found for user {discordUserId}. Verify the character name and ensure it belongs to you.")
    {
        DiscordUserId = discordUserId;
        CharacterName = characterName;
    }

    public CharacterNotFoundException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when character validation fails
/// </summary>
public class CharacterValidationException : ShadowrunException
{
    public string? FieldName { get; }
    public object? InvalidValue { get; }

    public CharacterValidationException(string message) : base(message) { }

    public CharacterValidationException(string fieldName, object invalidValue, string reason)
        : base($"Invalid value for {fieldName}: {invalidValue}. {reason}")
    {
        FieldName = fieldName;
        InvalidValue = invalidValue;
    }
}

/// <summary>
/// Exception thrown when a character already exists
/// </summary>
public class CharacterAlreadyExistsException : ShadowrunException
{
    public string CharacterName { get; }
    public ulong DiscordUserId { get; }

    public CharacterAlreadyExistsException(ulong discordUserId, string characterName)
        : base($"Character '{characterName}' already exists for this user. Choose a different name or delete the existing character first.")
    {
        DiscordUserId = discordUserId;
        CharacterName = characterName;
    }
}

/// <summary>
/// Exception thrown when combat session operations fail
/// </summary>
public class CombatSessionException : ShadowrunException
{
    public int? SessionId { get; }
    public ulong? ChannelId { get; }

    public CombatSessionException(string message) : base(message) { }

    public CombatSessionException(int sessionId, string message)
        : base($"Combat session {sessionId}: {message}")
    {
        SessionId = sessionId;
    }

    public CombatSessionException(ulong channelId, string message)
        : base($"Combat session in channel {channelId}: {message}")
    {
        ChannelId = channelId;
    }
}

/// <summary>
/// Exception thrown when an active combat session already exists
/// </summary>
public class ActiveCombatSessionExistsException : CombatSessionException
{
    public ActiveCombatSessionExistsException(ulong channelId)
        : base(channelId, "An active combat session already exists in this channel. End the current session before starting a new one.")
    {
    }
}

/// <summary>
/// Exception thrown when no active combat session exists
/// </summary>
public class NoActiveCombatSessionException : CombatSessionException
{
    public NoActiveCombatSessionException(ulong channelId)
        : base(channelId, "No active combat session found in this channel. Start a combat session first using /combat start.")
    {
    }
}

/// <summary>
/// Exception thrown when a combat participant operation fails
/// </summary>
public class CombatParticipantException : ShadowrunException
{
    public string? ParticipantName { get; }

    public CombatParticipantException(string participantName, string message)
        : base($"Combat participant '{participantName}': {message}")
    {
        ParticipantName = participantName;
    }
}

/// <summary>
/// Exception thrown when a dice roll operation fails
/// </summary>
public class DiceRollException : ShadowrunException
{
    public string? Notation { get; }

    public DiceRollException(string message) : base(message) { }

    public DiceRollException(string notation, string message)
        : base($"Invalid dice notation '{notation}': {message}")
    {
        Notation = notation;
    }
}

/// <summary>
/// Exception thrown when matrix/cyberdeck operations fail
/// </summary>
public class MatrixOperationException : ShadowrunException
{
    public int? CharacterId { get; }

    public MatrixOperationException(string message) : base(message) { }

    public MatrixOperationException(int characterId, string message)
        : base($"Matrix operation failed for character {characterId}: {message}")
    {
        CharacterId = characterId;
    }
}

/// <summary>
/// Exception thrown when magic/awakened operations fail
/// </summary>
public class MagicOperationException : ShadowrunException
{
    public int? CharacterId { get; }

    public MagicOperationException(string message) : base(message) { }

    public MagicOperationException(int characterId, string message)
        : base($"Magic operation failed for character {characterId}: {message}")
    {
        CharacterId = characterId;
    }
}

/// <summary>
/// Exception thrown when database operations fail
/// </summary>
public class DatabaseOperationException : ShadowrunException
{
    public string Operation { get; }

    public DatabaseOperationException(string operation, string message, Exception innerException)
        : base($"Database operation '{operation}' failed: {message}. Please try again or contact support if the issue persists.", innerException)
    {
        Operation = operation;
    }
}

/// <summary>
/// Exception thrown when authorization/permission checks fail
/// </summary>
public class UnauthorizedOperationException : ShadowrunException
{
    public ulong UserId { get; }
    public string ResourceType { get; }
    public int? ResourceId { get; }

    public UnauthorizedOperationException(ulong userId, string resourceType, int? resourceId = null)
        : base($"User {userId} is not authorized to access {resourceType}" + 
               (resourceId.HasValue ? $" with ID {resourceId}" : "") + 
               ". Ensure you have the necessary permissions or own the resource.")
    {
        UserId = userId;
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
