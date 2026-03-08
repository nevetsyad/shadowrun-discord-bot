# Changelog

All notable changes to the Shadowrun Discord Bot (.NET Edition) will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-03-08

### Added
- Complete .NET 8 rewrite from Node.js/Discord.js
- Discord.Net 3.15.0 integration with optimized connection handling
- SQLite database with Entity Framework Core for character persistence
- Comprehensive character management system
  - All 5 metatypes (Human, Elf, Dwarf, Ork, Troll)
  - 6 archetypes with default skills and equipment
  - Attribute tracking and derived stats
  - Karma and Nuyen management
  - Condition monitors (Physical/Stun)
- Friedman dice rolling algorithm
  - Standard dice notation support
  - Shadowrun-specific success counting (5+)
  - Glitch detection
  - Exploding dice for advanced rolls
  - Initiative calculation with multiple passes
  - Cryptographically secure RNG
- Combat system
  - Turn-based initiative tracking
  - Multiple initiative passes
  - Combat pool allocation
  - Damage tracking
- Matrix/cyberdeck system
  - Cyberdeck types (Micro, Standard, High, Elite)
  - MPCP ratings (3-12)
  - Program management
  - Matrix initiative
  - ICE countermeasures
- Magic system
  - Spellcasting mechanics
  - Spirit summoning (Hermetic/Shamanic)
  - Astral projection tracking
- Cyberware system
  - Essence tracking
  - Installation/removal
  - Attribute bonuses
- Web UI for GM tools
  - RESTful API on port 5000
  - JWT authentication
  - Rate limiting (100 req/min)
  - CORS support
  - Swagger/OpenAPI documentation
  - Health check endpoints
- Performance optimizations
  - Object pooling for high-frequency operations
  - Span-based string parsing
  - Async I/O streams
  - Memory caching for static data
  - Compiled database queries
- Security features
  - Input validation with FluentValidation
  - Rate limiting
  - JWT authentication
  - SQL injection prevention
  - Environment variable support
- Configuration
  - appsettings.json with SecretRef support
  - Environment variable overrides
  - Validation on startup
- Error handling
  - Structured logging
  - Error metrics tracking
  - User-friendly error messages
- Documentation
  - Comprehensive README
  - API documentation with Swagger
  - Code comments

### Changed
- Migrated from Node.js/Discord.js to .NET 8/Discord.Net
- Changed database from MySQL to SQLite for simpler deployment
- Improved performance with async/await patterns throughout
- Enhanced error handling with structured logging
- Better configuration management with SecretRef support

### Performance
- Cryptographically secure RNG for dice rolls
- Object pooling reduces GC pressure
- Span-based parsing improves command processing speed
- Async I/O prevents blocking operations
- Compiled queries optimize database access
- Memory caching for frequently accessed data

### Technical Debt
- Node.js dependencies removed
- Circular dependency issues resolved
- Consistent error handling patterns
- Improved code organization

## [0.0.0] - 2025-03-07

### Added
- Initial planning and architecture design
- Project structure defined
- Technology stack selected

---

## Version History

- **1.0.0** (2025-03-08): Complete .NET 8 rewrite - Production ready
- **0.0.0** (2025-03-07): Initial project planning

---

## Upcoming Features

### [1.1.0] - Planned
- Unit test coverage (80%+)
- Integration tests
- Performance benchmarks
- Docker optimization
- Kubernetes deployment configs

### [1.2.0] - Planned
- GraphQL API
- Real-time combat visualization
- Character sheet PDF export
- Advanced Matrix systems

### [1.3.0] - Planned
- Rigging enhancements
- Campaign management tools
- Edge and Flaw system
- Advanced skills
- Lifestyle tracking
