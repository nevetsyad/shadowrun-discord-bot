# 🪓 Shadowrun Discord Bot (.NET)

A comprehensive Shadowrun 3rd Edition Discord bot built with .NET 8, Discord.Net, and EF Core.

## 📋 Features

### Character System
- ✅ Create, view, update, and delete characters
- ✅ Full Shadowrun 3e attribute and skill tracking
- ✅ Cyberware and equipment management
- ✅ Magic and Astral projection support
- ✅ Matrix/Netrunning system
- ✅ Priority-based character creation
- ✅ All 5 metatypes: Human, Elf, Dwarf, Ork, Troll
- ✅ 6 archetypes: Mage, Street Samurai, Shaman, Rigger, Decker, Physical Adept
- ✅ Karma system for advancement and rerolls
- ✅ Contact management and legwork
- ✅ Damage/healing with condition monitors

### Magic System
- ✅ Spell casting with Shadowrun dice
- ✅ Focus management with bonding and astral signatures
- ✅ Spirit summoning (Hermetic and Shamanic traditions)
- ✅ Astral projection and perception
- ✅ Astral combat mechanics
- ✅ Drain calculation with Force vs Magic thresholds
- ✅ Spell categories: Combat, Detection, Health, Illusion, Manipulation

### Matrix System
- ✅ Cyberdeck/Commlink management (Micro, Standard, High, Elite)
- ✅ MPCP ratings (3-12)
- ✅ Program installation and management
- ✅ ICE counter and threat tracking
- ✅ Matrix initiative (AR: 2D6, VR: 4D6)
- ✅ ICE cracking and bypass (Probe, Killer, Black, Tar)
- ✅ Complex Forms
- ✅ Security tally tracking
- ✅ Full System ratings (Access, Control, Index, Files, Slave)
- ✅ Alert escalation (None → Passive → Active → Shutdown)
- ✅ Multiple IC types (White, Gray, Black) with specific behaviors

### Combat System
- ✅ Turn-based combat
- ✅ Initiative tracking with multiple passes
- ✅ Attack rolls with pools
- ✅ Defense with armor
- ✅ Glitch and critical glitch detection
- ✅ Round management
- ✅ Damage tracking (physical and stun) with staging
- ✅ Wound modifiers
- ✅ Combat Pool management (Quickness + Intelligence + Willpower) / 2
- ✅ Pool allocation (attack, defense, damage, other)
- ✅ Vehicle combat and drone control

### GM Toolkit
- ✅ **NPC Generator**: Generate NPCs with roles, stats, motivations, and backstories
  - Roles: Corporate Exec, Fixer, Street Doc, Shadowrunner, Corporate Guard, Terrorist
  - Includes: Name, Company, Description, Attributes, Motivation, Backstory
- ✅ **Mission Generator**: Create Shadowrun missions by type
  - Types: Cyberdeck, Assassination, Extraction, Theft, Investigation
  - Randomized templates with placeholders for customization
- ✅ **Location Generator**: Generate locations for your campaign
  - Types: Corporate, Seedy, Safehouse, Combat
- ✅ **Plot Hook Generator**: Get random plot hooks to spark adventures
- ✅ **Loot Generator**: Generate loot drops and rewards
- ✅ **Random Event Generator**: Add unexpected events to your sessions
- ✅ **Equipment Generator**: Generate weapons, armor, cyberware, and general equipment

### Dice System
- ✅ Standard dice notation (2d6, 1d20+5, 4d6k3)
- ✅ Shadowrun-specific success counting (5+ = success)
- ✅ Friedman dice (exploding sixes)
- ✅ Initiative calculation
- ✅ Cryptographically secure RNG
- ✅ Edge rolls with explosion
- ✅ Hacking Pool (Intelligence + Magic) / 2
- ✅ Magic Pool (Magic + Charisma) / 2
- ✅ Astral Combat Pool (Astral Quickness) / 2
- ✅ Task Pool for specialized activities

### Web UI
- ✅ GM Dashboard for character management
- ✅ Real-time combat status
- ✅ Matrix and ICE monitoring
- ✅ REST API for external integration
- ✅ Swagger/OpenAPI documentation
- ✅ JWT authentication
- ✅ Rate limiting
- ✅ Comprehensive documentation of all features

## 🚀 Installation

### Prerequisites
- .NET 8.0 SDK
- Discord Bot Token
- Discord Client ID
- Discord Guild ID (optional, for testing)

### Step 1: Clone the Repository
```bash
git clone https://github.com/yourusername/shadowrun-discord-bot.git
cd shadowrun-discord-bot
```

