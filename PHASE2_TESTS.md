# Phase 2 Test Scenarios

## Quick Verification Tests

### Test 1: Service Registration
```bash
# Build the project to verify all dependencies are correct
dotnet build

# Expected: Build succeeds with no errors
# If errors occur, check:
# - All using statements are correct
# - All dependencies are registered in Program.cs
# - Model namespaces are consistent
```

### Test 2: Database Migration
```bash
# Create migration for MissionDefinition table
dotnet ef migrations add AddMissionDefinitions

# Apply migration
dotnet ef database update

# Expected: Migration succeeds, database updated
# Verify table exists:
sqlite3 shadowrun.db ".schema MissionDefinitions"
```

### Test 3: Mission Generation (Manual Test)
```csharp
// This would be run in a test environment or via Discord bot

// Step 1: Create a game session
var session = await gameSessionService.StartSessionAsync(
    guildId: 123456789,
    channelId: 987654321,
    gmUserId: 111222333,
    sessionName: "Test Session"
);

// Step 2: Generate a mission
var mission = await autonomousMissionService.GenerateMissionAsync(
    channelId: 987654321,
    missionType: "investigation"
);

// Step 3: Verify mission was created
Assert.NotNull(mission);
Assert.NotNull(mission.GetObjective());
Assert.NotNull(mission.GetJohnson());
Assert.True(mission.Complexity >= 1 && mission.Complexity <= 6);
Assert.NotNull(mission.GetLocations());
Assert.NotNull(mission.GetObstacles());
Assert.NotNull(mission.GetNPCs());
Assert.NotNull(mission.GetDecisionPoints());

// Step 4: Verify mission persisted to database
var savedMission = await database.GetMissionDefinitionAsync(mission.Id);
Assert.NotNull(savedMission);
Assert.Equal(mission.MissionType, savedMission.MissionType);
```

### Test 4: Decision Execution (Manual Test)
```csharp
// Step 1: Generate a mission (see Test 3)
var mission = await autonomousMissionService.GenerateMissionAsync(
    channelId: 987654321,
    missionType: "cyberdeck"
);

// Step 2: Get first decision point
var decisions = mission.GetDecisionPoints();
var firstDecision = decisions.FirstOrDefault();

if (firstDecision != null)
{
    // Step 3: Execute a decision
    var result = await autonomousMissionService.ExecuteDecisionAsync(
        channelId: 987654321,
        decisionId: firstDecision.DecisionId,
        optionId: firstDecision.Options.First().OptionId
    );

    // Step 4: Verify result
    Assert.NotNull(result);
    Assert.Equal(firstDecision.DecisionId, result.DecisionId);
    Assert.NotEmpty(result.Consequences);
    Assert.True(result.NewStage >= Models.MissionStage.Planning);
}
```

### Test 5: NPC Dialogue Generation (Manual Test)
```csharp
// Step 1: Create an NPC relationship
await narrativeContextService.UpdateNPCRelationshipAsync(
    channelId: 987654321,
    npcName: "Fixer Mike",
    npcRole: "Fixer",
    organization: "Shadow Guild",
    attitudeDelta: 5,
    trustDelta: 3,
    interaction: "First meeting - discussed potential work"
);

// Step 2: Generate dialogue
var dialogue = await autonomousMissionService.GenerateNPCDialogueAsync(
    channelId: 987654321,
    npcName: "Fixer Mike",
    situation: "Negotiation",
    dialogueContext: new DialogueContext
    {
        PlayerInput = "What's the job?",
        CurrentStage = Models.MissionStage.Planning
    }
);

// Step 3: Verify dialogue
Assert.NotNull(dialogue);
Assert.NotEmpty(dialogue);
// Should reference the relationship (friendly attitude)
Assert.Contains("friend", dialogue.ToLower());
```

## Integration Test Scenarios

### Scenario 1: Full Mission Lifecycle
1. Start game session
2. Add participants with characters
3. Generate mission (complexity scales with team)
4. View mission details
5. Execute all decision points
6. Verify mission status changes
7. Verify narrative events recorded
8. Verify NPC relationships updated
9. End session
10. Verify session progress includes mission

### Scenario 2: Context-Aware Generation
1. Generate 3 different missions in same session
2. Verify each mission:
   - Has different Johnson (or reuses with memory)
   - References different locations
   - Has different obstacles
   - Scales with participant count
3. Verify NPC relationships persist across missions

### Scenario 3: Branching Paths
1. Generate mission with 4 decision points
2. Execute different option combinations
3. Verify different outcomes:
   - Different consequences
   - Different mission status
   - Different narrative events

### Scenario 4: NPC Memory
1. Meet NPC for first time (generate dialogue)
2. Complete a mission involving NPC
3. Meet NPC again
4. Verify dialogue references previous interaction
5. Make choices that affect attitude
6. Meet NPC third time
7. Verify attitude change reflected in dialogue

## Performance Tests

### Test 1: Generation Performance
```csharp
// Generate 100 missions
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

for (int i = 0; i < 100; i++)
{
    await autonomousMissionService.GenerateMissionAsync(
        channelId: testChannelId,
        missionType: missionTypes[i % 8]
    );
}

stopwatch.Stop();

// Should complete in under 30 seconds
Assert.True(stopwatch.ElapsedMilliseconds < 30000);
```

