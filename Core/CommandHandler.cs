using System.Buffers;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Commands;

namespace ShadowrunDiscordBot.Core;

/// <summary>
/// Command handler with optimized command routing and Span-based parsing
/// </summary>
public class CommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<CommandHandler> _logger;
    private readonly BotConfig _config;
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, Type> _commandTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly char[] _separatorBuffer = new char[1] { ' ' };

    public CommandHandler(
        DiscordSocketClient client,
        BotConfig config,
        IServiceProvider services,
        ILogger<CommandHandler> logger)
    {
        _client = client;
        _config = config;
        _services = services;
        _logger = logger;
    }

    public async Task InitializeAsync(DiscordSocketClient client)
    {
        // Discover all command modules
        DiscoverCommands();

        // Register interaction handler
        client.InteractionCreated += HandleInteractionAsync;

        _logger.LogInformation("Command handler initialized with {Count} commands", _commandTypes.Count);
    }

    public async Task RegisterCommandsAsync()
    {
        try
        {
            var guildId = ulong.Parse(_config.Discord.GuildId);
            var guild = _client.GetGuild(guildId);

            if (guild == null)
            {
                _logger.LogWarning("Guild {GuildId} not found, skipping command registration", guildId);
                return;
            }

            // Build slash commands
            var slashCommands = BuildSlashCommands();

            // Register to guild (faster updates than global)
            await guild.BulkOverwriteApplicationCommandAsync(slashCommands.ToArray());

            _logger.LogInformation("Successfully registered {Count} slash commands to guild {GuildName}",
                slashCommands.Count, guild.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register slash commands");
            throw;
        }
    }

    private void DiscoverCommands()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var baseCommandType = typeof(BaseCommandModule);

        foreach (var type in assembly.GetTypes())
        {
            if (baseCommandType.IsAssignableFrom(type) && !type.IsAbstract)
            {
                var commandName = type.Name.Replace("Commands", "").ToLowerInvariant();
                _commandTypes[commandName] = type;
                _logger.LogDebug("Discovered command module: {ModuleName}", commandName);
            }
        }
    }

    private List<SlashCommandBuilder> BuildSlashCommands()
    {
        var commands = new List<SlashCommandBuilder>();

        // Character commands
        commands.Add(new SlashCommandBuilder()
            .WithName("character")
            .WithDescription("Character management commands")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("create")
                .WithDescription("Create a new Shadowrun character")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Character name", isRequired: true)
                .AddOption("metatype", ApplicationCommandOptionType.String, "Metatype (Human, Elf, Dwarf, Ork, Troll)", isRequired: true)
                .AddOption("archetype", ApplicationCommandOptionType.String, "Archetype (Mage, Street Samurai, Shaman, Rigger, Decker, Physical Adept)", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("List your characters")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("view")
                .WithDescription("View character details")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Character name", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("delete")
                .WithDescription("Delete a character")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Character name", isRequired: true)));

        // Dice commands
        commands.Add(new SlashCommandBuilder()
            .WithName("dice")
            .WithDescription("Roll dice")
            .AddOption("notation", ApplicationCommandOptionType.String, "Dice notation (e.g., 2d6+3)", isRequired: true));

        // Shadowrun dice commands
        commands.Add(new SlashCommandBuilder()
            .WithName("shadowrun-dice")
            .WithDescription("Shadowrun-specific dice rolls")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("basic")
                .WithDescription("Basic dice pool roll")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("pool", ApplicationCommandOptionType.Integer, "Dice pool size", isRequired: true)
                .AddOption("target", ApplicationCommandOptionType.Integer, "Target number (default 4)", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("combat")
                .WithDescription("Combat roll with pool allocation")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("skill", ApplicationCommandOptionType.Integer, "Skill rating", isRequired: true)
                .AddOption("combat-pool", ApplicationCommandOptionType.Integer, "Combat pool to allocate", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("initiative")
                .WithDescription("Calculate initiative")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("reaction", ApplicationCommandOptionType.Integer, "Reaction attribute", isRequired: true)
                .AddOption("initiative-dice", ApplicationCommandOptionType.Integer, "Initiative dice (default 1D6)", isRequired: false)));

        // Combat commands
        commands.Add(new SlashCommandBuilder()
            .WithName("combat")
            .WithDescription("Combat system commands")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("start")
                .WithDescription("Start combat session")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("status")
                .WithDescription("View combat status")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("end")
                .WithDescription("End combat session")
                .WithType(ApplicationCommandOptionType.SubCommand)));

        // Magic commands
        commands.Add(new SlashCommandOptionBuilder()
            .WithName("magic")
            .WithDescription("Magic system commands")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("summon")
                .WithDescription("Summon a spirit")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("type", ApplicationCommandOptionType.String, "Spirit type", isRequired: true)
                .AddOption("force", ApplicationCommandOptionType.Integer, "Force rating", isRequired: true)));

        // Matrix commands
        commands.Add(new SlashCommandBuilder()
            .WithName("matrix")
            .WithDescription("Matrix/cyberdeck commands")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("deck-info")
                .WithDescription("View cyberdeck information")
                .WithType(ApplicationCommandOptionType.SubCommand)));

        // Cyberware commands
        commands.Add(new SlashCommandBuilder()
            .WithName("cyberware")
            .WithDescription("Cyberware and bioware management")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("List available cyberware")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("category", ApplicationCommandOptionType.String, "Cyberware category", isRequired: false)));

        // Help command
        commands.Add(new SlashCommandBuilder()
            .WithName("help")
            .WithDescription("Get help with bot commands")
            .AddOption("command", ApplicationCommandOptionType.String, "Command to get help with", isRequired: false));

        return commands;
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            if (interaction is not SocketSlashCommand slashCommand)
                return;

            _logger.LogDebug("Processing slash command: {CommandName} from {User}",
                slashCommand.CommandName, slashCommand.User.Username);

            // Route to appropriate command handler
            await RouteCommandAsync(slashCommand);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling interaction");

            if (interaction is SocketSlashCommand command)
            {
                await command.RespondAsync("An error occurred while processing your command.", ephemeral: true);
            }
        }
    }

    private async Task RouteCommandAsync(SocketSlashCommand command)
    {
        var commandName = command.CommandName.ToLowerInvariant();

        // Basic command routing
        switch (commandName)
        {
            case "dice":
                await HandleDiceCommandAsync(command);
                break;
            case "character":
                await HandleCharacterCommandAsync(command);
                break;
            case "shadowrun-dice":
                await HandleShadowrunDiceCommandAsync(command);
                break;
            case "help":
                await HandleHelpCommandAsync(command);
                break;
            default:
                await command.RespondAsync($"Unknown command: {commandName}", ephemeral: true);
                break;
        }
    }

    private async Task HandleDiceCommandAsync(SocketSlashCommand command)
    {
        var notation = command.Data.Options.First().Value.ToString();
        var diceService = _services.GetRequiredService<DiceService>();
        
        var result = diceService.ParseAndRoll(notation ?? "1d6");
        
        await command.RespondAsync($"🎲 Rolling {notation}: **{result.Total}**\n{result.Details}");
    }

    private async Task HandleShadowrunDiceCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();
        var diceService = _services.GetRequiredService<DiceService>();

        switch (subCommand.Name)
        {
            case "basic":
                var pool = (long)subCommand.Options.First(o => o.Name == "pool").Value;
                var target = subCommand.Options.FirstOrDefault(o => o.Name == "target")?.Value as long? ?? 4;
                
                var result = diceService.RollShadowrun((int)pool, (int)target);
                await command.RespondAsync($"🎲 Shadowrun Roll: {result.Successes} successes\n{result.Details}");
                break;

            case "initiative":
                var reaction = (long)subCommand.Options.First(o => o.Name == "reaction").Value;
                var initDice = subCommand.Options.FirstOrDefault(o => o.Name == "initiative-dice")?.Value as long? ?? 1;
                
                var initResult = diceService.RollInitiative((int)reaction, (int)initDice);
                await command.RespondAsync($"⚡ Initiative: **{initResult.Total}** ({reaction} + {initResult.DiceRoll})");
                break;

            default:
                await command.RespondAsync("Unknown subcommand", ephemeral: true);
                break;
        }
    }

    private async Task HandleCharacterCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();

        switch (subCommand.Name)
        {
            case "list":
                await command.RespondAsync("📋 Character list feature coming soon!");
                break;
            case "create":
                await command.RespondAsync("✨ Character creation feature coming soon!");
                break;
            default:
                await command.RespondAsync($"Unknown character subcommand: {subCommand.Name}", ephemeral: true);
                break;
        }
    }

    private async Task HandleHelpCommandAsync(SocketSlashCommand command)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Shadowrun Discord Bot - Help")
            .WithDescription("A Discord bot for Shadowrun 3rd Edition roleplaying")
            .WithColor(_config.Bot.DefaultColor)
            .AddField("Character Commands", "/character create, /character list, /character view, /character delete")
            .AddField("Dice Commands", "/dice [notation], /shadowrun-dice basic [pool], /shadowrun-dice initiative")
            .AddField("Combat Commands", "/combat start, /combat status, /combat end")
            .AddField("Magic Commands", "/magic summon [type] [force]")
            .AddField("Matrix Commands", "/matrix deck-info")
            .AddField("Cyberware Commands", "/cyberware list [category]")
            .Build();

        await command.RespondAsync(embed: embed);
    }
}
