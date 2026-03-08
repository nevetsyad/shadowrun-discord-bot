using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ShadowrunDiscordBot.Models;

/// <summary>
/// Persisted mission definition for autonomous mission execution
/// </summary>
public class MissionDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameSessionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string MissionType { get; set; } = "investigation";

    /// <summary>
    /// Complexity level (1-6)
    /// </summary>
    public int Complexity { get; set; } = 3;

    /// <summary>
    /// When this mission was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current mission status
    /// </summary>
    [Required]
    public MissionStatus Status { get; set; } = MissionStatus.Planning;

    /// <summary>
    /// Current execution stage
    /// </summary>
    [Required]
    public int CurrentStage { get; set; } = 0; // Maps to MissionStage enum

    #region Mission Components (JSON serialized)

    /// <summary>
    /// Johnson details (JSON serialized)
    /// </summary>
    public string? JohnsonJson { get; set; }

    /// <summary>
    /// Primary objective (JSON serialized)
    /// </summary>
    public string? ObjectiveJson { get; set; }

    /// <summary>
    /// Locations (JSON serialized list)
    /// </summary>
    public string? LocationsJson { get; set; }

    /// <summary>
    /// Obstacles (JSON serialized list)
    /// </summary>
    public string? ObstaclesJson { get; set; }

    /// <summary>
    /// NPCs (JSON serialized list)
    /// </summary>
    public string? NPCsJson { get; set; }

    /// <summary>
    /// Reward details (JSON serialized)
    /// </summary>
    public string? RewardJson { get; set; }

    /// <summary>
    /// Plot twist (JSON serialized, optional)
    /// </summary>
    public string? TwistJson { get; set; }

    /// <summary>
    /// Decision points (JSON serialized list)
    /// </summary>
    public string? DecisionPointsJson { get; set; }

    /// <summary>
    /// Intel opportunities (JSON serialized list)
    /// </summary>
    public string? IntelJson { get; set; }

    /// <summary>
    /// Mission deadline
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Completed decision IDs (JSON serialized list)
    /// </summary>
    public string? CompletedDecisionsJson { get; set; }

    /// <summary>
    /// Accumulated consequences (JSON serialized list)
    /// </summary>
    public string? ConsequencesJson { get; set; }

    #endregion

    #region Navigation Properties

    [Required]
    public virtual GameSession GameSession { get; set; } = null!;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Serialize mission data to JSON fields
    /// </summary>
    public void SerializeData(
        MissionJohnson johnson,
        MissionObjective objective,
        List<MissionLocation> locations,
        List<MissionObstacle> obstacles,
        List<MissionNPC> npcs,
        MissionReward reward,
        MissionTwist? twist,
        List<MissionDecisionPoint> decisions,
        List<string> intel)
    {
        var options = new JsonSerializerOptions { WriteIndented = false };

        JohnsonJson = JsonSerializer.Serialize(johnson, options);
        ObjectiveJson = JsonSerializer.Serialize(objective, options);
        LocationsJson = JsonSerializer.Serialize(locations, options);
        ObstaclesJson = JsonSerializer.Serialize(obstacles, options);
        NPCsJson = JsonSerializer.Serialize(npcs, options);
        RewardJson = JsonSerializer.Serialize(reward, options);
        TwistJson = twist != null ? JsonSerializer.Serialize(twist, options) : null;
        DecisionPointsJson = JsonSerializer.Serialize(decisions, options);
        IntelJson = JsonSerializer.Serialize(intel, options);
    }

    /// <summary>
    /// Deserialize Johnson from JSON
    /// </summary>
    public MissionJohnson? GetJohnson()
    {
        return string.IsNullOrEmpty(JohnsonJson) 
            ? null 
            : JsonSerializer.Deserialize<MissionJohnson>(JohnsonJson);
    }

    /// <summary>
    /// Deserialize objective from JSON
    /// </summary>
    public MissionObjective? GetObjective()
    {
        return string.IsNullOrEmpty(ObjectiveJson)
            ? null
            : JsonSerializer.Deserialize<MissionObjective>(ObjectiveJson);
    }

    /// <summary>
    /// Deserialize locations from JSON
    /// </summary>
    public List<MissionLocation>? GetLocations()
    {
        return string.IsNullOrEmpty(LocationsJson)
            ? null
            : JsonSerializer.Deserialize<List<MissionLocation>>(LocationsJson);
    }

    /// <summary>
    /// Deserialize obstacles from JSON
    /// </summary>
    public List<MissionObstacle>? GetObstacles()
    {
        return string.IsNullOrEmpty(ObstaclesJson)
            ? null
            : JsonSerializer.Deserialize<List<MissionObstacle>>(ObstaclesJson);
    }

    /// <summary>
    /// Deserialize NPCs from JSON
    /// </summary>
    public List<MissionNPC>? GetNPCs()
    {
        return string.IsNullOrEmpty(NPCsJson)
            ? null
            : JsonSerializer.Deserialize<List<MissionNPC>>(NPCsJson);
    }

    /// <summary>
    /// Deserialize reward from JSON
    /// </summary>
    public MissionReward? GetReward()
    {
        return string.IsNullOrEmpty(RewardJson)
            ? null
            : JsonSerializer.Deserialize<MissionReward>(RewardJson);
    }

    /// <summary>
    /// Deserialize twist from JSON
    /// </summary>
    public MissionTwist? GetTwist()
    {
        return string.IsNullOrEmpty(TwistJson)
            ? null
            : JsonSerializer.Deserialize<MissionTwist>(TwistJson);
    }

    /// <summary>
    /// Deserialize decision points from JSON
    /// </summary>
    public List<MissionDecisionPoint>? GetDecisionPoints()
    {
        return string.IsNullOrEmpty(DecisionPointsJson)
            ? null
            : JsonSerializer.Deserialize<List<MissionDecisionPoint>>(DecisionPointsJson);
    }

    /// <summary>
    /// Deserialize intel opportunities from JSON
    /// </summary>
    public List<string>? GetIntel()
    {
        return string.IsNullOrEmpty(IntelJson)
            ? null
            : JsonSerializer.Deserialize<List<string>>(IntelJson);
    }

    /// <summary>
    /// Get completed decisions
    /// </summary>
    public List<string> GetCompletedDecisions()
    {
        return string.IsNullOrEmpty(CompletedDecisionsJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(CompletedDecisionsJson) ?? new List<string>();
    }

    /// <summary>
    /// Set completed decisions
    /// </summary>
    public void SetCompletedDecisions(List<string> decisions)
    {
        CompletedDecisionsJson = JsonSerializer.Serialize(decisions);
    }

    /// <summary>
    /// Get accumulated consequences
    /// </summary>
    public List<MissionConsequence> GetConsequences()
    {
        return string.IsNullOrEmpty(ConsequencesJson)
            ? new List<MissionConsequence>()
            : JsonSerializer.Deserialize<List<MissionConsequence>>(ConsequencesJson) ?? new List<MissionConsequence>();
    }

    /// <summary>
    /// Set accumulated consequences
    /// </summary>
    public void SetConsequences(List<MissionConsequence> consequences)
    {
        ConsequencesJson = JsonSerializer.Serialize(consequences);
    }

    #endregion
}

