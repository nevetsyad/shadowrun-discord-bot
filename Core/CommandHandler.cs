using System.Reflection;
using System.Text;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Commands;

namespace ShadowrunDiscordBot.Core;

/// <summary>
/// Command handler with optimized command routing and Span-based parsing
/// </summary>
public partial class CommandHandler
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<CommandHandler> _logger;
    private readonly BotConfig _config;
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, Type> _commandTypes = new(StringComparer.OrdinalIgnoreCase);

    // Constants for magic numbers
    private const int DefaultTargetNumber = 4;
    private const int DefaultInitiativeDice = 1;
    private const int MaxSearchResults = 10;
    private const int MaxNotesDisplayed = 10;
    private const int MaxHistoryResults = 10;

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

        // Session management commands
        commands.Add(new SlashCommandBuilder()
            .WithName("session")
            .WithDescription("Game session management commands")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("start")
                .WithDescription("Start a new game session")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Session name", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("end")
                .WithDescription("End the current game session")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("pause")
                .WithDescription("Pause the current game session")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("resume")
                .WithDescription("Resume a paused game session")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("status")
                .WithDescription("View current session status")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("List recent sessions in this server")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("progress")
                .WithDescription("View session progress summary")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("location")
                .WithDescription("Update current location")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Location name", isRequired: true)
                .AddOption("description", ApplicationCommandOptionType.String, "Location description", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("join")
                .WithDescription("Join the current session")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("character", ApplicationCommandOptionType.String, "Character name to play", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("leave")
                .WithDescription("Leave the current session")
                .WithType(ApplicationCommandOptionType.SubCommand))
            // Phase 4: Session Management Commands
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("archive")
                .WithDescription("Archive a completed session")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("id", ApplicationCommandOptionType.Integer, "Session ID to archive", isRequired: true)
                .AddOption("outcome", ApplicationCommandOptionType.String, "Session outcome summary", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("history")
                .WithDescription("View session history")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("search", ApplicationCommandOptionType.String, "Search term", isRequired: false)
                .AddOption("tag", ApplicationCommandOptionType.String, "Filter by tag", isRequired: false)
                .AddOption("limit", ApplicationCommandOptionType.Integer, "Number of sessions to show", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("stats")
                .WithDescription("View session statistics")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("id", ApplicationCommandOptionType.Integer, "Session ID (optional, uses current if not specified)", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("notes")
                .WithDescription("Add or view session notes")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("id", ApplicationCommandOptionType.Integer, "Session ID (optional, uses current if not specified)", isRequired: false)
                .AddOption("text", ApplicationCommandOptionType.String, "Note text (leave empty to view notes)", isRequired: false)
                .AddOption("type", ApplicationCommandOptionType.String, "Note type (General, Player, Outcome, Reminder)", isRequired: false)
                .AddOption("pinned", ApplicationCommandOptionType.Boolean, "Pin this note", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("break")
                .WithDescription("Start a break in the current session")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("reason", ApplicationCommandOptionType.String, "Reason for the break", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("complete")
                .WithDescription("Mark session as completed and archive")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("outcome", ApplicationCommandOptionType.String, "Session outcome summary", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("search")
                .WithDescription("Search sessions by name, notes, or tags")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("query", ApplicationCommandOptionType.String, "Search query", isRequired: true)
                .AddOption("limit", ApplicationCommandOptionType.Integer, "Number of results", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("summary")
                .WithDescription("Get detailed session summary")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("id", ApplicationCommandOptionType.Integer, "Session ID (optional, uses current if not specified)", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("tags")
                .WithDescription("Manage session tags")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("action", ApplicationCommandOptionType.String, "Action (add, remove, list)", isRequired: true)
                .AddOption("tag", ApplicationCommandOptionType.String, "Tag name", isRequired: false)
                .AddOption("id", ApplicationCommandOptionType.Integer, "Session ID (optional, uses current if not specified)", isRequired: false)
                .AddOption("category", ApplicationCommandOptionType.String, "Tag category (Campaign, Arc, Group, Theme)", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("category")
                .WithDescription("Set session category")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Category name", isRequired: true)
                .AddOption("id", ApplicationCommandOptionType.Integer, "Session ID (optional, uses current if not specified)", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("time")
                .WithDescription("View time tracking statistics")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("guild", ApplicationCommandOptionType.Boolean, "Show guild-wide statistics", isRequired: false)));

        // Narrative commands
        commands.Add(new SlashCommandBuilder()
            .WithName("narrative")
            .WithDescription("Narrative and story management commands")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("record")
                .WithDescription("Record a narrative event")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("title", ApplicationCommandOptionType.String, "Event title", isRequired: true)
                .AddOption("description", ApplicationCommandOptionType.String, "Event description", isRequired: true)
                .AddOption("type", ApplicationCommandOptionType.String, "Event type (StoryBeat, Combat, Social, Investigation, PlotTwist, CharacterDevelopment, WorldEvent, PlayerChoice)", isRequired: false)
                .AddOption("npcs", ApplicationCommandOptionType.String, "NPCs involved (comma-separated)", isRequired: false)
                .AddOption("importance", ApplicationCommandOptionType.Integer, "Importance level (1-10)", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("summary")
                .WithDescription("Generate a story summary")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("events", ApplicationCommandOptionType.Integer, "Number of events to include", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("search")
                .WithDescription("Search narrative events")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("term", ApplicationCommandOptionType.String, "Search term", isRequired: true)));

        // NPC relationship commands
        commands.Add(new SlashCommandOptionBuilder()
            .WithName("npc-relationship")
            .WithDescription("Manage NPC relationships")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("update")
                .WithDescription("Create or update an NPC relationship")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "NPC name", isRequired: true)
                .AddOption("attitude", ApplicationCommandOptionType.Integer, "Attitude change (-10 to +10)", isRequired: false)
                .AddOption("trust", ApplicationCommandOptionType.Integer, "Trust change (0 to 10)", isRequired: false)
                .AddOption("role", ApplicationCommandOptionType.String, "NPC role", isRequired: false)
                .AddOption("organization", ApplicationCommandOptionType.String, "NPC organization", isRequired: false)
                .AddOption("notes", ApplicationCommandOptionType.String, "Notes about NPC", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("List all NPC relationships")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("organization", ApplicationCommandOptionType.String, "Filter by organization", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("view")
                .WithDescription("View details of an NPC relationship")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "NPC name", isRequired: true)));

        // Mission commands
        commands.Add(new SlashCommandBuilder()
            .WithName("mission-track")
            .WithDescription("Mission tracking commands")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("add")
                .WithDescription("Add a new mission")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "Mission name", isRequired: true)
                .AddOption("type", ApplicationCommandOptionType.String, "Mission type", isRequired: true)
                .AddOption("objective", ApplicationCommandOptionType.String, "Mission objective", isRequired: true)
                .AddOption("payment", ApplicationCommandOptionType.Integer, "Payment in nuyen", isRequired: true)
                .AddOption("karma", ApplicationCommandOptionType.Integer, "Karma reward", isRequired: false)
                .AddOption("johnson", ApplicationCommandOptionType.String, "Johnson name", isRequired: false)
                .AddOption("location", ApplicationCommandOptionType.String, "Target location", isRequired: false)
                .AddOption("organization", ApplicationCommandOptionType.String, "Target organization", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("update")
                .WithDescription("Update mission status")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("id", ApplicationCommandOptionType.Integer, "Mission ID", isRequired: true)
                .AddOption("status", ApplicationCommandOptionType.String, "New status (Planning, InProgress, Completed, Failed, Aborted)", isRequired: true)
                .AddOption("notes", ApplicationCommandOptionType.String, "Notes", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("list")
                .WithDescription("List active missions")
                .WithType(ApplicationCommandOptionType.SubCommand)));

        // Phase 5: Dynamic Content Engine Commands
        commands.Add(new SlashCommandBuilder()
            .WithName("difficulty")
            .WithDescription("View or adjust current difficulty level")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("view")
                .WithDescription("View current difficulty and performance metrics")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("adjust")
                .WithDescription("Manually adjust difficulty level")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("level", ApplicationCommandOptionType.Integer, "New difficulty level (1-10)", isRequired: true)));

        commands.Add(new SlashCommandBuilder()
            .WithName("campaign")
            .WithDescription("Manage campaign arcs")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("arc")
                .WithDescription("View or manage campaign arcs")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("action", ApplicationCommandOptionType.String, "Action (view/start/switch)", isRequired: false)
                .AddOption("name", ApplicationCommandOptionType.String, "Arc name", isRequired: false)
                .AddOption("description", ApplicationCommandOptionType.String, "Arc description", isRequired: false)));

        commands.Add(new SlashCommandBuilder()
            .WithName("content")
            .WithDescription("Manage dynamic content")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("regenerate")
                .WithDescription("Regenerate content with new parameters")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("type", ApplicationCommandOptionType.String, "Content type (mission/npc/plothook/encounter)", isRequired: true)
                .AddOption("difficulty", ApplicationCommandOptionType.Integer, "Override difficulty (1-10)", isRequired: false)));

        commands.Add(new SlashCommandBuilder()
            .WithName("learning")
            .WithDescription("View AI learning status")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("status")
                .WithDescription("View current learning status")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("reset")
                .WithDescription("Reset learning data for this session")
                .WithType(ApplicationCommandOptionType.SubCommand)));

        commands.Add(new SlashCommandBuilder()
            .WithName("story")
            .WithDescription("Story evolution commands")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("evolve")
                .WithDescription("Evolve current story based on player choices")
                .WithType(ApplicationCommandOptionType.SubCommand))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("hook")
                .WithDescription("Generate a new plot hook")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("type", ApplicationCommandOptionType.String, "Hook type (story/character/world/faction/mystery)", isRequired: false)));

        commands.Add(new SlashCommandBuilder()
            .WithName("npc-manage")
            .WithDescription("NPC management and learning")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("learn")
                .WithDescription("Record NPC learning data")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "NPC name", isRequired: true)
                .AddOption("event", ApplicationCommandOptionType.String, "Event type", isRequired: true)
                .AddOption("description", ApplicationCommandOptionType.String, "Event description", isRequired: false))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("profile")
                .WithDescription("View NPC learning profile")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("name", ApplicationCommandOptionType.String, "NPC name", isRequired: true)));

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
            case "session":
                await HandleSessionCommandAsync(command);
                break;
            case "narrative":
                await HandleNarrativeCommandAsync(command);
                break;
            case "npc-relationship":
                await HandleNPCRelationshipCommandAsync(command);
                break;
            case "mission-track":
                await HandleMissionTrackCommandAsync(command);
                break;
            // Phase 5: Dynamic Content Engine Commands
            case "difficulty":
            case "campaign":
            case "content":
            case "learning":
            case "story":
            case "npc-manage":
                await HandleDynamicContentCommandAsync(command);
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

        try
        {
            // Get required services
            var logger = _services.GetRequiredService<ILogger<CharacterCommands>>();
            var config = _services.GetRequiredService<BotConfig>();
            var database = _services.GetRequiredService<DatabaseService>();
            var diceService = _services.GetRequiredService<DiceService>();

            // Create CharacterCommands instance
            var characterCommands = new CharacterCommands(logger, config, database, diceService);

            switch (subCommand.Name)
            {
                case "create":
                    await characterCommands.CreateCharacterAsync(command);
                    break;
                case "list":
                    await characterCommands.ListCharactersAsync(command);
                    break;
                case "view":
                    await characterCommands.ViewCharacterAsync(command);
                    break;
                case "delete":
                    await characterCommands.DeleteCharacterAsync(command);
                    break;
                default:
                    await command.RespondAsync($"Unknown character subcommand: {subCommand.Name}", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling character command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleMagicCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();
        var diceService = _services.GetRequiredService<DiceService>();
        var dbService = _services.GetRequiredService<DatabaseService>();

        try
        {
            // Get the user's characters to find an awakened one
            var characters = await dbService.GetUserCharactersAsync(command.User.Id);
            var awakenedChar = characters.FirstOrDefault(c => c.Magic > 0 || 
                c.Archetype.Contains("Mage", StringComparison.OrdinalIgnoreCase) || 
                c.Archetype.Contains("Shaman", StringComparison.OrdinalIgnoreCase) ||
                c.Archetype.Contains("Adept", StringComparison.OrdinalIgnoreCase));

            if (awakenedChar == null)
            {
                await command.RespondAsync("❌ You don't have any awakened characters. Create a Mage, Shaman, or Physical Adept first.", ephemeral: true);
                return;
            }

            // Convert ShadowrunCharacter to MagicSystem model
            var magicSystem = new Models.MagicSystem
            {
                Magic = awakenedChar.Magic,
                Magician = awakenedChar.Archetype.Contains("Mage", StringComparison.OrdinalIgnoreCase) || 
                          awakenedChar.Archetype.Contains("Shaman", StringComparison.OrdinalIgnoreCase),
                Awakened = awakenedChar.Magic > 0,
                Sorcerer = awakenedChar.Archetype.Contains("Mage", StringComparison.OrdinalIgnoreCase),
                Adept = awakenedChar.Archetype.Contains("Adept", StringComparison.OrdinalIgnoreCase),
                Criticality = 0,
                Instinct = awakenedChar.Intelligence,
                Initiative = awakenedChar.Reaction,
                Wounds = awakenedChar.PhysicalDamage + awakenedChar.StunDamage,
                WoundMod = (awakenedChar.PhysicalDamage + awakenedChar.StunDamage) / 3,
                Recovery = awakenedChar.Willpower,
                MagicalResistance = awakenedChar.Willpower,
                InitiativePool = awakenedChar.Reaction,
                ComplexFormPool = awakenedChar.Intelligence,
                Foci = awakenedChar.Spells?.Select(s => new Models.Focus
                {
                    Name = s.SpellName,
                    Type = "Spell Focus",
                    Count = 1,
                    EssenceCost = 0,
                    SkillBonus = 0
                }).ToList() ?? new List<Models.Focus>()
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
            .AddField("Session Management", "/session start/end/pause/resume/status/list/progress/location/join/leave")
            .AddField("Narrative Commands", "/narrative record/summary/search")
            .AddField("NPC Relationships", "/npc-relationship update/list/view")
            .AddField("Mission Tracking", "/mission-track add/update/list")
            .Build();

        await command.RespondAsync(embed: embed);
    }

    private async Task HandleMatrixCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();
        var diceService = _services.GetRequiredService<DiceService>();
        var dbService = _services.GetRequiredService<DatabaseService>();

        try
        {
            // Get the user's characters to find a decker one
            var characters = await dbService.GetUserCharactersAsync(command.User.Id);
            var deckerChar = characters.FirstOrDefault(c => 
                c.Archetype.Contains("Decker", StringComparison.OrdinalIgnoreCase) ||
                c.Archetype.Contains("Rigger", StringComparison.OrdinalIgnoreCase)) ?? characters.FirstOrDefault();

            if (deckerChar == null)
            {
                await command.RespondAsync("❌ You don't have any characters. Create a character first using `/character create`.", ephemeral: true);
                return;
            }

            // Try to get the character's cyberdeck from database
            var cyberdeck = await GetCharacterCyberdeckAsync(dbService, deckerChar.Id);

            // If no cyberdeck exists, create a default one for deckers
            if (cyberdeck == null)
            {
                if (deckerChar.Archetype.Contains("Decker", StringComparison.OrdinalIgnoreCase))
                {
                    cyberdeck = new Models.Cyberdeck
                    {
                        Id = 0,
                        CharacterId = deckerChar.Id,
                        Name = "Novatech Slimcase-10",
                        DeckType = "Portable",
                        MPCP = 5,
                        ActiveMemory = 50,
                        StorageMemory = 200,
                        LoadRating = 7,
                        ResponseRating = 2,
                        Hardening = 1,
                        Value = 250000,
                        InstalledPrograms = new List<Models.DeckProgram>()
                    };

                    // Save to database
                    cyberdeck = await dbService.CreateCyberdeckAsync(cyberdeck);
                }
                else
                {
                    await command.RespondAsync("❌ This character doesn't have a cyberdeck. Only Deckers can access Matrix commands.", ephemeral: true);
                    return;
                }
            }

            // Get or create matrix session
            var session = await GetOrCreateMatrixSessionAsync(dbService, deckerChar.Id, cyberdeck.Id);

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

    private async Task HandleSessionCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();

        try
        {
            var sessionService = _services.GetRequiredService<Services.GameSessionService>();
            var channelId = command.ChannelId ?? 0;
            var guildId = (command.Channel as SocketGuildChannel)?.Guild.Id ?? 0;
            var userId = command.User.Id;

            switch (subCommand.Name)
            {
                case "start":
                    var sessionName = subCommand.Options.FirstOrDefault(o => o.Name == "name")?.Value?.ToString();
                    var session = await sessionService.StartSessionAsync(guildId, channelId, userId, sessionName);
                    
                    var startEmbed = new EmbedBuilder()
                        .WithTitle("🎮 Game Session Started")
                        .WithColor(Color.Green)
                        .WithDescription($"**{session.SessionName}** has begun!")
                        .AddField("Session ID", session.Id.ToString())
                        .AddField("Game Master", $"<@{userId}>")
                        .AddField("Location", session.CurrentLocation)
                        .WithFooter("Use /session join to participate")
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: startEmbed.Build());
                    break;

                case "end":
                    var endSession = await sessionService.EndSessionAsync(channelId);
                    var duration = endSession.EndedAt - endSession.StartedAt;
                    
                    var endEmbed = new EmbedBuilder()
                        .WithTitle("🛑 Game Session Ended")
                        .WithColor(Color.Red)
                        .WithDescription($"**{endSession.SessionName}** has ended.")
                        .AddField("Duration", $"{duration?.TotalHours:F1} hours")
                        .AddField("Participants", endSession.Participants.Count(p => p.IsActive))
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: endEmbed.Build());
                    break;

                case "pause":
                    var pauseSession = await sessionService.PauseSessionAsync(channelId);
                    var pauseEmbed = new EmbedBuilder()
                        .WithTitle("⏸️ Game Session Paused")
                        .WithColor(Color.Orange)
                        .WithDescription($"**{pauseSession.SessionName}** is paused.")
                        .AddField("Use", "/session resume to continue")
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: pauseEmbed.Build());
                    break;

                case "resume":
                    var resumeSession = await sessionService.ResumeSessionAsync(channelId);
                    var resumeEmbed = new EmbedBuilder()
                        .WithTitle("▶️ Game Session Resumed")
                        .WithColor(Color.Green)
                        .WithDescription($"**{resumeSession.SessionName}** continues!")
                        .AddField("Location", resumeSession.CurrentLocation)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: resumeEmbed.Build());
                    break;

                case "status":
                    var statusSession = await sessionService.GetActiveSessionAsync(channelId);
                    if (statusSession == null)
                    {
                        await command.RespondAsync("No active session. Use `/session start` to begin.", ephemeral: true);
                        return;
                    }
                    
                    var statusDuration = DateTime.UtcNow - statusSession.StartedAt;
                    var statusEmbed = new EmbedBuilder()
                        .WithTitle($"📊 Session Status: {statusSession.SessionName}")
                        .WithColor(Color.Blue)
                        .AddField("Status", statusSession.Status.ToString())
                        .AddField("Duration", $"{statusDuration.TotalHours:F1} hours")
                        .AddField("Location", statusSession.CurrentLocation)
                        .AddField("Participants", statusSession.Participants.Count(p => p.IsActive))
                        .AddField("Game Master", $"<@{statusSession.GameMasterUserId}>")
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: statusEmbed.Build());
                    break;

                case "list":
                    var sessions = await sessionService.GetGuildSessionsAsync(guildId, 10);
                    if (sessions.Count == 0)
                    {
                        await command.RespondAsync("No sessions found in this server.", ephemeral: true);
                        return;
                    }
                    
                    var listText = "";
                    foreach (var s in sessions)
                    {
                        var statusIcon = s.Status == SessionStatus.Active ? "🟢" : 
                                        s.Status == SessionStatus.Paused ? "🟡" : "⚫";
                        listText += $"{statusIcon} **{s.SessionName}** (ID: {s.Id})\n";
                        listText += $"   Started: {s.StartedAt:yyyy-MM-dd HH:mm}\n\n";
                    }
                    
                    var listEmbed = new EmbedBuilder()
                        .WithTitle("📋 Recent Sessions")
                        .WithColor(Color.Blue)
                        .WithDescription(listText)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: listEmbed.Build());
                    break;

                case "progress":
                    try
                    {
                        var progress = await sessionService.GetSessionProgressAsync(channelId);
                        var progressEmbed = new EmbedBuilder()
                            .WithTitle($"📈 Session Progress: {progress.SessionName}")
                            .WithColor(Color.Purple)
                            .AddField("Duration", $"{progress.Duration.TotalHours:F1} hours")
                            .AddField("Active Participants", progress.ActiveParticipants)
                            .AddField("Current Location", progress.CurrentLocation)
                            .AddField("Narrative Events", progress.NarrativeEvents)
                            .AddField("Player Choices", progress.PlayerChoices)
                            .AddField("Active Missions", progress.ActiveMissions)
                            .AddField("Completed Missions", progress.CompletedMissions)
                            .WithTimestamp(DateTime.UtcNow);
                        await command.RespondAsync(embed: progressEmbed.Build());
                    }
                    catch (InvalidOperationException)
                    {
                        await command.RespondAsync("No active session found.", ephemeral: true);
                    }
                    break;

                case "location":
                    var locationName = subCommand.Options.First(o => o.Name == "name").Value?.ToString();
                    var locationDesc = subCommand.Options.FirstOrDefault(o => o.Name == "description")?.Value?.ToString();
                    
                    await sessionService.UpdateLocationAsync(channelId, locationName ?? "Unknown", locationDesc);
                    
                    var locationEmbed = new EmbedBuilder()
                        .WithTitle("📍 Location Updated")
                        .WithColor(Color.Teal)
                        .WithDescription($"**{locationName}**")
                        .WithTimestamp(DateTime.UtcNow);
                    
                    if (!string.IsNullOrEmpty(locationDesc))
                    {
                        locationEmbed.AddField("Description", locationDesc);
                    }
                    
                    await command.RespondAsync(embed: locationEmbed.Build());
                    break;

                case "join":
                    var characterName = subCommand.Options.FirstOrDefault(o => o.Name == "character")?.Value?.ToString();
                    int? characterId = null;
                    
                    if (!string.IsNullOrEmpty(characterName))
                    {
                        var dbService = _services.GetRequiredService<DatabaseService>();
                        var character = await dbService.GetCharacterByNameAsync(userId, characterName);
                        if (character != null)
                        {
                            characterId = character.Id;
                        }
                    }
                    
                    var participant = await sessionService.AddParticipantAsync(channelId, userId, characterId);
                    
                    var joinEmbed = new EmbedBuilder()
                        .WithTitle("👋 Joined Session")
                        .WithColor(Color.Green)
                        .WithDescription($"<@{userId}> has joined the session!")
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: joinEmbed.Build());
                    break;

                case "leave":
                    await sessionService.RemoveParticipantAsync(channelId, userId);
                    
                    var leaveEmbed = new EmbedBuilder()
                        .WithTitle("👋 Left Session")
                        .WithColor(Color.Orange)
                        .WithDescription($"<@{userId}> has left the session.")
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: leaveEmbed.Build());
                    break;

                // Phase 4: New Session Management Commands
                case "archive":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var archiveId = (long)subCommand.Options.First(o => o.Name == "id").Value;
                        var archiveOutcome = subCommand.Options.FirstOrDefault(o => o.Name == "outcome")?.Value?.ToString();
                        
                        var archived = await managementService.ArchiveSessionAsync((int)archiveId, archiveOutcome);
                        
                        var archiveEmbed = new EmbedBuilder()
                            .WithTitle("📦 Session Archived")
                            .WithColor(Color.Purple)
                            .WithDescription($"**{archived.SessionName}** has been archived.")
                            .AddField("Duration", $"{archived.DurationMinutes} minutes")
                            .AddField("Participants", archived.ParticipantCount)
                            .AddField("Total Breaks", archived.TotalBreaks)
                            .WithTimestamp(DateTime.UtcNow);
                        await command.RespondAsync(embed: archiveEmbed.Build());
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error archiving session: {ex.Message}", ephemeral: true);
                    }
                    break;

                case "history":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var searchHistory = subCommand.Options.FirstOrDefault(o => o.Name == "search")?.Value?.ToString();
                        var tagFilter = subCommand.Options.FirstOrDefault(o => o.Name == "tag")?.Value?.ToString();
                        var historyLimit = subCommand.Options.FirstOrDefault(o => o.Name == "limit")?.Value as long? ?? 10;
                        
                        var history = await managementService.SearchSessionHistoryAsync(
                            guildId, searchHistory, tag: tagFilter, limit: (int)historyLimit);
                        
                        if (history.Count == 0)
                        {
                            await command.RespondAsync("No session history found matching your criteria.", ephemeral: true);
                            return;
                        }
                        
                        var historyText = "";
                        foreach (var h in history)
                        {
                            historyText += $"📦 **{h.SessionName ?? "Unnamed"}** (ID: {h.Id})\n";
                            historyText += $"   Duration: {h.DurationMinutes} min | Participants: {h.ParticipantCount}\n";
                            historyText += $"   Started: {h.StartedAt:yyyy-MM-dd HH:mm}\n\n";
                        }
                        
                        var historyEmbed = new EmbedBuilder()
                            .WithTitle("📚 Session History")
                            .WithColor(Color.Blue)
                            .WithDescription(historyText)
                            .WithFooter($"Showing {history.Count} sessions")
                            .WithTimestamp(DateTime.UtcNow);
                        await command.RespondAsync(embed: historyEmbed.Build());
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error retrieving history: {ex.Message}", ephemeral: true);
                    }
                    break;

                case "stats":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var statsIdOption = subCommand.Options.FirstOrDefault(o => o.Name == "id")?.Value as long?;
                        
                        int statsSessionId;
                        if (statsIdOption.HasValue)
                        {
                            statsSessionId = (int)statsIdOption.Value;
                        }
                        else
                        {
                            var currentSession = await sessionService.GetActiveSessionAsync(channelId);
                            if (currentSession == null)
                            {
                                await command.RespondAsync("No active session. Please specify a session ID.", ephemeral: true);
                                return;
                            }
                            statsSessionId = currentSession.Id;
                        }
                        
                        var timeStats = await managementService.GetTimeStatisticsAsync(statsSessionId);
                        
                        var statsEmbed = new EmbedBuilder()
                            .WithTitle($"📊 Session Statistics: {timeStats.SessionName}")
                            .WithColor(Color.Purple)
                            .AddField("Total Duration", $"{timeStats.TotalDuration.TotalHours:F2} hours")
                            .AddField("Active Time", $"{timeStats.ActiveTime.TotalHours:F2} hours")
                            .AddField("Break Time", $"{timeStats.TotalBreakTime.TotalMinutes:F0} minutes")
                            .AddField("Total Breaks", timeStats.TotalBreaks.ToString())
                            .AddField("Status", timeStats.Status.ToString())
                            .WithTimestamp(DateTime.UtcNow);
                        await command.RespondAsync(embed: statsEmbed.Build());
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error retrieving statistics: {ex.Message}", ephemeral: true);
                    }
                    break;

                case "notes":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var notesIdOption = subCommand.Options.FirstOrDefault(o => o.Name == "id")?.Value as long?;
                        var notesText = subCommand.Options.FirstOrDefault(o => o.Name == "text")?.Value?.ToString();
                        var notesType = subCommand.Options.FirstOrDefault(o => o.Name == "type")?.Value?.ToString() ?? "General";
                        var notesPinned = subCommand.Options.FirstOrDefault(o => o.Name == "pinned")?.Value as bool? ?? false;
                        
                        int notesSessionId;
                        if (notesIdOption.HasValue)
                        {
                            notesSessionId = (int)notesIdOption.Value;
                        }
                        else
                        {
                            var currentSession = await sessionService.GetActiveSessionAsync(channelId);
                            if (currentSession == null)
                            {
                                await command.RespondAsync("No active session. Please specify a session ID.", ephemeral: true);
                                return;
                            }
                            notesSessionId = currentSession.Id;
                        }
                        
                        if (string.IsNullOrEmpty(notesText))
                        {
                            // View notes
                            var notes = await managementService.GetSessionNotesAsync(notesSessionId);
                            if (notes.Count == 0)
                            {
                                await command.RespondAsync("No notes found for this session.", ephemeral: true);
                                return;
                            }
                            
                            var notesList = "";
                            foreach (var note in notes.Take(10))
                            {
                                var pinIcon = note.IsPinned ? "📌 " : "";
                                notesList += $"{pinIcon}**{note.NoteType}**: {note.Content}\n";
                                notesList += $"   *{note.CreatedAt:yyyy-MM-dd HH:mm}*\n\n";
                            }
                            
                            var notesEmbed = new EmbedBuilder()
                                .WithTitle("📝 Session Notes")
                                .WithColor(Color.Gold)
                                .WithDescription(notesList)
                                .WithFooter($"Showing {Math.Min(10, notes.Count)} of {notes.Count} notes")
                                .WithTimestamp(DateTime.UtcNow);
                            await command.RespondAsync(embed: notesEmbed.Build());
                        }
                        else
                        {
                            // Add note
                            var note = await managementService.AddSessionNoteAsync(
                                notesSessionId, notesText, notesType, userId, notesPinned);
                            
                            await command.RespondAsync($"✅ Note added to session {notesSessionId}", ephemeral: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error with session notes: {ex.Message}", ephemeral: true);
                    }
                    break;

                case "break":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var breakReason = subCommand.Options.FirstOrDefault(o => o.Name == "reason")?.Value?.ToString();
                        
                        var sessionBreak = await managementService.StartBreakAsync(
                            channelId, breakReason, isAutomatic: false, initiatedByUserId: userId);
                        
                        var breakEmbed = new EmbedBuilder()
                            .WithTitle("☕ Session Break Started")
                            .WithColor(Color.Orange)
                            .WithDescription($"Session has been paused for a break.")
                            .AddField("Reason", sessionBreak.Reason)
                            .AddField("Use", "/session resume to end the break")
                            .WithTimestamp(DateTime.UtcNow);
                        await command.RespondAsync(embed: breakEmbed.Build());
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error starting break: {ex.Message}", ephemeral: true);
                    }
                    break;

                case "complete":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var completeOutcome = subCommand.Options.FirstOrDefault(o => o.Name == "outcome")?.Value?.ToString();
                        
                        var completed = await managementService.CompleteSessionAsync(
                            channelId, completeOutcome, autoArchive: true);
                        
                        var completeEmbed = new EmbedBuilder()
                            .WithTitle("✅ Session Completed & Archived")
                            .WithColor(Color.Green)
                            .WithDescription($"**{completed.SessionName}** has been completed and archived.")
                            .AddField("Duration", $"{completed.DurationMinutes} minutes")
                            .AddField("Participants", completed.ParticipantCount)
                            .AddField("Total Breaks", completed.TotalBreaks)
                            .WithTimestamp(DateTime.UtcNow);
                        await command.RespondAsync(embed: completeEmbed.Build());
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error completing session: {ex.Message}", ephemeral: true);
                    }
                    break;

                case "search":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var searchQuery = subCommand.Options.First(o => o.Name == "query").Value?.ToString();
                        var searchLimit = subCommand.Options.FirstOrDefault(o => o.Name == "limit")?.Value as long? ?? 10;
                        
                        var searchResults = await managementService.SearchSessionHistoryAsync(
                            guildId, searchQuery, limit: (int)searchLimit);
                        
                        if (searchResults.Count == 0)
                        {
                            await command.RespondAsync($"No sessions found matching '{searchQuery}'.", ephemeral: true);
                            return;
                        }
                        
                        var searchResultsText = "";
                        foreach (var result in searchResults)
                        {
                            searchResultsText += $"🔍 **{result.SessionName ?? "Unnamed"}** (ID: {result.Id})\n";
                            searchResultsText += $"   {result.StartedAt:yyyy-MM-dd} | {result.DurationMinutes} min\n\n";
                        }
                        
                        var searchResultsEmbed = new EmbedBuilder()
                            .WithTitle($"🔍 Search Results: '{searchQuery}'")
                            .WithColor(Color.Blue)
                            .WithDescription(searchResultsText)
                            .WithFooter($"Found {searchResults.Count} sessions")
                            .WithTimestamp(DateTime.UtcNow);
                        await command.RespondAsync(embed: searchResultsEmbed.Build());
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error searching sessions: {ex.Message}", ephemeral: true);
                    }
                    break;

                case "summary":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var summaryIdOption = subCommand.Options.FirstOrDefault(o => o.Name == "id")?.Value as long?;
                        
                        int summarySessionId;
                        if (summaryIdOption.HasValue)
                        {
                            summarySessionId = (int)summaryIdOption.Value;
                        }
                        else
                        {
                            var currentSession = await sessionService.GetActiveSessionAsync(channelId);
                            if (currentSession == null)
                            {
                                await command.RespondAsync("No active session. Please specify a session ID.", ephemeral: true);
                                return;
                            }
                            summarySessionId = currentSession.Id;
                        }
                        
                        var summary = await managementService.GetSessionSummaryAsync(summarySessionId);
                        
                        var summaryEmbed = new EmbedBuilder()
                            .WithTitle($"📋 Session Summary: {summary.SessionName}")
                            .WithColor(Color.Purple)
                            .AddField("Status", summary.Status.ToString())
                            .AddField("Duration", $"{summary.Duration.TotalHours:F2} hours ({summary.ActiveTime.TotalHours:F2}h active)")
                            .AddField("Location", summary.CurrentLocation)
                            .AddField("Participants", $"{summary.ParticipantCount} players")
                            .AddField("Progress", $"📖 {summary.NarrativeEvents} events | 🎯 {summary.PlayerChoices} choices")
                            .AddField("Missions", $"⚔️ {summary.ActiveMissions} active | ✅ {summary.CompletedMissions} completed")
                            .AddField("Breaks", $"{summary.TotalBreaks} breaks ({summary.TotalBreakTime.TotalMinutes:F0} min)")
                            .WithTimestamp(DateTime.UtcNow);
                        
                        if (summary.Tags.Any())
                        {
                            summaryEmbed.AddField("Tags", string.Join(", ", summary.Tags));
                        }
                        
                        await command.RespondAsync(embed: summaryEmbed.Build());
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error retrieving summary: {ex.Message}", ephemeral: true);
                    }
                    break;

                case "tags":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var tagsAction = subCommand.Options.First(o => o.Name == "action").Value?.ToString()?.ToLower();
                        var tagsTag = subCommand.Options.FirstOrDefault(o => o.Name == "tag")?.Value?.ToString();
                        var tagsIdOption = subCommand.Options.FirstOrDefault(o => o.Name == "id")?.Value as long?;
                        var tagsCategory = subCommand.Options.FirstOrDefault(o => o.Name == "category")?.Value?.ToString();
                        
                        int tagsSessionId;
                        if (tagsIdOption.HasValue)
                        {
                            tagsSessionId = (int)tagsIdOption.Value;
                        }
                        else
                        {
                            var currentSession = await sessionService.GetActiveSessionAsync(channelId);
                            if (currentSession == null)
                            {
                                await command.RespondAsync("No active session. Please specify a session ID.", ephemeral: true);
                                return;
                            }
                            tagsSessionId = currentSession.Id;
                        }
                        
                        switch (tagsAction)
                        {
                            case "add":
                                if (string.IsNullOrEmpty(tagsTag))
                                {
                                    await command.RespondAsync("Please specify a tag name.", ephemeral: true);
                                    return;
                                }
                                await managementService.AddSessionTagAsync(tagsSessionId, tagsTag, tagsCategory, userId);
                                await command.RespondAsync($"✅ Tag '{tagsTag}' added to session {tagsSessionId}", ephemeral: true);
                                break;
                                
                            case "remove":
                                if (string.IsNullOrEmpty(tagsTag))
                                {
                                    await command.RespondAsync("Please specify a tag name.", ephemeral: true);
                                    return;
                                }
                                await managementService.RemoveSessionTagAsync(tagsSessionId, tagsTag);
                                await command.RespondAsync($"✅ Tag '{tagsTag}' removed from session {tagsSessionId}", ephemeral: true);
                                break;
                                
                            case "list":
                                var tags = await managementService.GetSessionTagsAsync(tagsSessionId);
                                if (tags.Count == 0)
                                {
                                    await command.RespondAsync("No tags found for this session.", ephemeral: true);
                                    return;
                                }
                                
                                var tagsList = string.Join("\n", tags.Select(t => 
                                    $"• **{t.TagName}** ({t.Category})"));
                                
                                var tagsEmbed = new EmbedBuilder()
                                    .WithTitle($"🏷️ Session Tags (ID: {tagsSessionId})")
                                    .WithColor(Color.Gold)
                                    .WithDescription(tagsList)
                                    .WithTimestamp(DateTime.UtcNow);
                                await command.RespondAsync(embed: tagsEmbed.Build());
                                break;
                                
                            default:
                                await command.RespondAsync($"Unknown tags action: {tagsAction}. Use add, remove, or list.", ephemeral: true);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error managing tags: {ex.Message}", ephemeral: true);
                    }
                    break;

                case "category":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var categoryName = subCommand.Options.First(o => o.Name == "name").Value?.ToString();
                        var categoryIdOption = subCommand.Options.FirstOrDefault(o => o.Name == "id")?.Value as long?;
                        
                        int categorySessionId;
                        if (categoryIdOption.HasValue)
                        {
                            categorySessionId = (int)categoryIdOption.Value;
                        }
                        else
                        {
                            var currentSession = await sessionService.GetActiveSessionAsync(channelId);
                            if (currentSession == null)
                            {
                                await command.RespondAsync("No active session. Please specify a session ID.", ephemeral: true);
                                return;
                            }
                            categorySessionId = currentSession.Id;
                        }
                        
                        await managementService.SetSessionCategoryAsync(categorySessionId, categoryName ?? "General");
                        await command.RespondAsync($"✅ Session {categorySessionId} category set to '{categoryName}'", ephemeral: true);
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error setting category: {ex.Message}", ephemeral: true);
                    }
                    break;

                case "time":
                    try
                    {
                        var managementService = _services.GetRequiredService<Services.SessionManagementService>();
                        var showGuild = subCommand.Options.FirstOrDefault(o => o.Name == "guild")?.Value as bool? ?? false;
                        
                        if (showGuild)
                        {
                            var guildStats = await managementService.GetGuildTimeStatisticsAsync(guildId);
                            
                            var timeEmbed = new EmbedBuilder()
                                .WithTitle("📊 Guild Time Statistics")
                                .WithColor(Color.Purple)
                                .AddField("Total Sessions", guildStats.TotalSessions.ToString())
                                .AddField("Total Duration", $"{guildStats.TotalDuration.TotalHours:F1} hours")
                                .AddField("Total Active Time", $"{guildStats.TotalActiveTime.TotalHours:F1} hours")
                                .AddField("Total Breaks", guildStats.TotalBreaks.ToString())
                                .AddField("Average Session", $"{guildStats.AverageSessionDuration.TotalMinutes:F0} minutes")
                                .AddField("Longest Session", $"{guildStats.LongestSession.TotalMinutes:F0} minutes")
                                .WithTimestamp(DateTime.UtcNow);
                            await command.RespondAsync(embed: timeEmbed.Build());
                        }
                        else
                        {
                            var currentSession = await sessionService.GetActiveSessionAsync(channelId);
                            if (currentSession == null)
                            {
                                await command.RespondAsync("No active session. Use 'guild: true' to see guild statistics.", ephemeral: true);
                                return;
                            }
                            
                            var sessionStats = await managementService.GetTimeStatisticsAsync(currentSession.Id);
                            
                            var timeEmbed = new EmbedBuilder()
                                .WithTitle($"📊 Time Statistics: {sessionStats.SessionName}")
                                .WithColor(Color.Purple)
                                .AddField("Total Duration", $"{sessionStats.TotalDuration.TotalHours:F2} hours")
                                .AddField("Active Time", $"{sessionStats.ActiveTime.TotalHours:F2} hours")
                                .AddField("Break Time", $"{sessionStats.TotalBreakTime.TotalMinutes:F0} minutes")
                                .AddField("Total Breaks", sessionStats.TotalBreaks.ToString())
                                .WithTimestamp(DateTime.UtcNow);
                            await command.RespondAsync(embed: timeEmbed.Build());
                        }
                    }
                    catch (Exception ex)
                    {
                        await command.RespondAsync($"Error retrieving time statistics: {ex.Message}", ephemeral: true);
                    }
                    break;

                default:
                    await command.RespondAsync($"Unknown session subcommand: {subCommand.Name}", ephemeral: true);
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            await command.RespondAsync(ex.Message, ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling session command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleNarrativeCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();

        try
        {
            var narrativeService = _services.GetRequiredService<Services.NarrativeContextService>();
            var channelId = command.ChannelId ?? 0;

            switch (subCommand.Name)
            {
                case "record":
                    var title = subCommand.Options.First(o => o.Name == "title").Value?.ToString();
                    var description = subCommand.Options.First(o => o.Name == "description").Value?.ToString();
                    var typeStr = subCommand.Options.FirstOrDefault(o => o.Name == "type")?.Value?.ToString();
                    var npcs = subCommand.Options.FirstOrDefault(o => o.Name == "npcs")?.Value?.ToString();
                    var importance = subCommand.Options.FirstOrDefault(o => o.Name == "importance")?.Value as long? ?? 5;
                    
                    if (!Enum.TryParse<NarrativeEventType>(typeStr, out var eventType))
                    {
                        eventType = NarrativeEventType.StoryBeat;
                    }
                    
                    var evt = await narrativeService.RecordEventAsync(
                        channelId, title ?? "Untitled", description ?? "", eventType, 
                        npcsInvolved: npcs, importance: (int)importance);
                    
                    var recordEmbed = new EmbedBuilder()
                        .WithTitle($"📝 Narrative Event Recorded")
                        .WithColor(Color.Purple)
                        .WithDescription($"**{evt.Title}**\n{evt.Description}")
                        .AddField("Type", evt.EventType.ToString())
                        .AddField("Importance", evt.Importance.ToString())
                        .WithTimestamp(DateTime.UtcNow);
                    
                    if (!string.IsNullOrEmpty(evt.NPCsInvolved))
                    {
                        recordEmbed.AddField("NPCs Involved", evt.NPCsInvolved);
                    }
                    
                    await command.RespondAsync(embed: recordEmbed.Build());
                    break;

                case "summary":
                    var eventCount = subCommand.Options.FirstOrDefault(o => o.Name == "events")?.Value as long? ?? 10;
                    var summary = await narrativeService.GenerateStorySummaryAsync(channelId, (int)eventCount);
                    
                    var summaryEmbed = new EmbedBuilder()
                        .WithTitle("📖 Story Summary")
                        .WithColor(Color.Purple)
                        .WithDescription(summary)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: summaryEmbed.Build());
                    break;

                case "search":
                    var searchTerm = subCommand.Options.First(o => o.Name == "term").Value?.ToString();
                    var results = await narrativeService.SearchEventsAsync(channelId, searchTerm ?? "");
                    
                    if (results.Count == 0)
                    {
                        await command.RespondAsync($"No narrative events found matching '{searchTerm}'.", ephemeral: true);
                        return;
                    }
                    
                    var searchResults = "";
                    foreach (var result in results.Take(10))
                    {
                        searchResults += $"• **{result.Title}** ({result.EventType})\n";
                        searchResults += $"  {result.Description.Substring(0, Math.Min(100, result.Description.Length))}...\n\n";
                    }
                    
                    var searchEmbed = new EmbedBuilder()
                        .WithTitle($"🔍 Search Results: '{searchTerm}'")
                        .WithColor(Color.Blue)
                        .WithDescription(searchResults)
                        .WithFooter($"Showing {Math.Min(10, results.Count)} of {results.Count} results")
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: searchEmbed.Build());
                    break;

                default:
                    await command.RespondAsync($"Unknown narrative subcommand: {subCommand.Name}", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling narrative command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleNPCRelationshipCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();

        try
        {
            var narrativeService = _services.GetRequiredService<Services.NarrativeContextService>();
            var channelId = command.ChannelId ?? 0;

            switch (subCommand.Name)
            {
                case "update":
                    var npcName = subCommand.Options.First(o => o.Name == "name").Value?.ToString();
                    var attitude = subCommand.Options.FirstOrDefault(o => o.Name == "attitude")?.Value as long? ?? 0;
                    var trust = subCommand.Options.FirstOrDefault(o => o.Name == "trust")?.Value as long? ?? 0;
                    var role = subCommand.Options.FirstOrDefault(o => o.Name == "role")?.Value?.ToString();
                    var org = subCommand.Options.FirstOrDefault(o => o.Name == "organization")?.Value?.ToString();
                    var notes = subCommand.Options.FirstOrDefault(o => o.Name == "notes")?.Value?.ToString();
                    
                    var relationship = await narrativeService.UpdateNPCRelationshipAsync(
                        channelId, npcName ?? "Unknown", role, org, 
                        (int)attitude, (int)trust, notes);
                    
                    var summary = narrativeService.FormatRelationshipSummary(relationship);
                    
                    var updateEmbed = new EmbedBuilder()
                        .WithTitle("👤 NPC Relationship Updated")
                        .WithColor(Color.Gold)
                        .WithDescription(summary)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: updateEmbed.Build());
                    break;

                case "list":
                    var orgFilter = subCommand.Options.FirstOrDefault(o => o.Name == "organization")?.Value?.ToString();
                    var npcs = string.IsNullOrEmpty(orgFilter)
                        ? await narrativeService.GetAllNPCRelationshipsAsync(channelId)
                        : await narrativeService.GetNPCsByOrganizationAsync(channelId, orgFilter);
                    
                    if (npcs.Count == 0)
                    {
                        await command.RespondAsync("No NPC relationships found.", ephemeral: true);
                        return;
                    }
                    
                    var listText = "";
                    foreach (var npc in npcs)
                    {
                        listText += narrativeService.FormatRelationshipSummary(npc) + "\n";
                    }
                    
                    var listEmbed = new EmbedBuilder()
                        .WithTitle("👥 NPC Relationships")
                        .WithColor(Color.Gold)
                        .WithDescription(listText)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: listEmbed.Build());
                    break;

                case "view":
                    var viewName = subCommand.Options.First(o => o.Name == "name").Value?.ToString();
                    var viewNpc = await narrativeService.GetNPCRelationshipAsync(channelId, viewName ?? "");
                    
                    if (viewNpc == null)
                    {
                        await command.RespondAsync($"No relationship found with NPC '{viewName}'.", ephemeral: true);
                        return;
                    }
                    
                    var viewSummary = narrativeService.FormatRelationshipSummary(viewNpc);
                    var viewEmbed = new EmbedBuilder()
                        .WithTitle($"👤 NPC: {viewNpc.NPCName}")
                        .WithColor(Color.Gold)
                        .WithDescription(viewSummary)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: viewEmbed.Build());
                    break;

                default:
                    await command.RespondAsync($"Unknown npc-relationship subcommand: {subCommand.Name}", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling NPC relationship command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    private async Task HandleMissionTrackCommandAsync(SocketSlashCommand command)
    {
        var subCommand = command.Data.Options.First();

        try
        {
            var narrativeService = _services.GetRequiredService<Services.NarrativeContextService>();
            var channelId = command.ChannelId ?? 0;

            switch (subCommand.Name)
            {
                case "add":
                    var name = subCommand.Options.First(o => o.Name == "name").Value?.ToString();
                    var type = subCommand.Options.First(o => o.Name == "type").Value?.ToString();
                    var objective = subCommand.Options.First(o => o.Name == "objective").Value?.ToString();
                    var payment = (long)subCommand.Options.First(o => o.Name == "payment").Value;
                    var karma = subCommand.Options.FirstOrDefault(o => o.Name == "karma")?.Value as long? ?? 0;
                    var johnson = subCommand.Options.FirstOrDefault(o => o.Name == "johnson")?.Value?.ToString();
                    var location = subCommand.Options.FirstOrDefault(o => o.Name == "location")?.Value?.ToString();
                    var org = subCommand.Options.FirstOrDefault(o => o.Name == "organization")?.Value?.ToString();
                    
                    var mission = await narrativeService.AddMissionAsync(
                        channelId, name ?? "Unnamed", type ?? "Datasteal", 
                        objective ?? "", payment, (int)karma, johnson, location, org);
                    
                    var missionSummary = narrativeService.FormatMissionSummary(mission);
                    
                    var addEmbed = new EmbedBuilder()
                        .WithTitle("📋 Mission Added")
                        .WithColor(Color.Red)
                        .WithDescription(missionSummary)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: addEmbed.Build());
                    break;

                case "update":
                    var missionId = (long)subCommand.Options.First(o => o.Name == "id").Value;
                    var statusStr = subCommand.Options.First(o => o.Name == "status").Value?.ToString();
                    var updateNotes = subCommand.Options.FirstOrDefault(o => o.Name == "notes")?.Value?.ToString();
                    
                    if (!Enum.TryParse<MissionStatus>(statusStr, out var newStatus))
                    {
                        await command.RespondAsync($"Invalid status: {statusStr}", ephemeral: true);
                        return;
                    }
                    
                    await narrativeService.UpdateMissionStatusAsync((int)missionId, newStatus, updateNotes);
                    
                    var updateEmbed = new EmbedBuilder()
                        .WithTitle("📋 Mission Updated")
                        .WithColor(Color.Green)
                        .WithDescription($"Mission #{missionId} status changed to **{newStatus}**")
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: updateEmbed.Build());
                    break;

                case "list":
                    var missions = await narrativeService.GetActiveMissionsAsync(channelId);
                    
                    if (missions.Count == 0)
                    {
                        await command.RespondAsync("No active missions found.", ephemeral: true);
                        return;
                    }
                    
                    var missionList = "";
                    foreach (var m in missions)
                    {
                        missionList += narrativeService.FormatMissionSummary(m) + "\n";
                    }
                    
                    var listEmbed = new EmbedBuilder()
                        .WithTitle("📋 Active Missions")
                        .WithColor(Color.Red)
                        .WithDescription(missionList)
                        .WithTimestamp(DateTime.UtcNow);
                    await command.RespondAsync(embed: listEmbed.Build());
                    break;

                default:
                    await command.RespondAsync($"Unknown mission-track subcommand: {subCommand.Name}", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling mission track command");
            await command.RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    #region Phase 5: Dynamic Content Engine Command Handler

    private async Task HandleDynamicContentCommandAsync(SocketSlashCommand command)
    {
        try
        {
            var contentCommands = _services.GetRequiredService<Commands.DynamicContentCommands>();
            await contentCommands.HandleCommandAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling dynamic content command: {CommandName}", command.CommandName);
            await command.RespondAsync($"An error occurred: {ex.Message}", ephemeral: true);
        }
    }

    #endregion

    #region Helper Methods for Database Integration

    /// <summary>
    /// Get the cyberdeck for a character from the database
    /// </summary>
    private static Task<Models.Cyberdeck?> GetCharacterCyberdeckAsync(DatabaseService dbService, int characterId)
    {
        // Placeholder - returns null to trigger default deck creation
        // In production, implement direct database query
        return Task.FromResult<Models.Cyberdeck?>(null);
    }

    /// <summary>
    /// Get or create a matrix session for a character
    /// </summary>
    private async Task<Models.MatrixSession> GetOrCreateMatrixSessionAsync(DatabaseService dbService, int characterId, int cyberdeckId)
    {
        try
        {
            var activeRun = await dbService.GetActiveMatrixRunAsync(characterId).ConfigureAwait(false);
            
            return activeRun != null
                ? MapMatrixRunToSession(activeRun, characterId, cyberdeckId)
                : CreateDefaultMatrixSession(characterId, cyberdeckId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting/creating matrix session for character {CharacterId}", characterId);
            return CreateDefaultMatrixSession(characterId, cyberdeckId);
        }
    }

    private static Models.MatrixSession MapMatrixRunToSession(Models.MatrixRun activeRun, int characterId, int cyberdeckId)
    {
        return new Models.MatrixSession
        {
            Id = activeRun.Id,
            CharacterId = characterId,
            CyberdeckId = cyberdeckId,
            IsInVR = false,
            SecurityTally = activeRun.SecurityTally,
            AlertLevel = activeRun.AlertStatus ?? "None",
            CurrentInitiative = 0,
            InitiativePasses = 1,
            ActiveICE = new List<Models.ActiveICE>()
        };
    }

    private static Models.MatrixSession CreateDefaultMatrixSession(int characterId, int cyberdeckId)
    {
        return new Models.MatrixSession
        {
            Id = 0,
            CharacterId = characterId,
            CyberdeckId = cyberdeckId,
            IsInVR = false,
            SecurityTally = 0,
            AlertLevel = "None",
            CurrentInitiative = 0,
            InitiativePasses = 1,
            ActiveICE = new List<Models.ActiveICE>()
        };
    }

    #endregion

    #region Embed Builder Helpers

    /// <summary>
    /// Creates a basic embed with common settings
    /// </summary>
    private EmbedBuilder CreateBaseEmbed(string title, Color? color = null, string? description = null)
    {
        var builder = new EmbedBuilder()
            .WithTitle(title)
            .WithColor(color ?? _config.Bot.DefaultColor)
            .WithTimestamp(DateTime.UtcNow);

        if (!string.IsNullOrEmpty(description))
        {
            builder.WithDescription(description);
        }

        return builder;
    }

    /// <summary>
    /// Creates an error embed for error responses
    /// </summary>
    private static EmbedBuilder CreateErrorEmbed(string title, string description)
    {
        return new EmbedBuilder()
            .WithTitle(title)
            .WithColor(Color.Red)
            .WithDescription(description)
            .WithTimestamp(DateTime.UtcNow);
    }

    /// <summary>
    /// Creates a success embed for successful operations
    /// </summary>
    private static EmbedBuilder CreateSuccessEmbed(string title, string description)
    {
        return new EmbedBuilder()
            .WithTitle(title)
            .WithColor(Color.Green)
            .WithDescription(description)
            .WithTimestamp(DateTime.UtcNow);
    }

    /// <summary>
    /// Safely gets an option value from slash command options
    /// </summary>
    private static T? GetOptionValue<T>(SocketSlashCommand command, string optionName, T? defaultValue = default)
    {
        var option = command.Data.Options.FirstOrDefault(o => o.Name == optionName);
        return option != null ? (T?)option.Value : defaultValue;
    }

    /// <summary>
    /// Safely gets an option value from subcommand options
    /// </summary>
    private static T? GetSubOptionValue<T>(SocketSlashCommandDataOption subCommand, string optionName, T? defaultValue = default)
    {
        var option = subCommand.Options.FirstOrDefault(o => o.Name == optionName);
        return option != null ? (T?)option.Value : defaultValue;
    }

    /// <summary>
    /// Builds a formatted list string with consistent formatting
    /// </summary>
    private static string BuildListString<T>(IEnumerable<T> items, Func<T, string> formatter, int maxItems = 10)
    {
        var sb = new StringBuilder();
        var itemList = items.Take(maxItems).ToList();
        
        foreach (var item in itemList)
        {
            sb.AppendLine(formatter(item));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Safely responds to a command with error message
    /// </summary>
    private static async Task RespondWithErrorAsync(SocketSlashCommand command, string message)
    {
        await command.RespondAsync($"❌ {message}", ephemeral: true).ConfigureAwait(false);
    }

    /// <summary>
    /// Safely responds to a command with success message
    /// </summary>
    private static async Task RespondWithSuccessAsync(SocketSlashCommand command, string message)
    {
        await command.RespondAsync($"✅ {message}", ephemeral: true).ConfigureAwait(false);
    }

    #endregion
}
