namespace ShadowrunDiscordBot.Core.Localization;

/// <summary>
/// Static class containing all localization key constants
/// </summary>
public static class LocalizationKeys
{
    // Character commands
    public const string CharacterCreated = "chat.character.created";
    public const string CharacterUpdated = "chat.character.updated";
    public const string CharacterDeleted = "chat.character.deleted";
    public const string CharacterNotFound = "chat.character.not_found";
    public const string CharacterAlreadyExists = "chat.character.already_exists";
    public const string CharacterDisplay = "chat.character.display";
    
    // Combat commands
    public const string CombatStarted = "chat.combat.started";
    public const string CombatEnded = "chat.combat.ended";
    public const string CombatJoined = "chat.combat.joined";
    public const string CombatTurn = "chat.combat.turn";
    public const string CombatNextPass = "chat.combat.next_pass";
    public const string CombatNextRound = "chat.combat.next_round";
    
    // Dice rolls
    public const string DiceRoll = "chat.dice.roll";
    public const string DiceGlitch = "chat.dice.glitch";
    public const string DiceCriticalGlitch = "chat.dice.critical_glitch";
    public const string DiceSuccess = "chat.dice.success";
    public const string DiceFailure = "chat.dice.failure";
    
    // Errors
    public const string InvalidParameter = "errors.invalid_parameter";
    public const string CommandFailed = "errors.command_failed";
    public const string NotAuthorized = "errors.not_authorized";
    public const string CharacterNotFound_Error = "errors.character_not_found";
    public const string CombatNotActive = "errors.combat_not_active";
    public const string DatabaseError = "errors.database_error";
    
    // General
    public const string Success = "general.success";
    public const string Error = "general.error";
    public const string NotImplemented = "general.not_implemented";
    public const string Help = "general.help";
}
