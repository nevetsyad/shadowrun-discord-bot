using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Main service for autonomous mission generation and execution in Shadowrun campaigns.
/// Implements dynamic mission generation, branching narratives, and context-aware NPC generation.
/// </summary>
public class AutonomousMissionService
{
    private readonly DatabaseService _database;
    private readonly GameSessionService _sessionService;
    private readonly NarrativeContextService _narrativeService;
    private readonly GMService _gmService;
    private readonly DiceService _diceService;
    private readonly ILogger<AutonomousMissionService> _logger;

    // Mission generation weights and configurations
    private static readonly Dictionary<string, MissionTypeConfig> MissionConfigs = new()
    {
        ["cyberdeck"] = new() { Complexity = 3, CombatChance = 0.3f, SocialChance = 0.2f, MatrixChance = 1.0f },
        ["assassination"] = new() { Complexity = 4, CombatChance = 0.9f, SocialChance = 0.1f, StealthChance = 0.6f },
        ["extraction"] = new() { Complexity = 3, CombatChance = 0.5f, SocialChance = 0.3f, StealthChance = 0.7f },
        ["theft"] = new() { Complexity = 3, CombatChance = 0.3f, SocialChance = 0.2f, StealthChance = 0.9f },
        ["investigation"] = new() { Complexity = 2, CombatChance = 0.2f, SocialChance = 0.8f, InvestigationChance = 1.0f },
        ["delivery"] = new() { Complexity = 2, CombatChance = 0.4f, SocialChance = 0.3f, StealthChance = 0.4f },
        ["recovery"] = new() { Complexity = 3, CombatChance = 0.5f, SocialChance = 0.4f, InvestigationChance = 0.7f },
        ["sabotage"] = new() { Complexity = 4, CombatChance = 0.6f, SocialChance = 0.2f, StealthChance = 0.8f }
    };

    public AutonomousMissionService(
        DatabaseService database,
        GameSessionService sessionService,
        NarrativeContextService narrativeService,
        GMService gmService,
        DiceService diceService,
        ILogger<AutonomousMissionService> logger)
    {
        _database = database;
        _sessionService = sessionService;
        _narrativeService = narrativeService;
        _gmService = gmService;
        _diceService = diceService;
        _logger = logger;
    }

    #region Dynamic Mission Generation

    /// <summary>
    /// Generate a dynamic mission based on player context, previous choices, and world state.
    /// Not template-based - procedurally combines elements into unique missions.
    /// </summary>
    public async Task<Models.MissionDefinition> GenerateMissionAsync(
        ulong channelId,
        string missionType,
        MissionGenerationContext? context = null)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session found");

        context ??= await BuildGenerationContextAsync(channelId);

        _logger.LogInformation("Generating {MissionType} mission for session {SessionId} with context: {Context}",
            missionType, session.Id, context);

        // Get mission configuration
        if (!MissionConfigs.TryGetValue(missionType, out var config))
            config = MissionConfigs["investigation"]; // Default fallback

        // Calculate complexity first (needed for reward calculation)
        var complexity = CalculateComplexity(config, context);

        // Generate mission components procedurally
        var johnson = await GenerateJohnsonAsync(session.Id, missionType, context);
        var objective = GenerateObjective(missionType, config, context);
        var locations = GenerateMissionLocations(missionType, context);
        var obstacles = GenerateObstacles(missionType, config, context);
        var npcs = await GenerateMissionNPCsAsync(session.Id, missionType, context);
        var reward = CalculateReward(complexity, context);
        var intel = GenerateIntelOpportunities(missionType, context);
        var decisions = GenerateDecisionPoints(missionType, complexity, context);
        var twist = ShouldGenerateTwist(context) ? GeneratePlotTwist(missionType, context) : null;
        var deadline = GenerateDeadline(complexity, context);

        // Create persistent mission definition
        var mission = new Models.MissionDefinition
        {
            MissionType = missionType,
            Complexity = complexity,
            GeneratedAt = DateTime.UtcNow,
            GameSessionId = session.Id,
            Status = MissionStatus.Planning,
            CurrentStage = (int)Models.MissionStage.Planning,
            Deadline = deadline
        };

        // Serialize all components
        mission.SerializeData(johnson, objective, locations, obstacles, npcs, reward, twist, decisions, intel);

        // Persist to database
        await _database.AddMissionDefinitionAsync(mission);

        _logger.LogInformation("Generated mission: {MissionName} with {ObstacleCount} obstacles, {NPCCount} NPCs, {DecisionCount} decision points",
            objective.Title, obstacles.Count, npcs.Count, decisions.Count);

