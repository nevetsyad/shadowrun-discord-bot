using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Main service for interactive storytelling - handles player inputs, skill checks, 
/// encounter generation, and NPC dialogue in an autonomous Shadowrun campaign.
/// Consolidates: Input parsing, skill checks, encounters, and dialogue generation.
/// </summary>
public class InteractiveStoryService
{
    private readonly DatabaseService _database;
    private readonly GameSessionService _sessionService;
    private readonly NarrativeContextService _narrativeService;
    private readonly AutonomousMissionService _missionService;
    private readonly DiceService _diceService;
    private readonly GMService _gmService;
    private readonly ILogger<InteractiveStoryService> _logger;

    // Skill definitions for Shadowrun 3e
    private static readonly Dictionary<string, SkillDefinition> SkillDefinitions = new()
    {
        // Combat Skills
        ["firearms"] = new() { Category = SkillCategory.Combat, LinkedAttribute = "Quickness", Default = true },
        ["edged weapons"] = new() { Category = SkillCategory.Combat, LinkedAttribute = "Quickness", Default = true },
        ["clubs"] = new() { Category = SkillCategory.Combat, LinkedAttribute = "Quickness", Default = true },
        ["unarmed combat"] = new() { Category = SkillCategory.Combat, LinkedAttribute = "Strength", Default = true },
        ["thrown weapons"] = new() { Category = SkillCategory.Combat, LinkedAttribute = "Quickness", Default = true },
        ["assault rifles"] = new() { Category = SkillCategory.Combat, LinkedAttribute = "Quickness", Default = false },
        ["shotguns"] = new() { Category = SkillCategory.Combat, LinkedAttribute = "Quickness", Default = false },
        ["heavy weapons"] = new() { Category = SkillCategory.Combat, LinkedAttribute = "Quickness", Default = false },
        
        // Physical Skills
        ["athletics"] = new() { Category = SkillCategory.Physical, LinkedAttribute = "Strength", Default = true },
        ["stealth"] = new() { Category = SkillCategory.Physical, LinkedAttribute = "Quickness", Default = true },
        ["driving"] = new() { Category = SkillCategory.Physical, LinkedAttribute = "Reaction", Default = true },
        ["pilot aircraft"] = new() { Category = SkillCategory.Physical, LinkedAttribute = "Reaction", Default = false },
        ["biotech"] = new() { Category = SkillCategory.Physical, LinkedAttribute = "Intelligence", Default = false },
        
        // Social Skills
        ["etiquette"] = new() { Category = SkillCategory.Social, LinkedAttribute = "Charisma", Default = true },
        ["negotiation"] = new() { Category = SkillCategory.Social, LinkedAttribute = "Charisma", Default = true },
        ["leadership"] = new() { Category = SkillCategory.Social, LinkedAttribute = "Charisma", Default = true },
        ["interrogation"] = new() { Category = SkillCategory.Social, LinkedAttribute = "Charisma", Default = true },
        ["con"] = new() { Category = SkillCategory.Social, LinkedAttribute = "Charisma", Default = true },
        
        // Technical Skills
        ["computers"] = new() { Category = SkillCategory.Technical, LinkedAttribute = "Intelligence", Default = false },
        ["electronics"] = new() { Category = SkillCategory.Technical, LinkedAttribute = "Intelligence", Default = false },
        ["demolitions"] = new() { Category = SkillCategory.Technical, LinkedAttribute = "Intelligence", Default = false },
        ["building"] = new() { Category = SkillCategory.Technical, LinkedAttribute = "Intelligence", Default = false },
        
        // Magic Skills
        ["sorcery"] = new() { Category = SkillCategory.Magic, LinkedAttribute = "Magic", Default = false },
        ["conjuring"] = new() { Category = SkillCategory.Magic, LinkedAttribute = "Magic", Default = false },
        ["enchanting"] = new() { Category = SkillCategory.Magic, LinkedAttribute = "Magic", Default = false },
        ["centering"] = new() { Category = SkillCategory.Magic, LinkedAttribute = "Magic", Default = false },
        
        // Knowledge Skills
        ["lore"] = new() { Category = SkillCategory.Knowledge, LinkedAttribute = "Intelligence", Default = false },
        ["knowledge"] = new() { Category = SkillCategory.Knowledge, LinkedAttribute = "Intelligence", Default = false },
        ["investigation"] = new() { Category = SkillCategory.Knowledge, LinkedAttribute = "Intelligence", Default = true },
        ["perception"] = new() { Category = SkillCategory.Knowledge, LinkedAttribute = "Intelligence", Default = true }
    };

    public InteractiveStoryService(
        DatabaseService database,
        GameSessionService sessionService,
        NarrativeContextService narrativeService,
        AutonomousMissionService missionService,
        DiceService diceService,
        GMService gmService,
        ILogger<InteractiveStoryService> logger)
    {
        _database = database;
        _sessionService = sessionService;
        _narrativeService = narrativeService;
        _missionService = missionService;
        _diceService = diceService;
        _gmService = gmService;
        _logger = logger;
    }

    #region Player Input Parsing

    /// <summary>
    /// Parse and execute a player command
    /// </summary>
    public async Task<StoryResponse> ProcessPlayerInputAsync(
        ulong channelId,
        ulong userId,
        string input)
    {
        var context = await BuildStoryContextAsync(channelId, userId);
        var parsed = ParseInput(input);

        _logger.LogInformation("Processing player input: {Command} from user {UserId}", parsed.CommandType, userId);

        return parsed.CommandType switch
        {
            CommandType.Roleplay => await HandleRoleplayAsync(context, parsed),
            CommandType.SkillCheck => await HandleSkillCheckAsync(context, parsed),
            CommandType.Investigate => await HandleInvestigateAsync(context, parsed),
            CommandType.Dialogue => await HandleDialogueAsync(context, parsed),
            CommandType.Search => await HandleSearchAsync(context, parsed),
            CommandType.Listen => await HandleListenAsync(context, parsed),
            CommandType.Interact => await HandleInteractAsync(context, parsed),
            CommandType.UseItem => await HandleUseItemAsync(context, parsed),
            CommandType.Talk => await HandleTalkAsync(context, parsed),
            CommandType.Describe => await HandleDescribeAsync(context, parsed),
            CommandType.Help => await HandleHelpAsync(context, parsed),
            _ => await HandleUnknownAsync(context, parsed)
        };
    }

    /// <summary>
    /// Parse player input into command type and parameters
    /// </summary>
    private ParsedInput ParseInput(string input)
    {
        input = input.Trim();
        
        // Handle slash commands
        if (input.StartsWith("/"))
        {
            var parts = input.Substring(1).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();
            var args = parts.Length > 1 ? parts[1] : "";

            return new ParsedInput
            {
                RawInput = input,
                CommandType = ParseCommandType(command),
                Command = command,
                Arguments = args,
                Parameters = ParseParameters(args)
            };
        }

        // Handle natural language
        return ParseNaturalLanguage(input);
    }

    /// <summary>
    /// Parse command type from string
    /// </summary>
    private CommandType ParseCommandType(string command)
    {
        return command.ToLower() switch
        {
            "roleplay" or "rp" or "emote" => CommandType.Roleplay,
            "check" or "roll" or "test" => CommandType.SkillCheck,
            "investigate" or "examine" or "look" => CommandType.Investigate,
            "dialogue" or "say" or "tell" => CommandType.Dialogue,
            "search" => CommandType.Search,
            "listen" => CommandType.Listen,
            "interact" or "use" => CommandType.Interact,
            "use" => CommandType.UseItem,
            "talk" or "speak" => CommandType.Talk,
            "describe" or "scene" => CommandType.Describe,
            "help" => CommandType.Help,
            _ => CommandType.Unknown
        };
    }

    /// <summary>
    /// Parse natural language input
    /// </summary>
    private ParsedInput ParseNaturalLanguage(string input)
    {
        var lowerInput = input.ToLower();

        // Detect intent from natural language
        if (ContainsAny(lowerInput, "roll", "check", "test", "make a", "attempt"))
        {
            var skill = ExtractSkillName(input);
            return new ParsedInput
            {
                RawInput = input,
                CommandType = CommandType.SkillCheck,
                Command = "check",
                Arguments = skill,
                Parameters = new Dictionary<string, string> { ["skill"] = skill }
            };
        }

        if (ContainsAny(lowerInput, "search", "look for", "find", "examine"))
        {
            return new ParsedInput
            {
                RawInput = input,
                CommandType = CommandType.Search,
                Command = "search",
                Arguments = input,
                Parameters = new Dictionary<string, string> { ["query"] = input }
            };
        }

        if (ContainsAny(lowerInput, "listen", "hear", "sounds"))
        {
            return new ParsedInput
            {
                RawInput = input,
                CommandType = CommandType.Listen,
                Command = "listen",
                Arguments = "",
                Parameters = new Dictionary<string, string>()
            };
        }

        if (ContainsAny(lowerInput, "talk to", "speak with", "ask", "tell"))
        {
            var npc = ExtractNPCName(input);
            return new ParsedInput
            {
                RawInput = input,
                CommandType = CommandType.Talk,
                Command = "talk",
                Arguments = npc,
                Parameters = new Dictionary<string, string> { ["npc"] = npc, ["message"] = input }
            };
        }

        if (ContainsAny(lowerInput, "investigate", "inspect", "check out"))
        {
            return new ParsedInput
            {
                RawInput = input,
                CommandType = CommandType.Investigate,
                Command = "investigate",
                Arguments = input,
                Parameters = new Dictionary<string, string> { ["target"] = input }
            };
        }

        // Default to roleplay
        return new ParsedInput
        {
            RawInput = input,
            CommandType = CommandType.Roleplay,
            Command = "roleplay",
            Arguments = input,
            Parameters = new Dictionary<string, string> { ["action"] = input }
        };
    }

