using Discord.WebSocket;

namespace ShadowrunDiscordBot.Commands;

/// <summary>
/// Base class for all command modules
/// </summary>
public abstract class BaseCommandModule
{
    protected readonly ILogger Logger;
    protected readonly BotConfig Config;
    protected readonly DatabaseService Database;
    protected readonly DiceService DiceService;

    protected BaseCommandModule(
        ILogger logger,
        BotConfig config,
        DatabaseService database,
        DiceService diceService)
    {
        Logger = logger;
        Config = config;
        Database = database;
        DiceService = diceService;
    }

    protected async Task LogCommandExecutionAsync(SocketSlashCommand command, string action)
    {
        Logger.LogInformation("Command {CommandName} executed by {User} in {Guild} - Action: {Action}",
            command.CommandName,
            command.User.Username,
            (command.Channel as SocketGuildChannel)?.Guild.Name ?? "DM",
            action);
    }

    protected async Task HandleErrorAsync(SocketSlashCommand command, Exception ex, string context)
    {
        Logger.LogError(ex, "Error in {Context} for command from {User}", context, command.User.Username);

        var errorMessage = ex switch
        {
            ArgumentException => "Invalid input provided. Please check your command and try again.",
            TimeoutException => "The operation timed out. Please try again.",
            UnauthorizedAccessException => "You don't have permission to perform this action.",
            _ => "An unexpected error occurred. Please try again or contact an administrator."
        };

        if (command.HasResponded)
        {
            await command.FollowupAsync(errorMessage, ephemeral: true);
        }
        else
        {
            await command.RespondAsync(errorMessage, ephemeral: true);
        }
    }
}