        return mission;
    }

    /// <summary>
    /// Build generation context from session state, player characters, and narrative history
    /// </summary>
    private async Task<MissionGenerationContext> BuildGenerationContextAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var recentEvents = await _narrativeService.GetRecentEventsAsync(channelId, 10);
        var npcRelationships = await _narrativeService.GetAllNPCRelationshipsAsync(channelId);
        var activeMissions = await _narrativeService.GetActiveMissionsAsync(channelId);
        var participants = await _sessionService.GetActiveParticipantsAsync(channelId);

        var context = new MissionGenerationContext
        {
            SessionId = session.Id,
            CurrentLocation = session.CurrentLocation,
            InGameDateTime = session.InGameDateTime ?? DateTime.Now,
            RecentNarrativeEvents = recentEvents,
            ExistingNPCs = npcRelationships,
            ActiveMissions = activeMissions,
            ParticipantCount = participants.Count
        };

        // Extract player character information
        foreach (var participant in participants.Where(p => p.Character != null))
        {
            context.PlayerCharacters.Add(new PlayerCharacterSummary
            {
                CharacterId = participant.CharacterId ?? 0,
                Name = participant.Character?.Name ?? "Unknown",
                PrimaryRole = DetermineCharacterRole(participant.Character),
                TotalKarma = participant.SessionKarma,
                TotalNuyen = participant.SessionNuyen
            });
        }

        // Analyze recent events for themes
        context.RecentThemes = AnalyzeNarrativeThemes(recentEvents);

        // Determine faction standing from NPCs
        context.FactionStandings = CalculateFactionStandings(npcRelationships);

        return context;
    }

    /// <summary>
    /// Generate the Johnson (mission giver) with context-aware personality and motivations
    /// </summary>
    private async Task<Models.MissionJohnson> GenerateJohnsonAsync(int sessionId, string missionType, MissionGenerationContext context)
    {
        // Check if there's an existing fixer/Johnson relationship to use
        var existingFixer = context.ExistingNPCs
            .FirstOrDefault(npc => npc.NPCRole?.Contains("fixer", StringComparison.OrdinalIgnoreCase) == true ||
                                   npc.NPCRole?.Contains("johnson", StringComparison.OrdinalIgnoreCase) == true);

        if (existingFixer != null && _diceService.RollDie(100) <= 60) // 60% chance to use existing
        {
            _logger.LogDebug("Using existing Johnson: {JohnsonName}", existingFixer.NPCName);

            return new Models.MissionJohnson
            {
                Name = existingFixer.NPCName,
                Role = existingFixer.NPCRole ?? "Fixer",
                Organization = existingFixer.Organization,
                Attitude = existingFixer.Attitude,
                TrustLevel = existingFixer.TrustLevel,
                IsExistingContact = true,
                MeetingLocation = GenerateMeetingLocation(context.CurrentLocation, "neutral"),
                NegotiationStyle = DetermineNegotiationStyle(existingFixer.Attitude, existingFixer.TrustLevel)
            };
        }

        // Generate new Johnson
        var corp = GenerateCorporation(context);
        var name = GenerateJohnsonName();
        var role = missionType switch
        {
            "assassination" => "Corporate Security Consultant",
            "extraction" => "Human Resources Specialist",
            "cyberdeck" => "IT Security Consultant",
            "investigation" => "Insurance Investigator",
            _ => "Mr. Johnson"
        };

        return new Models.MissionJohnson
        {
            Name = name,
            Role = role,
            Organization = corp,
            Attitude = 0, // Neutral
            TrustLevel = 0, // Unknown
            IsExistingContact = false,
            MeetingLocation = GenerateMeetingLocation(context.CurrentLocation, "neutral"),
            NegotiationStyle = DetermineNegotiationStyle(0, 0)
        };
    }

    /// <summary>
    /// Generate primary objective based on mission type and context
    /// </summary>
    private Models.MissionObjective GenerateObjective(string missionType, MissionTypeConfig config, MissionGenerationContext context)
    {
        var objective = new Models.MissionObjective
        {
            MissionType = missionType
        };

        // Generate objective components based on mission type
        objective.Title = missionType switch
        {
            "cyberdeck" => GenerateCyberdeckObjective(context),
            "assassination" => GenerateAssassinationObjective(context),
            "extraction" => GenerateExtractionObjective(context),
            "theft" => GenerateTheftObjective(context),
            "investigation" => GenerateInvestigationObjective(context),
            "delivery" => GenerateDeliveryObjective(context),
            "recovery" => GenerateRecoveryObjective(context),
            "sabotage" => GenerateSabotageObjective(context),
            _ => "Complete the mission"
        };

        objective.Description = GenerateObjectiveDescription(missionType, objective.Title, context);
        objective.SuccessCriteria = GenerateSuccessCriteria(missionType, config);
        objective.PartialSuccessCriteria = GeneratePartialSuccessCriteria(missionType);
        objective.FailureConditions = GenerateFailureConditions(missionType, config);

        return objective;
    }

    /// <summary>
    /// Generate mission locations with multiple possible entry points
    /// </summary>
    private List<Models.MissionLocation> GenerateMissionLocations(string missionType, MissionGenerationContext context)
    {
        var locations = new List<Models.MissionLocation>();
        var primaryLocation = _gmService.GenerateLocation(missionType == "investigation" ? "seedy" : "corporate");

        locations.Add(new Models.MissionLocation
        {
            Name = primaryLocation,
            Type = DetermineLocationType(primaryLocation),
            SecurityLevel = _diceService.RollDie(6), // 1-6
            EntryPoints = GenerateEntryPoints(missionType),
            KeyAreas = GenerateKeyAreas(missionType),
            IsPrimary = true
        });

        // Add secondary locations for complex missions
        if (context.ParticipantCount > 3 || _diceService.RollDie(6) >= 4)
        {
            locations.Add(new Models.MissionLocation
            {
                Name = _gmService.GenerateLocation("safehouse"),
                Type = "Safehouse",
                SecurityLevel = 2,
                IsPrimary = false,
                Purpose = "Staging Area"
            });
        }

        return locations;
    }

    /// <summary>
    /// Generate obstacles and challenges for the mission
    /// </summary>
    private List<Models.MissionObstacle> GenerateObstacles(string missionType, MissionTypeConfig config, MissionGenerationContext context)
    {
        var obstacles = new List<Models.MissionObstacle>();
        var obstacleCount = config.Complexity + _diceService.RollDie(3);

        for (int i = 0; i < obstacleCount; i++)
        {
            var obstacleType = DetermineObstacleType(config, i);
            obstacles.Add(GenerateObstacle(obstacleType, missionType, context, i));
        }

        return obstacles;
    }

    /// <summary>
    /// Generate context-aware NPCs for the mission
    /// </summary>
    private async Task<List<Models.MissionNPC>> GenerateMissionNPCsAsync(int sessionId, string missionType, MissionGenerationContext context)
    {
        var npcs = new List<Models.MissionNPC>();

        // Generate key NPCs based on mission type
        var keyNPCCount = missionType switch
        {
            "investigation" => _diceService.RollDie(4) + 2,
            "extraction" => _diceService.RollDie(3) + 1,
            "assassination" => _diceService.RollDie(2) + 1,
            _ => _diceService.RollDie(3)
        };

        for (int i = 0; i < keyNPCCount; i++)
        {
            var npc = await GenerateContextAwareNPCAsync(sessionId, missionType, context, i == 0);
            npcs.Add(npc);
        }

        // Add guards/security for combat missions
        if (config.CombatChance > 0.5f)
        {
            var guardCount = _diceService.RollDie(6) + 2;
            for (int i = 0; i < guardCount; i++)
            {
                npcs.Add(new Models.MissionNPC
                {
                    Name = $"Security Guard #{i + 1}",
                    Role = "Security",
                    Attitude = -5,
                    IsHostile = true,
                    ThreatLevel = _diceService.RollDie(6)
                });
            }
        }

        return npcs;
    }

    /// <summary>
    /// Generate a context-aware NPC with memory of previous interactions
    /// </summary>
    private async Task<Models.MissionNPC> GenerateContextAwareNPCAsync(int sessionId, string missionType, MissionGenerationContext context, bool isPrimary)
    {
        // Check if we should reuse an existing NPC
        if (context.ExistingNPCs.Any() && _diceService.RollDie(100) <= 30)
        {
            var existingNPC = context.ExistingNPCs[_diceService.RollDie(context.ExistingNPCs.Count) - 1];
            
            // Get interaction history
            var events = await _narrativeService.GetNPCEventsAsync(
                context.SessionId.ToString().GetHashCode().ToString(), 
                existingNPC.NPCName);

            return new Models.MissionNPC
            {
                Name = existingNPC.NPCName,
                Role = existingNPC.NPCRole ?? "Unknown",
                Organization = existingNPC.Organization,
                Attitude = existingNPC.Attitude,
                TrustLevel = existingNPC.TrustLevel,
                IsExistingContact = true,
                InteractionHistory = existingNPC.InteractionHistory?.Split('\n').ToList() ?? new List<string>(),
                DialogueHints = GenerateDialogueHints(existingNPC, events.ToList()),
                ThreatLevel = DetermineThreatLevel(existingNPC.NPCRole),
                IsPrimary = isPrimary
            };
        }

        // Generate new NPC
        var role = isPrimary ? GetPrimaryNPCRole(missionType) : GetSecondaryNPCRole(missionType);
        var npcData = _gmService.GenerateNPC(role);

        return new Models.MissionNPC
        {
            Name = ExtractNPCName(npcData),
            Role = role,
            Organization = ExtractNPCCompany(npcData),
            Attitude = 0,
            TrustLevel = 0,
            IsExistingContact = false,
            ThreatLevel = DetermineThreatLevel(role),
            IsPrimary = isPrimary,
            DialogueHints = new List<string> { "First meeting - no history" }
        };
    }

    /// <summary>
    /// Generate decision points for branching narrative paths
    /// </summary>
    private List<Models.MissionDecisionPoint> GenerateDecisionPoints(string missionType, int complexity, MissionGenerationContext context)
    {
        var decisions = new List<Models.MissionDecisionPoint>();
        var decisionCount = Math.Min(complexity, 4); // Max 4 decision points

        for (int i = 0; i < decisionCount; i++)
        {
            var decision = new Models.MissionDecisionPoint
            {
                DecisionId = $"decision_{i}",
                Stage = (Models.MissionStage)i,
                Context = GenerateDecisionContext(missionType, i),
                Options = GenerateDecisionOptions(missionType, i, context),
                Consequences = new List<Models.MissionConsequence>() // Filled when decision is made
            };

            decisions.Add(decision);
        }

        return decisions;
    }

    /// <summary>
    /// Generate plot twist based on context
    /// </summary>
    private Models.MissionTwist GeneratePlotTwist(string missionType, MissionGenerationContext context)
    {
        var twistTypes = new[]
        {
            "Double Cross",
            "Hidden Agenda",
            "Unexpected Ally",
            "Wrong Target",
            "Setup",
            "Competing Team",
            "Corporate Involvement",
            "Personal Connection"
        };

        var twistType = twistTypes[_diceService.RollDie(twistTypes.Length) - 1];

        return new Models.MissionTwist
        {
            TwistType = twistType,
            TriggerCondition = GenerateTwistTrigger(missionType),
            Description = GenerateTwistDescription(twistType, missionType, context),
            RevealedAt = Models.MissionStage.ApproachingTarget,
            ImpactLevel = _diceService.RollDie(6)
        };
    }

    #endregion

    #region Mission Execution

    /// <summary>
    /// Execute a player's decision at a decision point
    /// </summary>
    public async Task<DecisionResult> ExecuteDecisionAsync(
        ulong channelId,
        string decisionId,
        string optionId,
        Dictionary<string, object>? additionalData = null)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        _logger.LogInformation("Executing decision {DecisionId} with option {OptionId} in session {SessionId}",
            decisionId, optionId, session.Id);

        // Get or create active mission state
        var missionState = await GetOrCreateMissionStateAsync(channelId);
        var decision = missionState.DecisionPoints.FirstOrDefault(d => d.DecisionId == decisionId);

        if (decision == null)
            throw new InvalidOperationException($"Decision point {decisionId} not found");

        var option = decision.Options.FirstOrDefault(o => o.OptionId == optionId);
        if (option == null)
            throw new InvalidOperationException($"Option {optionId} not found");

        // Record the choice
        var playerChoice = await _narrativeService.RecordChoiceAsync(
            channelId,
            decisionId,
            optionId,
            null // Consequences determined below
        );

        // Determine consequences based on choice
        var consequences = await DetermineConsequencesAsync(missionState, decision, option, additionalData);

        // Update mission state
        missionState.CurrentStage = DetermineNextStage(decision.Stage, option);
        missionState.CompletedDecisions.Add(decisionId);
        missionState.AccumulatedConsequences.AddRange(consequences);

        // Record consequences
        await _narrativeService.ResolveChoiceAsync(playerChoice.Id, 
            string.Join("; ", consequences.Select(c => c.Description)));

        // Check for mission completion
        var missionStatus = EvaluateMissionStatus(missionState);

        return new DecisionResult
        {
            DecisionId = decisionId,
            ChosenOption = optionId,
            Consequences = consequences,
            NewStage = missionState.CurrentStage,
            MissionStatus = missionStatus,
            NarrativeUpdate = GenerateNarrativeUpdate(decision, option, consequences)
        };
    }

    /// <summary>
    /// Get or create active mission state for the session
    /// </summary>
    private async Task<MissionState> GetOrCreateMissionStateAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        // For now, use the first active mission
        // In a full implementation, this would be cached or stored in session metadata
        var activeMission = session.ActiveMissions.FirstOrDefault(m => m.Status == MissionStatus.InProgress);

        if (activeMission == null)
            throw new InvalidOperationException("No active mission in progress");

        // Reconstruct mission state from narrative events
        var events = await _narrativeService.GetRecentEventsAsync(channelId, 50);
        var choices = session.PlayerChoices.ToList();

        return new MissionState
        {
            MissionId = activeMission.Id,
            CurrentStage = DetermineCurrentStage(events),
            CompletedDecisions = choices.Where(c => c.IsResolved).Select(c => c.ChoiceDescription).ToList(),
            AccumulatedConsequences = new List<Models.MissionConsequence>()
        };
    }

    /// <summary>
    /// Determine consequences based on player choice and dice rolls
    /// </summary>
    private async Task<List<Models.MissionConsequence>> DetermineConsequencesAsync(
        MissionState missionState,
        Models.MissionDecisionPoint decision,
        Models.DecisionOption option,
        Dictionary<string, object>? additionalData)
    {
        var consequences = new List<Models.MissionConsequence>();

        // Roll for outcome quality
        var outcomeRoll = _diceService.RollShadowrun(6); // 6 dice pool
        var successLevel = outcomeRoll.Successes;

        // Determine immediate consequence
        var immediateConsequence = new Models.MissionConsequence
        {
            Type = (Models.ConsequenceType)DetermineConsequenceType(option, successLevel),
            Description = GenerateConsequenceDescription(decision, option, successLevel),
            Severity = CalculateSeverity(successLevel),
            AffectedEntities = DetermineAffectedEntities(option)
        };

        consequences.Add(immediateConsequence);

        // Add cascading consequences for critical failures/successes
        if (outcomeRoll.CriticalGlitch)
        {
            consequences.Add(new Models.MissionConsequence
            {
                Type = Models.ConsequenceType.Complication,
                Description = "Critical glitch creates unexpected complication",
                Severity = 4
            });
        }
        else if (successLevel >= 5)
        {
            consequences.Add(new Models.MissionConsequence
            {
                Type = Models.ConsequenceType.Opportunity,
                Description = "Exceptional success creates new opportunity",
                Severity = 1
            });
        }

        // Record narrative event
        var session = await _sessionService.GetActiveSessionAsync(0); // Will be overridden
        await _narrativeService.RecordEventAsync(
            session?.DiscordChannelId ?? 0,
            $"Decision: {decision.Context}",
            immediateConsequence.Description,
            NarrativeEventType.PlayerChoice
        );

        return consequences;
    }

    /// <summary>
    /// Evaluate current mission status based on accumulated consequences
    /// </summary>
    private MissionStatus EvaluateMissionStatus(MissionState state)
    {
        var negativeConsequences = state.AccumulatedConsequences
            .Count(c => c.Severity >= 4 && c.Type == Models.ConsequenceType.Failure);

        if (negativeConsequences >= 3)
            return MissionStatus.Failed;

        if (state.CurrentStage == Models.MissionStage.Extraction && 
            state.AccumulatedConsequences.Any(c => c.Type == Models.ConsequenceType.Success))
            return MissionStatus.Completed;

        return MissionStatus.InProgress;
    }

    #endregion

    #region NPC Dialogue Generation

    /// <summary>
    /// Generate contextual dialogue for an NPC based on relationship and situation
    /// </summary>
    public async Task<string> GenerateNPCDialogueAsync(
        ulong channelId,
        string npcName,
        string situation,
        DialogueContext dialogueContext)
    {
        var relationship = await _narrativeService.GetNPCRelationshipAsync(channelId, npcName);
        var session = await _sessionService.GetActiveSessionAsync(channelId);

        if (relationship == null)
            return GenerateFirstMeetingDialogue(npcName, situation);

        // Get recent interactions for context
        var recentEvents = await _narrativeService.GetNPCEventsAsync(channelId, npcName);
        var recentInteractions = recentEvents.Take(3).ToList();

        // Generate dialogue based on attitude and trust
        var dialogue = GenerateContextualDialogue(relationship, situation, dialogueContext, recentInteractions);

        // Record this interaction
        await _narrativeService.UpdateNPCRelationshipAsync(
            channelId,
            npcName,
            interaction: $"[{situation}] Player: {dialogueContext.PlayerInput ?? "N/A"} | NPC: {dialogue.Substring(0, Math.Min(100, dialogue.Length))}..."
        );

        return dialogue;
    }

    /// <summary>
    /// Generate dialogue for first meeting with an NPC
    /// </summary>
    private string GenerateFirstMeetingDialogue(string npcName, string situation)
    {
        var greetings = new Dictionary<string, List<string>>
        {
            ["neutral"] = new()
            {
                $"I don't know you, but I've heard things. What do you want?",
                $"You're new. Names are dangerous in this business.",
                $"Fresh face. Haven't seen you around before."
            },
            ["suspicious"] = new()
            {
                $"Not another word until I know who sent you.",
                $"Keep your distance. Trust is earned, not given.",
                $"I don't talk to strangers. Too many people have tried to kill me."
            },
            ["professional"] = new()
            {
                $"I assume you're here on business. Let's keep it professional.",
                $"Time is nuyen. What's the job?",
                $"I work with many people. What makes you different?"
            }
        };

        var attitude = _diceService.RollDie(3) switch
        {
            1 => "suspicious",
            2 => "professional",
            _ => "neutral"
        };

        var options = greetings[attitude];
        return options[_diceService.RollDie(options.Count) - 1];
    }

    /// <summary>
    /// Generate contextual dialogue based on relationship and history
    /// </summary>
    private string GenerateContextualDialogue(
        NPCRelationship relationship,
        string situation,
        DialogueContext context,
        List<NarrativeEvent> recentInteractions)
    {
        var sb = new System.Text.StringBuilder();

        // Opening based on attitude
        sb.AppendLine(GetAttitudeBasedOpening(relationship.Attitude));

        // Reference recent history if applicable
        if (recentInteractions.Any())
        {
            var lastInteraction = recentInteractions.First();
            sb.AppendLine($"*nods* Last time we met, {lastInteraction.Title}.");
        }

        // Situation-specific response
        sb.AppendLine(GetSituationResponse(situation, relationship.TrustLevel));

        // Closing based on trust
        sb.AppendLine(GetTrustBasedClosing(relationship.TrustLevel));

        return sb.ToString();
    }

    private string GetAttitudeBasedOpening(int attitude)
    {
        return attitude switch
        {
            >= 7 => "Good to see you again, friend.",
            >= 4 => "Ah, you're back. Good.",
            >= 1 => "Oh, it's you.",
            0 => "What do you need?",
            >= -3 => "You again. What now?",
            >= -6 => "I was hoping not to see your face again.",
            _ => "You have a lot of nerve showing up here."
        };
    }

    private string GetSituationResponse(string situation, int trustLevel)
    {
        // Situation-specific responses would be expanded in production
        return situation.ToLower() switch
        {
            "negotiation" => trustLevel >= 5 
                ? "For you? I'll see what I can do. No promises."
                : "Everything has a price. The question is whether you can pay it.",
            "combat" => "Sounds like trouble. Count me in... or out, depending on the pay.",
            "information" => "Information is my trade. But it'll cost you.",
            _ => "I'm listening. Make it quick."
        };
    }

    private string GetTrustBasedClosing(int trustLevel)
    {
        return trustLevel switch
        {
            >= 8 => "You know where to find me. Stay safe out there.",
            >= 5 => "We'll talk more when the job's done.",
            >= 3 => "Don't disappoint me.",
            _ => "Don't make me regret this."
        };
    }

    #endregion

    #region Helper Methods

    private int CalculateComplexity(MissionTypeConfig config, MissionGenerationContext context)
    {
        var baseComplexity = config.Complexity;
        var playerBonus = context.ParticipantCount > 3 ? 1 : 0;
        var experienceBonus = context.PlayerCharacters.Sum(p => p.TotalKarma) / 20;

        return Math.Clamp(baseComplexity + playerBonus + (experienceBonus / 2), 1, 6);
    }

    private Models.MissionReward CalculateReward(int complexity, MissionGenerationContext context)
    {
        var baseNuyen = complexity * 5000;
        var baseKarma = complexity * 2;

        // Adjust for team size
        var teamMultiplier = 1.0f + (context.ParticipantCount - 1) * 0.25f;

        return new Models.MissionReward
        {
            BaseNuyen = (long)(baseNuyen * teamMultiplier),
            BaseKarma = baseKarma + context.ParticipantCount,
            NegotiationBuffer = (long)(baseNuyen * 0.2), // 20% negotiation room
            BonusOpportunities = GenerateBonusOpportunities(complexity)
        };
    }

    private DateTime? GenerateDeadline(int complexity, MissionGenerationContext context)
    {
        var days = complexity switch
        {
            <= 2 => _diceService.RollDie(3),
            <= 4 => _diceService.RollDie(5) + 2,
            _ => _diceService.RollDie(7) + 5
        };

        return context.InGameDateTime.AddDays(days);
    }

    private List<string> AnalyzeNarrativeThemes(List<NarrativeEvent> events)
    {
        var themes = new List<string>();

        foreach (var evt in events)
        {
            if (evt.Description.Contains("betrayal", StringComparison.OrdinalIgnoreCase))
                themes.Add("betrayal");
            if (evt.Description.Contains("corporate", StringComparison.OrdinalIgnoreCase))
                themes.Add("corporate");
            if (evt.Description.Contains("magic", StringComparison.OrdinalIgnoreCase))
                themes.Add("magic");
            if (evt.Description.Contains("gang", StringComparison.OrdinalIgnoreCase))
                themes.Add("gang");
        }

        return themes.Distinct().ToList();
    }

    private Dictionary<string, int> CalculateFactionStandings(List<NPCRelationship> npcs)
    {
        var standings = new Dictionary<string, int>();

        foreach (var npc in npcs.Where(n => !string.IsNullOrEmpty(n.Organization)))
        {
            var org = npc.Organization!;
            if (!standings.ContainsKey(org))
                standings[org] = 0;

            standings[org] += npc.Attitude;
        }

        return standings;
    }

    private string DetermineCharacterRole(ShadowrunCharacter? character)
    {
        if (character == null) return "Unknown";

        // Simplified role determination based on skills/cyberware
        // In production, this would be more sophisticated
        var combatSkills = character.Skills?.Count(s => 
            s.SkillName.Contains("firearms", StringComparison.OrdinalIgnoreCase) ||
            s.SkillName.Contains("combat", StringComparison.OrdinalIgnoreCase)) ?? 0;

        var techSkills = character.Skills?.Count(s =>
            s.SkillName.Contains("hacking", StringComparison.OrdinalIgnoreCase) ||
            s.SkillName.Contains("electronics", StringComparison.OrdinalIgnoreCase)) ?? 0;

        var magicSkills = character.Spells?.Count ?? 0;

        if (magicSkills > 0) return "Mage";
        if (techSkills > combatSkills) return "Decker";
        if (combatSkills > 0) return "Samurai";
        return "Street Sam";
    }

    private string GenerateMeetingLocation(string currentLocation, string atmosphere)
    {
        var locations = new[]
        {
            "The Big Rhino - private booth in back",
            "Matchsticks - corner table, away from windows",
            "Underground parking garage, level B2",
            "Noodle shop in Redmond Barrens",
            "Private room at Club Penumbra",
            "Abandoned warehouse in the Industrial Zone"
        };

        return locations[_diceService.RollDie(locations.Length) - 1];
    }

    private string DetermineNegotiationStyle(int attitude, int trust)
    {
        return (attitude, trust) switch
        {
            (>= 5, >= 5) => "Friendly - willing to negotiate",
            (>= 0, >= 3) => "Professional - standard rates",
            (>= -3, _) => "Wary - higher rates for risk",
            _ => "Hostile - premium prices or refusal"
        };
    }

    private string GenerateCorporation(MissionGenerationContext context)
    {
        var corps = new[]
        {
            "Arasaka", "Renraku", "Saeder-Krupp", "Mitsuhama",
            "Ares Macrotechnology", "Aztechnology", "NeoNET",
            "Shiawase", "Wuxing", "Evo Corporation"
        };

        // Prefer corporations the team has history with
        if (context.FactionStandings.Any())
        {
            var existingCorps = context.FactionStandings.Keys.ToList();
            if (existingCorps.Any() && _diceService.RollDie(100) <= 40)
                return existingCorps[_diceService.RollDie(existingCorps.Count) - 1];
        }

        return corps[_diceService.RollDie(corps.Length) - 1];
    }

    private string GenerateJohnsonName()
    {
        var firstNames = new[] { "Kenji", "Hans", "Dmitri", "Maria", "Chen", "Johnson", "Sarah", "Mike" };
        var lastNames = new[] { "Tanaka", "Schmidt", "Volkov", "Santos", "Wei", "Smith", "Johnson", "Davis" };

        return $"{firstNames[_diceService.RollDie(firstNames.Length) - 1]} {lastNames[_diceService.RollDie(lastNames.Length) - 1]}";
    }

    private bool ShouldGenerateTwist(MissionGenerationContext context)
    {
        // More likely to have twists in longer campaigns
        var baseChance = 30;
        var historyBonus = context.RecentNarrativeEvents.Count * 5;
        return _diceService.RollDie(100) <= baseChance + historyBonus;
    }

    private List<string> GenerateEntryPoints(string missionType)
    {
        var entryPoints = new List<string>();

        // Always include stealth option
        entryPoints.Add("Rooftop access via maintenance ladder");
        entryPoints.Add("Service entrance during shift change");

        if (missionType != "theft")
            entryPoints.Add("Front door - disguise and credentials");

        if (missionType == "cyberdeck")
            entryPoints.Add("Remote matrix access from nearby location");

        entryPoints.Add("Sewer access to basement level");

        return entryPoints;
    }

    private List<string> GenerateKeyAreas(string missionType)
    {
        return missionType switch
        {
            "cyberdeck" => new List<string> { "Server Room", "Security Hub", "Backup Systems" },
            "extraction" => new List<string> { "Holding Area", "Security Office", "Parking Garage" },
            "assassination" => new List<string> { "Target's Office", "Security Detail Post", "Exit Routes" },
            "theft" => new List<string> { "Vault", "Alarm Control", "Security Station" },
            "investigation" => new List<string> { "Evidence Room", "Witness Locations", "Crime Scene" },
            _ => new List<string> { "Primary Objective", "Secondary Target", "Exit Point" }
        };
    }

    private string DetermineLocationType(string location)
    {
        if (location.Contains("corporate", StringComparison.OrdinalIgnoreCase)) return "Corporate";
        if (location.Contains("warehouse", StringComparison.OrdinalIgnoreCase)) return "Industrial";
        if (location.Contains("bar", StringComparison.OrdinalIgnoreCase) || location.Contains("club", StringComparison.OrdinalIgnoreCase)) return "Social";
        return "Urban";
    }

    private string DetermineObstacleType(MissionTypeConfig config, int index)
    {
        var roll = _diceService.RollDie(100) / 100.0f;

        if (roll < config.CombatChance) return "combat";
        if (roll < config.CombatChance + config.SocialChance) return "social";
        if (roll < config.CombatChance + config.SocialChance + config.StealthChance) return "stealth";
        if (config.InvestigationChance > 0.5f) return "investigation";
        if (config.MatrixChance > 0.5f) return "matrix";
        return "security";
    }

    private MissionObstacle GenerateObstacle(string obstacleType, string missionType, MissionGenerationContext context, int index)
    {
        return new Models.MissionObstacle
        {
            ObstacleType = obstacleType,
            Description = GenerateObstacleDescription(obstacleType, missionType, context),
            DifficultyRating = _diceService.RollDie(6),
            CanBeAvoided = _diceService.RollDie(100) <= 40,
            AlternativeSolutions = GenerateAlternativeSolutions(obstacleType),
            Position = index
        };
    }

    private string GenerateObstacleDescription(string obstacleType, string missionType, MissionGenerationContext context)
    {
        return obstacleType switch
        {
            "combat" => "Patrol team - 4 guards with combat training",
            "social" => "Security checkpoint requiring credentials or fast talk",
            "stealth" => "Motion sensors and pressure plates in hallway",
            "investigation" => "Encrypted data terminal requiring decryption",
            "matrix" => "Active ICE protecting target system",
            "security" => "Biometric scanner at restricted entrance",
            _ => "Unexpected complication blocks the path"
        };
    }

    private List<string> GenerateAlternativeSolutions(string obstacleType)
    {
        return obstacleType switch
        {
            "combat" => new List<string> { "Stealth around", "Create distraction", "Negotiate" },
            "social" => new List<string> { "Hack credentials", "Bribe", "Create diversion" },
            "stealth" => new List<string> { "Tech bypass", "Find alternate route", "Wait for patrol gap" },
            _ => new List<string> { "Find another way", "Use specialized skills", "Call for backup" }
        };
    }

    private string GetPrimaryNPCRole(string missionType)
    {
        return missionType switch
        {
            "assassination" => "Target",
            "extraction" => "Extractee",
            "investigation" => "Key Witness",
            "delivery" => "Recipient",
            _ => "Contact"
        };
    }

    private string GetSecondaryNPCRole(string missionType)
    {
        var roles = new[] { "Security Chief", "Corporate Liaison", "Street Informant", "Rival Runner", "Bystander" };
        return roles[_diceService.RollDie(roles.Length) - 1];
    }

    private int DetermineThreatLevel(string? role)
    {
        return role?.ToLower() switch
        {
            "security" => 4,
            "security chief" => 6,
            "target" => 5,
            "corporate liaison" => 3,
            _ => 2
        };
    }

    private List<string> GenerateDialogueHints(NPCRelationship npc, List<NarrativeEvent> events)
    {
        var hints = new List<string>();

        if (npc.Attitude >= 5)
            hints.Add("Generally friendly disposition");
        else if (npc.Attitude <= -5)
            hints.Add("Hostile or suspicious");

        if (events.Any(e => e.Title.Contains("betrayal", StringComparison.OrdinalIgnoreCase)))
            hints.Add("Previous incident involving betrayal");

        if (events.Any(e => e.Title.Contains("combat", StringComparison.OrdinalIgnoreCase)))
            hints.Add("Has been in combat with the team");

        return hints;
    }

    private string ExtractNPCName(string npcData)
    {
        // Simple extraction - in production would parse properly
        var lines = npcData.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("**Name:**"))
                return line.Replace("**Name:**", "").Trim();
        }
        return "Unknown";
    }

    private string ExtractNPCCompany(string npcData)
    {
        var lines = npcData.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("**Company:**"))
                return line.Replace("**Company:**", "").Trim();
        }
        return "Independent";
    }

    private string GenerateDecisionContext(string missionType, int stageIndex)
    {
        return stageIndex switch
        {
            0 => "Initial approach - how do you want to enter?",
            1 => "Obstacle encountered - how do you proceed?",
            2 => "Near objective - risk vs. caution?",
            3 => "Extraction phase - escape route choice",
            _ => "Critical decision point"
        };
    }

    private List<Models.DecisionOption> GenerateDecisionOptions(string missionType, int stageIndex, MissionGenerationContext context)
    {
        var options = new List<Models.DecisionOption>();

        // Generate 2-4 options based on stage
        options.Add(new Models.DecisionOption
        {
            OptionId = $"option_direct",
            Description = "Direct approach - high risk, high reward",
            RiskLevel = 4,
            SuccessChance = 0.4f,
            ConsequencePreview = "Fast but dangerous"
        });

        options.Add(new Models.DecisionOption
        {
            OptionId = $"option_careful",
            Description = "Careful approach - lower risk, takes longer",
            RiskLevel = 2,
            SuccessChance = 0.7f,
            ConsequencePreview = "Safe but time-consuming"
        });

        if (context.PlayerCharacters.Any(p => p.PrimaryRole == "Decker"))
        {
            options.Add(new Models.DecisionOption
            {
                OptionId = $"option_tech",
                Description = "Technical solution - use decker skills",
                RiskLevel = 3,
                SuccessChance = 0.6f,
                ConsequencePreview = "Requires matrix skills"
            });
        }

        if (context.PlayerCharacters.Any(p => p.PrimaryRole == "Mage"))
        {
            options.Add(new Models.DecisionOption
            {
                OptionId = $"option_magic",
                Description = "Magical approach - use spell/magic",
                RiskLevel = 3,
                SuccessChance = 0.6f,
                ConsequencePreview = "Requires magical ability"
            });
        }

        return options;
    }

    private string GenerateTwistTrigger(string missionType)
    {
        var triggers = new[]
        {
            "After primary objective is secured",
            "When approaching extraction point",
            "During negotiation with Johnson",
            "Mid-mission complication"
        };
        return triggers[_diceService.RollDie(triggers.Length) - 1];
    }

    private string GenerateTwistDescription(string twistType, string missionType, MissionGenerationContext context)
    {
        return twistType switch
        {
            "Double Cross" => "The Johnson has betrayed you - corporate security is waiting at extraction",
            "Hidden Agenda" => "The real objective is different - you've been set up",
            "Unexpected Ally" => "A former enemy offers unexpected help",
            "Wrong Target" => "The target isn't who you were told - moral dilemma",
            "Setup" => "This was a setup from the start - you're the target",
            "Competing Team" => "Another runner team is after the same objective",
            "Corporate Involvement" => "A megacorp is secretly behind this job",
            "Personal Connection" => "The mission connects to someone's past",
            _ => "Unexpected complication changes everything"
        };
    }

    private List<string> GenerateIntelOpportunities(string missionType, MissionGenerationContext context)
    {
        var intel = new List<string>
        {
            "Street contacts may have security schedules",
            "Matrix search on target organization",
            "Physical reconnaissance of site",
            "Bribe insider for access codes"
        };

        if (context.ExistingNPCs.Any(n => n.Organization?.Contains("Security") == true))
        {
            intel.Add("Known contact inside security team");
        }

        return intel;
    }

    private List<string> GenerateBonusOpportunities(int complexity)
    {
        var bonuses = new List<string>();

        if (_diceService.RollDie(6) >= 4)
            bonuses.Add("Secondary objective available - bonus payment");

        if (_diceService.RollDie(6) >= 5)
            bonuses.Add("Corporate data on sale - extra nuyen");

        if (_diceService.RollDie(6) >= 5)
            bonuses.Add("Paydata found - karma bonus");

        return bonuses;
    }

    // Objective generation helpers for each mission type
    private string GenerateCyberdeckObjective(MissionGenerationContext context)
    {
        var objectives = new[]
        {
            "Extract research data from secure server",
            "Plant false data in corporate database",
            "Disable security matrix node",
            "Steal encryption keys from host system"
        };
        return objectives[_diceService.RollDie(objectives.Length) - 1];
    }

    private string GenerateAssassinationObjective(MissionGenerationContext context)
    {
        var objectives = new[]
        {
            "Eliminate corporate executive during transit",
            "Neutralize rogue AI researcher",
            "Remove gang leader disrupting operations",
            "Take out witness before testimony"
        };
        return objectives[_diceService.RollDie(objectives.Length) - 1];
    }

    private string GenerateExtractionObjective(MissionGenerationContext context)
    {
        var objectives = new[]
        {
            "Extract defecting researcher from corp facility",
            "Rescue kidnapped fixer from gang territory",
            "Recover corporate mole before exposure",
            "Evacuate valuable asset from hostile zone"
        };
        return objectives[_diceService.RollDie(objectives.Length) - 1];
    }

    private string GenerateTheftObjective(MissionGenerationContext context)
    {
        var objectives = new[]
        {
            "Steal prototype cyberware from lab",
            "Acquire physical encryption key from executive",
            "Lift rare magical artifact from collection",
            "Obtain blackmail material from secure vault"
        };
        return objectives[_diceService.RollDie(objectives.Length) - 1];
    }

    private string GenerateInvestigationObjective(MissionGenerationContext context)
    {
        var objectives = new[]
        {
            "Investigate disappearance of street doc",
            "Uncover identity of serial killer in Barrens",
            "Trace source of new designer drug",
            "Find evidence of corporate conspiracy"
        };
        return objectives[_diceService.RollDie(objectives.Length) - 1];
    }

    private string GenerateDeliveryObjective(MissionGenerationContext context)
    {
        var objectives = new[]
        {
            "Transport sensitive data to dead drop",
            "Escort VIP through hostile territory",
            "Deliver experimental cyberware to client",
            "Move contraband past security cordon"
        };
        return objectives[_diceService.RollDie(objectives.Length) - 1];
    }

    private string GenerateRecoveryObjective(MissionGenerationContext context)
    {
        var objectives = new[]
        {
            "Recover stolen prototype from thieves",
            "Find missing team member's location",
            "Retrieve lost shipment from crash site",
            "Locate and extract kidnapped contact"
        };
        return objectives[_diceService.RollDie(objectives.Length) - 1];
    }

    private string GenerateSabotageObjective(MissionGenerationContext context)
    {
        var objectives = new[]
        {
            "Destroy prototype in manufacturing plant",
            "Disable power grid for district",
            "Corrupt research data permanently",
            "Sabotage corporate negotiations"
        };
        return objectives[_diceService.RollDie(objectives.Length) - 1];
    }

    private string GenerateObjectiveDescription(string missionType, string title, MissionGenerationContext context)
    {
        return $"Primary Objective: {title}\n\n" +
               $"Location: {context.CurrentLocation}\n" +
               $"Recommended Team Size: {Math.Max(3, context.ParticipantCount)}\n" +
               $"Estimated Difficulty: Moderate";
    }

    private string GenerateSuccessCriteria(string missionType, MissionTypeConfig config)
    {
        return missionType switch
        {
            "cyberdeck" => "Data successfully extracted and team extracted safely",
            "assassination" => "Target eliminated without witnesses",
            "extraction" => "Extractee recovered alive and delivered to safe location",
            "theft" => "Item secured and team extracted",
            "investigation" => "Evidence gathered and conclusion reached",
            "delivery" => "Package delivered intact to destination",
            "recovery" => "Target recovered successfully",
            "sabotage" => "Objective destroyed and team escaped",
            _ => "Mission objectives completed"
        };
    }

    private string GeneratePartialSuccessCriteria(string missionType)
    {
        return "Primary objective achieved but with complications - reduced payment";
    }

    private string GenerateFailureConditions(string missionType, MissionTypeConfig config)
    {
        return "Team captured, objective compromised, or civilian casualties";
    }

    private Models.ConsequenceType DetermineConsequenceType(Models.DecisionOption option, int successLevel)
    {
        if (successLevel == 0) return Models.ConsequenceType.Failure;
        if (successLevel >= 4) return Models.ConsequenceType.Success;
        if (option.RiskLevel >= 4 && successLevel < 3) return Models.ConsequenceType.Complication;
        return Models.ConsequenceType.PartialSuccess;
    }

    private string GenerateConsequenceDescription(Models.MissionDecisionPoint decision, Models.DecisionOption option, int successLevel)
    {
        return successLevel switch
        {
            0 => "Complete failure - plan falls apart",
            1 => "Marginal success - close call with complications",
            2 => "Partial success - achieved goal with cost",
            3 => "Solid success - objective completed",
            4 => "Excellent execution - bonus opportunity",
            _ => "Flawless execution - significant advantage gained"
        };
    }

    private int CalculateSeverity(int successLevel)
    {
        return Math.Abs(3 - successLevel); // 0-5 scale
    }

    private List<string> DetermineAffectedEntities(Models.DecisionOption option)
    {
        return new List<string> { "Team", "Mission", "Reputation" };
    }

    private MissionStage DetermineNextStage(Models.MissionStage currentStage, Models.DecisionOption option)
    {
        return currentStage switch
        {
            Models.MissionStage.Planning => Models.MissionStage.Infiltration,
            Models.MissionStage.Infiltration => Models.MissionStage.ApproachingTarget,
            Models.MissionStage.ApproachingTarget => Models.MissionStage.Objective,
            Models.MissionStage.Objective => Models.MissionStage.Extraction,
            Models.MissionStage.Extraction => Models.MissionStage.Completed,
            _ => (Models.MissionStage)((int)currentStage + 1)
        };
    }

    private Models.MissionStage DetermineCurrentStage(List<NarrativeEvent> events)
    {
        // Simplified stage determination
        if (events.Any(e => e.Title.Contains("extraction", StringComparison.OrdinalIgnoreCase)))
            return Models.MissionStage.Extraction;
        if (events.Any(e => e.Title.Contains("objective", StringComparison.OrdinalIgnoreCase)))
            return Models.MissionStage.Objective;
        if (events.Any(e => e.Title.Contains("infiltration", StringComparison.OrdinalIgnoreCase)))
            return Models.MissionStage.Infiltration;
        return Models.MissionStage.Planning;
    }

    private string GenerateNarrativeUpdate(Models.MissionDecisionPoint decision, Models.DecisionOption option, List<Models.MissionConsequence> consequences)
    {
        return $"Decision made: {decision.Context}\n" +
               $"Choice: {option.Description}\n" +
               $"Outcome: {consequences.FirstOrDefault()?.Description ?? "Unknown"}";
    }

    #endregion
}