    /// <summary>
    /// Parse command parameters from arguments string
    /// </summary>
    private Dictionary<string, string> ParseParameters(string args)
    {
        var parameters = new Dictionary<string, string>();
        
        if (string.IsNullOrWhiteSpace(args))
            return parameters;

        // Simple parameter parsing - can be extended for more complex syntax
        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < parts.Length; i++)
        {
            // Check for key:value syntax
            if (parts[i].Contains(':'))
            {
                var keyValue = parts[i].Split(':', 2);
                parameters[keyValue[0].ToLower()] = keyValue[1];
            }
        }

        // Store raw args as well
        parameters["_raw"] = args;
        
        return parameters;
    }

    #endregion

    #region Command Handlers

    /// <summary>
    /// Handle /roleplay command - character action narration
    /// </summary>
    private async Task<StoryResponse> HandleRoleplayAsync(StoryContext context, ParsedInput parsed)
    {
        var action = parsed.Arguments;
        
        // Record the action
        await _narrativeService.RecordEventAsync(
            context.ChannelId,
            $"Character Action: {context.Character?.Name ?? "Unknown"}",
            action,
            NarrativeEventType.CharacterDevelopment,
            context.CurrentLocation,
            importance: 3
        );

        // Update session activity
        await _sessionService.UpdateActivityAsync(context.ChannelId);

        // Check if action triggers anything
        var triggers = await CheckForTriggersAsync(context, action);
        
        return new StoryResponse
        {
            Success = true,
            ResponseType = ResponseType.Narrative,
            Message = FormatRoleplayResponse(context, action),
            Triggers = triggers,
            RequiresFollowUp = triggers.Any()
        };
    }

    /// <summary>
    /// Handle /check command - skill check with full SR3e mechanics
    /// </summary>
    private async Task<StoryResponse> HandleSkillCheckAsync(StoryContext context, ParsedInput parsed)
    {
        var skillName = parsed.Parameters.TryGetValue("skill", out var s) ? s : parsed.Arguments;
        var skillNameLower = skillName.ToLower().Trim();

        // Find matching skill
        var skillDef = FindSkillDefinition(skillNameLower);
        if (skillDef == null)
        {
            return new StoryResponse
            {
                Success = false,
                ResponseType = ResponseType.Error,
                Message = $"Unknown skill: '{skillName}'. Use /help to see available skills."
            };
        }

        // Get character's skill rating and attribute
        var (skillRating, attributeName, attributeValue) = await GetCharacterSkillInfoAsync(context, skillNameLower, skillDef);

        // Calculate dice pool
        var poolSize = skillRating + attributeValue + context.WoundModifier;
        poolSize = Math.Max(1, poolSize); // Minimum 1 die

        // Determine target number based on difficulty
        var (targetNumber, difficultyName) = DetermineTargetNumber(context, skillNameLower);

        // Roll the dice
        var rollResult = _diceService.RollShadowrun(poolSize, targetNumber);

        // Generate narrative response
        var narrative = GenerateSkillCheckNarrative(context, skillNameLower, skillDef.Category, 
            rollResult, difficultyName, attributeName, attributeValue, skillRating);

        // Record the check
        await _narrativeService.RecordEventAsync(
            context.ChannelId,
            $"Skill Check: {skillName}",
            $"{context.Character?.Name} attempted {skillName}. Result: {rollResult.Successes} successes.",
            NarrativeEventType.StoryBeat,
            context.CurrentLocation,
            importance: 4,
            tags: $"skill,check,{skillNameLower}"
        );

        // Check for consequences
        var consequences = await DetermineSkillCheckConsequencesAsync(context, skillNameLower, rollResult);

        return new StoryResponse
        {
            Success = true,
            ResponseType = ResponseType.SkillCheck,
            Message = narrative,
            SkillCheckResult = new SkillCheckResult
            {
                SkillName = skillName,
                Category = skillDef.Category,
                PoolSize = poolSize,
                TargetNumber = targetNumber,
                Successes = rollResult.Successes,
                Glitch = rollResult.Glitch,
                CriticalGlitch = rollResult.CriticalGlitch,
                Rolls = rollResult.Rolls,
                Attribute = attributeName,
                AttributeValue = attributeValue,
                SkillRating = skillRating,
                Difficulty = difficultyName
            },
            Consequences = consequences
        };
    }

    /// <summary>
    /// Handle /investigate command - search for clues at a location
    /// </summary>
    private async Task<StoryResponse> HandleInvestigateAsync(StoryContext context, ParsedInput parsed)
    {
        var target = parsed.Arguments;
        
        // Roll perception/investigation
        var poolSize = await CalculateInvestigationPoolAsync(context);
        var rollResult = _diceService.RollShadowrun(poolSize, 4);

        // Generate clues based on success level
        var clues = await GenerateCluesAsync(context, target, rollResult.Successes);

        // Record investigation
        await _narrativeService.RecordEventAsync(
            context.ChannelId,
            "Investigation",
            $"Investigated {target}. Found: {string.Join(", ", clues)}",
            NarrativeEventType.Investigation,
            context.CurrentLocation,
            importance: 5,
            tags: "investigation,clues"
        );

        return new StoryResponse
        {
            Success = true,
            ResponseType = ResponseType.Investigation,
            Message = FormatInvestigationResponse(context, target, rollResult, clues),
            SkillCheckResult = new SkillCheckResult
            {
                SkillName = "Investigation",
                Category = SkillCategory.Knowledge,
                PoolSize = poolSize,
                TargetNumber = 4,
                Successes = rollResult.Successes,
                Rolls = rollResult.Rolls
            }
        };
    }

    /// <summary>
    /// Handle /dialogue command - talk to an NPC with specific message
    /// </summary>
    private async Task<StoryResponse> HandleDialogueAsync(StoryContext context, ParsedInput parsed)
    {
        var parts = parsed.Arguments.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var npcName = parts[0];
        var message = parts.Length > 1 ? parts[1] : "";

        return await GenerateNPCDialogueResponseAsync(context, npcName, message);
    }

    /// <summary>
    /// Handle /search command - search the current area
    /// </summary>
    private async Task<StoryResponse> HandleSearchAsync(StoryContext context, ParsedInput parsed)
    {
        var query = parsed.Arguments;
        
        // Roll perception
        var poolSize = await CalculatePerceptionPoolAsync(context);
        var rollResult = _diceService.RollShadowrun(poolSize, 4);

        // Generate search results
        var findings = await GenerateSearchResultsAsync(context, query, rollResult.Successes);

        return new StoryResponse
        {
            Success = true,
            ResponseType = ResponseType.Search,
            Message = FormatSearchResponse(context, query, rollResult, findings),
            SkillCheckResult = new SkillCheckResult
            {
                SkillName = "Perception",
                Category = SkillCategory.Knowledge,
                PoolSize = poolSize,
                TargetNumber = 4,
                Successes = rollResult.Successes,
                Rolls = rollResult.Rolls
            }
        };
    }

    /// <summary>
    /// Handle /listen command - listen for sounds
    /// </summary>
    private async Task<StoryResponse> HandleListenAsync(StoryContext context, ParsedInput parsed)
    {
        var poolSize = await CalculatePerceptionPoolAsync(context);
        var rollResult = _diceService.RollShadowrun(poolSize, 5); // Hearing is harder

        var sounds = await GenerateAuditoryCluesAsync(context, rollResult.Successes);

        return new StoryResponse
        {
            Success = true,
            ResponseType = ResponseType.Listen,
            Message = FormatListenResponse(context, rollResult, sounds),
            SkillCheckResult = new SkillCheckResult
            {
                SkillName = "Perception",
                Category = SkillCategory.Knowledge,
                PoolSize = poolSize,
                TargetNumber = 5,
                Successes = rollResult.Successes,
                Rolls = rollResult.Rolls
            }
        };
    }

    /// <summary>
    /// Handle /interact command - interact with an object
    /// </summary>
    private async Task<StoryResponse> HandleInteractAsync(StoryContext context, ParsedInput parsed)
    {
        var objectName = parsed.Arguments;

        // Determine what skill to use based on the object
        var (skillName, poolSize) = await DetermineInteractionSkillAsync(context, objectName);
        var rollResult = _diceService.RollShadowrun(poolSize, 4);

        var interactionResult = await GenerateInteractionResultAsync(context, objectName, rollResult);

        // Record the interaction
        await _narrativeService.RecordEventAsync(
            context.ChannelId,
            $"Interact: {objectName}",
            interactionResult.Description,
            NarrativeEventType.StoryBeat,
            context.CurrentLocation,
            importance: 3
        );

        return new StoryResponse
        {
            Success = true,
            ResponseType = ResponseType.Interaction,
            Message = FormatInteractResponse(context, objectName, rollResult, interactionResult),
            SkillCheckResult = new SkillCheckResult
            {
                SkillName = skillName,
                Category = SkillCategory.Physical,
                PoolSize = poolSize,
                TargetNumber = 4,
                Successes = rollResult.Successes,
                Rolls = rollResult.Rolls
            }
        };
    }

    /// <summary>
    /// Handle /use command - use an item
    /// </summary>
    private async Task<StoryResponse> HandleUseItemAsync(StoryContext context, ParsedInput parsed)
    {
        var itemName = parsed.Arguments;
        
        // Check if character has the item
        var item = context.Character?.Gear?.FirstOrDefault(g => 
            g.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) && g.IsEquipped);

        if (item == null)
        {
            return new StoryResponse
            {
                Success = false,
                ResponseType = ResponseType.Error,
                Message = $"You don't have '{itemName}' equipped or available."
            };
        }

        var result = await GenerateItemUseResultAsync(context, item);

        return new StoryResponse
        {
            Success = true,
            ResponseType = ResponseType.ItemUse,
            Message = result
        };
    }

    /// <summary>
    /// Handle /talk command - converse with NPC
    /// </summary>
    private async Task<StoryResponse> HandleTalkAsync(StoryContext context, ParsedInput parsed)
    {
        var npcName = parsed.Parameters.TryGetValue("npc", out var npc) ? npc : parsed.Arguments;
        var message = parsed.Parameters.TryGetValue("message", out var msg) ? msg : "";

        return await GenerateNPCDialogueResponseAsync(context, npcName, message);
    }

    /// <summary>
    /// Handle /describe command - get current scene description
    /// </summary>
    private async Task<StoryResponse> HandleDescribeAsync(StoryContext context, ParsedInput parsed)
    {
        var description = await GenerateSceneDescriptionAsync(context);

        return new StoryResponse
        {
            Success = true,
            ResponseType = ResponseType.Description,
            Message = description
        };
    }

    /// <summary>
    /// Handle /help command
    /// </summary>
    private Task<StoryResponse> HandleHelpAsync(StoryContext context, ParsedInput parsed)
    {
        var helpText = GenerateHelpText();
        return Task.FromResult(new StoryResponse
        {
            Success = true,
            ResponseType = ResponseType.Help,
            Message = helpText
        });
    }

    /// <summary>
    /// Handle unknown command
    /// </summary>
    private Task<StoryResponse> HandleUnknownAsync(StoryContext context, ParsedInput parsed)
    {
        return Task.FromResult(new StoryResponse
        {
            Success = false,
            ResponseType = ResponseType.Error,
            Message = $"Unknown command: '{parsed.Command}'. Use /help to see available commands."
        });
    }

    #endregion

    #region Skill Check System

    /// <summary>
    /// Find skill definition by name (with aliases)
    /// </summary>
    private SkillDefinition? FindSkillDefinition(string skillName)
    {
        // Direct match
        if (SkillDefinitions.TryGetValue(skillName, out var def))
            return def;

        // Alias matching
        var aliases = new Dictionary<string, string>
        {
            ["shoot"] = "firearms",
            ["gun"] = "firearms",
            ["melee"] = "edged weapons",
            ["sword"] = "edged weapons",
            ["fight"] = "unarmed combat",
            ["punch"] = "unarmed combat",
            ["sneak"] = "stealth",
            ["hide"] = "stealth",
            ["hack"] = "computers",
            ["deck"] = "computers",
            ["persuade"] = "negotiation",
            ["convince"] = "negotiation",
            ["spot"] = "perception",
            ["notice"] = "perception",
            ["cast"] = "sorcery",
            ["summon"] = "conjuring"
        };

        if (aliases.TryGetValue(skillName, out var actualSkill))
        {
            if (SkillDefinitions.TryGetValue(actualSkill, out def))
                return def;
        }

        // Partial match
        foreach (var kvp in SkillDefinitions)
        {
            if (kvp.Key.Contains(skillName) || skillName.Contains(kvp.Key))
                return kvp.Value;
        }

        return null;
    }

    /// <summary>
    /// Get character's skill rating and linked attribute
    /// </summary>
    private async Task<(int Rating, string AttributeName, int AttributeValue)> GetCharacterSkillInfoAsync(
        StoryContext context, string skillName, SkillDefinition skillDef)
    {
        var character = context.Character;
        
        if (character == null)
        {
            // Default values for characters without sheets
            return (0, skillDef.LinkedAttribute, 3);
        }

        // Find skill rating
        var skill = character.Skills?.FirstOrDefault(s => 
            s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase));
        var skillRating = skill?.Rating ?? 0;

        // If no skill but can default
        if (skillRating == 0 && !skillDef.Default)
        {
            return (0, skillDef.LinkedAttribute, 0);
        }

        // Get linked attribute value
        var (attrName, attrValue) = GetAttribute(character, skillDef.LinkedAttribute);

        return (skillRating, attrName, attrValue);
    }

    /// <summary>
    /// Get attribute value by name
    /// </summary>
    private (string Name, int Value) GetAttribute(ShadowrunCharacter character, string attributeName)
    {
        return attributeName.ToLower() switch
        {
            "body" => ("Body", character.Body),
            "quickness" or "agility" => ("Quickness", character.Quickness),
            "strength" => ("Strength", character.Strength),
            "charisma" => ("Charisma", character.Charisma),
            "intelligence" => ("Intelligence", character.Intelligence),
            "willpower" => ("Willpower", character.Willpower),
            "reaction" => ("Reaction", character.Reaction),
            "magic" => ("Magic", character.Magic),
            _ => ("Intelligence", character.Intelligence)
        };
    }

    /// <summary>
    /// Determine target number based on context and skill
    /// </summary>
    private (int TargetNumber, string DifficultyName) DetermineTargetNumber(StoryContext context, string skillName)
    {
        // Base difficulty modifiers
        var modifiers = new List<int> { 4 }; // Base TN 4

        // Environmental modifiers
        if (context.IsDark) modifiers.Add(2);
        if (context.IsRaining) modifiers.Add(1);
        if (context.IsNoisy) modifiers.Add(1);
        if (context.IsDistracted) modifiers.Add(2);

        // Stress modifiers
        if (context.InCombat) modifiers.Add(2);
        if (context.IsTimePressure) modifiers.Add(1);

        // Wound modifier (affects pool, not TN, but we track it)
        // Wounds are handled in pool calculation

        var totalTN = modifiers.Sum();
        totalTN = Math.Clamp(totalTN, 2, 10); // TN range is 2-10

        var difficulty = totalTN switch
        {
            <= 2 => "Trivial",
            3 => "Easy",
            4 => "Normal",
            5 => "Challenging",
            6 => "Hard",
            7 => "Difficult",
            8 => "Very Difficult",
            9 => "Extremely Hard",
            _ => "Nearly Impossible"
        };

        return (totalTN, difficulty);
    }

    /// <summary>
    /// Generate narrative description for skill check
    /// </summary>
    private string GenerateSkillCheckNarrative(
        StoryContext context,
        string skillName,
        SkillCategory category,
        ShadowrunDiceResult roll,
        string difficulty,
        string attributeName,
        int attributeValue,
        int skillRating)
    {
        var sb = new System.Text.StringBuilder();
        var characterName = context.Character?.Name ?? "You";

        // Header
        sb.AppendLine($"**{characterName}** attempts a **{skillName}** check...");
        sb.AppendLine();

        // Pool breakdown
        sb.AppendLine($"🎲 **Dice Pool:** {roll.PoolSize} dice ({attributeName} {attributeValue} + {skillName} {skillRating}");
        if (context.WoundModifier < 0)
            sb.AppendLine($"   ⚠️ Wound modifier: {context.WoundModifier}");
        sb.AppendLine($"   📊 Difficulty: {difficulty} (TN {roll.TargetNumber})");
        sb.AppendLine();

        // Roll results
        sb.AppendLine($"**Roll:** [{string.Join(", ", roll.Rolls)}]");
        sb.AppendLine($"**Result:** {roll.Successes} success{(roll.Successes != 1 ? "es" : "")}");

        // Special results
        if (roll.CriticalGlitch)
        {
            sb.AppendLine();
            sb.AppendLine("💀 **CRITICAL GLITCH!** Something goes terribly wrong!");
        }
        else if (roll.Glitch)
        {
            sb.AppendLine();
            sb.AppendLine("⚠️ **Glitch!** Something goes slightly wrong alongside the success.");
        }

        sb.AppendLine();

        // Narrative outcome
        sb.AppendLine(GenerateOutcomeNarrative(skillName, category, roll.Successes, roll.Glitch));

        return sb.ToString();
    }

    /// <summary>
    /// Generate narrative outcome based on success level
    /// </summary>
    private string GenerateOutcomeNarrative(string skillName, SkillCategory category, int successes, bool glitch)
    {
        var outcomes = category switch
        {
            SkillCategory.Combat => GetCombatOutcomes(skillName, successes, glitch),
            SkillCategory.Physical => GetPhysicalOutcomes(skillName, successes, glitch),
            SkillCategory.Social => GetSocialOutcomes(skillName, successes, glitch),
            SkillCategory.Technical => GetTechnicalOutcomes(skillName, successes, glitch),
            SkillCategory.Magic => GetMagicOutcomes(skillName, successes, glitch),
            SkillCategory.Knowledge => GetKnowledgeOutcomes(skillName, successes, glitch),
            _ => GetGenericOutcomes(successes, glitch)
        };

        return outcomes[_diceService.RollDie(outcomes.Count) - 1];
    }

    private List<string> GetCombatOutcomes(string skill, int successes, bool glitch)
    {
        return successes switch
        {
            0 when glitch => new List<string> { "You fumble badly, leaving yourself wide open!", "Disaster! Your weapon jams or you trip over your own feet." },
            0 => new List<string> { "You miss completely.", "Your attack fails to connect.", "The target easily avoids your attack." },
            1 => new List<string> { "A glancing blow - barely connects.", "You manage to hit, but without much force.", "Minimal success - just enough to count." },
            2 => new List<string> { "A solid hit! You connect well.", "Good form - your training shows.", "The attack lands cleanly." },
            3 => new List<string> { "Excellent strike! The target reels from the impact.", "A precise, powerful attack!", "You find the perfect opening and exploit it." },
            _ => new List<string> { "PERFECT STRIKE! Devastating accuracy and power!", "Masterful! A textbook example of combat excellence!", "Incredible! The attack couldn't have been better!" }
        };
    }

    private List<string> GetPhysicalOutcomes(string skill, int successes, bool glitch)
    {
        return successes switch
        {
            0 when glitch => new List<string> { "Catastrophic failure! You make significant noise or leave obvious signs.", "You stumble badly, drawing attention to yourself." },
            0 => new List<string> { "You fail to accomplish your goal.", "The task proves too difficult.", "You don't quite manage it." },
            1 => new List<string> { "Barely successful - you manage it, but just barely.", "A marginal success with some noise or delay.", "You accomplish the task, but not gracefully." },
            2 => new List<string> { "Good success - you handle it competently.", "Solid performance - no issues.", "You succeed without problems." },
            3 => new List<string> { "Excellent work! Quick and efficient.", "Impressive! You make it look easy.", "Very smooth - professional quality." },
            _ => new List<string> { "Flawless execution! Olympic-level performance!", "Perfect! You couldn't have done better!", "Outstanding! A demonstration of peak ability!" }
        };
    }

    private List<string> GetSocialOutcomes(string skill, int successes, bool glitch)
    {
        return successes switch
        {
            0 when glitch => new List<string> { "Social disaster! You offend them badly.", "You say exactly the wrong thing." },
            0 => new List<string> { "They're not buying it.", "Your attempt falls flat.", "No progress - they remain unconvinced." },
            1 => new List<string> { "Slight progress - they're somewhat receptive.", "A minor concession from them.", "You get a lukewarm response." },
            2 => new List<string> { "Good result - they're on board.", "They agree to your request.", "Successful interaction - they like your approach." },
            3 => new List<string> { "Excellent rapport! They're very impressed.", "They not only agree, they're enthusiastic!", "You charm them completely." },
            _ => new List<string> { "Masterful! They're putty in your hands!", "Legendary social success! You gain a devoted ally!", "Incredible charisma! They'll remember this for years!" }
        };
    }

    private List<string> GetTechnicalOutcomes(string skill, int successes, bool glitch)
    {
        return successes switch
        {
            0 when glitch => new List<string> { "Critical system failure! You trigger an alarm or corrupt data.", "The device sparks and smokes - you've broken something important." },
            0 => new List<string> { "The technology defeats you.", "You can't figure out how to proceed.", "Access denied." },
            1 => new List<string> { "Partial success - you get limited functionality.", "Slow progress, but you're making headway.", "Basic access achieved." },
            2 => new List<string> { "Solid technical work - everything functions.", "You bypass the security as intended.", "The system does what you need." },
            3 => new List<string> { "Expert hack! Full access achieved quickly.", "Elegant solution - very clean work.", "You've definitely done this before." },
            _ => new List<string> { "Elite decker status! Root access in seconds!", "Legendary! The system practically opens for you!", "Masterful! Security doesn't even know you were here!" }
        };
    }

    private List<string> GetMagicOutcomes(string skill, int successes, bool glitch)
    {
        return successes switch
        {
            0 when glitch => new List<string> { "Magical backlash! Drain hits hard!", "The mana spirals out of control!" },
            0 => new List<string> { "The spell fizzles.", "The magic slips away.", "No effect - the mana won't cooperate." },
            1 => new List<string> { "Minimal magical effect achieved.", "The spell works, but weakly.", "Barely enough mana to manifest." },
            2 => new List<string> { "Solid casting - the spell works as intended.", "Good control over the mana.", "The magic flows smoothly." },
            3 => new List<string> { "Powerful casting! The effect is enhanced.", "Excellent magical technique!", "The mana responds beautifully to your will." },
            _ => new List<string> { "MASTERFUL MAGIC! The spell reaches its full potential!", "Legendary casting! Astral space itself bends to your will!", "Incredible! The mana flows like water!" }
        };
    }

    private List<string> GetKnowledgeOutcomes(string skill, int successes, bool glitch)
    {
        return successes switch
        {
            0 when glitch => new List<string> { "You remember wrong - critically false information!", "Your mind goes completely blank at the worst moment." },
            0 => new List<string> { "You can't recall anything useful.", "The information escapes you.", "Nothing comes to mind." },
            1 => new List<string> { "A vague recollection - some details emerge.", "You remember the basics.", "Partial information comes to mind." },
            2 => new List<string> { "Good recall - you know what you need.", "The information is clear in your mind.", "You piece together the relevant details." },
            3 => new List<string> { "Excellent memory! Detailed knowledge surfaces.", "You recall obscure but useful details.", "Expert-level understanding demonstrated." },
            _ => new List<string> { "Perfect recall! Every detail is crystal clear!", "Encyclopedic knowledge! You could write a paper on this!", "Total mastery! The information is yours to command!" }
        };
    }

    private List<string> GetGenericOutcomes(int successes, bool glitch)
    {
        return successes switch
        {
            0 when glitch => new List<string> { "Critical failure with complications!", "Disaster strikes!" },
            0 => new List<string> { "Failure.", "The attempt doesn't work.", "No success." },
            1 => new List<string> { "Marginal success.", "Barely made it.", "Just enough." },
            2 => new List<string> { "Solid success.", "Good result.", "Well done." },
            3 => new List<string> { "Excellent success!", "Great result!", "Impressive!" },
            _ => new List<string> { "Perfect! Outstanding result!", "Incredible success!", "Flawless execution!" }
        };
    }

    #endregion

    #region Encounter Generation

    /// <summary>
    /// Generate an on-the-fly encounter based on context
    /// </summary>
    public async Task<EncounterResult> GenerateEncounterAsync(ulong channelId, EncounterTrigger trigger)
    {
        var context = await BuildStoryContextAsync(channelId, 0);

        var encounter = new EncounterResult
        {
            EncounterType = DetermineEncounterType(trigger, context),
            Difficulty = CalculateEncounterDifficulty(context),
            Description = GenerateEncounterDescription(trigger, context),
            Enemies = new List<EncounterEnemy>(),
            EnvironmentalFactors = new List<string>(),
            PotentialOutcomes = new List<string>()
        };

        // Generate enemies if combat encounter
        if (encounter.EncounterType == EncounterType.Combat)
        {
            encounter.Enemies = GenerateEncounterEnemies(context, encounter.Difficulty);
        }

        // Add environmental factors
        encounter.EnvironmentalFactors = GenerateEnvironmentalFactors(context);

        // Generate potential outcomes
        encounter.PotentialOutcomes = GeneratePotentialOutcomes(encounter);

        // Record the encounter
        await _narrativeService.RecordEventAsync(
            channelId,
            $"Encounter: {encounter.EncounterType}",
            encounter.Description,
            NarrativeEventType.Combat,
            context.CurrentLocation,
            importance: 7,
            tags: $"encounter,{encounter.EncounterType.ToString().ToLower()}"
        );

        return encounter;
    }

    /// <summary>
    /// Determine encounter type based on trigger and context
    /// </summary>
    private EncounterType DetermineEncounterType(EncounterTrigger trigger, StoryContext context)
    {
        return trigger.Type switch
        {
            "combat" => EncounterType.Combat,
            "social" => EncounterType.Social,
            "puzzle" => EncounterType.Puzzle,
            "chase" => EncounterType.Chase,
            "stealth" => EncounterType.Stealth,
            _ => _diceService.RollDie(6) switch
            {
                1 or 2 => EncounterType.Combat,
                3 or 4 => EncounterType.Social,
                5 => EncounterType.Puzzle,
                _ => EncounterType.Stealth
            }
        };
    }

    /// <summary>
    /// Calculate encounter difficulty based on party composition
    /// </summary>
    private int CalculateEncounterDifficulty(StoryContext context)
    {
        var baseDifficulty = 3;
        
        // Adjust for party size
        baseDifficulty += context.PartySize / 2;
        
        // Adjust for player skill
        if (context.AveragePartyKarma > 50) baseDifficulty += 1;
        if (context.AveragePartyKarma > 100) baseDifficulty += 1;

        return Math.Clamp(baseDifficulty, 1, 6);
    }

    /// <summary>
    /// Generate encounter description
    /// </summary>
    private string GenerateEncounterDescription(EncounterTrigger trigger, StoryContext context)
    {
        var templates = new Dictionary<EncounterType, List<string>>
        {
            [EncounterType.Combat] = new()
            {
                $"Suddenly, {trigger.Count} hostile figures emerge from the shadows!",
                $"Your instincts scream danger as enemies appear!",
                $"Combat erupts without warning!"
            },
            [EncounterType.Social] = new()
            {
                $"A group blocks your path, clearly wanting to talk.",
                $"Someone calls out to you - this could get complicated.",
                $"A tense social situation develops."
            },
            [EncounterType.Puzzle] = new()
            {
                $"You encounter a complex obstacle requiring careful thought.",
                $"A puzzle blocks your path - brute force won't work here.",
                $"Your wits are about to be tested."
            },
            [EncounterType.Chase] = new()
            {
                $"Something's coming - time to run!",
                $"Pursuit! You need to move fast!",
                $"A chase begins - don't look back!"
            },
            [EncounterType.Stealth] = new()
            {
                $"You need to get past unseen. Stealth is your only option.",
                $"Guards ahead - time to be very, very quiet.",
                $"A security patrol approaches. Hide or be caught!"
            }
        };

        var options = templates.GetValueOrDefault(context.CurrentEncounterType, templates[EncounterType.Combat]);
        return options[_diceService.RollDie(options.Count) - 1];
    }

    /// <summary>
    /// Generate enemies for combat encounter
    /// </summary>
    private List<EncounterEnemy> GenerateEncounterEnemies(StoryContext context, int difficulty)
    {
        var enemies = new List<EncounterEnemy>();
        var enemyCount = Math.Max(1, difficulty - 1 + _diceService.RollDie(3));

        var enemyTypes = new[]
        {
            ("Corporate Security", 4, 5, "Ares Predator"),
            ("Gang Member", 3, 3, "Knife"),
            ("Mercenary", 5, 6, "Assault Rifle"),
            ("Street Samurai", 6, 7, "Katana"),
            ("Combat Decker", 4, 4, "Cyberdeck"),
            ("Mage", 4, 4, "Spells"),
            ("Spirit", 5, 5, "Force"),
            ("Drone", 4, 6, "Mounted Weapon")
        };

        for (int i = 0; i < enemyCount; i++)
        {
            var (name, body, reaction, weapon) = enemyTypes[_diceService.RollDie(enemyTypes.Length) - 1];
            enemies.Add(new EncounterEnemy
            {
                Name = $"{name} #{i + 1}",
                Type = name,
                Body = body + (difficulty / 2),
                Reaction = reaction + (difficulty / 2),
                PrimaryWeapon = weapon,
                ThreatLevel = difficulty,
                IsAlive = true
            });
        }

        return enemies;
    }

    /// <summary>
    /// Generate environmental factors for encounter
    /// </summary>
    private List<string> GenerateEnvironmentalFactors(StoryContext context)
    {
        var factors = new List<string>();

        if (context.IsDark) factors.Add("Low light (-2 to visual tests)");
        if (context.IsRaining) factors.Add("Rain (-1 to ranged combat)");
        if (context.IsNoisy) factors.Add("Loud environment (-2 to hearing tests)");
        if (context.InCombat) factors.Add("Active combat zone");

        // Add random factors
        if (_diceService.RollDie(6) >= 4)
        {
            var randomFactors = new[]
            {
                "Slippery surface",
                "Limited cover available",
                "Civilians in the area",
                "Security cameras present",
                "Escape routes available",
                "Hazardous materials nearby"
            };
            factors.Add(randomFactors[_diceService.RollDie(randomFactors.Length) - 1]);
        }

        return factors;
    }

    /// <summary>
    /// Generate potential outcomes for encounter
    /// </summary>
    private List<string> GeneratePotentialOutcomes(EncounterResult encounter)
    {
        return encounter.EncounterType switch
        {
            EncounterType.Combat => new List<string>
            {
                "Defeat all enemies",
                "Force enemies to retreat",
                "Capture an enemy alive",
                "Escape without casualties"
            },
            EncounterType.Social => new List<string>
            {
                "Persuade them to help",
                "Negotiate a deal",
                "Intimidate them into leaving",
                "Gain valuable information"
            },
            EncounterType.Puzzle => new List<string>
            {
                "Solve the puzzle",
                "Find an alternative solution",
                "Bypass through force",
                "Retreat and find another way"
            },
            _ => new List<string>
            {
                "Success",
                "Partial success",
                "Failure with escape",
                "Complete failure"
            }
        };
    }

    #endregion

    #region NPC Dialogue Generation

    /// <summary>
    /// Generate NPC dialogue response
    /// </summary>
    private async Task<StoryResponse> GenerateNPCDialogueResponseAsync(
        StoryContext context, string npcName, string playerMessage)
    {
        // Get or create NPC relationship
        var relationship = await _narrativeService.GetNPCRelationshipAsync(context.ChannelId, npcName);
        var isNewNPC = relationship == null;

        if (isNewNPC)
        {
            relationship = await CreateNewNPCRelationshipAsync(context, npcName);
        }

        // Get NPC role and personality
        var npcRole = relationship.NPCRole ?? DetermineNPCRole(npcName, context);
        var personality = DetermineNPCPersonality(relationship, npcRole);

        // Calculate response attitude based on relationship and message
        var attitudeModifier = CalculateAttitudeModifier(playerMessage, relationship);

        // Generate dialogue
        var dialogue = await GenerateContextualDialogueAsync(
            context, npcName, npcRole, personality, playerMessage, relationship, attitudeModifier);

        // Update relationship
        await UpdateNPCRelationshipAfterDialogueAsync(context.ChannelId, npcName, playerMessage, dialogue, attitudeModifier);

        // Record the conversation
        await _narrativeService.RecordEventAsync(
            context.ChannelId,
            $"Dialogue: {npcName}",
            $"Player: \"{playerMessage}\" | {npcName}: \"{dialogue.Response.Substring(0, Math.Min(100, dialogue.Response.Length))}...\"",
            NarrativeEventType.Social,
            context.CurrentLocation,
            npcsInvolved: npcName,
            importance: 4
        );

        return new StoryResponse
        {
            Success = true,
            ResponseType = ResponseType.Dialogue,
            Message = FormatDialogueResponse(npcName, dialogue, relationship, isNewNPC),
            NPCDialogue = dialogue
        };
    }

    /// <summary>
    /// Create new NPC relationship
    /// </summary>
    private async Task<NPCRelationship> CreateNewNPCRelationshipAsync(StoryContext context, string npcName)
    {
        var role = DetermineNPCRole(npcName, context);
        var org = DetermineNPCOrganization(role, context);

        return await _narrativeService.UpdateNPCRelationshipAsync(
            context.ChannelId,
            npcName,
            npcRole: role,
            organization: org,
            attitudeDelta: 0,
            trustDelta: 0
        );
    }

    /// <summary>
    /// Determine NPC role based on context
    /// </summary>
    private string DetermineNPCRole(string npcName, StoryContext context)
    {
        // Check for keywords in name
        var nameLower = npcName.ToLower();

        if (nameLower.Contains("guard") || nameLower.Contains("security"))
            return "Security";
        if (nameLower.Contains("fixer") || nameLower.Contains("johnson"))
            return "Fixer";
        if (nameLower.Contains("doc") || nameLower.Contains("medic"))
            return "Street Doc";
        if (nameLower.Contains("decker"))
            return "Decker";
        if (nameLower.Contains("mage") || nameLower.Contains("shaman"))
            return "Mage";
        if (nameLower.Contains("bartender") || nameLower.Contains("waiter"))
            return "Service";

        // Random based on location
        return context.CurrentLocation?.ToLower() switch
        {
            var loc when loc.Contains("bar") || loc.Contains("club") => _diceService.RollDie(3) switch
            {
                1 => "Bartender",
                2 => "Patron",
                _ => "Bouncer"
            },
            var loc when loc.Contains("corp") || loc.Contains("office") => _diceService.RollDie(3) switch
            {
                1 => "Corporate Employee",
                2 => "Security Guard",
                _ => "Executive"
            },
            var loc when loc.Contains("street") || loc.Contains("alley") => _diceService.RollDie(3) switch
            {
                1 => "Ganger",
                2 => "Homeless",
                _ => "Street Vendor"
            },
            _ => _diceService.RollDie(4) switch
            {
                1 => "Contact",
                2 => "Informant",
                3 => "Civilian",
                _ => "Shadowrunner"
            }
        };
    }

    /// <summary>
    /// Determine NPC organization
    /// </summary>
    private string DetermineNPCOrganization(string role, StoryContext context)
    {
        if (role == "Security" || role == "Corporate Employee" || role == "Executive")
        {
            var corps = new[] { "Arasaka", "Renraku", "Saeder-Krupp", "Mitsuhama", "Ares", "Aztechnology", "NeoNET", "Wuxing" };
            return corps[_diceService.RollDie(corps.Length) - 1];
        }

        if (role == "Ganger")
        {
            var gangs = new[] { "The Ancients", "Halloweeners", "Humanis Policlub", "Seattle Spikes", "Crimson Crush" };
            return gangs[_diceService.RollDie(gangs.Length) - 1];
        }

        return "Independent";
    }

    /// <summary>
    /// Determine NPC personality based on role and relationship
    /// </summary>
    private NPCPersonality DetermineNPCPersonality(NPCRelationship? relationship, string role)
    {
        var personality = new NPCPersonality();

        if (relationship != null)
        {
            personality.TrustLevel = relationship.TrustLevel;
            personality.Attitude = relationship.Attitude;
        }

        personality.Traits = role.ToLower() switch
        {
            "fixer" => new List<string> { "cautious", "business-minded", "connected" },
            "security" => new List<string> { "alert", "suspicious", "professional" },
            "street doc" => new List<string> { "pragmatic", "knowledgeable", "discreet" },
            "decker" => new List<string> { "curious", "technical", "paranoid" },
            "mage" => new List<string> { "mysterious", "insightful", "cautious" },
            "ganger" => new List<string> { "aggressive", "territorial", "loyal" },
            _ => new List<string> { "neutral", "observant", "cautious" }
        };

        return personality;
    }

    /// <summary>
    /// Calculate attitude modifier based on player message
    /// </summary>
    private int CalculateAttitudeModifier(string message, NPCRelationship? relationship)
    {
        var modifier = 0;
        var lowerMessage = message.ToLower();

        // Positive keywords
        if (ContainsAny(lowerMessage, "please", "thank", "help", "friend", "ally", "cooperate"))
            modifier += 1;

        // Negative keywords
        if (ContainsAny(lowerMessage, "threat", "kill", "hurt", "force", "demand", "idiot"))
            modifier -= 1;

        // Very negative
        if (ContainsAny(lowerMessage, "die", "destroy", "betray", "lie"))
            modifier -= 2;

        // Adjust based on existing relationship
        if (relationship != null)
        {
            // High trust makes them more forgiving
            if (relationship.TrustLevel >= 5 && modifier < 0)
                modifier += 1;

            // Low trust makes them more suspicious
            if (relationship.TrustLevel <= 2 && modifier > 0)
                modifier -= 1;
        }

        return modifier;
    }

    /// <summary>
    /// Generate contextual dialogue
    /// </summary>
    private async Task<NPCDialogue> GenerateContextualDialogueAsync(
        StoryContext context,
        string npcName,
        string npcRole,
        NPCPersonality personality,
        string playerMessage,
        NPCRelationship? relationship,
        int attitudeModifier)
    {
        var dialogue = new NPCDialogue
        {
            NPCName = npcName,
            NPCRole = npcRole,
            Attitude = personality.Attitude + attitudeModifier
        };

        // Generate opening
        dialogue.Opening = GenerateDialogueOpening(personality, relationship, playerMessage);

        // Generate main response
        dialogue.Response = GenerateDialogueResponse(npcRole, personality, playerMessage, attitudeModifier);

        // Generate follow-up options
        dialogue.FollowUpOptions = GenerateFollowUpOptions(npcRole, personality, attitudeModifier);

        // Determine if NPC offers anything
        if (attitudeModifier > 0 || (relationship?.TrustLevel ?? 0) >= 3)
        {
            dialogue.Offer = GenerateNPCOffer(npcRole, context);
        }

        // Check for warnings or threats
        if (attitudeModifier < 0)
        {
            dialogue.Warning = GenerateNPCWarning(npcRole, personality);
        }

        return dialogue;
    }

    /// <summary>
    /// Generate dialogue opening based on relationship
    /// </summary>
    private string GenerateDialogueOpening(NPCPersonality personality, NPCRelationship? relationship, string playerMessage)
    {
        if (relationship == null || relationship.TrustLevel == 0)
        {
            return personality.Attitude switch
            {
                >= 5 => "*nods in recognition*",
                >= 1 => "*looks up with mild interest*",
                0 => "*regards you carefully*",
                >= -3 => "*looks suspicious*",
                _ => "*glowers*"
            };
        }

        return relationship.Attitude switch
        {
            >= 7 => $"*smiles warmly* Good to see you again.",
            >= 4 => "*nods* Back again?",
            >= 1 => "*acknowledges you*",
            0 => "*watches you carefully*",
            >= -3 => "*looks annoyed* You again?",
            _ => "*scowls* What do you want?"
        };
    }

    /// <summary>
    /// Generate main dialogue response
    /// </summary>
    private string GenerateDialogueResponse(string role, NPCPersonality personality, string playerMessage, int attitudeMod)
    {
        var responses = GetRoleBasedResponses(role, personality, attitudeMod);
        return responses[_diceService.RollDie(responses.Count) - 1];
    }

    private List<string> GetRoleBasedResponses(string role, NPCPersonality personality, int attitudeMod)
    {
        var baseResponses = new Dictionary<string, List<string>>
        {
            ["fixer"] = new()
            {
                "Information costs, chummer. What's your budget?",
                "I might know something. Depends on what you're offering.",
                "In this business, trust is earned. What do you need?",
                "Jobs are available if you're interested. Nothing too hot... yet.",
                "I've got contacts who might help. But they'll want something in return."
            },
            ["security"] = new()
            {
                "This area is restricted. State your business.",
                "Identification, please. Now.",
                "Move along. Nothing to see here.",
                "I don't recognize you. Who authorized your presence?",
                "Suspicious activity detected. Explain yourself."
            },
            ["street doc"] = new()
            {
                "You look like you've seen better days. What happened?",
                "Medical services available. Payment upfront.",
                "I've seen worse. Barely. What do you need?",
                "Cyberware acting up? I can take a look.",
                "Quiet about your business, quiet about mine. Deal?"
            },
            ["decker"] = new()
            {
                "Interesting... the Matrix has been... unusual lately.",
                "Looking for data? Everyone is. Specifics matter.",
                "Hosts are getting smarter. ICE is getting meaner. Be careful.",
                "I might be able to help... for the right price.",
                "That's a dangerous question. Who wants to know?"
            },
            ["mage"] = new()
            {
                "The astral speaks of... interesting times ahead.",
                "Magic has its price. Always.",
                "I sense... potential in you. Or danger. Hard to tell.",
                "Spirits are restless. Something's coming.",
                "Your aura is... unusual. What are you hiding?"
            },
            ["default"] = new()
            {
                "What can I do for you?",
                "I'm listening.",
                "Go on...",
                "Interesting. Tell me more.",
                "I'm not sure I can help with that.",
                "That's a dangerous topic around here."
            }
        };

        var responses = baseResponses.GetValueOrDefault(role.ToLower(), baseResponses["default"]);

        // Modify based on attitude
        if (attitudeMod < 0)
        {
            responses.AddRange(new[]
            {
                "I don't like your tone.",
                "Watch yourself.",
                "That's not going to work on me.",
                "You're pushing your luck."
            });
        }
        else if (attitudeMod > 0)
        {
            responses.AddRange(new[]
            {
                "For you, I might be able to do something.",
                "Let me see what I can do.",
                "I appreciate the respect.",
                "Between friends, I can be more helpful."
            });
        }

        return responses;
    }

    /// <summary>
    /// Generate follow-up dialogue options
    /// </summary>
    private List<string> GenerateFollowUpOptions(string role, NPCPersonality personality, int attitudeMod)
    {
        var options = new List<string>();

        if (attitudeMod >= 0)
        {
            options.AddRange(new[]
            {
                "Ask for more information",
                "Offer payment for help",
                "Ask about current events",
                "Request an introduction"
            });
        }

        if (attitudeMod >= 1 || personality.TrustLevel >= 3)
        {
            options.AddRange(new[]
            {
                "Ask about sensitive topics",
                "Request a favor",
                "Share information"
            });
        }

        if (attitudeMod < 0)
        {
            options.AddRange(new[]
            {
                "Apologize and try again",
                "Offer a peace offering",
                "Leave before things get worse"
            });
        }

        return options.Take(4).ToList();
    }

    /// <summary>
    /// Generate NPC offer
    /// </summary>
    private string? GenerateNPCOffer(string role, StoryContext context)
    {
        if (_diceService.RollDie(6) < 4) return null; // 50% chance

        return role.ToLower() switch
        {
            "fixer" => "I've got a job that might interest you. Pays well.",
            "street doc" => "I've got some surplus medical supplies. Interested?",
            "decker" => "I found some interesting data. Might be worth something to you.",
            "mage" => "I could teach you something... for the right compensation.",
            _ => "I might be able to help you, for a price."
        };
    }

    /// <summary>
    /// Generate NPC warning
    /// </summary>
    private string? GenerateNPCWarning(string role, NPCPersonality personality)
    {
        if (personality.Attitude >= 0) return null;

        return role.ToLower() switch
        {
            "security" => "One more step and I'll call backup.",
            "ganger" => "You're on our turf. Tread carefully.",
            "fixer" => "I've got friends who don't like troublemakers.",
            _ => "I'd watch my back if I were you."
        };
    }

    /// <summary>
    /// Update NPC relationship after dialogue
    /// </summary>
    private async Task UpdateNPCRelationshipAfterDialogueAsync(
        ulong channelId, string npcName, string playerMessage, NPCDialogue dialogue, int attitudeModifier)
    {
        await _narrativeService.UpdateNPCRelationshipAsync(
            channelId,
            npcName,
            attitudeDelta: attitudeModifier,
            trustDelta: attitudeModifier > 0 ? 1 : (attitudeModifier < -1 ? -1 : 0),
            interaction: $"Player: \"{playerMessage.Truncate(50)}\" | Response: \"{dialogue.Response.Truncate(50)}\""
        );
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Build story context for the current session
    /// </summary>
    private async Task<StoryContext> BuildStoryContextAsync(ulong channelId, ulong userId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        var participants = session != null 
            ? await _sessionService.GetActiveParticipantsAsync(channelId) 
            : new List<SessionParticipant>();

        var participant = participants.FirstOrDefault(p => p.DiscordUserId == userId);
        var character = participant?.Character;

        var recentEvents = session != null
            ? await _narrativeService.GetRecentEventsAsync(channelId, 5)
            : new List<NarrativeEvent>();

        return new StoryContext
        {
            ChannelId = channelId,
            UserId = userId,
            SessionId = session?.Id ?? 0,
            Character = character,
            CurrentLocation = session?.CurrentLocation ?? "Unknown",
            InGameDateTime = session?.InGameDateTime,
            WoundModifier = character?.GetWoundModifier() ?? 0,
            PartySize = participants.Count,
            AveragePartyKarma = participants.Any() ? participants.Average(p => p.SessionKarma) : 0,
            RecentEvents = recentEvents,
            InCombat = recentEvents.Any(e => e.EventType == NarrativeEventType.Combat),
            IsDark = false, // Would be set based on time/location
            IsRaining = false, // Would be set based on weather
            IsNoisy = false,
            IsDistracted = false,
            IsTimePressure = false
        };
    }

    /// <summary>
    /// Check for triggers in player action
    /// </summary>
    private async Task<List<StoryTrigger>> CheckForTriggersAsync(StoryContext context, string action)
    {
        var triggers = new List<StoryTrigger>();
        var actionLower = action.ToLower();

        // Check for combat triggers
        if (ContainsAny(actionLower, "attack", "shoot", "fight", "kill", "punch"))
        {
            triggers.Add(new StoryTrigger
            {
                Type = "combat",
                Description = "Combat initiated",
                Priority = 10
            });
        }

        // Check for alarm triggers
        if (ContainsAny(actionLower, "loud", "explosion", "scream", "alarm"))
        {
            triggers.Add(new StoryTrigger
            {
                Type = "alarm",
                Description = "Noise may attract attention",
                Priority = 8
            });
        }

        // Check for skill check opportunities
        if (ContainsAny(actionLower, "try", "attempt", "check"))
        {
            triggers.Add(new StoryTrigger
            {
                Type = "skillcheck",
                Description = "Opportunity for skill check",
                Priority = 5
            });
        }

        return triggers;
    }

    /// <summary>
    /// Calculate investigation pool
    /// </summary>
    private async Task<int> CalculateInvestigationPoolAsync(StoryContext context)
    {
        var character = context.Character;
        if (character == null) return 6; // Default

        var investigation = character.Skills?.FirstOrDefault(s => 
            s.SkillName.Equals("investigation", StringComparison.OrdinalIgnoreCase))?.Rating ?? 0;
        var perception = character.Skills?.FirstOrDefault(s => 
            s.SkillName.Equals("perception", StringComparison.OrdinalIgnoreCase))?.Rating ?? 0;

        var pool = Math.Max(investigation, perception) + character.Intelligence + context.WoundModifier;
        return Math.Max(1, pool);
    }

    /// <summary>
    /// Calculate perception pool
    /// </summary>
    private async Task<int> CalculatePerceptionPoolAsync(StoryContext context)
    {
        var character = context.Character;
        if (character == null) return 6;

        var perception = character.Skills?.FirstOrDefault(s => 
            s.SkillName.Equals("perception", StringComparison.OrdinalIgnoreCase))?.Rating ?? 0;

        var pool = perception + character.Intelligence + context.WoundModifier;
        return Math.Max(1, pool);
    }

    /// <summary>
    /// Generate clues based on success level
    /// </summary>
    private async Task<List<string>> GenerateCluesAsync(StoryContext context, string target, int successes)
    {
        var clues = new List<string>();

        if (successes == 0)
        {
            clues.Add("You find nothing of interest.");
            return clues;
        }

        // Basic clue
        clues.Add($"You notice something about {target}.");

        if (successes >= 2)
            clues.Add("You spot additional details others might miss.");

        if (successes >= 3)
            clues.Add("You discover a hidden detail!");

        if (successes >= 4)
            clues.Add("You find something that changes your understanding of the situation.");

        return clues;
    }

    /// <summary>
    /// Generate search results
    /// </summary>
    private async Task<List<string>> GenerateSearchResultsAsync(StoryContext context, string query, int successes)
    {
        var results = new List<string>();

        if (successes == 0)
        {
            results.Add("Your search turns up empty.");
            return results;
        }

        results.Add($"You find something related to '{query}'.");

        if (successes >= 2)
            results.Add("Your thorough search reveals a secondary item.");

        if (successes >= 3)
            results.Add("Hidden compartment or secret discovered!");

        return results;
    }

    /// <summary>
    /// Generate auditory clues
    /// </summary>
    private async Task<List<string>> GenerateAuditoryCluesAsync(StoryContext context, int successes)
    {
        var sounds = new List<string>();

        if (successes == 0)
        {
            sounds.Add("You hear nothing unusual.");
            return sounds;
        }

        var ambientSounds = new[]
        {
            "distant traffic",
            "the hum of electronics",
            "footsteps somewhere nearby",
            "the whir of ventilation",
            "muffled voices",
            "something dripping"
        };

        sounds.Add($"You hear {ambientSounds[_diceService.RollDie(ambientSounds.Length) - 1]}.");

        if (successes >= 2)
            sounds.Add("You pick up on a rhythm or pattern in the sounds.");

        if (successes >= 3)
            sounds.Add("You hear something significant that others might miss!");

        return sounds;
    }

    /// <summary>
    /// Determine interaction skill
    /// </summary>
    private async Task<(string SkillName, int PoolSize)> DetermineInteractionSkillAsync(StoryContext context, string objectName)
    {
        var objectLower = objectName.ToLower();

        if (ContainsAny(objectLower, "lock", "door", "panel", "terminal"))
            return ("Electronics", await CalculateSkillPoolAsync(context, "electronics"));

        if (ContainsAny(objectLower, "computer", "deck", "matrix", "host"))
            return ("Computers", await CalculateSkillPoolAsync(context, "computers"));

        if (ContainsAny(objectLower, "lift", "move", "push", "pull"))
            return ("Athletics", await CalculateSkillPoolAsync(context, "athletics"));

        return ("Perception", await CalculatePerceptionPoolAsync(context));
    }

    /// <summary>
    /// Calculate skill pool
    /// </summary>
    private async Task<int> CalculateSkillPoolAsync(StoryContext context, string skillName)
    {
        var character = context.Character;
        if (character == null) return 6;

        var skill = character.Skills?.FirstOrDefault(s => 
            s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase));

        var skillRating = skill?.Rating ?? 0;
        var skillDef = FindSkillDefinition(skillName);
        var (_, attrValue) = skillDef != null ? GetAttribute(character, skillDef.LinkedAttribute) : ("Intelligence", 3);

        return Math.Max(1, skillRating + attrValue + context.WoundModifier);
    }

    /// <summary>
    /// Generate interaction result
    /// </summary>
    private async Task<InteractionResult> GenerateInteractionResultAsync(StoryContext context, string objectName, ShadowrunDiceResult roll)
    {
        return new InteractionResult
        {
            ObjectName = objectName,
            Success = roll.Successes > 0,
            Description = roll.Successes > 0
                ? $"Successfully interacted with {objectName}."
                : $"Failed to interact with {objectName}.",
            AdditionalInfo = roll.Successes >= 3 ? "Discovered something unexpected!" : null
        };
    }

    /// <summary>
    /// Generate item use result
    /// </summary>
    private async Task<string> GenerateItemUseResultAsync(StoryContext context, CharacterGear item)
    {
        return $"You use the {item.Name}.";
    }

    /// <summary>
    /// Generate scene description
    /// </summary>
    private async Task<string> GenerateSceneDescriptionAsync(StoryContext context)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"📍 **Location:** {context.CurrentLocation}");
        sb.AppendLine();

        if (context.InGameDateTime.HasValue)
        {
            sb.AppendLine($"🕐 **Time:** {context.InGameDateTime:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
        }

        sb.AppendLine("**Current Situation:**");
        
        if (context.InCombat)
            sb.AppendLine("⚔️ Combat is ongoing!");
        else
            sb.AppendLine("The area is relatively quiet for now.");

        sb.AppendLine();
        sb.AppendLine("**Notable Elements:**");
        sb.AppendLine("• Various environmental details");
        sb.AppendLine("• NPCs in the area");
        sb.AppendLine("• Points of interest");

        if (context.RecentEvents.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**Recent Events:**");
            foreach (var evt in context.RecentEvents.Take(3))
            {
                sb.AppendLine($"• {evt.Title}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determine skill check consequences
    /// </summary>
    private async Task<List<StoryConsequence>> DetermineSkillCheckConsequencesAsync(
        StoryContext context, string skillName, ShadowrunDiceResult roll)
    {
        var consequences = new List<StoryConsequence>();

        if (roll.CriticalGlitch)
        {
            consequences.Add(new StoryConsequence
            {
                Type = ConsequenceType.Complication,
                Severity = 5,
                Description = "Critical glitch creates a significant complication!"
            });
        }
        else if (roll.Successes >= 4)
        {
            consequences.Add(new StoryConsequence
            {
                Type = ConsequenceType.Opportunity,
                Severity = 1,
                Description = "Exceptional success creates an opportunity!"
            });
        }

        return consequences;
    }

    /// <summary>
    /// Generate help text
    /// </summary>
    private string GenerateHelpText()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("**Shadowrun Interactive Story Commands**");
        sb.AppendLine();
        sb.AppendLine("**Roleplay & Actions:**");
        sb.AppendLine("• `/roleplay [text]` - Describe your character's actions");
        sb.AppendLine("• `/describe` - Get current scene description");
        sb.AppendLine();
        sb.AppendLine("**Skill Checks:**");
        sb.AppendLine("• `/check [skill]` - Make a skill check");
        sb.AppendLine("• Example: `/check stealth` or `/check firearms`");
        sb.AppendLine();
        sb.AppendLine("**Investigation:**");
        sb.AppendLine("• `/search` - Search the current area");
        sb.AppendLine("• `/investigate [target]` - Examine something specific");
        sb.AppendLine("• `/listen` - Listen for sounds");
        sb.AppendLine();
        sb.AppendLine("**Interaction:**");
        sb.AppendLine("• `/interact [object]` - Interact with something");
        sb.AppendLine("• `/use [item]` - Use an equipped item");
        sb.AppendLine();
        sb.AppendLine("**NPC Dialogue:**");
        sb.AppendLine("• `/talk [npc]` - Start a conversation");
        sb.AppendLine("• `/dialogue [npc] [message]` - Say something specific to an NPC");
        sb.AppendLine();
        sb.AppendLine("**Available Skills:**");
        sb.AppendLine("Combat: firearms, edged weapons, clubs, unarmed combat");
        sb.AppendLine("Physical: athletics, stealth, driving");
        sb.AppendLine("Social: etiquette, negotiation, leadership, con");
        sb.AppendLine("Technical: computers, electronics, biotech");
        sb.AppendLine("Magic: sorcery, conjuring");
        sb.AppendLine("Knowledge: investigation, perception, lore");

        return sb.ToString();
    }

    /// <summary>
    /// Format roleplay response
    /// </summary>
    private string FormatRoleplayResponse(StoryContext context, string action)
    {
        var characterName = context.Character?.Name ?? "Unknown Runner";
        return $"**{characterName}** {action}";
    }

    /// <summary>
    /// Format investigation response
    /// </summary>
    private string FormatInvestigationResponse(StoryContext context, string target, ShadowrunDiceResult roll, List<string> clues)
    {
        var sb = new System.Text.StringBuilder();
        var characterName = context.Character?.Name ?? "You";

        sb.AppendLine($"**{characterName}** investigates {target}...");
        sb.AppendLine($"🎲 Roll: [{string.Join(", ", roll.Rolls)}] → {roll.Successes} successes");
        sb.AppendLine();
        sb.AppendLine("**Findings:**");
        foreach (var clue in clues)
        {
            sb.AppendLine($"• {clue}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format search response
    /// </summary>
    private string FormatSearchResponse(StoryContext context, string query, ShadowrunDiceResult roll, List<string> findings)
    {
        var sb = new System.Text.StringBuilder();
        var characterName = context.Character?.Name ?? "You";

        sb.AppendLine($"**{characterName}** searches the area...");
        sb.AppendLine($"🎲 Roll: [{string.Join(", ", roll.Rolls)}] → {roll.Successes} successes");
        sb.AppendLine();
        sb.AppendLine("**Results:**");
        foreach (var finding in findings)
        {
            sb.AppendLine($"• {finding}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format listen response
    /// </summary>
    private string FormatListenResponse(StoryContext context, ShadowrunDiceResult roll, List<string> sounds)
    {
        var sb = new System.Text.StringBuilder();
        var characterName = context.Character?.Name ?? "You";

        sb.AppendLine($"**{characterName}** listens carefully...");
        sb.AppendLine($"🎲 Roll: [{string.Join(", ", roll.Rolls)}] → {roll.Successes} successes");
        sb.AppendLine();
        sb.AppendLine("**You hear:**");
        foreach (var sound in sounds)
        {
            sb.AppendLine($"• {sound}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format interact response
    /// </summary>
    private string FormatInteractResponse(StoryContext context, string objectName, ShadowrunDiceResult roll, InteractionResult result)
    {
        var sb = new System.Text.StringBuilder();
        var characterName = context.Character?.Name ?? "You";

        sb.AppendLine($"**{characterName}** interacts with {objectName}...");
        sb.AppendLine($"🎲 Roll: [{string.Join(", ", roll.Rolls)}] → {roll.Successes} successes");
        sb.AppendLine();
        sb.AppendLine(result.Description);

        if (!string.IsNullOrEmpty(result.AdditionalInfo))
        {
            sb.AppendLine($"✨ {result.AdditionalInfo}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format dialogue response
    /// </summary>
    private string FormatDialogueResponse(string npcName, NPCDialogue dialogue, NPCRelationship? relationship, bool isNewNPC)
    {
        var sb = new System.Text.StringBuilder();

        // NPC header
        sb.AppendLine($"👤 **{npcName}** ({dialogue.NPCRole})");
        
        if (relationship != null)
        {
            var attitudeEmoji = relationship.Attitude switch
            {
                >= 7 => "💚",
                >= 4 => "😊",
                >= 1 => "😐",
                0 => "🤔",
                >= -3 => "😒",
                _ => "😠"
            };
            sb.AppendLine($"{attitudeEmoji} Attitude: {relationship.Attitude}/10 | Trust: {relationship.TrustLevel}/10");
        }
        else if (isNewNPC)
        {
            sb.AppendLine("🆕 First meeting");
        }
        
        sb.AppendLine();

        // Opening
        if (!string.IsNullOrEmpty(dialogue.Opening))
        {
            sb.AppendLine(dialogue.Opening);
            sb.AppendLine();
        }

        // Main response
        sb.AppendLine(dialogue.Response);

        // Offer
        if (!string.IsNullOrEmpty(dialogue.Offer))
        {
            sb.AppendLine();
            sb.AppendLine($"💬 *{dialogue.Offer}*");
        }

        // Warning
        if (!string.IsNullOrEmpty(dialogue.Warning))
        {
            sb.AppendLine();
            sb.AppendLine($"⚠️ *{dialogue.Warning}*");
        }

        // Follow-up options
        if (dialogue.FollowUpOptions.Any())
        {
            sb.AppendLine();
            sb.AppendLine("**Options:**");
            for (int i = 0; i < dialogue.FollowUpOptions.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {dialogue.FollowUpOptions[i]}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Check if string contains any of the keywords
    /// </summary>
    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extract skill name from natural language
    /// </summary>
    private string ExtractSkillName(string input)
    {
        var skillKeywords = new Dictionary<string, string>
        {
            ["shoot"] = "firearms", ["gun"] = "firearms", ["firearm"] = "firearms",
            ["sneak"] = "stealth", ["hide"] = "stealth",
            ["persuade"] = "negotiation", ["convince"] = "negotiation",
            ["spot"] = "perception", ["notice"] = "perception",
            ["hack"] = "computers", ["computer"] = "computers",
            ["fight"] = "unarmed combat", ["punch"] = "unarmed combat",
            ["cast"] = "sorcery", ["spell"] = "sorcery"
        };

        foreach (var kvp in skillKeywords)
        {
            if (input.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return "perception"; // Default
    }

    /// <summary>
    /// Extract NPC name from natural language
    /// </summary>
    private string ExtractNPCName(string input)
    {
        var patterns = new[] { "talk to", "speak with", "ask", "tell" };
        foreach (var pattern in patterns)
        {
            var index = input.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var remainder = input.Substring(index + pattern.Length).Trim();
                var words = remainder.Split(' ');
                if (words.Length > 0)
                    return words[0];
            }
        }
        return "stranger";
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Story context for command processing
/// </summary>
public class StoryContext
{
    public ulong ChannelId { get; set; }
    public ulong UserId { get; set; }
    public int SessionId { get; set; }
    public ShadowrunCharacter? Character { get; set; }
    public string CurrentLocation { get; set; } = "Unknown";
    public DateTime? InGameDateTime { get; set; }
    public int WoundModifier { get; set; }
    public int PartySize { get; set; }
    public double AveragePartyKarma { get; set; }
    public List<NarrativeEvent> RecentEvents { get; set; } = new();
    public bool InCombat { get; set; }
    public bool IsDark { get; set; }
    public bool IsRaining { get; set; }
    public bool IsNoisy { get; set; }
    public bool IsDistracted { get; set; }
    public bool IsTimePressure { get; set; }
    public EncounterType CurrentEncounterType { get; set; }
}

/// <summary>
/// Parsed player input
/// </summary>
public class ParsedInput
{
    public string RawInput { get; set; } = string.Empty;
    public CommandType CommandType { get; set; }
    public string Command { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// Command types
/// </summary>
public enum CommandType
{
    Unknown,
    Roleplay,
    SkillCheck,
    Investigate,
    Dialogue,
    Search,
    Listen,
    Interact,
    UseItem,
    Talk,
    Describe,
    Help
}

/// <summary>
/// Response types
/// </summary>
public enum ResponseType
{
    Narrative,
    SkillCheck,
    Investigation,
    Search,
    Listen,
    Interaction,
    ItemUse,
    Dialogue,
    Description,
    Help,
    Error
}

/// <summary>
/// Story response
/// </summary>
public class StoryResponse
{
    public bool Success { get; set; }
    public ResponseType ResponseType { get; set; }
    public string Message { get; set; } = string.Empty;
    public SkillCheckResult? SkillCheckResult { get; set; }
    public NPCDialogue? NPCDialogue { get; set; }
    public List<StoryTrigger> Triggers { get; set; } = new();
    public List<StoryConsequence> Consequences { get; set; } = new();
    public bool RequiresFollowUp { get; set; }
}

/// <summary>
/// Skill check result
/// </summary>
public class SkillCheckResult
{
    public string SkillName { get; set; } = string.Empty;
    public SkillCategory Category { get; set; }
    public int PoolSize { get; set; }
    public int TargetNumber { get; set; }
    public int Successes { get; set; }
    public bool Glitch { get; set; }
    public bool CriticalGlitch { get; set; }
    public int[] Rolls { get; set; } = Array.Empty<int>();
    public string Attribute { get; set; } = string.Empty;
    public int AttributeValue { get; set; }
    public int SkillRating { get; set; }
    public string Difficulty { get; set; } = "Normal";
}

/// <summary>
/// Skill categories
/// </summary>
public enum SkillCategory
{
    Combat,
    Physical,
    Social,
    Technical,
    Magic,
    Knowledge
}

/// <summary>
/// Skill definition
/// </summary>
public class SkillDefinition
{
    public SkillCategory Category { get; set; }
    public string LinkedAttribute { get; set; } = "Intelligence";
    public bool Default { get; set; } = true;
}

/// <summary>
/// Story trigger
/// </summary>
public class StoryTrigger
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
}

/// <summary>
/// Story consequence
/// </summary>
public class StoryConsequence
{
    public ConsequenceType Type { get; set; }
    public int Severity { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// NPC dialogue result
/// </summary>
public class NPCDialogue
{
    public string NPCName { get; set; } = string.Empty;
    public string NPCRole { get; set; } = string.Empty;
    public int Attitude { get; set; }
    public string Opening { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public List<string> FollowUpOptions { get; set; } = new();
    public string? Offer { get; set; }
    public string? Warning { get; set; }
}

/// <summary>
/// NPC personality
/// </summary>
public class NPCPersonality
{
    public int TrustLevel { get; set; }
    public int Attitude { get; set; }
    public List<string> Traits { get; set; } = new();
}

/// <summary>
/// Interaction result
/// </summary>
public class InteractionResult
{
    public string ObjectName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AdditionalInfo { get; set; }
}

/// <summary>
/// Encounter types
/// </summary>
public enum EncounterType
{
    Combat,
    Social,
    Puzzle,
    Chase,
    Stealth
}

/// <summary>
/// Encounter trigger
/// </summary>
public class EncounterTrigger
{
    public string Type { get; set; } = "random";
    public int Count { get; set; } = 1;
    public string? Context { get; set; }
}

/// <summary>
/// Encounter result
/// </summary>
public class EncounterResult
{
    public EncounterType EncounterType { get; set; }
    public int Difficulty { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<EncounterEnemy> Enemies { get; set; } = new();
    public List<string> EnvironmentalFactors { get; set; } = new();
    public List<string> PotentialOutcomes { get; set; } = new();
}

/// <summary>
/// Encounter enemy
/// </summary>
public class EncounterEnemy
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Body { get; set; }
    public int Reaction { get; set; }
    public string PrimaryWeapon { get; set; } = string.Empty;
    public int ThreatLevel { get; set; }
    public bool IsAlive { get; set; }
}

/// <summary>
/// String extension for truncation
/// </summary>
public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}

#endregion
