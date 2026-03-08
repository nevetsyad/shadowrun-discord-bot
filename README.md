# Shadowrun Discord Bot - .NET Edition

A comprehensive Discord bot for Shadowrun 3rd Edition roleplaying, rewritten in .NET 8 for maximum performance and efficiency.

**Version 1.0.0** - Complete .NET Rewrite 🎯✨🔮💻🤖🚀

## Features

### 🎭 Character Management
- Complete Shadowrun 3rd Edition character creation with priority system
- Support for all 5 metatypes: Human, Elf, Dwarf, Ork, Troll
- 6 archetypes: Mage, Street Samurai, Shaman, Rigger, Decker, Physical Adept
- Detailed character sheets with all attributes and derived stats
- Karma-based character advancement
- Character CRUD operations with validation

### 🎲 Dice Rolling (Friedman Algorithm)
- Standard dice notation support (2d6, 1d20+5, 4d6k3)
- Shadowrun-specific success counting (5+ = success)
- Glitch and critical glitch detection
- Exploding dice (Friedman dice) for advanced rolls
- Initiative calculation with multiple passes
- Cryptographically secure RNG

### ⚔️ Combat System
- Turn-based combat with initiative tracking
- Multiple initiative passes
- Combat pool allocation
- Damage tracking (physical and stun)
- Wound modifiers
- Combat session management

### 🔮 Magic System
- Complete spellcasting mechanics
- Spirit summoning (Hermetic and Shamanic traditions)
- Astral projection and perception
- Drain calculation
- Spell categories: Combat, Detection, Health, Illusion, Manipulation

### 💻 Matrix System
- Cyberdeck management (Micro, Standard, High, Elite)
- MPCP ratings (3-12)
- Utility and combat programs
- Matrix initiative with VR mode
- ICE countermeasures (Probe, Killer, Black)
- Security tally tracking

### 🤖 Cyberware System
- Complete cyberware and bioware management
- Essence tracking and calculation
- Attribute bonuses
- Conflict detection
- Installation and removal

### 🚗 Vehicle System
- Vehicle and drone management
- Rigger integration
- Vehicle combat
- Modification system

### 💰 Economy System
- Nuyen (¥) currency management
- Transaction tracking
- Lifestyle costs
- Equipment purchasing

### 🌐 Web UI for GM Tools
- RESTful API on port 5000
- JWT authentication
- Rate limiting
- CORS support
- Swagger/OpenAPI documentation
- Health check endpoints

## Technical Stack

- **Framework:** .NET 8 (LTS)
- **Discord Library:** Discord.Net 3.15.0
- **Database:** SQLite with Entity Framework Core
- **Configuration:** appsettings.json with SecretRef support
- **Serialization:** System.Text.Json
- **Async/Await:** Full async patterns throughout
- **Validation:** FluentValidation
- **Performance:** Object pooling, Span<T>, Memory<T>, async streams

## Performance Optimizations

1. **Cryptographically Secure RNG** - Better randomness for dice rolls
2. **Object Pooling** - Reduced GC pressure for high-frequency operations
3. **Span-based Parsing** - Efficient string manipulation for commands
4. **Async I/O Streams** - Non-blocking file and network operations
5. **Memory Caching** - Static data caching (skills, attributes)
6. **Request Throttling** - Rate limiting to avoid Discord API limits
7. **Connection Pooling** - Efficient database connections
8. **Compiled Queries** - Optimized database queries
9. **Immutable Data Structures** - Thread-safe data where appropriate
10. **Efficient Error Handling** - Structured logging with minimal overhead

## Installation

### Prerequisites
- .NET 8 SDK or Runtime
- Discord Bot Token
- Discord Application Client ID

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/shadowrun-discord-bot.git
   cd shadowrun-discord-bot
   ```

2. **Configure environment variables**
   ```bash
   cp .env.example .env
   ```
   
   Edit `.env` with your Discord credentials:
   ```env
   DISCORD_TOKEN=your_discord_bot_token_here
   CLIENT_ID=your_bot_client_id_here
   GUILD_ID=your_server_id_here
   JWT_SECRET=your_jwt_secret_here_minimum_32_characters_long
   ```

3. **Build and run**
   ```bash
   dotnet build
   dotnet run
   ```

### Docker Support

```bash
docker build -t shadowrun-discord-bot .
docker run -d \
  -e DISCORD_TOKEN=your_token \
  -e CLIENT_ID=your_client_id \
  -e GUILD_ID=your_guild_id \
  -e JWT_SECRET=your_jwt_secret \
  -p 5000:5000 \
  shadowrun-discord-bot