### Step 2: Install Dependencies
```bash
dotnet restore
```

### Step 3: Configure Discord Bot
1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Create a new application
3. Go to the "Bot" tab and create a bot
4. Copy the **Token**
5. Copy the **Client ID**
6. Enable necessary intents (Message Content, Guild Messages, etc.)

### Step 4: Set Environment Variables

**Option A: Environment Variables**
```bash
export DISCORD_TOKEN="your_bot_token_here"
export CLIENT_ID="your_client_id_here"
export GUILD_ID="your_guild_id_here"
export JWT_SECRET="your_jwt_secret_minimum_32_characters"
```

**Option B: appsettings.json**
```json
{
  "Discord": {
    "Token": "your_bot_token_here",
    "ClientId": "your_client_id_here",
    "GuildId": "your_guild_id_here"
  },
  "WebUI": {
    "JwtSecret": "your_jwt_secret_here"
  }
}
```

**Option C: .env File**
```bash
cp .env.example .env
# Edit .env with your credentials
```

### Step 5: Run the Bot
```bash
dotnet run
```

The bot will:
1. Ask for your Discord Token interactively (if not configured)
2. Create the SQLite database
3. Register all slash commands
4. Connect to Discord
5. Display the Web UI at http://localhost:5000
6. Show all available commands

## 📖 Usage

### Core Commands

**Dice Rolling:**
- `/dice [notation]` - Roll dice (e.g., `/dice 2d6+3`)
- `/shadowrun-dice basic [pool] [target]` - Shadowrun dice pool roll
- `/shadowrun-dice combat [skill] [combat-pool]` - Combat roll with pool allocation
- `/shadowrun-dice initiative [reaction] [initiative-dice]` - Roll initiative

**Character Management:**
- `/character create` - Create a new character
- `/character list` - List your characters
- `/character view [name]` - View your character sheet
- `/character delete [name]` - Delete your character

### Magic Commands

**Magic Status:**
- `/magic status` - View your magic rating and attributes
- `/magic spells` - Browse all known spells
- `/magic foci` - View active magical foci

**Spell Casting:**
- `/magic cast [spell]` - Cast a spell with dice rolling
- `/magic summon [type] [force]` - Summon a spirit

### Matrix Commands

**Matrix Status:**
- `/matrix status` - View Matrix status
- `/matrix deck-info` - View cyberdeck specs
- `/matrix programs` - List installed programs
- `/matrix ice` - View deployed ICE
- `/matrix session` - View current Matrix session

**Matrix Actions:**
- `/matrix initiative` - Roll Matrix initiative
- `/matrix crack-ice [type] [rating]` - Attempt to break ICE
- `/matrix attack [target] [program-rating]` - Launch Matrix attack
- `/matrix bypass [system-type] [rating]` - Bypass security
- `/matrix program-list [type]` - Browse available programs
- `/matrix load-program [name]` - Load a program into active memory
- `/matrix unload-program [name]` - Unload a program
- `/matrix install-program [name] [type] [rating] [memory]` - Install a new program
- `/matrix toggle-vr` - Toggle between AR and VR mode

### Combat Commands

**Combat Management:**
- `/combat start` - Start a new combat
- `/combat status` - View combat status and initiative order
- `/combat end` - End the active combat
- `/combat add [name] [type]` - Add a combatant (type: player/enemy)
- `/combat remove [name]` - Remove a combatant
- `/combat next` - Advance to the next turn
- `/combat attack [attacker] [target] [attack-pool]` - Make an attack
- `/combat reroll-init` - Reroll initiative for all combatants

### Cyberware Commands
- `/cyberware list [category]` - List available cyberware

### Help
- `/help` - Get general help
- `/help [command]` - Get specific command help

## 🌐 Web UI

The bot includes a comprehensive GM Dashboard available at `http://localhost:5000`

### Features:
- **Character Management**: View and manage all characters
- **Combat Tracking**: Real-time combat status and initiative order
- **Matrix Monitoring**: View deck info, programs, and ICE
- **Tools**: Quick access to dice roller and spell database
- **API Documentation**: Swagger UI for all endpoints

### API Endpoints:
- `GET /health` - Health check
- `GET /api/character/all` - List all characters
- `GET /api/character/{id}` - Get specific character
- `GET /api/combat/active` - Get active combat session
- `POST /api/combat/start` - Start a new combat
- `POST /api/combat/{id}/add-participant` - Add combatant
- `POST /api/dice/roll` - Roll dice (body: `{ "notation": "2d6+3" }`)
- `POST /api/dice/shadowrun` - Shadowrun roll (body: `{ "poolSize": 6, "targetNumber": 4 }`)

