# Autonomous Mission System - Quick Reference

## Service Registration
```csharp
// In Program.cs ConfigureServices
services.AddSingleton<AutonomousMissionService>();
services.AddSingleton<GMService>(); // Required dependency
```

## Core Methods

### GenerateMissionAsync
```csharp
Task<Models.MissionDefinition> GenerateMissionAsync(
    ulong channelId,
    string missionType,
    MissionGenerationContext? context = null)
```

**Mission Types**: cyberdeck, assassination, extraction, theft, investigation, delivery, recovery, sabotage

**Returns**: Persistent MissionDefinition with all components

### ExecuteDecisionAsync
```csharp
Task<DecisionResult> ExecuteDecisionAsync(
    ulong channelId,
    string decisionId,
    string optionId,
    Dictionary<string, object>? additionalData = null)
```

**Returns**: Decision result with consequences, new stage, and mission status

### GenerateNPCDialogueAsync
```csharp
Task<string> GenerateNPCDialogueAsync(
    ulong channelId,
    string npcName,
    string situation,
    DialogueContext dialogueContext)
```

**Returns**: Contextual dialogue based on NPC relationship

## Mission Components

### Accessing Mission Data
```csharp
var mission = await missionService.GenerateMissionAsync(channelId, "investigation");

// Deserialize components
var objective = mission.GetObjective();
var johnson = mission.GetJohnson();
var locations = mission.GetLocations();
var obstacles = mission.GetObstacles();
var npcs = mission.GetNPCs();
var reward = mission.GetReward();
var twist = mission.GetTwist();
var decisions = mission.GetDecisionPoints();
var intel = mission.GetIntel();
```

### Mission Properties
```csharp
mission.Id                 // Database ID
mission.GameSessionId      // Parent session
mission.MissionType        // Type string
mission.Complexity         // 1-6 scale
mission.Status             // Planning/InProgress/Completed/Failed/Aborted
mission.CurrentStage       // 0-5 (MissionStage enum)
mission.Deadline           // DateTime?
mission.GeneratedAt        // DateTime
```

## Enums

### MissionStage
```csharp
Planning = 0
Infiltration = 1
ApproachingTarget = 2
Objective = 3
Extraction = 4
Completed = 5
```

### ConsequenceType
```csharp
Success = 0
PartialSuccess = 1
Failure = 2
Complication = 3
Opportunity = 4
```

### MissionStatus (from Phase 1)
```csharp
Planning
InProgress
Completed
Failed
Aborted
```

## Common Patterns

### Pattern 1: Generate and Display
```csharp
var mission = await _missionService.GenerateMissionAsync(
    Context.Channel.Id,
    "cyberdeck"
);

var objective = mission.GetObjective();
var reward = mission.GetReward();

await ReplyAsync(
    $"**{objective?.Title}**\n" +
    $"Payment: {reward?.BaseNuyen ?? 0:N0}¥\n" +
    $"Complexity: {mission.Complexity}/6"
);
```

### Pattern 2: Execute Decision
```csharp
var decisions = mission.GetDecisionPoints();
var firstDecision = decisions?.FirstOrDefault();

if (firstDecision != null)
{
    var result = await _missionService.ExecuteDecisionAsync(
        Context.Channel.Id,
        firstDecision.DecisionId,
        firstDecision.Options.First().OptionId
    );
    
    await ReplyAsync(
        $"Outcome: {result.Consequences.FirstOrDefault()?.Description}\n" +
        $"New Stage: {result.NewStage}"
    );
}
```

### Pattern 3: NPC Dialogue
```csharp
var dialogue = await _missionService.GenerateNPCDialogueAsync(
    Context.Channel.Id,
    "Fixer Mike",
    "Negotiation",
    new DialogueContext
    {
        PlayerInput = "What's the pay?",
        CurrentStage = Models.MissionStage.Planning
    }
);

await ReplyAsync($"**Fixer Mike:** {dialogue}");
```

## Database Queries

### Get Active Mission
```csharp
var mission = await _database.GetActiveMissionDefinitionAsync(sessionId);
```

### Get All Session Missions
```csharp
var missions = await _database.GetSessionMissionDefinitionsAsync(sessionId);
```

### Update Mission
```csharp
mission.Status = MissionStatus.InProgress;
mission.CurrentStage = (int)Models.MissionStage.Infiltration;
await _database.UpdateMissionDefinitionAsync(mission);
```

## Context Building

The system automatically builds context including:
- Player characters and roles
- Recent narrative events (last 10)
- Existing NPC relationships
- Active missions
- Faction standings
- Session location and time

## Performance Tips

1. **Reuse Context**: Context is built once per generation
2. **Lazy Loading**: Components deserialized only when accessed
3. **Batch Operations**: Generate multiple missions in sequence
4. **Cache Frequently**: Mission definitions can be cached per session

## Error Handling

```csharp
try
{
    var mission = await _missionService.GenerateMissionAsync(
        channelId,
        missionType
    );
}
catch (InvalidOperationException ex)
{
    // No active session or other validation error
    _logger.LogError(ex, "Mission generation failed");
}
```

## Integration with Phase 1

### With GameSessionService
```csharp
// Sessions track active missions
var session = await _sessionService.GetActiveSessionAsync(channelId);
var mission = await _missionService.GenerateMissionAsync(channelId, type);
// Mission automatically linked to session via GameSessionId
```

### With NarrativeContextService
```csharp
// All decisions recorded as narrative events
var result = await _missionService.ExecuteDecisionAsync(...);
// Automatically creates PlayerChoice and NarrativeEvent
```

### With DiceService
```csharp
// Outcomes determined by Shadowrun dice
// Critical glitches create complications
// Exceptional successes create opportunities
```

## Testing Quick Commands

```bash
# Generate each mission type
!generate cyberdeck
!generate assassination
!generate extraction
!generate theft
!generate investigation
!generate delivery
!generate recovery
!generate sabotage

# View current mission
!mission

# Make a decision
!decide decision_0 option_direct

# Talk to NPC
!talk Johnson I'm interested
```

## Common Issues

### Issue: "No active session"
**Solution**: Start a session first
```csharp
await _sessionService.StartSessionAsync(guildId, channelId, gmUserId);
```

### Issue: Mission not persisting
**Solution**: Check database migration
```bash
dotnet ef migrations add AddMissionDefinitions
dotnet ef database update
```

### Issue: NPC doesn't remember
**Solution**: NPC relationships are per-session
- Use same session across interactions
- Check NPCRelationships table
- Verify NPC name matches exactly

## Quick Stats

- **Generation Time**: < 1 second typically
- **Mission Components**: 8-15 per mission
- **Decision Points**: 1-4 per mission
- **NPC Memory**: Unlimited (database-backed)
- **Concurrent Missions**: Unlimited per session

## Best Practices

1. **Always check for active session** before generating
2. **Handle null components** gracefully (objective, johnson, etc.)
3. **Log all decisions** for debugging
4. **Cache mission definitions** during active gameplay
5. **Update mission status** as players progress
6. **Record narrative events** for story continuity

## Support

For detailed information, see:
- **USAGE_EXAMPLES.md** - Detailed examples
- **PHASE2_SETUP.md** - Implementation details
- **PHASE2_TESTS.md** - Testing procedures
- **PHASE2_DELIVERABLES.md** - Complete deliverables list
