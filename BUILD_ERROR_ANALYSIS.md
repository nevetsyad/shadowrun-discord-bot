# Build Error Analysis - Shadowrun Discord Bot Migration

## Summary
**Total Errors:** 439 compilation errors across the project.

## Error Categories

### 1. Type Mismatches (Models vs Domain Entities) - ~250 errors
**Root Cause:** Services and commands use old `ShadowrunDiscordBot.Models.*` types while repositories return new `ShadowrunDiscordBot.Domain.Entities.*` types.

**Affected Files:**
- `Services/DatabaseService.cs` - Character, CombatSession, MatrixRun conversions
- `Services/DatabaseService.Enhanced.cs` - CombatPoolState, Vehicle, Drone, KarmaRecord conversions
- `Services/DatabaseService.GameSession.cs` - GameSession, SessionParticipant, NarrativeEvent conversions
- `Services/DatabaseService.MissionDefinition.cs` - MissionDefinition conversions
- `Services/DatabaseService.SessionManagement.cs` - SessionBreak, CompletedSession conversions

**Pattern:**
```csharp
// Old: return ShadowrunDiscordBot.Models.ShadowrunCharacter
// New: return ShadowrunDiscordBot.Domain.Entities.Character
return _context.Characters.Find(id); // Returns Domain entity, not Model
```

### 2. Missing DbSets in ShadowrunDbContext - ~50 errors
**Root Cause:** Phase 5 entities not registered as DbSets in DbContext.

**Missing DbSets Found:**
- `HostICE` - Matrix/ICE encounters (DatabaseService.Enhanced.cs)
- `SessionBreaks` - Session breaks tracking (DatabaseService.SessionManagement.cs)
- `NarrativeEvents` - Story events (DatabaseService.GameSession.cs)

**Error Examples:**
```csharp
'ShadowrunDbContext' does not contain a definition for 'HostICE'
'GameSession' does not contain a definition for 'Breaks'
```

### 3. Property Mismatches - ~40 errors
**Root Cause:** Different property names between old Models and new Domain entities.

**Examples:**
- `CharacterContact.Name` → should be `CharacterContact.DisplayName` or similar
- `CompletedSession.TotalKarmaAwarded`, `TotalNuyenAwarded`, `Summary` - missing properties
- `GameSession.Name`, `Description`, `IsActive`, `CreatedBy` - missing properties
- `CharacterSkill.Attribute` - missing property
- `CharacterCyberware.Type`, `Grade`, `Description` - missing properties

### 4. Enum Conflicts & API Incompatibilities - ~100 errors
**Root Cause:** Old enum types vs new enum types, nullable reference issues.

**Examples:**
- `SessionStatus` - does not exist in current context (CommandHandler.cs)
- `MissionStatus` - does not exist in current context
- `NarrativeEventType` - could not be found
- `ConsequenceType` vs `int` comparisons (AutonomousMissionService.cs)
- `Color.Cyan` - does not exist in Discord.NET

### 5. Nullability Issues - ~30 errors
**Root Cause:** C# nullable reference type mismatches.

**Examples:**
- `Task<IEnumerable<DomainEvent?>>` vs `Task<IEnumerable<DomainEvent>>` (EventStore.cs)
- Non-nullable properties not initialized in constructors

### 6. Method/Property Not Found - ~50 errors
**Root Cause:** Old API methods removed or renamed.

**Examples:**
- `DatabaseService.GetContext()` - does not exist
- `DiceService.Roll()` - does not exist
- `DiscordSocketClient.IsConnected` → should use `ConnectionState` enum
- `DiscordSocketClient.BulkOverwriteGuildCommandAsync()` - removed API

## Priority Fix Plan

### Phase 1: Critical Database Layer Fixes (1 hour)
**Goal:** Get DbContext working with all required DbSets

1. Add missing DbSets to ShadowrunDbContext:
   - `DbSet<HostICE> HostICE { get; set; }`
   - `DbSet<SessionBreak> SessionBreaks { get; set; }`
   - `DbSet<NarrativeEvent> NarrativeEvents { get; set; }`

2. Fix `ShadowrunDbContext` constructor and registration

### Phase 2: Mapper Classes (2-3 hours)
**Goal:** Create bidirectional mappers between Models and Domain entities

Create mapper classes:
- `CharacterMapper` - ShadowrunCharacter ↔ Character
- `CombatSessionMapper` - CombatSession ↔ CombatSession (Domain)
- `GameSessionMapper` - GameSession ↔ GameSession (Domain)
- `MatrixRunMapper` - MatrixRun ↔ MatrixRun (Domain)
- `VehicleMapper` - Vehicle ↔ Vehicle (Domain)
- `DroneMapper` - Drone ↔ Drone (Domain)

### Phase 3: Update Services (2 hours)
**Goal:** Replace Model usage with Domain entities in services

1. Update `DatabaseService.cs` to use mappers
2. Update `CharacterService.cs` to use Domain entities
3. Update all DatabaseService partial files

### Phase 4: Enum & API Fixes (1 hour)
**Goal:** Fix enum conflicts and deprecated APIs

1. Replace `SessionStatus` with correct enum
2. Update Discord.NET API calls to v4+ syntax
3. Fix nullable reference warnings

## Next Steps
1. ✅ Analyze errors (DONE)
2. 🔴 Add missing DbSets to ShadowrunDbContext
3. 🟡 Create mapper classes for all entity pairs
4. 🟢 Update DatabaseService and CharacterService to use mappers
5. 🔵 Fix enum conflicts and API incompatibilities
