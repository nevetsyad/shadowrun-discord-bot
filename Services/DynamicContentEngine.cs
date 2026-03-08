using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;
using System.Text.Json;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Advanced procedural content generation engine with adaptive difficulty,
/// story evolution, and optional machine learning integration for Shadowrun campaigns.
/// Consolidates: Difficulty tracking, story generation, NPC learning, campaign arcs.
/// </summary>
public class DynamicContentEngine
{
    private readonly DatabaseService _database;
    private readonly GameSessionService _sessionService;
    private readonly NarrativeContextService _narrativeService;
    private readonly AutonomousMissionService _missionService;
    private readonly InteractiveStoryService _storyService;
    private readonly ContentDatabaseService _contentDb;
    private readonly DiceService _diceService;
    private readonly GMService _gmService;
    private readonly ILogger<DynamicContentEngine> _logger;

    // Difficulty configuration
    private const int MinDifficulty = 1;
    private const int MaxDifficulty = 10;
    private const int DefaultDifficulty = 5;
    private const int PerformanceSampleSize = 10; // Number of recent events to analyze

    public DynamicContentEngine(
        DatabaseService database,
        GameSessionService sessionService,
        NarrativeContextService narrativeService,
        AutonomousMissionService missionService,
        InteractiveStoryService storyService,
        ContentDatabaseService contentDb,
        DiceService diceService,
        GMService gmService,
        ILogger<DynamicContentEngine> logger)
    {
        _database = database;
        _sessionService = sessionService;
        _narrativeService = narrativeService;
        _missionService = missionService;
        _storyService = storyService;
        _contentDb = contentDb;
        _diceService = diceService;
        _gmService = gmService;
        _logger = logger;
    }

    #region Adaptive Difficulty System

    /// <summary>
    /// Get current difficulty level for a session
    /// </summary>
    public async Task<DifficultyStatus> GetDifficultyAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var metrics = await GetPerformanceMetricsAsync(channelId);
        var contentData = await _contentDb.GetSessionContentDataAsync(session.Id);

        var currentDifficulty = contentData?.CurrentDifficulty ?? DefaultDifficulty;