#region Supporting Types (Runtime Helpers)

/// <summary>
/// Configuration for mission type generation
/// </summary>
public class MissionTypeConfig
{
    public int Complexity { get; set; } = 3;
    public float CombatChance { get; set; } = 0.5f;
    public float SocialChance { get; set; } = 0.3f;
    public float StealthChance { get; set; } = 0.5f;
    public float InvestigationChance { get; set; } = 0.0f;
    public float MatrixChance { get; set; } = 0.0f;
}

/// <summary>
/// Context for mission generation
/// </summary>
public class MissionGenerationContext
{
    public int SessionId { get; set; }
    public string CurrentLocation { get; set; } = "Seattle";
    public DateTime InGameDateTime { get; set; }
    public List<NarrativeEvent> RecentNarrativeEvents { get; set; } = new();
    public List<NPCRelationship> ExistingNPCs { get; set; } = new();
    public List<ActiveMission> ActiveMissions { get; set; } = new();
    public List<PlayerCharacterSummary> PlayerCharacters { get; set; } = new();
    public List<string> RecentThemes { get; set; } = new();
    public Dictionary<string, int> FactionStandings { get; set; } = new();
    public int ParticipantCount { get; set; }
}

/// <summary>
/// Summary of player character for context
/// </summary>
public class PlayerCharacterSummary
{
    public int CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PrimaryRole { get; set; } = "Street Sam";
    public int TotalKarma { get; set; }
    public long TotalNuyen { get; set; }
}

/// <summary>
/// Runtime state of an active mission
/// </summary>
public class MissionState
{
    public int MissionId { get; set; }
    public Models.MissionStage CurrentStage { get; set; }
    public List<string> CompletedDecisions { get; set; } = new();
    public List<Models.MissionConsequence> AccumulatedConsequences { get; set; } = new();
    public List<Models.MissionDecisionPoint> DecisionPoints { get; set; } = new();
}

/// <summary>
/// Result of a player decision
/// </summary>
public class DecisionResult
{
    public string DecisionId { get; set; } = string.Empty;
    public string ChosenOption { get; set; } = string.Empty;
    public List<Models.MissionConsequence> Consequences { get; set; } = new();
    public Models.MissionStage NewStage { get; set; }
    public MissionStatus MissionStatus { get; set; }
    public string NarrativeUpdate { get; set; } = string.Empty;
}

/// <summary>
/// Context for NPC dialogue generation
/// </summary>
public class DialogueContext
{
    public string? PlayerInput { get; set; }
    public string? PreviousNPCLine { get; set; }
    public Models.MissionStage? CurrentStage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

#endregion
