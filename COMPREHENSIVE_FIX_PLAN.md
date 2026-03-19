# Shadowrun Discord Bot - 4-Step Fix Plan

**Goal:** Reduce ~395 compilation errors → build passes cleanly. Estimated 8-12 hours if done carefully, with clear checkpoints.

---

## Step 1: Add Missing DbSet Properties (Phase5 Entities)
**Estimated Time:** ~60 minutes

### What It Does:
- Register all missing DbSet properties in `ShadowrunDiscordBot.Infrastructure.Data.ShadowDbContext`
  - Characters, Vehicles, Spells (Phase5 entities)
- Ensure foreign keys exist where necessary

### How to Do It:
```csharp
// In ShadowDbContext.cs, add properties like this (after existing DbSet declarations):
public class Characters { get; set; } // => Set<Character>()
```

### Checklist:
- [ ] Add DbSet for each missing entity type (check BUILD_ERROR_ANALYSIS.md)
  - Characters, Vehicles maybe more?
- [ ] Verify all properties are accessible with correct types
[ ]
```

### How to Validate:
- `dotnet build` should show **~350 errors** less than current 395

### Notes:
- This is the low-hanging fruit — just boilerplate setup, no logic changes

---

## Step 2: Create Mapper Classes (Type Mismatches)
**Estimated Time:** ~150-180 minutes

### What It Does:
Create mapper classes to bridge old Models ↔ new Domain entities. You already identified 7 services with model/entity mismatches.

### How to Do It:
Create `Mappers/` folder and implement 7 mappers:

- **CharacterMapper.cs** – Map between `Model.Character` ↔ Domain.Entities.Player
  - For services that return Players but use Characters, or vice versa

- **VehicleMapper.cs** – Map `Model.Vehicle` ↔ Domain.Entities.GameObject
    - For services that need vehicles

- **SpellMapper.cs** – Map `Model.Spell` ↔ Domain.Entities.GameEffect
  - For effect/spell services

- **LocationMapper.cs** – Map `Model.Location` ↔ Domain.Entities.SpatialObject
  - For location-based services

- **SkillMapper.cs** – Map `Model.Skill` ↔ Domain.Entities.Ability
  - For skill checks

- **GearMapper.cs** – Map `Model.Gear` ↔ Domain.Entities.Item
  - For equipment services

- **PackerMapper.cs** – (if applicable) Map `Model.Pack` ↔ Domain.Entities.Inventory

### Patterns to Use:
```csharp
public class CharacterMapper {
    public static Player ModelToEntity(Model.Character m) { ... }
  [ ]
```

### Checklist:
- Create `Mappers/CharacterMapper.cs`
[ ] Implement Model→Entity conversion for all properties
  - [ ]
- Create `Mappers/VehicleMapper.cs`
[ ] Implement Model→Entity conversion
  - [ ]
- Create `Mappers/SpellMapper.cs`
[ ] Implement Model→Entity conversion
  - [ ]
- Repeat for remaining mappers listed above

```

### How to Validate:
Run build after each mapper — errors should drop by ~200 (the type mismatch count)

### Notes:
- Reuse mappings where possible — e.g., CharacterMapper may also be used by Location if appropriate

---

## Step 3: Refactor DatabaseService to Use Mappers + Proper Types
**Estimated Time:** ~60-90 minutes

### What It Does:
Update `Services/DatabaseService.cs` to use mappers and fix entity type usage

### How to Do It:
```csharp
// OLD (broken):
public IEnumerable<Model.Character> GetCharacters() { ... } // Returns Models, but callers expect Domain

// NEW (fixed):
public IEnumerable<Player> GetPlayers() { // Returns Entities, typed correctly
  var result = _context.Entities.ToList();
[ ] return CharacterMapper.ModelToEntity(result); // Convert to Models if services need them, or just work with Entities directly

```

### Checklist:
- Update all repository methods to return proper entity types (not Models)
  - [ ] `GetCharacter()` → returns Entity, with mapper if needed by callers
[ ]
- Apply CharacterMapper where Model→Entity conversion is required

```

### How to Validate:
Build should show **~140 errors** (395 - 200 mappers, leaving ~70 unresolved)

### Notes:
- Start with methods that are causing most errors — check BUILD_ERROR_ANALYSIS.md for top 5 error messages

---

## Step 4: Refactor CharacterService (Domain/Application Layer Integration)
**Estimated Time:** ~120-150 minutes

### What It Does:
Bring CharacterService into the new architecture — use mappers, work with proper entity types

### How to Do It:
```csharp
// OLD (broken):
public async Task<ServiceResult> AddCharacter(...)
[ ] { var entity = _repo.Get(); // Wrong type, wrong context

// NEW (fixed):
public async Task<ServiceResult> AddCharacter(...)
{ ]
  var entity = await _repo.CreateAsync(entity); // Proper Entity type, proper context

```

### Checklist:
- Identify all CharacterService methods that use old Models or wrong types
  - [ ] Update each method signature to accept Entity, return mapped Model if needed

```

### How to Validate:
Run build — should pass or show **< 70 errors** (mostly edge cases)

### Notes:
- This is the biggest refactor — be methodical, test incrementally

---

## Emergency Backtrack Points (If Something Breaks)

**Checkpoint A:** After Step 1, if errors don't drop by ~50:
- Revert DbSet changes and check for typos

**Checkpoint B:** After Step 2, if errors don't drop by ~200:
- Revert last mapper and redo with different approach

**Checkpoint C:** After Step 5, if ANY compilation errors remain:
- Use error messages as your new starting point — they'll be simpler

---

## Final Validation Checklist:
- [ ] All 395 errors are gone (or dropped to <10 trivial issues)
[ ]
- [ ] `dotnet build` completes without errors in Production project

```

### Notes:
If you hit blockers, paste the exact error lines from `dotnet build` and I'll help fix that specific section — no need to redo already-fixed work.