### Swagger Documentation
Access the API documentation at: `http://localhost:5000/api-docs`

## 🔧 Development

### Project Structure
```
shadowrun-discord-bot/
├── Models/              # EF Core entities
│   ├── ShadowrunCharacter.cs
│   ├── MagicSystem.cs
│   ├── MatrixSystem.cs
│   ├── CombatSystem.cs
│   └── DiceRollResult.cs
├── Services/            # Business logic
│   ├── DatabaseService.cs
│   ├── DiceService.cs
│   ├── MagicService.cs
│   ├── MatrixService.cs
│   ├── CombatService.cs
│   ├── WebUIService.cs
│   └── ErrorHandlingService.cs
├── Controllers/         # Web API controllers
│   ├── CharacterController.cs
│   ├── CombatController.cs
│   ├── DashboardController.cs
│   └── DiceController.cs
├── Core/                # Discord bot logic
│   ├── BotService.cs
│   ├── CommandHandler.cs
│   └── ErrorHandler.cs
├── Commands/            # Command modules
│   ├── BaseCommandModule.cs
│   └── CharacterCommands.cs
├── Resources/           # Static data files
├── Program.cs           # Entry point
├── BotConfig.cs         # Configuration
└── ShadowrunDiscordBot.csproj
```

### Running in Development
```bash
# Build in development mode
dotnet build

# Run with hot reload
dotnet watch run

# Run tests
dotnet test

# Format code
dotnet format
```

### Adding New Commands
1. Add the command definition to `CommandHandler.cs` in the `BuildSlashCommands` method
2. Create the handler method in `CommandHandler.cs`
3. Add necessary input validation
4. Test the command in Discord

### Database Migrations
```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update
```

## 🔒 Security

The bot implements several security measures:
- **Rate Limiting**: 100 requests per minute per IP
- **CORS**: Configured CORS for web UI
- **Input Validation**: Comprehensive validation for all inputs (FluentValidation)
- **Error Handling**: Graceful error handling with detailed logging
- **Token Storage**: Token should be stored securely (environment variables or config file)
- **JWT Authentication**: Secure API access with JWT tokens
- **SQL Injection Prevention**: EF Core parameterized queries
- **Cryptographically Secure RNG**: For dice rolls

## 🐛 Troubleshooting

### Bot not connecting to Discord
- Verify your Discord Token is correct
- Check your internet connection
- Ensure Discord Developer Portal settings are correct
- Verify the bot has necessary intents enabled

### Database errors
- Delete `shadowrun.db` (or `data/shadowrun_characters.db`) and restart the bot
- The database will be recreated automatically
- Check file permissions for the database directory

### Commands not working
- Verify you're using slash commands (not text commands)
- Make sure you've waited a few minutes for commands to propagate
- Try `/help` to see all available commands
- Check the bot logs for error messages

### Web UI not accessible
- Verify port 5000 is not in use by another application
- Check firewall settings
- Ensure the WebUIService is running

## 📚 Resources