/// <summary>
/// Johnson (mission giver) details
/// </summary>
public class MissionJohnson
{
    public string Name { get; set; } = "Mr. Johnson";
    public string Role { get; set; } = "Fixer";
    public string? Organization { get; set; }
    public int Attitude { get; set; }
    public int TrustLevel { get; set; }
    public bool IsExistingContact { get; set; }
    public string MeetingLocation { get; set; } = "Undisclosed location";
    public string NegotiationStyle { get; set; } = "Professional";
}

/// <summary>
/// Mission objective details
/// </summary>
public class MissionObjective
{
    public string MissionType { get; set; } = "investigation";
    public string Title { get; set; } = "Complete the mission";
    public string Description { get; set; } = string.Empty;
    public string SuccessCriteria { get; set; } = string.Empty;
    public string PartialSuccessCriteria { get; set; } = string.Empty;
    public string FailureConditions { get; set; } = string.Empty;
}

/// <summary>
/// Mission location details
/// </summary>
public class MissionLocation
{
    public string Name { get; set; } = "Unknown Location";
    public string Type { get; set; } = "Urban";
    public int SecurityLevel { get; set; } = 3;
    public List<string> EntryPoints { get; set; } = new();
    public List<string> KeyAreas { get; set; } = new();
    public bool IsPrimary { get; set; }
    public string? Purpose { get; set; }
}

/// <summary>
/// Mission obstacle/challenge
/// </summary>
public class MissionObstacle
{
    public string ObstacleType { get; set; } = "security";
    public string Description { get; set; } = string.Empty;
    public int DifficultyRating { get; set; } = 3;
    public bool CanBeAvoided { get; set; } = true;
    public List<string> AlternativeSolutions { get; set; } = new();
    public int Position { get; set; }
}

/// <summary>
/// NPC in mission context
/// </summary>
public class MissionNPC
{
    public string Name { get; set; } = "Unknown";
    public string Role { get; set; } = "Contact";
    public string? Organization { get; set; }
    public int Attitude { get; set; }
    public int TrustLevel { get; set; }
    public bool IsExistingContact { get; set; }
    public List<string> InteractionHistory { get; set; } = new();
    public List<string> DialogueHints { get; set; } = new();
    public int ThreatLevel { get; set; }
    public bool IsHostile { get; set; }
    public bool IsPrimary { get; set; }
}

/// <summary>
/// Mission reward details
/// </summary>
public class MissionReward
{
    public long BaseNuyen { get; set; }
    public long NegotiationBuffer { get; set; }
    public int BaseKarma { get; set; }
    public List<string> BonusOpportunities { get; set; } = new();
}

/// <summary>
/// Mission plot twist
/// </summary>
public class MissionTwist
{
    public string TwistType { get; set; } = "Unexpected";
    public string TriggerCondition { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RevealedAt { get; set; } // Maps to MissionStage enum
    public int ImpactLevel { get; set; }
}

/// <summary>
/// Decision point in mission
/// </summary>
public class MissionDecisionPoint
{
    public string DecisionId { get; set; } = string.Empty;
    public int Stage { get; set; } // Maps to MissionStage enum
    public string Context { get; set; } = string.Empty;
    public List<DecisionOption> Options { get; set; } = new();
    public List<MissionConsequence> Consequences { get; set; } = new();
}

/// <summary>
/// Option at a decision point
/// </summary>
public class DecisionOption
{
    public string OptionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RiskLevel { get; set; }
    public float SuccessChance { get; set; }
    public string ConsequencePreview { get; set; } = string.Empty;
}

/// <summary>
/// Consequence of a decision
/// </summary>
public class MissionConsequence
{
    public int Type { get; set; } // Maps to ConsequenceType enum
    public string Description { get; set; } = string.Empty;
    public int Severity { get; set; }
    public List<string> AffectedEntities { get; set; } = new();
}

/// <summary>
/// Type of consequence
/// </summary>
public enum ConsequenceType
{
    Success = 0,
    PartialSuccess = 1,
    Failure = 2,
    Complication = 3,
    Opportunity = 4
}

/// <summary>
/// Mission execution stages
/// </summary>
public enum MissionStage
{
    Planning = 0,
    Infiltration = 1,
    ApproachingTarget = 2,
    Objective = 3,
    Extraction = 4,
    Completed = 5
}