        return new DifficultyStatus
        {
            SessionId = session.Id,
            CurrentDifficulty = currentDifficulty,
            PerformanceScore = metrics.OverallScore,
            Recommendation = GetDifficultyRecommendation(currentDifficulty, metrics),
            Metrics = metrics,
            LastAdjustedAt = contentData?.DifficultyLastAdjusted,
            AdjustmentHistory = contentData?.DifficultyHistory != null
                ? JsonSerializer.Deserialize<List<DifficultyAdjustment>>(contentData.DifficultyHistory) ?? new()
                : new()
        };
    }

    /// <summary>
    /// Manually adjust difficulty level
    /// </summary>
    public async Task<DifficultyStatus> AdjustDifficultyAsync(ulong channelId, int newDifficulty, string? reason = null)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        newDifficulty = Math.Clamp(newDifficulty, MinDifficulty, MaxDifficulty);

        var contentData = await _contentDb.GetOrCreateSessionContentDataAsync(session.Id);
        var oldDifficulty = contentData.CurrentDifficulty;

        // Record adjustment
        var history = contentData.DifficultyHistory != null
            ? JsonSerializer.Deserialize<List<DifficultyAdjustment>>(contentData.DifficultyHistory) ?? new()
            : new List<DifficultyAdjustment>();

        history.Add(new DifficultyAdjustment
        {
            FromDifficulty = oldDifficulty,
            ToDifficulty = newDifficulty,
            AdjustedAt = DateTime.UtcNow,
            Reason = reason ?? "Manual adjustment",
            IsAutomatic = false
        });

        contentData.CurrentDifficulty = newDifficulty;
        contentData.DifficultyLastAdjusted = DateTime.UtcNow;
        contentData.DifficultyHistory = JsonSerializer.Serialize(history);

        await _contentDb.UpdateSessionContentDataAsync(contentData);

        _logger.LogInformation("Adjusted difficulty for session {SessionId} from {Old} to {New}: {Reason}",
            session.Id, oldDifficulty, newDifficulty, reason);

        return await GetDifficultyAsync(channelId);
    }

    /// <summary>
    /// Automatically adjust difficulty based on performance metrics
    /// </summary>
    public async Task<bool> AutoAdjustDifficultyAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null) return false;

        var metrics = await GetPerformanceMetricsAsync(channelId);
        var contentData = await _contentDb.GetOrCreateSessionContentDataAsync(session.Id);
        var currentDifficulty = contentData.CurrentDifficulty;

        // Determine if adjustment is needed
        var (shouldAdjust, newDifficulty, reason) = EvaluateDifficultyAdjustment(currentDifficulty, metrics);

        if (!shouldAdjust) return false;

        // Apply adjustment
        var history = contentData.DifficultyHistory != null
            ? JsonSerializer.Deserialize<List<DifficultyAdjustment>>(contentData.DifficultyHistory) ?? new()
            : new List<DifficultyAdjustment>();

        history.Add(new DifficultyAdjustment
        {
            FromDifficulty = currentDifficulty,
            ToDifficulty = newDifficulty,
            AdjustedAt = DateTime.UtcNow,
            Reason = reason,
            IsAutomatic = true
        });

        contentData.CurrentDifficulty = newDifficulty;
        contentData.DifficultyLastAdjusted = DateTime.UtcNow;
        contentData.DifficultyHistory = JsonSerializer.Serialize(history);

        await _contentDb.UpdateSessionContentDataAsync(contentData);

        _logger.LogInformation("Auto-adjusted difficulty for session {SessionId} from {Old} to {New}: {Reason}",
            session.Id, currentDifficulty, newDifficulty, reason);

        return true;
    }

    /// <summary>
    /// Get comprehensive performance metrics for a session
    /// </summary>
    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var recentEvents = session.NarrativeEvents
            .OrderByDescending(e => e.RecordedAt)
            .Take(PerformanceSampleSize)
            .ToList();

        var metrics = new PerformanceMetrics
        {
            SessionId = session.Id,
            CalculatedAt = DateTime.UtcNow
        };

        // Analyze skill check success rates
        var skillEvents = recentEvents.Where(e => e.Tags?.Contains("skill") == true || e.Tags?.Contains("check") == true).ToList();
        if (skillEvents.Any())
        {
            // Parse success/failure from event descriptions
            var successes = skillEvents.Count(e => 
                e.Description.Contains("success", StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains("passed", StringComparison.OrdinalIgnoreCase));
            metrics.SkillCheckSuccessRate = (double)successes / skillEvents.Count;
        }

        // Analyze mission completion rates
        var completedMissions = session.ActiveMissions.Count(m => m.Status == MissionStatus.Completed);
        var totalMissions = session.ActiveMissions.Count;
        if (totalMissions > 0)
        {
            metrics.MissionCompletionRate = (double)completedMissions / totalMissions;
        }

        // Analyze NPC interaction outcomes
        var npcInteractions = session.NPCRelationships
            .Where(r => r.InteractionHistory != null)
            .Sum(r => r.InteractionHistory?.Split('\n').Length ?? 0);
        var positiveInteractions = session.NPCRelationships.Count(r => r.Attitude > 0);
        var totalNPCs = session.NPCRelationships.Count(r => r.IsActive);

        if (totalNPCs > 0)
        {
            metrics.PositiveInteractionRate = (double)positiveInteractions / totalNPCs;
        }

        // Analyze combat outcomes (from narrative events)
        var combatEvents = recentEvents.Where(e => e.EventType == NarrativeEventType.Combat).ToList();
        if (combatEvents.Any())
        {
            var victories = combatEvents.Count(e =>
                e.Description.Contains("victory", StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains("defeated", StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains("won", StringComparison.OrdinalIgnoreCase));
            metrics.CombatVictoryRate = (double)victories / combatEvents.Count;
        }

        // Calculate overall performance score (0-100)
        metrics.OverallScore = CalculateOverallScore(metrics);

        return metrics;
    }

    /// <summary>
    /// Evaluate whether difficulty should be adjusted
    /// </summary>
    private (bool ShouldAdjust, int NewDifficulty, string Reason) EvaluateDifficultyAdjustment(
        int currentDifficulty, PerformanceMetrics metrics)
    {
        var score = metrics.OverallScore;

        // Player struggling (score < 30)
        if (score < 30 && currentDifficulty > MinDifficulty)
        {
            var adjustment = Math.Min(2, (int)((30 - score) / 15));
            var newDifficulty = Math.Max(MinDifficulty, currentDifficulty - adjustment);
            return (true, newDifficulty, $"Player struggling (score: {score:F1}) - reducing difficulty");
        }

        // Player dominating (score > 80)
        if (score > 80 && currentDifficulty < MaxDifficulty)
        {
            var adjustment = Math.Min(2, (int)((score - 80) / 10));
            var newDifficulty = Math.Min(MaxDifficulty, currentDifficulty + adjustment);
            return (true, newDifficulty, $"Player dominating (score: {score:F1}) - increasing difficulty");
        }

        // No adjustment needed
        return (false, currentDifficulty, "Performance balanced");
    }

    /// <summary>
    /// Calculate overall performance score from metrics
    /// </summary>
    private double CalculateOverallScore(PerformanceMetrics metrics)
    {
        var weights = new Dictionary<string, (double Value, double Weight)>
        {
            ["skill"] = (metrics.SkillCheckSuccessRate * 100, 0.25),
            ["mission"] = (metrics.MissionCompletionRate * 100, 0.30),
            ["social"] = (metrics.PositiveInteractionRate * 100, 0.20),
            ["combat"] = (metrics.CombatVictoryRate * 100, 0.25)
        };

        var totalWeight = weights.Sum(w => w.Value.Weight);
        var weightedScore = weights.Sum(w => w.Value.Value * w.Value.Weight);

        return weightedScore / totalWeight;
    }

    /// <summary>
    /// Get difficulty recommendation based on current state
    /// </summary>
    private string GetDifficultyRecommendation(int currentDifficulty, PerformanceMetrics metrics)
    {
        var score = metrics.OverallScore;

        if (score < 30)
            return "Consider reducing difficulty - players may be struggling";
        if (score > 80)
            return "Consider increasing difficulty - players are performing very well";
        if (score >= 50 && score <= 70)
            return "Difficulty well-balanced for current party performance";

        return "Monitor performance trends before adjusting";
    }

    #endregion

    #region Mission Complexity Scaling

    /// <summary>
    /// Scale mission complexity based on difficulty level
    /// </summary>
    public MissionComplexity GetMissionComplexity(int difficulty)
    {
        return difficulty switch
        {
            <= 3 => new MissionComplexity
            {
                Level = ComplexityLevel.Simple,
                DecisionPoints = new Range(1, 2),
                NPCSkillLevel = "Basic",
                SecurityLevel = new Range(1, 3),
                ObstacleCount = new Range(2, 4),
                Description = "Simple mission with minimal complications"
            },
            <= 6 => new MissionComplexity
            {
                Level = ComplexityLevel.Medium,
                DecisionPoints = new Range(3, 4),
                NPCSkillLevel = "Skilled",
                SecurityLevel = new Range(3, 5),
                ObstacleCount = new Range(4, 7),
                Description = "Moderate mission with meaningful choices"
            },
            _ => new MissionComplexity
            {
                Level = ComplexityLevel.Complex,
                DecisionPoints = new Range(5, 7),
                NPCSkillLevel = "Elite",
                SecurityLevel = new Range(5, 8),
                ObstacleCount = new Range(6, 10),
                Description = "Complex mission with multiple decision points and elite opposition"
            }
        };
    }

    /// <summary>
    /// Generate scaled mission parameters based on difficulty
    /// </summary>
    public async Task<ScaledMissionParams> GenerateScaledMissionParamsAsync(ulong channelId, string missionType)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var contentData = await _contentDb.GetOrCreateSessionContentDataAsync(session.Id);
        var difficulty = contentData.CurrentDifficulty;
        var complexity = GetMissionComplexity(difficulty);

        var decisionCount = _diceService.RollDie(complexity.DecisionPoints.End - complexity.DecisionPoints.Start + 1)
            + complexity.DecisionPoints.Start - 1;
        var obstacleCount = _diceService.RollDie(complexity.ObstacleCount.End - complexity.ObstacleCount.Start + 1)
            + complexity.ObstacleCount.Start - 1;
        var securityLevel = _diceService.RollDie(complexity.SecurityLevel.End - complexity.SecurityLevel.Start + 1)
            + complexity.SecurityLevel.Start - 1;

        return new ScaledMissionParams
        {
            Difficulty = difficulty,
            Complexity = complexity,
            MissionType = missionType,
            DecisionPointCount = decisionCount,
            ObstacleCount = obstacleCount,
            SecurityLevel = securityLevel,
            NPCSkillLevel = complexity.NPCSkillLevel,
            BaseReward = CalculateScaledReward(difficulty, missionType),
            EnemyThreatLevels = GenerateEnemyThreatLevels(difficulty, obstacleCount)
        };
    }

    /// <summary>
    /// Calculate scaled reward based on difficulty
    /// </summary>
    private MissionReward CalculateScaledReward(int difficulty, string missionType)
    {
        var baseNuyen = difficulty * 3000;
        var baseKarma = difficulty + 2;

        // Adjust for mission type
        var typeMultiplier = missionType.ToLower() switch
        {
            "assassination" => 1.5,
            "extraction" => 1.3,
            "cyberdeck" => 1.2,
            "sabotage" => 1.3,
            _ => 1.0
        };

        return new MissionReward
        {
            BaseNuyen = (long)(baseNuyen * typeMultiplier),
            BaseKarma = (int)(baseKarma * typeMultiplier),
            NegotiationBuffer = (long)(baseNuyen * 0.25 * typeMultiplier),
            BonusOpportunities = GenerateBonusOpportunities(difficulty)
        };
    }

    /// <summary>
    /// Generate enemy threat levels based on difficulty
    /// </summary>
    private List<int> GenerateEnemyThreatLevels(int difficulty, int count)
    {
        var threats = new List<int>();
        var baseThreat = Math.Max(1, difficulty - 2);

        for (int i = 0; i < count; i++)
        {
            var variance = _diceService.RollDie(3) - 2; // -1, 0, or +1
            threats.Add(Math.Clamp(baseThreat + variance, 1, 8));
        }

        return threats;
    }

    /// <summary>
    /// Generate bonus opportunities based on difficulty
    /// </summary>
    private List<string> GenerateBonusOpportunities(int difficulty)
    {
        var bonuses = new List<string>();

        if (difficulty >= 3)
            bonuses.Add("Secondary objective available - bonus payment");

        if (difficulty >= 5)
            bonuses.Add("Corporate paydata on-site - extra nuyen");

        if (difficulty >= 7)
        {
            bonuses.Add("High-value target present - karma bonus");
            bonuses.Add("Intel opportunity - future mission advantage");
        }

        return bonuses;
    }

    #endregion

    #region Context-Aware Story Building

    /// <summary>
    /// Build context for procedural generation
    /// </summary>
    public async Task<ProceduralContext> BuildProceduralContextAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var contentData = await _contentDb.GetOrCreateSessionContentDataAsync(session.Id);
        var participants = await _sessionService.GetActiveParticipantsAsync(channelId);
        var npcRelationships = await _narrativeService.GetAllNPCRelationshipsAsync(channelId);
        var recentEvents = await _narrativeService.GetRecentEventsAsync(channelId, 20);
        var activeMissions = await _narrativeService.GetActiveMissionsAsync(channelId);

        var context = new ProceduralContext
        {
            SessionId = session.Id,
            CurrentDifficulty = contentData.CurrentDifficulty,
            CurrentLocation = session.CurrentLocation,
            InGameDateTime = session.InGameDateTime ?? DateTime.Now,
            CampaignArc = contentData.CurrentCampaignArc,
            ArcProgress = contentData.ArcProgress,
            PlayerCharacters = participants
                .Where(p => p.Character != null)
                .Select(p => new PlayerCharacterSummary
                {
                    CharacterId = p.CharacterId ?? 0,
                    Name = p.Character?.Name ?? "Unknown",
                    PrimaryRole = DetermineCharacterRole(p.Character),
                    TotalKarma = p.SessionKarma,
                    TotalNuyen = p.SessionNuyen
                }).ToList(),
            ExistingNPCs = npcRelationships.ToList(),
            RecentEvents = recentEvents.ToList(),
            ActiveMissions = activeMissions.ToList(),
            StoryThemes = ExtractStoryThemes(recentEvents.ToList()),
            FactionStandings = CalculateFactionStandings(npcRelationships.ToList()),
            PendingConsequences = GetPendingConsequences(contentData)
        };

        return context;
    }

    /// <summary>
    /// Generate dynamic plot hook based on context
    /// </summary>
    public async Task<PlotHook> GeneratePlotHookAsync(ulong channelId, PlotHookType hookType = PlotHookType.StoryDriven)
    {
        var context = await BuildProceduralContextAsync(channelId);
        var hook = new PlotHook
        {
            HookType = hookType,
            GeneratedAt = DateTime.UtcNow
        };

        // Generate hook based on type and context
        hook.Title = hookType switch
        {
            PlotHookType.StoryDriven => GenerateStoryDrivenHook(context),
            PlotHookType.CharacterFocused => GenerateCharacterFocusedHook(context),
            PlotHookType.WorldEvent => GenerateWorldEventHook(context),
            PlotHookType.FactionConflict => GenerateFactionConflictHook(context),
            PlotHookType.Mystery => GenerateMysteryHook(context),
            _ => GenerateStoryDrivenHook(context)
        };

        hook.Description = GenerateHookDescription(hook.Title, context);
        hook.PotentialConsequences = GeneratePotentialConsequences(hook.Title, context);
        hook.RelatedNPCs = SelectRelatedNPCs(context);
        hook.SuggestedMissions = GenerateSuggestedMissions(hook.Title, context);

        return hook;
    }

    /// <summary>
    /// Generate story-driven hook
    /// </summary>
    private string GenerateStoryDrivenHook(ProceduralContext context)
    {
        var hooks = new List<string>();

        // Based on recent events
        if (context.RecentEvents.Any(e => e.EventType == NarrativeEventType.PlotTwist))
        {
            hooks.AddRange(new[]
            {
                "The consequences of recent events ripple outward",
                "An old enemy resurfaces seeking revenge",
                "A loose end from a previous run demands attention"
            });
        }

        // Based on NPC relationships
        if (context.ExistingNPCs.Any(n => n.Attitude < -3))
        {
            hooks.AddRange(new[]
            {
                "A hostile contact makes an unexpected move",
                "Someone you've wronged is making alliances against you",
                "A former ally-turned-enemy reaches out with an offer"
            });
        }

        // Based on campaign arc
        if (!string.IsNullOrEmpty(context.CampaignArc))
        {
            hooks.AddRange(new[]
            {
                $"The {context.CampaignArc} arc takes an unexpected turn",
                $"New information surfaces about {context.CampaignArc}",
                $"An obstacle in the {context.CampaignArc} path needs addressing"
            });
        }

        // Default hooks
        hooks.AddRange(new[]
        {
            "A mysterious contact has information you need",
            "An opportunity arises that's too good to ignore",
            "A job offer comes with unusual strings attached",
            "Someone from the shadows reaches out with a proposition"
        });

        return hooks[_diceService.RollDie(hooks.Count) - 1];
    }

    /// <summary>
    /// Generate character-focused hook
    /// </summary>
    private string GenerateCharacterFocusedHook(ProceduralContext context)
    {
        if (!context.PlayerCharacters.Any())
            return "A job offer arrives that could benefit the team";

        var character = context.PlayerCharacters[_diceService.RollDie(context.PlayerCharacters.Count) - 1];

        var hooks = new List<string>
        {
            $"{character.Name}'s past catches up with the team",
            $"An opportunity arises that plays to {character.Name}'s strengths",
            $"Someone from {character.Name}'s history reaches out",
            $"A job specifically requests someone with {character.Name}'s skills"
        };

        return hooks[_diceService.RollDie(hooks.Count) - 1];
    }

    /// <summary>
    /// Generate world event hook
    /// </summary>
    private string GenerateWorldEventHook(ProceduralContext context)
    {
        var events = new[]
        {
            "Corporate tensions escalate into open conflict",
            "A gang war threatens to spill into your territory",
            "A magical phenomenon draws unwanted attention",
            "Matrix crashes cause chaos across the city",
            "A VIP announcement creates opportunity",
            "Natural disaster creates both danger and opportunity"
        };

        return events[_diceService.RollDie(events.Length) - 1];
    }

    /// <summary>
    /// Generate faction conflict hook
    /// </summary>
    private string GenerateFactionConflictHook(ProceduralContext context)
    {
        if (context.FactionStandings.Any())
        {
            var faction = context.FactionStandings
                .OrderBy(f => _diceService.RollDie(100))
                .First();

            return $"Conflict erupts involving {faction.Key} - your relationship may be tested";
        }

        var corps = new[] { "Arasaka", "Renraku", "Saeder-Krupp", "Mitsuhama", "Ares" };
        var corp = corps[_diceService.RollDie(corps.Length) - 1];

        return $"Corporate maneuvering by {corp} creates opportunities in the shadows";
    }

    /// <summary>
    /// Generate mystery hook
    /// </summary>
    private string GenerateMysteryHook(ProceduralContext context)
    {
        var mysteries = new[]
        {
            "A series of disappearances point to something sinister",
            "Strange occurrences suggest a hidden agenda",
            "Evidence of a conspiracy surfaces unexpectedly",
            "An unsolved case from the past gains new relevance",
            "Cryptic messages point to a larger mystery"
        };

        return mysteries[_diceService.RollDie(mysteries.Length) - 1];
    }

    /// <summary>
    /// Generate hook description
    /// </summary>
    private string GenerateHookDescription(string title, ProceduralContext context)
    {
        return $"**{title}**\n\n" +
               $"Location: {context.CurrentLocation}\n" +
               $"Complexity: Scaled to difficulty {context.CurrentDifficulty}\n" +
               $"Recommended Team Size: {Math.Max(3, context.PlayerCharacters.Count)}";
    }

    /// <summary>
    /// Generate potential consequences for a hook
    /// </summary>
    private List<string> GeneratePotentialConsequences(string title, ProceduralContext context)
    {
        return new List<string>
        {
            "Reputation changes with involved factions",
            "New contacts or enemies made",
            "Access to new resources or information",
            "Collateral damage affecting future opportunities"
        };
    }

    /// <summary>
    /// Select NPCs related to a plot hook
    /// </summary>
    private List<string> SelectRelatedNPCs(ProceduralContext context)
    {
        var npcs = new List<string>();

        // Include existing contacts with high relevance
        var relevantNPCs = context.ExistingNPCs
            .Where(n => n.IsActive && n.TrustLevel >= 3)
            .Take(3)
            .Select(n => n.NPCName);

        npcs.AddRange(relevantNPCs);
        return npcs;
    }

    /// <summary>
    /// Generate suggested missions for a hook
    /// </summary>
    private List<string> GenerateSuggestedMissions(string title, ProceduralContext context)
    {
        return new List<string>
        {
            "Investigation - gather more information",
            "Negotiation - meet with involved parties",
            "Extraction - remove a key asset",
            "Sabotage - disrupt enemy operations"
        };
    }

    #endregion

    #region Dynamic NPC Dialogue Generation

    /// <summary>
    /// Generate dynamic NPC dialogue with personality adaptation
    /// </summary>
    public async Task<DynamicDialogueResponse> GenerateDynamicDialogueAsync(
        ulong channelId,
        string npcName,
        string playerInput,
        DialogueSituation situation)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var relationship = await _narrativeService.GetNPCRelationshipAsync(channelId, npcName);
        var npcData = await _contentDb.GetNPCPersonalityDataAsync(session.Id, npcName);

        // Get or create personality model
        var personality = npcData != null
            ? JsonSerializer.Deserialize<NPCPersonalityModel>(npcData.PersonalityJson ?? "{}") ?? new NPCPersonalityModel()
            : new NPCPersonalityModel();

        // Adapt personality based on interaction history
        AdaptPersonalityFromHistory(personality, relationship);

        // Generate response
        var response = new DynamicDialogueResponse
        {
            NPCName = npcName,
            GeneratedAt = DateTime.UtcNow
        };

        response.OpeningLine = GenerateOpeningLine(personality, relationship, situation);
        response.MainResponse = GenerateMainResponse(personality, relationship, playerInput, situation);
        response.ClosingLine = GenerateClosingLine(personality, relationship);
        response.MoodIndicator = DetermineMoodIndicator(personality, relationship);
        response.SuggestedResponses = GenerateSuggestedResponses(personality, situation);
        response.SecretRevealed = TryRevealSecret(personality, relationship);

        // Update personality based on this interaction
        UpdatePersonalityFromInteraction(personality, playerInput, situation);
        
        // Save updated personality
        await SaveNPCPersonalityAsync(session.Id, npcName, personality);

        return response;
    }

    /// <summary>
    /// Adapt personality from interaction history
    /// </summary>
    private void AdaptPersonalityFromHistory(NPCPersonalityModel personality, NPCRelationship? relationship)
    {
        if (relationship == null) return;

        // Adapt trust tendency based on observed trust changes
        if (relationship.TrustLevel >= 5)
        {
            personality.TrustTendency = Math.Min(10, personality.TrustTendency + 0.1);
        }
        else if (relationship.TrustLevel <= 2)
        {
            personality.TrustTendency = Math.Max(0, personality.TrustTendency - 0.1);
        }

        // Adapt aggression based on attitude
        if (relationship.Attitude < -3)
        {
            personality.AggressionLevel = Math.Min(10, personality.AggressionLevel + 0.1);
        }
        else if (relationship.Attitude > 3)
        {
            personality.AggressionLevel = Math.Max(0, personality.AggressionLevel - 0.1);
        }
    }

    /// <summary>
    /// Generate opening line based on personality and relationship
    /// </summary>
    private string GenerateOpeningLine(NPCPersonalityModel personality, NPCRelationship? relationship, DialogueSituation situation)
    {
        var formality = personality.FormalityLevel;
        var trust = relationship?.TrustLevel ?? 0;

        if (situation == DialogueSituation.FirstMeeting)
        {
            return formality switch
            {
                >= 7 => "*nods formally* I don't believe we've been introduced.",
                >= 4 => "*sizes you up* New face. What brings you here?",
                _ => "*leans back* Don't see many new faces around here. Who are you?"
            };
        }

        if (trust >= 7)
        {
            return "*smiles genuinely* Good to see you again, friend.";
        }

        if (trust >= 4)
        {
            return "*nods in recognition* Back again. What do you need?";
        }

        if (trust <= 2)
        {
            return "*eyes narrow* You again. What do you want this time?";
        }

        return "*looks up* What can I do for you?";
    }

    /// <summary>
    /// Generate main response
    /// </summary>
    private string GenerateMainResponse(
        NPCPersonalityModel personality,
        NPCRelationship? relationship,
        string playerInput,
        DialogueSituation situation)
    {
        var responses = new List<string>();

        // Base responses on personality traits
        if (personality.Traits.Contains("cautious"))
        {
            responses.AddRange(new[]
            {
                "I'll need to think about that. Trust isn't given freely.",
                "That's... interesting. I'll need more information before committing.",
                "I don't make hasty decisions. Give me time to consider."
            });
        }

        if (personality.Traits.Contains("greedy"))
        {
            responses.AddRange(new[]
            {
                "Everything has a price. What's your offer?",
                "I might be able to help... for the right compensation.",
                "Let's talk numbers first. Then we can discuss details."
            });
        }

        if (personality.Traits.Contains("loyal"))
        {
            responses.AddRange(new[]
            {
                "I stand by my contacts. That hasn't changed.",
                "Loyalty goes both ways. Remember that.",
                "I've always been straight with you. Expect the same."
            });
        }

        // Add situation-specific responses
        responses.AddRange(situation switch
        {
            DialogueSituation.Negotiation => new[]
            {
                "Let's discuss terms. I'm reasonable, but I know my worth.",
                "Negotiation is an art. Let's see what we can work out.",
                "Name your price, I'll name mine. We'll meet somewhere."
            },
            DialogueSituation.Intimidation => new[]
            {
                "Threats? I've dealt with worse than you.",
                "You're making a mistake. Back off while you can.",
                "*laughs* You think that scares me?"
            },
            DialogueSituation.InformationGathering => new[]
            {
                "Information is my trade. But it costs.",
                "I might know something. Depends on what you're offering.",
                "The streets talk. I listen. What do you want to know?"
            },
            _ => new[]
            {
                "I'm listening. Make it quick.",
                "Go on. I've got time.",
                "Spit it out. What's on your mind?"
            }
        });

        return responses[_diceService.RollDie(responses.Count) - 1];
    }

    /// <summary>
    /// Generate closing line
    /// </summary>
    private string GenerateClosingLine(NPCPersonalityModel personality, NPCRelationship? relationship)
    {
        var trust = relationship?.TrustLevel ?? 0;

        if (trust >= 6)
        {
            return "Stay safe out there. You know where to find me.";
        }

        if (trust >= 3)
        {
            return "We'll talk more later. Don't do anything stupid.";
        }

        if (trust <= 1)
        {
            return "Don't waste my time again unless it's worth my while.";
        }

        return "Time will tell if this works out. We'll see.";
    }

    /// <summary>
    /// Determine mood indicator emoji
    /// </summary>
    private string DetermineMoodIndicator(NPCPersonalityModel personality, NPCRelationship? relationship)
    {
        var attitude = relationship?.Attitude ?? 0;

        return attitude switch
        {
            >= 7 => "💚",
            >= 4 => "😊",
            >= 1 => "😐",
            0 => "🤔",
            >= -3 => "😒",
            _ => "😠"
        };
    }

    /// <summary>
    /// Generate suggested player responses
    /// </summary>
    private List<string> GenerateSuggestedResponses(NPCPersonalityModel personality, DialogueSituation situation)
    {
        return situation switch
        {
            DialogueSituation.Negotiation => new List<string>
            {
                "Make a counter-offer",
                "Ask for more details",
                "Accept the terms",
                "Walk away"
            },
            DialogueSituation.Intimidation => new List<string>
            {
                "Back down and apologize",
                "Stand your ground",
                "Offer a compromise",
                "Escalate the threat"
            },
            _ => new List<string>
            {
                "Ask for more information",
                "Make a proposition",
                "Change the subject",
                "End the conversation"
            }
        };
    }

    /// <summary>
    /// Try to reveal a secret based on trust and interaction count
    /// </summary>
    private string? TryRevealSecret(NPCPersonalityModel personality, NPCRelationship? relationship)
    {
        if (relationship == null || relationship.TrustLevel < 5) return null;
        if (_diceService.RollDie(100) > 20) return null; // 20% chance

        return personality.Secrets.FirstOrDefault() ?? "There's something I haven't told anyone...";
    }

    /// <summary>
    /// Update personality based on current interaction
    /// </summary>
    private void UpdatePersonalityFromInteraction(NPCPersonalityModel personality, string playerInput, DialogueSituation situation)
    {
        var inputLower = playerInput.ToLower();

        // Detect aggression in player input
        if (inputLower.Contains("threat") || inputLower.Contains("kill") || inputLower.Contains("hurt"))
        {
            personality.AggressionLevel = Math.Min(10, personality.AggressionLevel + 0.2);
            personality.TrustTendency = Math.Max(0, personality.TrustTendency - 0.1);
        }

        // Detect friendliness
        if (inputLower.Contains("please") || inputLower.Contains("thank") || inputLower.Contains("friend"))
        {
            personality.AggressionLevel = Math.Max(0, personality.AggressionLevel - 0.1);
            personality.TrustTendency = Math.Min(10, personality.TrustTendency + 0.1);
        }

        // Increment interaction count
        personality.TotalInteractions++;
    }

    /// <summary>
    /// Save NPC personality to database
    /// </summary>
    private async Task SaveNPCPersonalityAsync(int sessionId, string npcName, NPCPersonalityModel personality)
    {
        await _contentDb.UpdateNPCPersonalityDataAsync(sessionId, npcName, JsonSerializer.Serialize(personality));
    }

    #endregion

    #region Story Evolution & Campaign Arcs

    /// <summary>
    /// Get current campaign arc status
    /// </summary>
    public async Task<CampaignArcStatus> GetCampaignArcStatusAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var contentData = await _contentDb.GetOrCreateSessionContentDataAsync(session.Id);

        var status = new CampaignArcStatus
        {
            SessionId = session.Id,
            CurrentArc = contentData.CurrentCampaignArc ?? "No Active Arc",
            ArcProgress = contentData.ArcProgress,
            ArcDescription = contentData.ArcDescription,
            StartedAt = contentData.ArcStartedAt,
            StoryArcs = contentData.StoryArcsJson != null
                ? JsonSerializer.Deserialize<List<StoryArc>>(contentData.StoryArcsJson) ?? new()
                : new()
        };

        // Calculate progress percentage
        if (status.StoryArcs.Any())
        {
            var completedArcs = status.StoryArcs.Count(a => a.IsCompleted);
            status.ProgressPercentage = (double)completedArcs / status.StoryArcs.Count * 100;
        }

        return status;
    }

    /// <summary>
    /// Start a new campaign arc
    /// </summary>
    public async Task<CampaignArcStatus> StartCampaignArcAsync(ulong channelId, string arcName, string description)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var contentData = await _contentDb.GetOrCreateSessionContentDataAsync(session.Id);

        // Archive current arc if exists
        if (!string.IsNullOrEmpty(contentData.CurrentCampaignArc))
        {
            var arcs = contentData.StoryArcsJson != null
                ? JsonSerializer.Deserialize<List<StoryArc>>(contentData.StoryArcsJson) ?? new()
                : new List<StoryArc>();

            var currentArc = arcs.FirstOrDefault(a => a.Name == contentData.CurrentCampaignArc && !a.IsCompleted);
            if (currentArc != null)
            {
                currentArc.IsCompleted = true;
                currentArc.CompletedAt = DateTime.UtcNow;
            }

            contentData.StoryArcsJson = JsonSerializer.Serialize(arcs);
        }

        // Start new arc
        contentData.CurrentCampaignArc = arcName;
        contentData.ArcDescription = description;
        contentData.ArcProgress = 0;
        contentData.ArcStartedAt = DateTime.UtcNow;

        // Add to arcs list
        var allArcs = contentData.StoryArcsJson != null
            ? JsonSerializer.Deserialize<List<StoryArc>>(contentData.StoryArcsJson) ?? new()
            : new List<StoryArc>();

        allArcs.Add(new StoryArc
        {
            Name = arcName,
            Description = description,
            StartedAt = DateTime.UtcNow,
            IsCompleted = false
        });

        contentData.StoryArcsJson = JsonSerializer.Serialize(allArcs);

        await _contentDb.UpdateSessionContentDataAsync(contentData);

        _logger.LogInformation("Started campaign arc '{ArcName}' for session {SessionId}", arcName, session.Id);

        return await GetCampaignArcStatusAsync(channelId);
    }

    /// <summary>
    /// Evolve story based on player choices and consequences
    /// </summary>
    public async Task<StoryEvolutionResult> EvolveStoryAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var contentData = await _contentDb.GetOrCreateSessionContentDataAsync(session.Id);
        var recentEvents = await _narrativeService.GetRecentEventsAsync(channelId, 10);
        var playerChoices = session.PlayerChoices.OrderByDescending(c => c.MadeAt).Take(5).ToList();

        var result = new StoryEvolutionResult
        {
            SessionId = session.Id,
            EvolvedAt = DateTime.UtcNow
        };

        // Analyze recent choices for patterns
        var choicePatterns = AnalyzeChoicePatterns(playerChoices);
        result.DetectedPatterns = choicePatterns;

        // Generate story evolution based on patterns
        result.EvolutionDescription = GenerateEvolutionDescription(choicePatterns, recentEvents.ToList());

        // Update arc progress if applicable
        if (!string.IsNullOrEmpty(contentData.CurrentCampaignArc))
        {
            var progressIncrease = CalculateProgressIncrease(playerChoices, recentEvents.ToList());
            contentData.ArcProgress += progressIncrease;
            result.ArcProgressIncrease = progressIncrease;

            // Check for arc completion
            if (contentData.ArcProgress >= 100)
            {
                result.ArcCompleted = true;
                result.ArcCompletionSummary = GenerateArcCompletionSummary(contentData);
                contentData.ArcProgress = 100;
            }

            await _contentDb.UpdateSessionContentDataAsync(contentData);
        }

        // Generate new plot hooks based on evolution
        result.NewPlotHooks = await GenerateEvolvedPlotHooksAsync(channelId, choicePatterns);

        // Update story themes
        result.UpdatedThemes = UpdateStoryThemes(contentData, recentEvents.ToList());
        contentData.StoryThemesJson = JsonSerializer.Serialize(result.UpdatedThemes);
        await _contentDb.UpdateSessionContentDataAsync(contentData);

        return result;
    }

    /// <summary>
    /// Analyze patterns in player choices
    /// </summary>
    private List<ChoicePattern> AnalyzeChoicePatterns(List<PlayerChoice> choices)
    {
        var patterns = new List<ChoicePattern>();

        if (!choices.Any()) return patterns;

        // Detect aggressive tendencies
        var aggressiveChoices = choices.Count(c =>
            c.PlayerDecision.Contains("attack", StringComparison.OrdinalIgnoreCase) ||
            c.PlayerDecision.Contains("kill", StringComparison.OrdinalIgnoreCase) ||
            c.PlayerDecision.Contains("threat", StringComparison.OrdinalIgnoreCase));

        if (aggressiveChoices > choices.Count / 2)
        {
            patterns.Add(new ChoicePattern
            {
                PatternType = "Aggressive",
                Frequency = (double)aggressiveChoices / choices.Count,
                Description = "Team tends toward violent solutions"
            });
        }

        // Detect diplomatic tendencies
        var diplomaticChoices = choices.Count(c =>
            c.PlayerDecision.Contains("negotiate", StringComparison.OrdinalIgnoreCase) ||
            c.PlayerDecision.Contains("talk", StringComparison.OrdinalIgnoreCase) ||
            c.PlayerDecision.Contains("persuade", StringComparison.OrdinalIgnoreCase));

        if (diplomaticChoices > choices.Count / 3)
        {
            patterns.Add(new ChoicePattern
            {
                PatternType = "Diplomatic",
                Frequency = (double)diplomaticChoices / choices.Count,
                Description = "Team prefers diplomatic solutions"
            });
        }

        // Detect stealth tendencies
        var stealthChoices = choices.Count(c =>
            c.PlayerDecision.Contains("sneak", StringComparison.OrdinalIgnoreCase) ||
            c.PlayerDecision.Contains("stealth", StringComparison.OrdinalIgnoreCase) ||
            c.PlayerDecision.Contains("avoid", StringComparison.OrdinalIgnoreCase));

        if (stealthChoices > choices.Count / 3)
        {
            patterns.Add(new ChoicePattern
            {
                PatternType = "Stealthy",
                Frequency = (double)stealthChoices / choices.Count,
                Description = "Team prefers stealth and avoidance"
            });
        }

        return patterns;
    }

    /// <summary>
    /// Generate evolution description based on patterns and events
    /// </summary>
    private string GenerateEvolutionDescription(List<ChoicePattern> patterns, List<NarrativeEvent> recentEvents)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("**Story Evolution Analysis**\n");

        if (patterns.Any())
        {
            sb.AppendLine("**Detected Patterns:**");
            foreach (var pattern in patterns)
            {
                sb.AppendLine($"• {pattern.PatternType}: {pattern.Frequency:P0} - {pattern.Description}");
            }
            sb.AppendLine();
        }

        // Generate narrative consequences
        if (patterns.Any(p => p.PatternType == "Aggressive"))
        {
            sb.AppendLine("Your reputation for violence precedes you. Some contacts are more wary, others more respectful.");
        }

        if (patterns.Any(p => p.PatternType == "Diplomatic"))
        {
            sb.AppendLine("Your measured approach has built trust. New diplomatic options may open up.");
        }

        if (patterns.Any(p => p.PatternType == "Stealthy"))
        {
            sb.AppendLine("Your team's reputation for clean, quiet work attracts certain employers.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Calculate progress increase for campaign arc
    /// </summary>
    private int CalculateProgressIncrease(List<PlayerChoice> choices, List<NarrativeEvent> events)
    {
        var baseIncrease = 5;

        // Bonus for completed missions
        var completedMissions = events.Count(e => e.Title.Contains("completed", StringComparison.OrdinalIgnoreCase));
        baseIncrease += completedMissions * 10;

        // Bonus for resolved choices
        baseIncrease += choices.Count(c => c.IsResolved) * 2;

        // Cap the increase
        return Math.Min(baseIncrease, 25);
    }

    /// <summary>
    /// Generate arc completion summary
    /// </summary>
    private string GenerateArcCompletionSummary(SessionContentData contentData)
    {
        return $"**Campaign Arc Complete: {contentData.CurrentCampaignArc}**\n\n" +
               $"{contentData.ArcDescription}\n\n" +
               "The team has reached a significant milestone in this story arc.";
    }

    /// <summary>
    /// Generate evolved plot hooks based on story evolution
    /// </summary>
    private async Task<List<PlotHook>> GenerateEvolvedPlotHooksAsync(ulong channelId, List<ChoicePattern> patterns)
    {
        var hooks = new List<PlotHook>();

        // Generate hooks based on detected patterns
        foreach (var pattern in patterns)
        {
            var hookType = pattern.PatternType switch
            {
                "Aggressive" => PlotHookType.FactionConflict,
                "Diplomatic" => PlotHookType.StoryDriven,
                "Stealthy" => PlotHookType.Mystery,
                _ => PlotHookType.StoryDriven
            };

            var hook = await GeneratePlotHookAsync(channelId, hookType);
            hooks.Add(hook);
        }

        return hooks.Take(3).ToList();
    }

    /// <summary>
    /// Update story themes based on recent events
    /// </summary>
    private List<string> UpdateStoryThemes(SessionContentData contentData, List<NarrativeEvent> recentEvents)
    {
        var themes = contentData.StoryThemesJson != null
            ? JsonSerializer.Deserialize<List<string>>(contentData.StoryThemesJson) ?? new()
            : new List<string>();

        // Extract themes from recent events
        foreach (var evt in recentEvents)
        {
            if (evt.Tags != null)
            {
                var eventTags = evt.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t));

                foreach (var tag in eventTags)
                {
                    if (!themes.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        themes.Add(tag);
                    }
                }
            }
        }

        // Keep only most relevant themes
        return themes.Distinct().Take(10).ToList();
    }

    #endregion

    #region Learning System (Optional ML Integration)

    /// <summary>
    /// Get AI learning status for a session
    /// </summary>
    public async Task<LearningStatus> GetLearningStatusAsync(ulong channelId)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var contentData = await _contentDb.GetOrCreateSessionContentDataAsync(session.Id);
        var npcPersonalities = await _contentDb.GetSessionNPCPersonalityDataAsync(session.Id);

        var status = new LearningStatus
        {
            SessionId = session.Id,
            LearningEnabled = true,
            NPCLearningActive = npcPersonalities.Any(),
            TotalNPCProfilesLearned = npcPersonalities.Count,
            StoryPreferenceTracking = contentData.StoryPreferencesJson != null,
            LastLearningUpdate = contentData.LastLearningUpdate
        };

        // Extract story preferences if available
        if (contentData.StoryPreferencesJson != null)
        {
            status.StoryPreferences = JsonSerializer.Deserialize<StoryPreferences>(contentData.StoryPreferencesJson)
                ?? new StoryPreferences();
        }
        else
        {
            status.StoryPreferences = new StoryPreferences();
        }

        // Calculate learning metrics
        status.Metrics = CalculateLearningMetrics(contentData, npcPersonalities);

        return status;
    }

    /// <summary>
    /// Record player choice for learning
    /// </summary>
    public async Task RecordChoiceForLearningAsync(ulong channelId, PlayerChoice choice)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null) return;

        var contentData = await _contentDb.GetOrCreateSessionContentDataAsync(session.Id);
        var preferences = contentData.StoryPreferencesJson != null
            ? JsonSerializer.Deserialize<StoryPreferences>(contentData.StoryPreferencesJson) ?? new()
            : new StoryPreferences();

        // Analyze choice and update preferences
        UpdatePreferencesFromChoice(preferences, choice);

        contentData.StoryPreferencesJson = JsonSerializer.Serialize(preferences);
        contentData.LastLearningUpdate = DateTime.UtcNow;

        await _contentDb.UpdateSessionContentDataAsync(contentData);
    }

    /// <summary>
    /// Update story preferences from a player choice
    /// </summary>
    private void UpdatePreferencesFromChoice(StoryPreferences preferences, PlayerChoice choice)
    {
        var decision = choice.PlayerDecision.ToLower();

        // Track mission type preferences
        if (decision.Contains("combat") || decision.Contains("fight"))
            preferences.CombatPreference = Math.Min(10, preferences.CombatPreference + 0.1);
        else if (decision.Contains("talk") || decision.Contains("negotiate"))
            preferences.SocialPreference = Math.Min(10, preferences.SocialPreference + 0.1);
        else if (decision.Contains("stealth") || decision.Contains("sneak"))
            preferences.StealthPreference = Math.Min(10, preferences.StealthPreference + 0.1);
        else if (decision.Contains("hack") || decision.Contains("matrix"))
            preferences.TechPreference = Math.Min(10, preferences.TechPreference + 0.1);

        // Track risk tolerance
        if (decision.Contains("risky") || decision.Contains("direct"))
            preferences.RiskTolerance = Math.Min(10, preferences.RiskTolerance + 0.1);
        else if (decision.Contains("careful") || decision.Contains("cautious"))
            preferences.RiskTolerance = Math.Max(0, preferences.RiskTolerance - 0.1);

        // Track moral alignment
        if (decision.Contains("help") || decision.Contains("save"))
            preferences.HeroicTendency = Math.Min(10, preferences.HeroicTendency + 0.1);
        else if (decision.Contains("kill") || decision.Contains("betray"))
            preferences.HeroicTendency = Math.Max(0, preferences.HeroicTendency - 0.1);

        preferences.TotalChoicesRecorded++;
    }

    /// <summary>
    /// Calculate learning metrics
    /// </summary>
    private LearningMetrics CalculateLearningMetrics(SessionContentData contentData, List<NPCPersonalityData> npcData)
    {
        var metrics = new LearningMetrics();

        // NPC adaptation metrics
        if (npcData.Any())
        {
            metrics.AverageNPCAdaptation = npcData.Average(n => 
                JsonSerializer.Deserialize<NPCPersonalityModel>(n.PersonalityJson ?? "{}")?.TotalInteractions ?? 0);
        }

        // Story preference confidence
        if (contentData.StoryPreferencesJson != null)
        {
            var prefs = JsonSerializer.Deserialize<StoryPreferences>(contentData.StoryPreferencesJson);
            if (prefs != null)
            {
                metrics.PreferenceConfidence = Math.Min(100, prefs.TotalChoicesRecorded * 5);
            }
        }

        // Learning effectiveness score
        metrics.EffectivenessScore = (metrics.AverageNPCAdaptation * 10 + metrics.PreferenceConfidence) / 2;

        return metrics;
    }

    #endregion

    #region Content Regeneration

    /// <summary>
    /// Regenerate content with new parameters
    /// </summary>
    public async Task<RegeneratedContent> RegenerateContentAsync(
        ulong channelId,
        ContentType contentType,
        RegenerationParameters parameters)
    {
        var session = await _sessionService.GetActiveSessionAsync(channelId);
        if (session == null)
            throw new InvalidOperationException("No active session");

        var result = new RegeneratedContent
        {
            ContentType = contentType,
            RegeneratedAt = DateTime.UtcNow,
            Parameters = parameters
        };

        switch (contentType)
        {
            case ContentType.Mission:
                result.Content = await RegenerateMissionAsync(channelId, parameters);
                break;
            case ContentType.NPC:
                result.Content = await RegenerateNPCAsync(channelId, parameters);
                break;
            case ContentType.PlotHook:
                result.Content = await RegeneratePlotHookAsync(channelId, parameters);
                break;
            case ContentType.Encounter:
                result.Content = await RegenerateEncounterAsync(channelId, parameters);
                break;
        }

        // Store regeneration for future reference
        await _contentDb.AddContentRegenerationAsync(session.Id, contentType, result.Content);

        return result;
    }

    /// <summary>
    /// Regenerate mission with new parameters
    /// </summary>
    private async Task<string> RegenerateMissionAsync(ulong channelId, RegenerationParameters parameters)
    {
        var missionType = parameters.CustomParameters.GetValueOrDefault("missionType", "investigation");
        var scaledParams = await GenerateScaledMissionParamsAsync(channelId, missionType?.ToString() ?? "investigation");

        return JsonSerializer.Serialize(scaledParams, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Regenerate NPC with new parameters
    /// </summary>
    private async Task<string> RegenerateNPCAsync(ulong channelId, RegenerationParameters parameters)
    {
        var npcRole = parameters.CustomParameters.GetValueOrDefault("role", "contact");
        var npc = _gmService.GenerateNPC(npcRole?.ToString() ?? "contact");

        return npc;
    }

    /// <summary>
    /// Regenerate plot hook with new parameters
    /// </summary>
    private async Task<string> RegeneratePlotHookAsync(ulong channelId, RegenerationParameters parameters)
    {
        var hookType = Enum.Parse<PlotHookType>(parameters.CustomParameters.GetValueOrDefault("hookType", "StoryDriven")?.ToString() ?? "StoryDriven");
        var hook = await GeneratePlotHookAsync(channelId, hookType);

        return JsonSerializer.Serialize(hook, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Regenerate encounter with new parameters
    /// </summary>
    private async Task<string> RegenerateEncounterAsync(ulong channelId, RegenerationParameters parameters)
    {
        var encounter = await _storyService.GenerateEncounterAsync(channelId, new EncounterTrigger
        {
            Type = parameters.CustomParameters.GetValueOrDefault("encounterType", "combat")?.ToString() ?? "combat",
            Count = Convert.ToInt32(parameters.CustomParameters.GetValueOrDefault("count", 3))
        });

        return JsonSerializer.Serialize(encounter, new JsonSerializerOptions { WriteIndented = true });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Determine character role from character sheet
    /// </summary>
    private string DetermineCharacterRole(ShadowrunCharacter? character)
    {
        if (character == null) return "Unknown";

        // Check for awakened
        if (character.Magic > 0) return "Mage";

        // Check for cyberware
        var hasCyberware = character.Cyberware?.Any() == true;
        var hasDeck = character.Gear?.Any(g => g.Name.Contains("cyberdeck", StringComparison.OrdinalIgnoreCase)) == true;

        // Check skills
        var combatSkills = character.Skills?.Count(s =>
            s.SkillName.Contains("firearms", StringComparison.OrdinalIgnoreCase) ||
            s.SkillName.Contains("combat", StringComparison.OrdinalIgnoreCase)) ?? 0;

        var techSkills = character.Skills?.Count(s =>
            s.SkillName.Contains("computers", StringComparison.OrdinalIgnoreCase) ||
            s.SkillName.Contains("electronics", StringComparison.OrdinalIgnoreCase)) ?? 0;

        if (hasDeck && techSkills > 0) return "Decker";
        if (hasCyberware && combatSkills > 0) return "Street Samurai";
        if (combatSkills > 0) return "Combat Specialist";

        return "Specialist";
    }

    /// <summary>
    /// Extract story themes from recent events
    /// </summary>
    private List<string> ExtractStoryThemes(List<NarrativeEvent> events)
    {
        var themes = new List<string>();

        foreach (var evt in events)
        {
            if (!string.IsNullOrEmpty(evt.Tags))
            {
                var tags = evt.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLower());
                themes.AddRange(tags);
            }
        }

        return themes.Distinct().Take(5).ToList();
    }

    /// <summary>
    /// Calculate faction standings from NPC relationships
    /// </summary>
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

    /// <summary>
    /// Get pending consequences from content data
    /// </summary>
    private List<PendingConsequence> GetPendingConsequences(SessionContentData contentData)
    {
        if (string.IsNullOrEmpty(contentData.PendingConsequencesJson))
            return new List<PendingConsequence>();

        return JsonSerializer.Deserialize<List<PendingConsequence>>(contentData.PendingConsequencesJson)
            ?? new List<PendingConsequence>();
    }

    #endregion
}

#region Data Transfer Objects

/// <summary>
/// Difficulty status information
/// </summary>
public class DifficultyStatus
{
    public int SessionId { get; set; }
    public int CurrentDifficulty { get; set; }
    public double PerformanceScore { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public PerformanceMetrics Metrics { get; set; } = new();
    public DateTime? LastAdjustedAt { get; set; }
    public List<DifficultyAdjustment> AdjustmentHistory { get; set; } = new();
}

/// <summary>
/// Difficulty adjustment record
/// </summary>
public class DifficultyAdjustment
{
    public int FromDifficulty { get; set; }
    public int ToDifficulty { get; set; }
    public DateTime AdjustedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsAutomatic { get; set; }
}

/// <summary>
/// Performance metrics for difficulty calculation
/// </summary>
public class PerformanceMetrics
{
    public int SessionId { get; set; }
    public DateTime CalculatedAt { get; set; }
    public double SkillCheckSuccessRate { get; set; }
    public double MissionCompletionRate { get; set; }
    public double PositiveInteractionRate { get; set; }
    public double CombatVictoryRate { get; set; }
    public double OverallScore { get; set; }
}

/// <summary>
/// Mission complexity configuration
/// </summary>
public class MissionComplexity
{
    public ComplexityLevel Level { get; set; }
    public Range DecisionPoints { get; set; }
    public string NPCSkillLevel { get; set; } = "Basic";
    public Range SecurityLevel { get; set; }
    public Range ObstacleCount { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Complexity level enumeration
/// </summary>
public enum ComplexityLevel
{
    Simple,
    Medium,
    Complex
}

/// <summary>
/// Range for random generation
/// </summary>
public class Range
{
    public int Start { get; set; }
    public int End { get; set; }

    public Range() { }

    public Range(int start, int end)
    {
        Start = start;
        End = end;
    }
}

/// <summary>
/// Scaled mission parameters
/// </summary>
public class ScaledMissionParams
{
    public int Difficulty { get; set; }
    public MissionComplexity Complexity { get; set; } = new();
    public string MissionType { get; set; } = "investigation";
    public int DecisionPointCount { get; set; }
    public int ObstacleCount { get; set; }
    public int SecurityLevel { get; set; }
    public string NPCSkillLevel { get; set; } = "Basic";
    public MissionReward BaseReward { get; set; } = new();
    public List<int> EnemyThreatLevels { get; set; } = new();
}

/// <summary>
/// Procedural generation context
/// </summary>
public class ProceduralContext
{
    public int SessionId { get; set; }
    public int CurrentDifficulty { get; set; }
    public string CurrentLocation { get; set; } = "Seattle";
    public DateTime InGameDateTime { get; set; }
    public string? CampaignArc { get; set; }
    public int ArcProgress { get; set; }
    public List<PlayerCharacterSummary> PlayerCharacters { get; set; } = new();
    public List<NPCRelationship> ExistingNPCs { get; set; } = new();
    public List<NarrativeEvent> RecentEvents { get; set; } = new();
    public List<ActiveMission> ActiveMissions { get; set; } = new();
    public List<string> StoryThemes { get; set; } = new();
    public Dictionary<string, int> FactionStandings { get; set; } = new();
    public List<PendingConsequence> PendingConsequences { get; set; } = new();
}

/// <summary>
/// Pending consequence
/// </summary>
public class PendingConsequence
{
    public string Description { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public bool IsResolved { get; set; }
    public string? Resolution { get; set; }
}

/// <summary>
/// Plot hook for story generation
/// </summary>
public class PlotHook
{
    public PlotHookType HookType { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> PotentialConsequences { get; set; } = new();
    public List<string> RelatedNPCs { get; set; } = new();
    public List<string> SuggestedMissions { get; set; } = new();
}

/// <summary>
/// Plot hook type enumeration
/// </summary>
public enum PlotHookType
{
    StoryDriven,
    CharacterFocused,
    WorldEvent,
    FactionConflict,
    Mystery
}

/// <summary>
/// Dynamic dialogue response
/// </summary>
public class DynamicDialogueResponse
{
    public string NPCName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string OpeningLine { get; set; } = string.Empty;
    public string MainResponse { get; set; } = string.Empty;
    public string ClosingLine { get; set; } = string.Empty;
    public string MoodIndicator { get; set; } = "😐";
    public List<string> SuggestedResponses { get; set; } = new();
    public string? SecretRevealed { get; set; }
}

/// <summary>
/// Dialogue situation enumeration
/// </summary>
public enum DialogueSituation
{
    FirstMeeting,
    Negotiation,
    Intimidation,
    InformationGathering,
    Friendly,
    Hostile
}

/// <summary>
/// NPC personality model for learning
/// </summary>
public class NPCPersonalityModel
{
    public double TrustTendency { get; set; } = 5.0;
    public double AggressionLevel { get; set; } = 3.0;
    public double FormalityLevel { get; set; } = 5.0;
    public int TotalInteractions { get; set; }
    public List<string> Traits { get; set; } = new() { "cautious", "pragmatic" };
    public List<string> Secrets { get; set; } = new();
}

/// <summary>
/// Campaign arc status
/// </summary>
public class CampaignArcStatus
{
    public int SessionId { get; set; }
    public string CurrentArc { get; set; } = "No Active Arc";
    public int ArcProgress { get; set; }
    public double ProgressPercentage { get; set; }
    public string? ArcDescription { get; set; }
    public DateTime? StartedAt { get; set; }
    public List<StoryArc> StoryArcs { get; set; } = new();
}

/// <summary>
/// Story arc record
/// </summary>
public class StoryArc
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
}

/// <summary>
/// Story evolution result
/// </summary>
public class StoryEvolutionResult
{
    public int SessionId { get; set; }
    public DateTime EvolvedAt { get; set; }
    public List<ChoicePattern> DetectedPatterns { get; set; } = new();
    public string EvolutionDescription { get; set; } = string.Empty;
    public int ArcProgressIncrease { get; set; }
    public bool ArcCompleted { get; set; }
    public string? ArcCompletionSummary { get; set; }
    public List<PlotHook> NewPlotHooks { get; set; } = new();
    public List<string> UpdatedThemes { get; set; } = new();
}

/// <summary>
/// Choice pattern detection
/// </summary>
public class ChoicePattern
{
    public string PatternType { get; set; } = string.Empty;
    public double Frequency { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// AI learning status
/// </summary>
public class LearningStatus
{
    public int SessionId { get; set; }
    public bool LearningEnabled { get; set; }
    public bool NPCLearningActive { get; set; }
    public int TotalNPCProfilesLearned { get; set; }
    public bool StoryPreferenceTracking { get; set; }
    public DateTime? LastLearningUpdate { get; set; }
    public StoryPreferences StoryPreferences { get; set; } = new();
    public LearningMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Story preferences learned from player choices
/// </summary>
public class StoryPreferences
{
    public double CombatPreference { get; set; } = 5.0;
    public double SocialPreference { get; set; } = 5.0;
    public double StealthPreference { get; set; } = 5.0;
    public double TechPreference { get; set; } = 5.0;
    public double RiskTolerance { get; set; } = 5.0;
    public double HeroicTendency { get; set; } = 5.0;
    public int TotalChoicesRecorded { get; set; }
}

/// <summary>
/// Learning metrics
/// </summary>
public class LearningMetrics
{
    public double AverageNPCAdaptation { get; set; }
    public double PreferenceConfidence { get; set; }
    public double EffectivenessScore { get; set; }
}

/// <summary>
/// Regenerated content result
/// </summary>
public class RegeneratedContent
{
    public ContentType ContentType { get; set; }
    public DateTime RegeneratedAt { get; set; }
    public RegenerationParameters Parameters { get; set; } = new();
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Content type enumeration
/// </summary>
public enum ContentType
{
    Mission,
    NPC,
    PlotHook,
    Encounter
}

/// <summary>
/// Regeneration parameters
/// </summary>
public class RegenerationParameters
{
    public bool UseCurrentContext { get; set; } = true;
    public int? OverrideDifficulty { get; set; }
    public Dictionary<string, object> CustomParameters { get; set; } = new();
}

#endregion