### Test 2: Large Context Performance
```csharp
// Create session with 50 narrative events
for (int i = 0; i < 50; i++)
{
    await narrativeContextService.RecordEventAsync(
        channelId: testChannelId,
        title: $"Event {i}",
        description: $"Description {i}"
    );
}

// Generate mission with large context
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var mission = await autonomousMissionService.GenerateMissionAsync(
    channelId: testChannelId,
    missionType: "investigation"
);
stopwatch.Stop();

// Should complete in under 5 seconds
Assert.True(stopwatch.ElapsedMilliseconds < 5000);
```

### Test 3: Serialization Performance
```csharp
// Generate complex mission
var mission = await autonomousMissionService.GenerateMissionAsync(
    channelId: testChannelId,
    missionType: "sabotage" // High complexity
);

// Time deserialization
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    var objective = mission.GetObjective();
    var npcs = mission.GetNPCs();
    var decisions = mission.GetDecisionPoints();
}
stopwatch.Stop();

// Should deserialize 1000 times in under 1 second
Assert.True(stopwatch.ElapsedMilliseconds < 1000);
```

## Edge Case Tests

### Test 1: No Active Session
```csharp
try
{
    var mission = await autonomousMissionService.GenerateMissionAsync(
        channelId: 999999999,
        missionType: "investigation"
    );
    Assert.Fail("Should have thrown exception");
}
catch (InvalidOperationException ex)
{
    Assert.Contains("No active session", ex.Message);
}
```

### Test 2: Invalid Mission Type
```csharp
// Should default to investigation
var mission = await autonomousMissionService.GenerateMissionAsync(
    channelId: testChannelId,
    missionType: "invalid_type"
);

Assert.Equal("investigation", mission.MissionType);
```

### Test 3: Empty Context
```csharp
// New session with no history
var session = await gameSessionService.StartSessionAsync(
    guildId: testGuildId,
    channelId: newChannelId,
    gmUserId: testGmId
);

// Should still generate valid mission
var mission = await autonomousMissionService.GenerateMissionAsync(
    channelId: newChannelId,
    missionType: "theft"
);

Assert.NotNull(mission);
Assert.NotNull(mission.GetObjective());
```

### Test 4: Invalid Decision ID
```csharp
try
{
    var result = await autonomousMissionService.ExecuteDecisionAsync(
        channelId: testChannelId,
        decisionId: "invalid_id",
        optionId: "invalid_option"
    );
    Assert.Fail("Should have thrown exception");
}
catch (InvalidOperationException ex)
{
    Assert.Contains("not found", ex.Message);
}
```

## Automated Test Script

```bash
#!/bin/bash
# run_phase2_tests.sh

echo "Running Phase 2 Tests..."

# Build
echo "1. Building project..."
dotnet build || { echo "Build failed!"; exit 1; }
echo "✓ Build succeeded"

# Create test database
echo "2. Creating test database..."
rm -f test_shadowrun.db
cp shadowrun.db test_shadowrun.db 2>/dev/null || true
dotnet ef database update --connection "Data Source=test_shadowrun.db" || { echo "Migration failed!"; exit 1; }
echo "✓ Database created"

# Run unit tests (if test project exists)
if [ -d "ShadowrunDiscordBot.Tests" ]; then
    echo "3. Running unit tests..."
    dotnet test || { echo "Tests failed!"; exit 1; }
    echo "✓ Tests passed"
fi

# Cleanup
echo "4. Cleaning up..."
rm -f test_shadowrun.db
echo "✓ Cleanup complete"

echo ""
echo "=========================================="
echo "All Phase 2 tests passed successfully!"
echo "=========================================="
```

## Manual Testing Checklist

- [ ] Bot starts without errors
- [ ] Database migration applies successfully
- [ ] Can start a game session
- [ ] Can generate each mission type (8 total)
- [ ] Mission displays correctly
- [ ] Can execute decisions
- [ ] Consequences are applied
- [ ] Mission status updates correctly
- [ ] NPC dialogue generates
- [ ] NPC remembers previous interactions
- [ ] Session progress includes mission data
- [ ] Can end session cleanly
- [ ] Mission persists across restarts

## Debugging Tips

1. **Enable Debug Logging**
```csharp
// In appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ShadowrunDiscordBot": "Debug"
    }
  }
}
```

2. **Check Database**
```bash
sqlite3 shadowrun.db "SELECT * FROM MissionDefinitions LIMIT 5;"
```

3. **Monitor Logs**
```bash
tail -f logs/shadowrun-bot.log | grep "AutonomousMissionService"
```

4. **Test Individual Methods**
```csharp
// Add test command to bot
[Command("testmission")]
public async Task TestMission()
{
    var mission = await _missionService.GenerateMissionAsync(
        Context.Channel.Id,
        "investigation"
    );
    
    var debug = $"Mission ID: {mission.Id}\n" +
                $"Type: {mission.MissionType}\n" +
                $"Complexity: {mission.Complexity}\n" +
                $"Objective: {mission.GetObjective()?.Title}\n" +
                $"NPCs: {mission.GetNPCs()?.Count ?? 0}\n" +
                $"Decisions: {mission.GetDecisionPoints()?.Count ?? 0}";
    
    await ReplyAsync($"```\n{debug}\n```");
}
```

## Success Criteria

Phase 2 implementation is considered successful when:
- ✅ All code compiles without errors
- ✅ Database migrations apply cleanly
- ✅ Can generate all 8 mission types
- ✅ Missions persist to database
- ✅ Decisions can be executed
- ✅ Consequences are tracked
- ✅ NPC dialogue is context-aware
- ✅ Performance is acceptable (< 5s per generation)
- ✅ Integration with Phase 1 services works
- ✅ Documentation is complete
