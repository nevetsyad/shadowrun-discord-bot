# Autonomous Mission System - Usage Examples

## Overview
The AutonomousMissionService provides dynamic, procedural mission generation for Shadowrun campaigns. It integrates with the existing game session management, narrative context tracking, and dice rolling systems.

## Setup Instructions

### 1. Database Migration
Run the following commands to create/update the database schema:
```bash
cd shadowrun-discord-bot
dotnet ef migrations add AddMissionDefinitions
dotnet ef database update
```

### 2. Service Registration
Add the AutonomousMissionService to the DI container in Program.cs:
```csharp
// In Program.cs, within service configuration section
services.AddScoped<AutonomousMissionService>();
```

### 3. Service Dependencies
The AutonomousMissionService requires these services (already registered in Phase 1):
- DatabaseService
- GameSessionService
- NarrativeContextService
- GMService
- DiceService

## Usage Examples

### Example 1: Generate a New Mission

```csharp
// Inject AutonomousMissionService
public class MissionCommands : ModuleBase<SocketCommandContext>
{
    private readonly AutonomousMissionService _missionService;
    private readonly GameSessionService _sessionService;

    public MissionCommands(
        AutonomousMissionService missionService,
        GameSessionService sessionService)
    {
        _missionService = missionService;
        _sessionService = sessionService;
    }

    [Command("generate")]
    public async Task GenerateMission(string missionType = "investigation")
    {
        // Ensure there's an active session
        var session = await _sessionService.GetActiveSessionAsync(Context.Channel.Id);
        if (session == null)
        {
            await ReplyAsync("No active session. Start one with `!startsession` first.");
            return;
        }

        // Generate a new mission
        var mission = await _missionService.GenerateMissionAsync(
            Context.Channel.Id,
            missionType
        );

        // Display mission details
        var objective = mission.GetObjective();
        var johnson = mission.GetJohnson();
        var reward = mission.GetReward();

        var embed = new EmbedBuilder()
            .WithTitle($"New Mission: {objective?.Title ?? "Unknown"}")
            .AddField("Johnson", johnson?.Name ?? "Mr. Johnson")
            .AddField("Type", mission.MissionType)
            .AddField("Complexity", mission.Complexity)
            .AddField("Payment", $"{reward?.BaseNuyen ?? 0:N0}¥ + {reward?.NegotiationBuffer ?? 0:N0}¥ negotiation room")
            .AddField("Karma", reward?.BaseKarma ?? 0)
            .AddField("Deadline", mission.Deadline?.ToString("yyyy-MM-dd") ?? "No deadline")
            .WithColor(Color.DarkBlue);

        await ReplyAsync(embed: embed.Build());
    }
}
```

### Example 2: View Mission Details

```csharp
[Command("mission")]
public async Task ViewMission()
{
    var session = await _sessionService.GetActiveSessionAsync(Context.Channel.Id);
    if (session == null)
    {
        await ReplyAsync("No active session.");
        return;
    }

    // Get the active mission definition
    var mission = await _database.GetActiveMissionDefinitionAsync(session.Id);
    if (mission == null)
    {
        await ReplyAsync("No active mission. Generate one with `!generate <type>`");
        return;
    }

    var objective = mission.GetObjective();
    var locations = mission.GetLocations();
    var obstacles = mission.GetObstacles();
    var npcs = mission.GetNPCs();
    var decisions = mission.GetDecisionPoints();

    var sb = new StringBuilder();
    sb.AppendLine($"**{objective?.Title}**");
    sb.AppendLine(objective?.Description);
    sb.AppendLine();
    sb.AppendLine($"**Success Criteria:** {objective?.SuccessCriteria}");
    sb.AppendLine();
    
    if (locations?.Any() == true)
    {
        sb.AppendLine("**Locations:**");
        foreach (var loc in locations)
        {
            sb.AppendLine($"- {loc.Name} (Security: {loc.SecurityLevel})");
        }
        sb.AppendLine();
    }

    if (obstacles?.Any() == true)
    {
        sb.AppendLine($"**Obstacles:** {obstacles.Count} challenges await");
    }

    if (npcs?.Any() == true)
    {
        sb.AppendLine($"**Key NPCs:** {npcs.Count(n => n.IsPrimary)} primary contacts");
    }

    sb.AppendLine($"**Decision Points:** {decisions?.Count ?? 0} branching paths");

    await ReplyAsync(sb.ToString());
}
```

### Example 3: Execute a Decision

