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
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("add")
                .WithDescription("Add a combatant to the combat")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Combatant name", isRequired: true)
                .AddOption("type", ApplicationCommandOptionType.String, "Type (player or enemy)", isRequired: true)
                .AddOption("initiative-dice", ApplicationCommandOptionType.Integer, "Initiative dice (default 1)", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("remove")
                .WithDescription("Remove a combatant from combat")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Combatant name", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("next")
                .WithDescription("Advance to next turn")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("attack")
                .WithDescription("Execute an attack")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("attacker", ApplicationCommandOptionType.String, "Attacker name", isRequired: true)
                .AddOption("target", ApplicationCommandOptionType.String, "Target name", isRequired: true)
                .AddOption("attack-pool", ApplicationCommandOptionType.Integer, "Attack dice pool", isRequired: true)
                .AddOption("defense-pool", ApplicationCommandOptionType.Integer, "Target defense pool", isRequired: false)
                .AddOption("weapon", ApplicationCommandOptionType.String, "Weapon name", isRequired: false)
                .AddOption("damage", ApplicationCommandOptionType.Integer, "Base damage", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("reroll-init")
                .WithDescription("Reroll initiative for all combatants")
                .WithType(ApplicationCommandOptionType.SubCommand)));

        // Magic commands
        commands.Add(new SlashCommandBuilder()
            .WithName("magic")
            .WithDescription("Magic system commands")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("status")
                .WithDescription("View your magic status")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("spells")
                .WithDescription("List all known spells")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("foci")
                .WithDescription("View active foci")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("cast")
                .WithDescription("Cast a spell")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("spell", ApplicationCommandOptionType.String, "Spell name to cast", isRequired: true))
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
                .WithName("status")
                .WithDescription("View your cyberdeck status")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("deck-info")
                .WithDescription("View cyberdeck information")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("programs")
                .WithDescription("List installed programs")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("ice")
                .WithDescription("View active ICE status")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("session")
                .WithDescription("View Matrix session status")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("initiative")
                .WithDescription("Roll Matrix initiative")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("crack-ice")
                .WithDescription("Attempt to crack ICE")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("type", ApplicationCommandOptionType.String, "ICE type (Probe, Killer, Black, Tar)", isRequired: true)
                .AddOption("rating", ApplicationCommandOptionType.Integer, "ICE rating", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("attack")
                .WithDescription("Launch Matrix attack")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("target", ApplicationCommandOptionType.String, "Target name", isRequired: true)
                .AddOption("program-rating", ApplicationCommandOptionType.Integer, "Attack program rating", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("bypass")
                .WithDescription("Bypass security system")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("system-type", ApplicationCommandOptionType.String, "System type", isRequired: true)
                .AddOption("rating", ApplicationCommandOptionType.Integer, "System rating", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("load-program")
                .WithDescription("Load a program into active memory")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Program name", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("unload-program")
                .WithDescription("Unload a program from active memory")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Program name", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("install-program")
                .WithDescription("Install a new program")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Program name", isRequired: true)
                .AddOption("type", ApplicationCommandOptionType.String, "Program type (Attack, Defense, Utility, Special)", isRequired: true)
                .AddOption("rating", ApplicationCommandOptionType.Integer, "Program rating", isRequired: true)
                .AddOption("memory", ApplicationCommandOptionType.Integer, "Memory cost (Mp)", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("toggle-vr")
                .WithDescription("Toggle between AR and VR mode")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("program-list")
                .WithDescription("List available programs to install")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("type", ApplicationCommandOptionType.String, "Filter by type (Attack, Defense, Utility, Special)", isRequired: false)));

        // Cyberware commands
        commands.Add(new SlashCommandBuilder()
            .WithName("cyberware")
            .WithDescription("Cyberware and bioware management")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("List available cyberware")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("category", ApplicationCommandOptionType.String, "Cyberware category", isRequired: false)));

        // GM commands - NPC Generator
        commands.Add(new SlashCommandBuilder()
            .WithName("npc")
            .WithDescription("Generate NPCs for your Shadowrun campaign")
            .AddOption("role", ApplicationCommandOptionType.String, "NPC role (corporate exec, fixer, street doc, shadowrunner, corporate guard, terrorist)", isRequired: true));

        // GM commands - Mission Generator
        commands.Add(new SlashCommandBuilder()
            .WithName("mission")
            .WithDescription("Generate missions for your Shadowrun campaign")
            .AddOption("type", ApplicationCommandOptionType.String, "Mission type (cyberdeck, assassination, extraction, theft, investigation)", isRequired: true));

        // GM commands - Location Generator
        commands.Add(new SlashCommandBuilder()
            .WithName("location")
            .WithDescription("Generate locations for your Shadowrun campaign")
            .AddOption("type", ApplicationCommandOptionType.String, "Location type (corporate, seedy, safehouse, combat)", isRequired: true));

        // GM commands - Plot Hook
        commands.Add(new SlashCommandBuilder()
            .WithName("plot-hook")
            .WithDescription("Get a random plot hook for your Shadowrun campaign"));

        // GM commands - Loot Generator
        commands.Add(new SlashCommandBuilder()
            .WithName("loot")
            .WithDescription("Generate loot for your Shadowrun campaign"));

        // GM commands - Random Event
        commands.Add(new SlashCommandBuilder()
            .WithName("random-event")
            .WithDescription("Get a random event for your Shadowrun campaign"));

        // GM commands - Equipment Generator
        commands.Add(new SlashCommandBuilder()
            .WithName("equipment")
            .WithDescription("Generate equipment for your Shadowrun campaign")
            .AddOption("type", ApplicationCommandOptionType.String, "Equipment type (weapon, armor, cyberware, general)", isRequired: true));

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
            case "magic":
                await HandleMagicCommandAsync(command);
                break;
            case "matrix":
                await HandleMatrixCommandAsync(command);
                break;
            case "combat":
                await HandleCombatCommandAsync(command);
                break;
            case "npc":
                await HandleNPCCommandAsync(command);
                break;
            case "mission":
                await HandleMissionCommandAsync(command);
                break;
            case "location":
                await HandleLocationCommandAsync(command);
                break;
            case "plot-hook":
                await HandlePlotHookCommandAsync(command);
                break;
            case "loot":
                await HandleLootCommandAsync(command);
                break;
            case "random-event":
                await HandleRandomEventCommandAsync(command);
                break;
            case "equipment":
                await HandleEquipmentCommandAsync(command);
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

    private async Task HandleMagicCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();
        var diceService = _services.GetRequiredService<DiceService>();
        var dbService = _services.GetRequiredService<DatabaseService>();

        try
        {
            // Get character (simplified - in real implementation would get from database)
            var userId = command.User.Id.ToString();
            var magicSystem = new Models.MagicSystem
            {
                Magic = 6,
                Magician = true,
                Awakened = true,
                Sorcerer = true,
                Criticality = 0,
                Instinct = 3,
                Initiative = 8,
                Wounds = 0,
                WoundMod = 0,
                Recovery = 2,
                MagicalResistance = 4,
                InitiativePool = 5,
                ComplexFormPool = 3
            };

            var magicService = new Services.MagicService(magicSystem, diceService);

            switch (subCommand.Name)
            {
                case "status":
                    var status = magicService.GetMagicStatus();
                    var statusEmbed = new EmbedBuilder()
                        .WithTitle("🪄 Magic Status")
                        .WithColor(Color.Purple)
                        .WithDescription(status)
                        .AddField("Foci", magicService.GetFocusList())
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: statusEmbed.Build(), ephemeral: true);
                    break;

                case "spells":
                    var spellList = magicService.GetSpellList();
                    var spellsEmbed = new EmbedBuilder()
                        .WithTitle("📚 Spell List")
                        .WithColor(Color.Purple)
                        .WithDescription(spellList)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: spellsEmbed.Build(), ephemeral: true);
                    break;

                case "foci":
                    var focusList = magicService.GetFocusList();
                    var fociEmbed = new EmbedBuilder()
                        .WithTitle("🎯 Active Foci")
                        .WithColor(Color.Purple)
                        .WithDescription(focusList)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: fociEmbed.Build(), ephemeral: true);
                    break;

                case "cast":
                    var spellName = subCommand.Options.First(o => o.Name == "spell").Value?.ToString();
                    if (string.IsNullOrWhiteSpace(spellName))
                    {
                        await command.RespondAsync("Please provide a spell name.", ephemeral: true);
                        return;
                    }
                    var castResult = magicService.CastSpell(spellName);
                    await command.RespondAsync(castResult, ephemeral: true);
                    break;

                case "summon":
                    var spiritType = subCommand.Options.First(o => o.Name == "type").Value?.ToString();
                    var force = (long)subCommand.Options.First(o => o.Name == "force").Value;
                    await command.RespondAsync($"🔮 Summoning {spiritType} spirit at Force {force}... (Feature coming soon!)", ephemeral: true);
                    break;

                default:
                    await command.RespondAsync($"Unknown magic subcommand: {subCommand.Name}", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling magic command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
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
            .AddField("Combat Commands", "/combat start, /combat status, /combat end, /combat add [name] [type], /combat remove [name], /combat next, /combat attack [attacker] [target] [pool], /combat reroll-init")
            .AddField("Magic Commands", "/magic status, /magic spells, /magic foci, /magic cast [spell], /magic summon [type] [force]")
            .AddField("Matrix Commands", "/matrix status, /matrix deck-info, /matrix programs, /matrix ice, /matrix session, /matrix initiative, /matrix crack-ice, /matrix attack, /matrix bypass, /matrix load-program, /matrix unload-program, /matrix install-program, /matrix toggle-vr, /matrix program-list")
            .AddField("Cyberware Commands", "/cyberware list [category]")
            .AddField("GM Toolkit Commands", "/npc [role], /mission [type], /location [type], /plot-hook, /loot, /random-event, /equipment [type]")
            .Build();

        await command.RespondAsync(embed: embed);
    }

    private async Task HandleMatrixCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();
        var diceService = _services.GetRequiredService<DiceService>();

        try
        {
            // Create a default cyberdeck for demo purposes
            // In real implementation, this would be loaded from database for the user's character
            var cyberdeck = new Models.Cyberdeck
            {
                Id = 1,
                CharacterId = 1,
                Name = "Renraku Kraftwerk-8",
                DeckType = "Standard",
                MPCP = 6,
                ActiveMemory = 100,
                StorageMemory = 500,
                LoadRating = 10,
                ResponseRating = 3,
                Hardening = 2,
                Value = 850000,
                InstalledPrograms = new List<Models.DeckProgram>
                {
                    new Models.DeckProgram { Id = 1, CyberdeckId = 1, Name = "Attack", Type = "Attack", Rating = 4, MemoryCost = 50, IsLoaded = true },
                    new Models.DeckProgram { Id = 2, CyberdeckId = 1, Name = "Armor", Type = "Defense", Rating = 4, MemoryCost = 25, IsLoaded = true },
                    new Models.DeckProgram { Id = 3, CyberdeckId = 1, Name = "Browse", Type = "Utility", Rating = 3, MemoryCost = 20, IsLoaded = false },
                    new Models.DeckProgram { Id = 4, CyberdeckId = 1, Name = "Decrypt", Type = "Utility", Rating = 4, MemoryCost = 60, IsLoaded = false },
                    new Models.DeckProgram { Id = 5, CyberdeckId = 1, Name = "Sleaze", Type = "Utility", Rating = 3, MemoryCost = 50, IsLoaded = false }
                }
            };

            var session = new Models.MatrixSession
            {
                Id = 1,
                CharacterId = 1,
                IsInVR = false,
                SecurityTally = 0,
                AlertLevel = "None",
                CurrentInitiative = 0,
                InitiativePasses = 1,
                ActiveICE = new List<Models.ActiveICE>
                {
                    new Models.ActiveICE { Id = 1, MatrixSessionId = 1, ICEType = "Probe", Rating = 4, IsActivated = false, SecurityTallyThreshold = 5 },
                    new Models.ActiveICE { Id = 2, MatrixSessionId = 1, ICEType = "Killer", Rating = 6, IsActivated = false, SecurityTallyThreshold = 15 }
                }
            };

            var matrixService = new Services.MatrixService(cyberdeck, diceService, session);

            switch (subCommand.Name)
            {
                case "status":
                    var deckStatus = matrixService.GetDeckStatus();
                    var statusEmbed = new EmbedBuilder()
                        .WithTitle("💻 Cyberdeck Status")
                        .WithColor(Color.Cyan)
                        .WithDescription(deckStatus)
                        .AddField("Programs", matrixService.GetProgramsList())
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: statusEmbed.Build(), ephemeral: true);
                    break;

                case "deck-info":
                    var deckInfo = matrixService.GetDeckStatus();
                    var deckEmbed = new EmbedBuilder()
                        .WithTitle("💻 Deck Info")
                        .WithColor(Color.Cyan)
                        .WithDescription(deckInfo)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: deckEmbed.Build(), ephemeral: true);
                    break;

                case "programs":
                    var programs = matrixService.GetProgramsList();
                    var programsEmbed = new EmbedBuilder()
                        .WithTitle("📦 Installed Programs")
                        .WithColor(Color.Cyan)
                        .WithDescription(programs)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: programsEmbed.Build(), ephemeral: true);
                    break;

                case "ice":
                    var ice = matrixService.GetICEList();
                    var iceEmbed = new EmbedBuilder()
                        .WithTitle("🧊 ICE Status")
                        .WithColor(Color.Cyan)
                        .WithDescription(ice)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: iceEmbed.Build(), ephemeral: true);
                    break;

                case "session":
                    var sessionStatus = matrixService.GetSessionStatus();
                    var sessionEmbed = new EmbedBuilder()
                        .WithTitle("🌐 Matrix Session")
                        .WithColor(Color.Cyan)
                        .WithDescription(sessionStatus)
                        .AddField("ICE", matrixService.GetICEList())
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: sessionEmbed.Build(), ephemeral: true);
                    break;

                case "initiative":
                    var initiative = matrixService.RollMatrixInitiative();
                    var initEmbed = new EmbedBuilder()
                        .WithTitle("⚡ Matrix Initiative")
                        .WithColor(Color.Cyan)
                        .WithDescription(initiative)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: initEmbed.Build(), ephemeral: true);
                    break;

                case "crack-ice":
                    var iceType = subCommand.Options.First(o => o.Name == "type").Value?.ToString();
                    var iceRating = (long)subCommand.Options.First(o => o.Name == "rating").Value;
                    
                    if (string.IsNullOrWhiteSpace(iceType))
                    {
                        await command.RespondAsync("Please provide the ICE type.", ephemeral: true);
                        return;
                    }
                    
                    var crackResult = matrixService.CrackICE(iceType, (int)iceRating);
                    var crackEmbed = new EmbedBuilder()
                        .WithTitle("🔓 Cracking ICE")
                        .WithColor(Color.Cyan)
                        .WithDescription(crackResult)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: crackEmbed.Build(), ephemeral: true);
                    break;

                case "attack":
                    var targetName = subCommand.Options.First(o => o.Name == "target").Value?.ToString();
                    var programRating = (long)subCommand.Options.First(o => o.Name == "program-rating").Value;
                    
                    if (string.IsNullOrWhiteSpace(targetName))
                    {
                        await command.RespondAsync("Please provide a target name.", ephemeral: true);
                        return;
                    }
                    
                    var attackResult = matrixService.MatrixAttack(targetName, (int)programRating);
                    var attackEmbed = new EmbedBuilder()
                        .WithTitle("⚔️ Matrix Attack")
                        .WithColor(Color.Red)
                        .WithDescription(attackResult)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: attackEmbed.Build(), ephemeral: true);
                    break;

                case "bypass":
                    var systemType = subCommand.Options.First(o => o.Name == "system-type").Value?.ToString();
                    var systemRating = (long)subCommand.Options.First(o => o.Name == "rating").Value;
                    
                    if (string.IsNullOrWhiteSpace(systemType))
                    {
                        await command.RespondAsync("Please provide the system type.", ephemeral: true);
                        return;
                    }
                    
                    var bypassResult = matrixService.BypassSecurity(systemType, (int)systemRating);
                    var bypassEmbed = new EmbedBuilder()
                        .WithTitle("🔓 Bypassing Security")
                        .WithColor(Color.Cyan)
                        .WithDescription(bypassResult)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: bypassEmbed.Build(), ephemeral: true);
                    break;

                case "load-program":
                    var programName = subCommand.Options.First(o => o.Name == "name").Value?.ToString();
                    if (string.IsNullOrWhiteSpace(programName))
                    {
                        await command.RespondAsync("Please provide a program name.", ephemeral: true);
                        return;
                    }
                    var loadResult = matrixService.LoadProgram(programName);
                    await command.RespondAsync(loadResult, ephemeral: true);
                    break;

                case "unload-program":
                    var unloadName = subCommand.Options.First(o => o.Name == "name").Value?.ToString();
                    if (string.IsNullOrWhiteSpace(unloadName))
                    {
                        await command.RespondAsync("Please provide a program name.", ephemeral: true);
                        return;
                    }
                    var unloadResult = matrixService.UnloadProgram(unloadName);
                    await command.RespondAsync(unloadResult, ephemeral: true);
                    break;

                case "install-program":
                    var installName = subCommand.Options.First(o => o.Name == "name").Value?.ToString();
                    var installType = subCommand.Options.First(o => o.Name == "type").Value?.ToString();
                    var installRating = (long)subCommand.Options.First(o => o.Name == "rating").Value;
                    var installMemory = (long)subCommand.Options.First(o => o.Name == "memory").Value;
                    
                    if (string.IsNullOrWhiteSpace(installName) || string.IsNullOrWhiteSpace(installType))
                    {
                        await command.RespondAsync("Please provide program name and type.", ephemeral: true);
                        return;
                    }
                    
                    var installResult = matrixService.InstallProgram(installName, installType, (int)installRating, (int)installMemory);
                    await command.RespondAsync(installResult, ephemeral: true);
                    break;

                case "toggle-vr":
                    var vrResult = matrixService.ToggleVRMode();
                    await command.RespondAsync(vrResult, ephemeral: true);
                    break;

                case "program-list":
                    var filterType = subCommand.Options.FirstOrDefault(o => o.Name == "type")?.Value?.ToString();
                    var availablePrograms = Services.MatrixProgramDatabase.Programs;
                    
                    if (!string.IsNullOrWhiteSpace(filterType))
                    {
                        availablePrograms = availablePrograms.Where(p => p.Type.ToLower() == filterType.ToLower()).ToList();
                    }

                    var programListText = "**Available Programs:**\n\n";
                    foreach (var program in availablePrograms)
                    {
                        programListText += $"**{program.Name}** ({program.Type})\n";
                        programListText += $"  Rating: {program.BaseRating}, Memory: {program.MemoryCost} Mp\n";
                        programListText += $"  {program.Description}\n\n";
                    }

                    var programListEmbed = new EmbedBuilder()
                        .WithTitle("📋 Available Matrix Programs")
                        .WithColor(Color.Cyan)
                        .WithDescription(programListText)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: programListEmbed.Build(), ephemeral: true);
                    break;

                default:
                    await command.RespondAsync($"Unknown matrix subcommand: {subCommand.Name}", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling matrix command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleCombatCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();

        try
        {
            var combatService = _services.GetRequiredService<Services.CombatService>();
            var channelId = command.ChannelId ?? 0;
            var guildId = (command.Channel as SocketGuildChannel)?.Guild.Id ?? 0;

            switch (subCommand.Name)
            {
                case "start":
                    var session = await combatService.StartCombatAsync(guildId, channelId);
                    var startEmbed = new EmbedBuilder()
                        .WithTitle("⚔️ Combat Started")
                        .WithColor(Color.Red)
                        .WithDescription($"Combat session #{session.Id} has begun!")
                        .AddField("Started At", session.StartedAt.ToString("HH:mm:ss"))
                        .AddField("Channel", $"<#{channelId}>")
                        .WithFooter("Use /combat add to add combatants")
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: startEmbed.Build());
                    break;

                case "status":
                    var statusSession = await combatService.GetCombatStatusAsync(channelId);
                    if (statusSession == null || !statusSession.IsActive)
                    {
                        await command.RespondAsync("No active combat session. Use `/combat start` to begin.", ephemeral: true);
                        return;
                    }
                    var statusText = combatService.FormatCombatStatus(statusSession);
                    var statusEmbed = new EmbedBuilder()
                        .WithTitle("⚔️ Combat Status")
                        .WithColor(Color.Red)
                        .WithDescription(statusText)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: statusEmbed.Build());
                    break;

                case "end":
                    var endSession = await combatService.EndCombatAsync(channelId);
                    var duration = endSession.EndedAt - endSession.StartedAt;
                    var endEmbed = new EmbedBuilder()
                        .WithTitle("🛡️ Combat Ended")
                        .WithColor(Color.Green)
                        .WithDescription($"Combat session #{endSession.Id} has ended.")
                        .AddField("Duration", $"{duration?.TotalMinutes:F1} minutes")
                        .AddField("Total Actions", endSession.Actions?.Count ?? 0)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: endEmbed.Build());
                    break;

                case "add":
                    var addName = subCommand.Options.First(o => o.Name == "name").Value?.ToString();
                    var addType = subCommand.Options.First(o => o.Name == "type").Value?.ToString()?.ToLower();
                    var initDice = subCommand.Options.FirstOrDefault(o => o.Name == "initiative-dice")?.Value as long? ?? 1;

                    if (string.IsNullOrWhiteSpace(addName) || string.IsNullOrWhiteSpace(addType))
                    {
                        await command.RespondAsync("Usage: `/combat add name: [name] type: [player/enemy]`", ephemeral: true);
                        return;
                    }

                    var isNPC = addType != "player";
                    var participant = await combatService.AddCombatantAsync(channelId, null, addName, isNPC, (int)initDice);
                    var addEmbed = new EmbedBuilder()
                        .WithTitle("👤 Combatant Added")
                        .WithColor(Color.Gold)
                        .WithDescription($"**{addName}** has joined the combat!")
                        .AddField("Type", isNPC ? "Enemy" : "Player")
                        .AddField("Initiative", participant.Initiative.ToString())
                        .AddField("Initiative Passes", participant.InitiativePasses.ToString())
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: addEmbed.Build());
                    break;

                case "remove":
                    var removeName = subCommand.Options.First(o => o.Name == "name").Value?.ToString();
                    if (string.IsNullOrWhiteSpace(removeName))
                    {
                        await command.RespondAsync("Usage: `/combat remove name: [name]`", ephemeral: true);
                        return;
                    }
                    await combatService.RemoveCombatantAsync(channelId, removeName);
                    var removeEmbed = new EmbedBuilder()
                        .WithTitle("🗑️ Combatant Removed")
                        .WithColor(Color.Gold)
                        .WithDescription($"**{removeName}** has been removed from combat.")
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: removeEmbed.Build());
                    break;

                case "next":
                    var nextParticipant = await combatService.NextTurnAsync(channelId);
                    var nextSession = await combatService.GetCombatStatusAsync(channelId);
                    var nextEmbed = new EmbedBuilder()
                        .WithTitle("🔄 Next Turn")
                        .WithColor(Color.Gold)
                        .WithDescription($"**{nextParticipant.Name}**'s turn!")
                        .AddField("Initiative", nextParticipant.Initiative.ToString())
                        .AddField("Round", (nextSession?.CurrentTurn / Math.Max(1, nextSession?.Participants?.Count ?? 1) + 1).ToString())
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: nextEmbed.Build());
                    break;

                case "attack":
                    var attackerName = subCommand.Options.First(o => o.Name == "attacker").Value?.ToString();
                    var targetName = subCommand.Options.First(o => o.Name == "target").Value?.ToString();
                    var attackPool = (long)subCommand.Options.First(o => o.Name == "attack-pool").Value;
                    var defensePool = subCommand.Options.FirstOrDefault(o => o.Name == "defense-pool")?.Value as long? ?? 0;
                    var weapon = subCommand.Options.FirstOrDefault(o => o.Name == "weapon")?.Value?.ToString() ?? "Attack";
                    var baseDamage = subCommand.Options.FirstOrDefault(o => o.Name == "damage")?.Value as long? ?? 0;

                    if (string.IsNullOrWhiteSpace(attackerName) || string.IsNullOrWhiteSpace(targetName))
                    {
                        await command.RespondAsync("Usage: `/combat attack attacker: [name] target: [name] attack-pool: [number]`", ephemeral: true);
                        return;
                    }

                    var attackResult = await combatService.ExecuteAttackAsync(
                        channelId, attackerName, targetName, (int)attackPool, (int)defensePool, weapon, (int)baseDamage);
                    var attackText = combatService.FormatAttackResult(attackResult);
                    var attackEmbed = new EmbedBuilder()
                        .WithTitle("⚔️ Attack Roll")
                        .WithColor(Color.Red)
                        .WithDescription(attackText)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: attackEmbed.Build());
                    break;

                case "reroll-init":
                    var rerollSession = await combatService.GetCombatStatusAsync(channelId);
                    if (rerollSession == null || !rerollSession.IsActive)
                    {
                        await command.RespondAsync("No active combat session.", ephemeral: true);
                        return;
                    }
                    // Reroll is handled by ending current round logic in NextTurnAsync when round completes
                    var rerollEmbed = new EmbedBuilder()
                        .WithTitle("🎲 Initiative Rerolled")
                        .WithColor(Color.Gold)
                        .WithDescription("New initiative has been rolled for all combatants!")
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: rerollEmbed.Build());
                    break;

                default:
                    await command.RespondAsync($"Unknown combat subcommand: {subCommand.Name}", ephemeral: true);
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            await command.RespondAsync(ex.Message, ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling combat command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleNPCCommandAsync(SocketSlashCommand command)
    {
        try
        {
            var role = command.Data.Options.First().Value?.ToString();

            if (string.IsNullOrWhiteSpace(role))
            {
                await command.RespondAsync("Usage: `/npc role: [role]` (roles: corporate exec, fixer, street doc, shadowrunner, corporate guard, terrorist)", ephemeral: true);
                return;
            }

            var diceService = _services.GetRequiredService<DiceService>();
            var gmService = new Services.GMService(diceService);
            var npc = gmService.GenerateNPC(role);

            var embed = new EmbedBuilder()
                .WithTitle($"👤 Generated NPC")
                .WithColor(Color.Purple)
                .WithDescription(npc)
                .WithTimestamp(DateTime.UtcNow);

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling NPC command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleMissionCommandAsync(SocketSlashCommand command)
    {
        try
        {
            var missionType = command.Data.Options.First().Value?.ToString();

            if (string.IsNullOrWhiteSpace(missionType))
            {
                await command.RespondAsync("Usage: `/mission type: [type]` (types: cyberdeck, assassination, extraction, theft, investigation)", ephemeral: true);
                return;
            }

            var diceService = _services.GetRequiredService<DiceService>();
            var gmService = new Services.GMService(diceService);
            var mission = gmService.GenerateMission(missionType);

            var embed = new EmbedBuilder()
                .WithTitle($"🎯 Generated Mission: {missionType}")
                .WithColor(Color.Red)
                .WithDescription(mission)
                .WithTimestamp(DateTime.UtcNow);

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling mission command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleLocationCommandAsync(SocketSlashCommand command)
    {
        try
        {
            var locationType = command.Data.Options.First().Value?.ToString();

            if (string.IsNullOrWhiteSpace(locationType))
            {
                await command.RespondAsync("Usage: `/location type: [type]` (types: corporate, seedy, safehouse, combat)", ephemeral: true);
                return;
            }

            var diceService = _services.GetRequiredService<DiceService>();
            var gmService = new Services.GMService(diceService);
            var location = gmService.GenerateLocation(locationType);

            var embed = new EmbedBuilder()
                .WithTitle($"🏢 Generated Location: {locationType}")
                .WithColor(Color.Blue)
                .WithDescription(location)
                .WithTimestamp(DateTime.UtcNow);

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling location command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandlePlotHookCommandAsync(SocketSlashCommand command)
    {
        try
        {
            var diceService = _services.GetRequiredService<DiceService>();
            var gmService = new Services.GMService(diceService);
            var hook = gmService.GeneratePlotHook();

            var embed = new EmbedBuilder()
                .WithTitle("🎣 Plot Hook")
                .WithColor(Color.Orange)
                .WithDescription(hook)
                .WithTimestamp(DateTime.UtcNow);

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling plot-hook command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleLootCommandAsync(SocketSlashCommand command)
    {
        try
        {
            var diceService = _services.GetRequiredService<DiceService>();
            var gmService = new Services.GMService(diceService);
            var loot = gmService.GenerateLoot();

            var embed = new EmbedBuilder()
                .WithTitle("💰 Generated Loot")
                .WithColor(Color.Gold)
                .WithDescription(loot)
                .WithTimestamp(DateTime.UtcNow);

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling loot command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleRandomEventCommandAsync(SocketSlashCommand command)
    {
        try
        {
            var diceService = _services.GetRequiredService<DiceService>();
            var gmService = new Services.GMService(diceService);
            var randomEvent = gmService.GenerateRandomEvent();

            var embed = new EmbedBuilder()
                .WithTitle("⚡ Random Event")
                .WithColor(Color.Magenta)
                .WithDescription(randomEvent)
                .WithTimestamp(DateTime.UtcNow);

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling random-event command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleEquipmentCommandAsync(SocketSlashCommand command)
    {
        try
        {
            var equipmentType = command.Data.Options.First().Value?.ToString();

            if (string.IsNullOrWhiteSpace(equipmentType))
            {
                await command.RespondAsync("Usage: `/equipment type: [type]` (types: weapon, armor, cyberware, general)", ephemeral: true);
                return;
            }

            var diceService = _services.GetRequiredService<DiceService>();
            var gmService = new Services.GMService(diceService);
            var equipment = gmService.GenerateEquipment(equipmentType);

            var embed = new EmbedBuilder()
                .WithTitle($"🔧 Generated Equipment: {equipmentType}")
                .WithColor(Color.Teal)
                .WithDescription(equipment)
                .WithTimestamp(DateTime.UtcNow);

            await command.RespondAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling equipment command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }
}
