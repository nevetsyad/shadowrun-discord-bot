# Phase 3: Interactive Storytelling System - Implementation Guide

## Overview

The Interactive Storytelling System (Phase 3) enables autonomous Shadowrun GM gameplay through natural language commands, skill checks, dynamic encounters, and context-aware NPC dialogue.

## Files Created

### 1. Services/InteractiveStoryService.cs
**Main service for handling player interactions and generating responses.**

Key Features:
- **Player Input Parser**: Natural language command parsing with intent extraction
- **Skill Check System**: Full Shadowrun 3e mechanics (attribute + skill dice pools)
- **Encounter Generation**: On-the-fly encounters with dynamic difficulty
- **NPC Dialogue Generation**: Context-aware dialogue with personality and relationships

### 2. Commands/InteractiveStoryCommands.cs
**Discord command module for interactive story commands.**

Commands implemented:
- `/roleplay [text]` - Character action narration
- `/check [skill]` - Skill check with SR3e mechanics
- `/investigate [target]` - Investigation check
- `/search` - Area search
- `/listen` - Auditory perception
- `/interact [object]` - Object interaction
- `/use [item]` - Item usage
- `/talk [npc]` - NPC conversation
- `/dialogue [npc] [message]` - Specific dialogue
- `/describe` - Scene description
- `/relationships` - View NPC relationships
- `/encounter [type]` - Generate encounter (GM only)

### 3. Updated Files

#### Program.cs
- Added `InteractiveStoryService` registration

#### Services/NarrativeContextService.cs
- Added `RecordChoiceAsync` overload for mission decisions

#### Services/AutonomousMissionService.cs
- Fixed bug: `mission.Complexity` used before mission creation

## Skill Check System

### Shadowrun 3e Mechanics
```
Dice Pool = Skill Rating + Linked Attribute + Wound Modifier
Target Number (TN) = Base 4 + Environmental Modifiers
Success = Die roll >= Target Number
Glitch = More than half dice showing 1s
Critical Glitch = Glitch with 0 successes
```

### Skill Categories
- **Combat**: firearms, edged weapons, clubs, unarmed combat, thrown weapons, assault rifles, shotguns, heavy weapons
- **Physical**: athletics, stealth, driving, pilot aircraft, biotech
- **Social**: etiquette, negotiation, leadership, interrogation, con
- **Technical**: computers, electronics, demolitions, building
- **Magic**: sorcery, conjuring, enchanting, centering
- **Knowledge**: lore, knowledge, investigation, perception

### Target Number Modifiers
| Condition | Modifier |
|-----------|----------|
| Base TN | 4 |
| Darkness | +2 |
| Rain | +1 |
| Noise | +1 |
| Distracted | +2 |
| Combat | +2 |
| Time Pressure | +1 |

## Natural Language Parsing

The system understands natural language in addition to slash commands:

| Natural Input | Parsed As |
|---------------|-----------|
| "I roll stealth" | `/check stealth` |
| "I search the room" | `/search` |
| "I talk to the bartender" | `/talk bartender` |
| "I listen for sounds" | `/listen` |
| "I look at the door" | `/investigate door` |

## NPC Dialogue System

### Relationship Tracking
- **Attitude**: -10 (Hostile) to +10 (Allied)
- **Trust Level**: 0 (Stranger) to 10 (Complete Trust)
- **Interaction History**: Logged conversations

### Dialogue Generation Factors
1. **NPC Role**: Determines dialogue style and available information
2. **Attitude**: Affects openness and helpfulness
3. **Trust Level**: Unlocks deeper conversation options
4. **Recent Events**: References past interactions
5. **Player Message**: Analyzed for tone and keywords

### Attitude Modifiers
| Player Message Keywords | Attitude Change |
|-------------------------|-----------------|
| "please", "thank", "help", "friend" | +1 |
| "threat", "kill", "hurt", "force" | -1 |
| "die", "destroy", "betray" | -2 |

## Encounter Generation

### Encounter Types
1. **Combat**: Enemy NPCs with stats
2. **Social**: Conversation challenges
3. **Puzzle**: Problem-solving obstacles
4. **Chase**: Pursuit scenarios
5. **Stealth**: Avoidance challenges

### Difficulty Scaling
- Base difficulty: 3
- +1 per 2 party members
- +1 if average karma > 50
- +1 if average karma > 100

### Enemy Generation
Enemies are generated with:
- Name and type
- Body and Reaction attributes
- Primary weapon
- Threat level (1-6)

## Usage Examples

### Starting a Session
```
/session start
```

### Roleplay Actions
```
/roleplay carefully opens the door and peers inside
/roleplay draws my Ares Predator and checks the corners
```

### Skill Checks
```
/check stealth
/check firearms
/check negotiation
/check computers
```

### Investigation
```
/investigate security terminal
/search
/listen
```

### NPC Interaction
```
/talk Johnson
/dialogue Johnson I'll take the job, but I want double
/relationships
```

### Scene Management
```
/describe
/story
```

### GM Commands
```
/encounter combat
/encounter social
```

## Integration Points

The InteractiveStoryService integrates with:

1. **AutonomousMissionService**: For mission context and decision points
2. **NarrativeContextService**: For event tracking and NPC relationships
3. **GameSessionService**: For session state and participant management
4. **DiceService**: For Shadowrun dice mechanics
5. **DatabaseService**: For persistence

## Setup Instructions

1. **Build the project**:
   ```bash
   cd shadowrun-discord-bot
   dotnet build
   ```

2. **Run the bot**:
   ```bash
   dotnet run --token YOUR_DISCORD_TOKEN
   ```

3. **Start a session**:
   ```
   /session start
   ```

4. **Use interactive commands**:
   ```
   /roleplay I enter the room cautiously
   /check perception
   /talk fixer
   ```

## Optimizations Implemented

1. **Shared Context Building**: `BuildStoryContextAsync` consolidates session, character, and event loading
2. **Reusable Skill Definitions**: Static dictionary for skill metadata
3. **Consolidated Outcome Generation**: `GenerateOutcomeNarrative` handles all skill categories
4. **Efficient Dice Pool Calculation**: Single method with skill-specific overrides
5. **Shared Formatting Methods**: Embed builders are reused across response types
6. **Dictionary-Based Responses**: Response templates organized by type for quick lookup

## Alignment Notes (Misalignment Check)

### Expected vs Implemented

| Expected | Implemented | Status |
|----------|-------------|--------|
| Player input parser | ✅ Full natural language support | Complete |
| Skill check system | ✅ Full SR3e mechanics | Complete |
| Encounter generation | ✅ Dynamic with difficulty scaling | Complete |
| NPC dialogue | ✅ Context-aware with relationships | Complete |
| Interactive commands | ✅ All specified commands | Complete |

### Phase 2 Integration

The AutonomousMissionService was already well-implemented with:
- Dynamic mission generation
- Decision points
- NPC generation
- Consequences system

Phase 3 extends this with:
- Real-time player interaction
- Natural language understanding
- On-demand skill checks
- Dynamic dialogue generation

### Gaps Addressed

1. **No input parsing** → Added comprehensive natural language parser
2. **Basic NPC dialogue** → Enhanced with personality, trust, and context
3. **No skill check UI** → Added Discord commands with rich embeds
4. **No encounter system** → Added dynamic encounter generation

## Future Enhancements

1. **AI Integration**: Connect to LLM for more dynamic dialogue
2. **Combat Flow**: Auto-initiative and turn management
3. **Inventory Integration**: Better item interaction
4. **Map System**: Location-based navigation
5. **Party Coordination**: Multi-player skill checks