```csharp
[Command("decide")]
public async Task MakeDecision(string decisionId, string optionId)
{
    var result = await _missionService.ExecuteDecisionAsync(
        Context.Channel.Id,
        decisionId,
        optionId
    );

    var embed = new EmbedBuilder()
        .WithTitle("Decision Made")
        .AddField("Choice", result.ChosenOption)
        .AddField("Outcome", result.Consequences.FirstOrDefault()?.Description ?? "Resolved")
        .AddField("New Stage", result.NewStage.ToString())
        .AddField("Mission Status", result.MissionStatus.ToString());

    await ReplyAsync(embed: embed.Build());

    // If mission is complete, show summary
    if (result.MissionStatus == MissionStatus.Completed || 
        result.MissionStatus == MissionStatus.Failed)
    {
        await ShowMissionSummary(Context.Channel.Id);
    }
}
```

### Example 4: Generate NPC Dialogue

```csharp
[Command("talk")]
public async Task TalkToNPC(string npcName, [Remainder] string playerInput)
{
    var session = await _sessionService.GetActiveSessionAsync(Context.Channel.Id);
    if (session == null)
    {
        await ReplyAsync("No active session.");
        return;
    }

    var dialogueContext = new DialogueContext
    {
        PlayerInput = playerInput,
        CurrentStage = Models.MissionStage.Infiltration
    };

    var response = await _missionService.GenerateNPCDialogueAsync(
        Context.Channel.Id,
        npcName,
        "Negotiation",
        dialogueContext
    );

    await ReplyAsync($"**{npcName}:** {response}");
}
```

### Example 5: Mission with Player Context

```csharp
// Generate mission that considers player team composition
[Command("generaterun")]
public async Task GenerateContextualMission(string missionType)
{
    // The service automatically builds context including:
    // - Player characters and their roles
    // - Recent narrative events
    // - Existing NPC relationships
    // - Active missions
    // - Faction standings
    
    var mission = await _missionService.GenerateMissionAsync(
        Context.Channel.Id,
        missionType
    );

    // The generated mission will:
    // - Scale difficulty based on team size and experience
    // - Include decision options based on team capabilities (decker, mage, etc.)
    // - Potentially reuse existing NPCs as contacts
    // - Reference recent events in dialogue/objectives
    // - Consider faction standings when generating Johnsons
    
    await ReplyAsync($"Mission generated with context-aware elements!");
}
```

## Mission Types

The system supports 8 mission types, each with unique generation logic:

1. **cyberdeck** - Matrix intrusion, datasteal, node hacking
2. **assassination** - High-profile target elimination
3. **extraction** - Rescue or extract persons/assets
4. **theft** - Steal items from secure locations
5. **investigation** - Mysteries, clues, detective work
6. **delivery** - Protect and deliver assets
7. **recovery** - Find and recover lost items/people
8. **sabotage** - Destroy infrastructure or systems

## Integration Points

### With GameSessionService
- Missions are tied to active game sessions
- Session progress updates when missions complete
- Participant karma/nuyen tracked per session

### With NarrativeContextService
- All mission events are recorded as narrative events
- Player choices tracked with consequences
- NPC relationships updated based on interactions
- Story summaries include mission outcomes

### With DiceService
- Success/failure determined by Shadowrun dice rolls
- Glitch detection for complications
- Edge/exploding dice for dramatic moments

### With GMService
- Uses existing content generators as building blocks
- Location, NPC, and equipment generation
- Plot hooks integrated into missions

## Database Schema

### MissionDefinition Table
```sql
CREATE TABLE MissionDefinitions (
    Id INTEGER PRIMARY KEY,
    GameSessionId INTEGER NOT NULL,
    MissionType TEXT NOT NULL,
    Complexity INTEGER,
    GeneratedAt TEXT,
    Status INTEGER,
    CurrentStage INTEGER,
    JohnsonJson TEXT,
    ObjectiveJson TEXT,
    LocationsJson TEXT,
    ObstaclesJson TEXT,
    NPCsJson TEXT,
    RewardJson TEXT,
    TwistJson TEXT,
    DecisionPointsJson TEXT,
    IntelJson TEXT,
    Deadline TEXT,
    CompletedDecisionsJson TEXT,
    ConsequencesJson TEXT,
    FOREIGN KEY (GameSessionId) REFERENCES GameSessions(Id)
);
```

## Performance Optimizations

1. **JSON Serialization** - Complex mission data stored as JSON for flexibility
2. **Lazy Loading** - Mission components deserialized only when accessed
3. **Context Caching** - Generation context built once and reused
4. **Async Operations** - All database operations are async
5. **Connection Pooling** - EF Core manages database connections efficiently

## Error Handling

The service includes comprehensive error handling:
- Validates active session before mission generation
- Checks for existing missions before creating new ones
- Validates decision IDs and option IDs before execution
- Logs all operations for debugging
- Graceful degradation when context data is missing

## Future Enhancements

Potential improvements for future phases:
1. AI-powered dialogue generation
2. Mission templates for specific campaign arcs
3. Cross-session story continuity
4. Faction relationship evolution
5. Dynamic world state based on completed missions
6. Procedural dungeon/map generation
7. Integration with virtual tabletop tools
