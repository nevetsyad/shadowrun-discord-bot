# Phase 5: Dynamic Content Engine - Setup Guide

## Overview

Phase 5 adds an advanced procedural content generation engine with adaptive difficulty, story evolution, and optional machine learning integration for Shadowrun campaigns.

## New Files Created

### Services
1. **Services/DynamicContentEngine.cs** - Main procedural generation engine
   - Adaptive difficulty system (1-10 scale)
   - Mission complexity scaling
   - Context-aware story building
   - Dynamic NPC dialogue generation
   - Story evolution and campaign arc management
   - Learning system for player preferences

2. **Services/ContentDatabaseService.cs** - Persistent content storage
   - Session content data storage
   - NPC personality data persistence
   - Generated content storage
   - Performance metrics tracking
   - Story preferences storage

3. **Services/DatabaseService.Phase5.cs** - Database service extension
   - GetContext() method for DbContext access
   - Phase 5 entity operations

### Commands
4. **Commands/DynamicContentCommands.cs** - Discord command handlers
   - /difficulty - View/adjust difficulty
   - /campaign arc - Campaign arc management
   - /content regenerate - Content regeneration
   - /learning status - AI learning status
   - /story evolve/hook - Story evolution
   - /npc learn/profile - NPC learning

### Extensions
5. **Extensions/ServiceCollectionExtensions.Phase5.cs** - DI registration

## Database Migration

The Phase 5 entities will be automatically created when the database is initialized. No manual migration is required for SQLite.

### New Tables Created

1. **SessionContentData** - Stores session-level dynamic content state
2. **NPCPersonalityData** - Stores learned NPC personalities
3. **NPCLearningEvents** - Records NPC learning events
4. **GeneratedContent** - Stores generated content for reuse
5. **PerformanceMetricsRecords** - Performance metrics history
6. **StoryPreferencesRecords** - Player preference tracking
7. **ContentRegenerations** - Content regeneration history
8. **CampaignArcRecords** - Campaign arc tracking

## Installation

### Step 1: Add Service Registration

In your `Program.cs` or service configuration:

```csharp
using ShadowrunDiscordBot.Extensions;

// After other service registrations
services.AddPhase5DynamicContent();

// After database initialization
await services.InitializePhase5Async();
```

### Step 2: Register Commands

In your command registration code:

```csharp
using ShadowrunDiscordBot.Commands;

// In your command handler setup
var contentCommands = services.GetRequiredService<DynamicContentCommands>();
await DynamicContentCommands.RegisterCommandsAsync(client, guildId);

// In your slash command handler
if (command.Data.Name is "difficulty" or "campaign" or "content" or "learning" or "story" or "npc")
{
    await contentCommands.HandleCommandAsync(command);
    return;
}
```

### Step 3: Initialize Database

The database tables will be automatically created on first run. If using an existing database, the new tables will be added automatically.

## Usage Examples

### Viewing Current Difficulty

```
/difficulty
```

Output:
```
📊 Difficulty Status

Current Difficulty: 5/10
Performance Score: 62.5/100
Recommendation: Difficulty well-balanced for current party performance

Performance Metrics:
• Skill Check Rate: 65%
• Mission Completion: 75%
• Social Interactions: 60%
• Combat Victories: 50%
```

### Adjusting Difficulty

```
/difficulty adjust level:7
```

### Managing Campaign Arcs

```
/campaign arc action:start name:"Shadows of Seattle" description:"A campaign about corporate espionage in Seattle"
```

### Generating Plot Hooks

```
/story hook type:mystery
```

### Evolving the Story

```
/story evolve
```

### Viewing Learning Status

```
/learning status
```

### Recording NPC Learning

```
/npc learn name:"Ghostslicer" event:"positive_interaction" description:"Helped the team with intel"
```

### Viewing NPC Profile

```
/npc profile name:"Ghostslicer"
```

### Regenerating Content

```
/content regenerate type:mission difficulty:7
```

## Adaptive Difficulty System

### How It Works

1. **Performance Analysis**: The system analyzes:
   - Skill check success rates
   - Mission completion rates
   - NPC interaction outcomes
   - Combat encounter outcomes

2. **Automatic Adjustment**:
   - Players struggling (score < 30): Difficulty decreases
   - Players dominating (score > 80): Difficulty increases
   - Balanced performance (50-70): No adjustment

3. **Manual Override**: GMs can manually set difficulty at any time

### Difficulty Effects

| Level | Complexity | Decision Points | NPC Skill | Security |
|-------|------------|-----------------|-----------|----------|
| 1-3   | Simple     | 1-2            | Basic     | 1-3      |
| 4-6   | Medium     | 3-4            | Skilled   | 3-5      |
| 7-10  | Complex    | 5-7            | Elite     | 5-8      |

## Story Evolution

### Choice Pattern Detection

The system detects patterns in player choices:
- **Aggressive** - Violence-first approach
- **Diplomatic** - Negotiation preference
- **Stealthy** - Avoidance preference

### Consequences

Patterns affect:
- NPC reactions and attitudes
- Mission opportunities
- Faction relationships
- Story direction

## Learning System

### NPC Personality Adaptation

NPCs learn from interactions:
- Trust levels evolve
- Attitudes adjust
- Secrets may be revealed
- Personalities develop

### Story Preference Learning

The system tracks:
- Combat vs. Social preference
- Stealth vs. Direct approach
- Risk tolerance
- Moral alignment

## Optimizations Implemented

1. **Consolidated Code Structure**
   - Single DynamicContentEngine handles all content generation
   - ContentDatabaseService provides unified data access
   - Shared patterns for difficulty scaling

2. **Efficient Database Operations**
   - Batch operations where possible
   - Indexed lookups for common queries
   - Lazy loading of JSON-serialized data

3. **Minimal Complexity**
   - Simple weighted scoring for difficulty
   - Straightforward pattern detection
   - Clean separation of concerns

4. **ML Integration Design**
   - Personality models are simple weight adjustments
   - Preference tracking uses incremental updates
   - No external ML dependencies required

## Configuration Options

In your `appsettings.json`:

```json
{
  "DynamicContent": {
    "AutoDifficultyAdjustment": true,
    "PerformanceSampleSize": 10,
    "MinDifficulty": 1,
    "MaxDifficulty": 10,
    "DefaultDifficulty": 5,
    "LearningEnabled": true,
    "CleanupOldDataAfterDays": 90
  }
}
```

## Troubleshooting

### Difficulty Not Adjusting

- Check that there are enough recorded events (default: 10)
- Verify performance metrics are being calculated
- Check logs for auto-adjustment attempts

### Learning Not Working

- Ensure learning is enabled in configuration
- Check that NPC interactions are being recorded
- Verify database connections

### Commands Not Registering

- Ensure bot has `applications.commands` scope
- Check guild ID is correct
- Verify command registration ran successfully

## Future Enhancements

1. **Advanced ML Integration**
   - Integration with external ML services
   - More sophisticated pattern detection
   - Predictive content generation

2. **Enhanced Story Continuity**
   - Cross-session memory
   - Long-term relationship tracking
   - Multi-campaign support

3. **Content Templates**
   - Customizable mission templates
   - NPC archetype definitions
   - Story beat libraries

## Support

For issues or questions, please refer to the main project documentation or create an issue in the repository.