- [Shadowrun 3rd Edition](https://www.shadowrun.com/rpg/)
- [Discord.Net Documentation](https://discordnet.readthedocs.io/)
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

## 🐳 Docker Support

```bash
# Build the Docker image
docker build -t shadowrun-discord-bot .

# Run the container
docker run -d \
  -e DISCORD_TOKEN=your_token \
  -e CLIENT_ID=your_client_id \
  -e GUILD_ID=your_guild_id \
  -e JWT_SECRET=your_jwt_secret \
  -p 5000:5000 \
  shadowrun-discord-bot

# Or use docker-compose
docker-compose up -d
```

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 👥 Credits

- Built with [.NET 8](https://dotnet.microsoft.com/)
- Powered by [Discord.Net](https://github.com/discord-net/Discord.Net)
- Database: [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- Shadowrun 3rd Edition rules

## 📚 Command Reference

### Character Commands
- `/character create [name] [metatype] [archetype]` - Create a new character
- `/character list` - List your characters
- `/character view [name]` - View character details
- `/character delete [name]` - Delete a character

### Dice Commands
- `/dice [notation]` - Roll dice using standard notation (e.g., 2d6+3)
- `/shadowrun-dice basic [pool] [target]` - Roll Shadowrun dice pool
- `/shadowrun-dice initiative [reaction] [dice]` - Calculate initiative

### Combat Commands
- `/combat start` - Start a combat session
- `/combat status` - View current combat status
- `/combat end` - End the combat session
- `/combat add [name] [type] [init-dice]` - Add a combatant
- `/combat remove [name]` - Remove a combatant
- `/combat next` - Advance to next turn
- `/combat attack [attacker] [target] [pool] [defense] [weapon] [damage]` - Execute attack
- `/combat reroll-init` - Reroll initiative for all combatants

### Magic Commands
- `/magic status` - View your magic status
- `/magic spells` - List known spells
- `/magic foci` - View active foci
- `/magic cast [spell]` - Cast a spell
- `/magic summon [type] [force]` - Summon a spirit

### Matrix Commands
- `/matrix status` - View cyberdeck status
- `/matrix deck-info` - View deck information
- `/matrix programs` - List installed programs
- `/matrix ice` - View active ICE
- `/matrix session` - View Matrix session status
- `/matrix initiative` - Roll Matrix initiative
- `/matrix crack-ice [type] [rating]` - Attempt to crack ICE
- `/matrix attack [target] [program-rating]` - Launch Matrix attack
- `/matrix bypass [system-type] [rating]` - Bypass security system
- `/matrix load-program [name]` - Load a program
- `/matrix unload-program [name]` - Unload a program
- `/matrix install-program [name] [type] [rating] [memory]` - Install a new program
- `/matrix toggle-vr` - Toggle between AR and VR mode
- `/matrix program-list [type]` - List available programs

### Cyberware Commands
- `/cyberware list [category]` - List available cyberware

### GM Toolkit Commands
- `/npc [role]` - Generate an NPC (roles: corporate exec, fixer, street doc, shadowrunner, corporate guard, terrorist)
- `/mission [type]` - Generate a mission (types: cyberdeck, assassination, extraction, theft, investigation)
- `/location [type]` - Generate a location (types: corporate, seedy, safehouse, combat)
- `/plot-hook` - Get a random plot hook
- `/loot` - Generate loot
- `/random-event` - Get a random event
- `/equipment [type]` - Generate equipment (types: weapon, armor, cyberware, general)

### Enhanced Systems (SR3 Rulebook Complete)

**Astral Space:**
- `/astral project` - Begin astral projection
- `/astral perception` - Toggle astral sight
- `/astral combat` - Perform astral combat actions
- `/magic foci bond` - Bond a magical focus
- `/magic foci activate` - Activate a focus

**Matrix Depth:**
- `/matrix system-stats` - View system ratings
- `/matrix security tally` - Check current security level
- `/matrix alert-level` - See current alert status
- `/matrix ice [type]` - Deploy or view IC by type

**Combat Pool:**
- `/combat pool [character]` - View combat pool allocation
- `/combat assign-pool [character] [attack/defense/damage/other] [amount]` - Allocate pool

**Vehicle Combat:**
- `/vehicle maneuver [character] [score]` - Set maneuver score
- `/vehicle sensors [character] [range]` - Check sensor range
- `/vehicle control-mode [mode]` - Set drone control mode

**Contacts & Legwork:**
- `/contacts list` - View all contacts
- `/contacts [name] [level]` - Add or update contact
- `/legwork [type]` - Perform legwork actions
- `/johnson meet` - Initiate Johnson negotiation

**Karma System:**
- `/karma status` - View karma points
- `/karma improve [attribute/skill] [name]` - Improve attribute or skill
- `/karma pool` - View karma pool for rerolls

**Damage & Healing:**
- `/damage [type] [value]` - Apply damage with staging
- `/heal [method] [character]` - Perform healing
- `/check [stat]` - Check condition monitor status

## 📋 Changelog

### Version 1.2.0 (2026-03-09)
- ✅ **Astral Space**: Full projection, perception, combat, and foci system
- ✅ **Matrix Depth**: System ratings, security tally, alert escalation, multiple IC types
- ✅ **Combat Pool**: Formula-based pool calculation with allocation system
- ✅ **Vehicle Combat**: Maneuver scores, sensor tests, drone control modes
- ✅ **Contacts/Legwork**: Contact levels, etiquette tests, Johnson meetings, fixers
- ✅ **Karma System**: Advancement tracking, skill/attribute improvements, reroll pool
- ✅ **Damage/Healing**: Condition monitors, staging, healing times, biotech/first aid
- ✅ Comprehensive documentation and command reference

### Version 1.0.0 (2025-03-08)
- Complete .NET 8 rewrite
- Implemented all Shadowrun 3rd Edition systems
- Added Web UI for GM tools
- Performance optimizations throughout
- Comprehensive error handling and logging
- Full async/await patterns
- SQLite database with EF Core
- RESTful API with Swagger documentation

---

**Made with ❤️ for the Shadowrun community**
