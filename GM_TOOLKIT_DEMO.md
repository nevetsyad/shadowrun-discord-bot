# GM Toolkit Demo

This document demonstrates the GM Toolkit features added to the Shadowrun Discord Bot.

## Features Implemented

### 1. NPC Generator (`/npc` command)
Generate NPCs with comprehensive details:
- **Roles Available**: corporate exec, fixer, street doc, shadowrunner, corporate guard, terrorist
- **Generated Details**:
  - Name (randomized from cyberpunk-themed names)
  - Company affiliation
  - Physical description
  - Attribute stats (INT, BODY, REA, STR, CHR, EDGE)
  - Motivation
  - Backstory
  - Difficulty rating

**Example Usage**:
```
/npc role: fixer
```

**Sample Output**:
```
👤 Generated NPC

Name: Alex Chen
Role: fixer
Company: Arasaka
Description: Wearing expensive cyberware, holding a datapad
Attributes: INT 2 | BODY 1 | REA 3 | STR 2 | CHR 3 | EDGE 2
Motivation: Money - Wants to pay off debt
Backstory: Used to work for {{company_name}}, got burned, now runs freelance

---
🎲 Difficulty: 4
```

### 2. Mission Generator (`/mission` command)
Generate missions by type:
- **Types Available**: cyberdeck, assassination, extraction, theft, investigation
- **Templates**: Each type has 5 different randomized templates
- **Placeholders**: Templates include placeholders for customization ({{company_name}}, {{target}}, etc.)

**Example Usage**:
```
/mission type: extraction
```

**Sample Output**:
```
🎯 Generated Mission: extraction

Extract {{hostage_name}} from {{location}}. Extraction method: {{extraction_method}}. Transport: {{transport}}.
```

### 3. Location Generator (`/location` command)
Generate locations by type:
- **Types Available**: corporate, seedy, safehouse, combat
- **5 locations per type**

**Example Usage**:
```
/location type: corporate
```

**Sample Output**:
```
🏢 Generated Location: corporate

Arasaka Corporate Tower - Executive Suite
```

### 4. Plot Hook Generator (`/plot-hook` command)
Generate random plot hooks to spark adventures:
- 8 different plot hook templates
- Randomized each time

**Example Usage**:
```
/plot-hook
```

**Sample Output**:
```
🎣 Plot Hook

A mysterious package arrives with a single name inside. Who wants it?
```

### 5. Loot Generator (`/loot` command)
Generate loot drops and rewards:
- 8 different loot types
- Includes data shards, nuyen, cyberware, weapons, intel, etc.

**Example Usage**:
```
/loot
```

**Sample Output**:
```
💰 Generated Loot

Nuyen - 5,000 ¥
```

### 6. Random Event Generator (`/random-event` command)
Add unexpected events to your sessions:
- 8 different event types
- Complications, threats, and opportunities

**Example Usage**:
```
/random-event
```

**Sample Output**:
```
⚡ Random Event

A corporate security team is investigating the area. Time is running out!
```

### 7. Equipment Generator (`/equipment` command)
Generate equipment by type:
- **Types Available**: weapon, armor, cyberware, general
- **Multiple options per type**

**Example Usage**:
```
/equipment type: weapon
```

**Sample Output**:
```
🔧 Generated Equipment: weapon

Ares Predator - 6EV
```

## Integration

All GM commands are integrated into:
- Discord slash command system
- Help command (`/help`)
- Command routing system
- Error handling

## Files Created/Modified

### Created Files:
1. `Services/GMService.cs` - Main GM service with all generators
2. `GM_TOOLKIT_DEMO.md` - This demo file

### Modified Files:
1. `Core/CommandHandler.cs` - Added GM command registration and handlers
2. `README.md` - Updated with GM Toolkit features and command reference

## Technical Details

### GMService Class
- **Dependencies**: DiceService (for difficulty rolls)
- **Methods**:
  - `GenerateMission(string missionType)` - Mission generator
  - `GenerateNPC(string role)` - NPC generator
  - `GenerateLocation(string locationType)` - Location generator
  - `GeneratePlotHook()` - Plot hook generator
  - `GenerateLoot()` - Loot generator
  - `GenerateRandomEvent()` - Random event generator
  - `GenerateEquipment(string type)` - Equipment generator

### Command Handler Integration
- All 7 GM commands registered as slash commands
- Proper error handling with try-catch blocks
- Discord embeds for rich formatting
- Ephemeral responses for some commands
- Logging for all errors

## Future Enhancements

Potential improvements for future versions:
1. **Template Variable Substitution**: Automatically fill in {{placeholder}} variables
2. **Persistent NPC Storage**: Save generated NPCs to database
3. **Custom Templates**: Allow GMs to create custom mission/location templates
4. **Weighted Randomness**: Adjust probabilities based on campaign settings
5. **Loot Tables**: More sophisticated loot generation with rarity tiers
6. **Event Chains**: Generate connected series of events
7. **NPC Relationships**: Generate connections between NPCs
8. **Campaign Integration**: Tie generators to specific campaign settings

## Testing

To test the GM Toolkit:
1. Start the bot
2. Use `/help` to see all commands
3. Try each GM command:
   ```
   /npc role: shadowrunner
   /mission type: cyberdeck
   /location type: seedy
   /plot-hook
   /loot
   /random-event
   /equipment type: cyberware
   ```

All commands should respond with rich embeds containing generated content.