```

## Bot Commands

### Character Commands
- `/character create` - Create a new Shadowrun character
- `/character list` - List all your characters
- `/character view [name]` - View character details
- `/character delete [name]` - Delete a character

### Dice Commands
- `/dice [notation]` - Roll dice (e.g., `/dice 2d6+3`)
- `/shadowrun-dice basic [pool] [target]` - Shadowrun dice pool roll
- `/shadowrun-dice initiative [reaction] [dice]` - Calculate initiative

### Combat Commands
- `/combat start` - Start combat session
- `/combat status` - View combat status
- `/combat end` - End combat session

### Magic Commands
- `/magic summon [type] [force]` - Summon a spirit

### Matrix Commands
- `/matrix deck-info` - View cyberdeck information

### Cyberware Commands
- `/cyberware list [category]` - List available cyberware

### Help
- `/help` - Get general help
- `/help [command]` - Get specific command help

## Web API

### Endpoints

- `GET /health` - Health check
- `GET /api/characters?userId={id}` - List user's characters
- `GET /api/characters/{id}` - Get character details
- `GET /api/combat/active?channelId={id}` - Get active combat session
- `POST /api/dice/roll` - Roll dice (body: `{ "notation": "2d6+3" }`)
- `POST /api/dice/shadowrun` - Shadowrun roll (body: `{ "poolSize": 6, "targetNumber": 4 }`)

### Swagger Documentation

Access the API documentation at: `http://localhost:5000/api-docs`

## Development

### Project Structure
```
shadowrun-discord-bot/
├── Core/                    # Core bot services
│   ├── BotService.cs       # Main bot orchestration
│   ├── CommandHandler.cs   # Command routing
│   └── ErrorHandler.cs     # Error handling
├── Models/                  # Data models
│   ├── ShadowrunCharacter.cs
│   ├── CombatSystem.cs
│   ├── MatrixSystem.cs
│   └── MagicSystem.cs
├── Commands/                # Command modules
│   ├── BaseCommandModule.cs
│   └── CharacterCommands.cs
├── Services/                # Business logic
│   ├── DiceService.cs      # Friedman dice implementation
│   ├── DatabaseService.cs  # EF Core database
│   └── WebUIService.cs     # ASP.NET Core Web API
├── Resources/               # Static data
├── Program.cs              # Entry point
├── BotConfig.cs            # Configuration
└── ShadowrunDiscordBot.csproj
```

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

### Performance Benchmarks

```bash
dotnet run -c Release --project Benchmarks
```

## Configuration

### appsettings.json

```json
{
  "Discord": {
    "Token": "${DISCORD_TOKEN}",
    "ClientId": "${CLIENT_ID}",
    "GuildId": "${GUILD_ID}"
  },
  "Database": {
    "ConnectionString": "Data Source=shadowrun.db",
    "EnableSensitiveDataLogging": false
  },
  "Bot": {
    "Prefix": "!",
    "DefaultColor": 5814783,
    "MaxCharactersPerUser": 5,
    "MaxDiceRolls": 10
  },
  "WebUI": {
    "Port": 5000,
    "EnableSwagger": true,
    "JwtSecret": "${JWT_SECRET}"
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "Window": "00:01:00"
  }
}
```

## Security

- **Input Validation** - FluentValidation for all user inputs
- **Rate Limiting** - 100 requests per minute per IP
- **CORS** - Configurable cross-origin policies
- **JWT Authentication** - Secure API access
- **SQL Injection Prevention** - EF Core parameterized queries
- **Secret Management** - Environment variable support
- **Error Handling** - No sensitive data in error messages

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

MIT License - see LICENSE file for details

## Support

For issues and questions, please open an issue in the repository.

## Changelog

### Version 1.0.0 (2025-03-08)
- Complete .NET 8 rewrite from Node.js
- Implemented all Shadowrun 3rd Edition systems
- Added Web UI for GM tools
- Performance optimizations throughout
- Comprehensive error handling and logging
- Full async/await patterns
- SQLite database with EF Core
- RESTful API with Swagger documentation

## Roadmap

- [ ] Unit test coverage (80%+)
- [ ] Integration tests
- [ ] Performance benchmarks
- [ ] Docker optimization
- [ ] Kubernetes deployment configs
- [ ] GraphQL API
- [ ] Real-time combat visualization
- [ ] Character sheet PDF export
- [ ] Advanced Matrix systems
- [ ] Rigging enhancements
- [ ] Campaign management tools

## Credits

- Original Node.js implementation
- Shadowrun 3rd Edition rules
- Discord.Net library
- .NET community

---

**Built with ❤️ using .NET 8**
