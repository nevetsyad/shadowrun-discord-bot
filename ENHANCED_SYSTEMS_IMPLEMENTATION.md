# Shadowrun 3rd Edition Enhanced Systems Implementation

## Overview
This document summarizes the implementation of missing SR3 rulebook features in the Discord bot.

## Implementation Summary

### 1. Astral Space Rules ✅
**File:** `Services/AstralService.cs`

#### Features Implemented:
- **Astral Projection**
  - Begin/end projection for awakened characters
  - Astral attribute calculation (Astral Body, Strength, Quickness)
  - Duration tracking with stun damage for over-extension
  
- **Astral Perception**
  - Toggle astral sight without full projection
  
- **Astral Combat**
  - Attack rolls using astral combat pool
  - Defense resolution
  - Damage staging (Light → Moderate → Serious → Deadly)
  - Damage resistance using Willpower
  
- **Spirit Forms**
  - Check if spirits can materialize
  - Track astral vs materialized forms
  
- **Foci System**
  - Bond foci using karma (Cost = Force)
  - Activate/deactivate foci
  - Essence cost tracking when active
  - Multiple focus types: Sorcery, Spell, Spirit, Power, Weapon, Sustaining
  
- **Astral Signatures**
  - Detect signatures using Assensing skill
  - Leave signatures from spell casting/summoning
  - Signature decay over time (Force × 6 hours)

### 2. Matrix Depth ✅
**File:** `Services/EnhancedMatrixService.cs`

#### Features Implemented:
- **System Ratings (SR3)**
  - Access Rating - Getting into the system
  - Control Rating - Controlling system functions
  - Index Rating - Finding files/data
  - Files Rating - Manipulating data
  - Slave Rating - Controlling connected devices
  - Security Code - Base detection threshold
  
- **Security Tally Tracking**
  - Incremental tally system
  - Persistent tracking across Matrix run
  
- **Alert Escalation System**
  - None → Passive (tally 10)
  - Passive → Active (tally 20)
  - Active → Shutdown (tally 30)
  - Alert effects (IC activation, response modifiers, trace modifiers)
  
- **IC Types**
  - **White IC** (Non-lethal):
    - Probe - Reconnaissance
    - Trace - Physical location tracing
    - Killer - Deck damage
  - **Gray IC** (Lethal to deck):
    - Blaster - High deck damage
    - Tar - Program destruction
    - Tar Baby - Traps decker
  - **Black IC** (Lethal to decker):
    - Black IC - Biofeedback damage (stun/physical)
    - Black Hammer - Lethal biofeedback
    
- **Matrix Actions**
  - Subsystem access with security consequences
  - IC crashing mechanics

### 3. Combat Pool ✅
**File:** `Services/CombatPoolService.cs`

#### Features Implemented:
- **Combat Pool Calculation**
  - Formula: (Quickness + Intelligence + Willpower) / 2
  
- **Pool Allocation**
  - Attack pool allocation
  - Defense pool allocation
  - Damage pool allocation
  - Other (called shots, etc.)
  
- **Pool Usage**
  - Use allocated dice for actions
  - Roll with skill + pool dice
  - Track successes
  
- **Pool Refresh**
  - Automatic refresh each Combat Turn
  - Reset all allocations
  
- **Additional Pool Types**
  - Hacking Pool = MPCP / 2 (for deckers)
  - Magic Pool = Magic rating (for magicians)
  - Astral Combat Pool = (Charisma + Intelligence + Willpower) / 2
  - Task Pool = Control Rig Rating × 2 (for riggers)

### 4. Vehicle Combat ✅
**File:** `Services/VehicleCombatService.cs`

#### Features Implemented:
- **Vehicle Management**
  - Create vehicles with Body, Armor, Speed, Handling
  - Rigger adaptation installation
  - Control Rig rating tracking
  
- **Maneuver Scores**
  - Base = Handling
  - With driver = Handling + Pilot Skill
  
- **Sensor Tests**
  - Detection rolls using Sensor Rating
  - Range modifiers (Short/Medium/Long/Extreme)
  - Environmental modifiers
  - Target detection and detailed info
  
- **Vehicle Combat**
  - Vehicle initiative (Reaction + 1D6, or +2D6 for riggers)
  - Vehicle combatants in combat sessions
  
- **Drone Control Modes**
  - **Autonomous** - Pilot + 1D6 initiative
  - **Remote** - Rigger Reaction + 1D6
  - **Rigged (VR)** - Rigger Reaction + 2D6
  
- **Drone Operations**
  - Autosoft installation (Targeting, Clearsight, Stealth, etc.)
  - Drone attacks using Pilot + Autosoft
  - Drone damage resistance
  
- **Vehicle Combat Actions**
  - Sensor-enhanced gunnery (Gunnery + Sensor Rating)
  - Manual gunnery (Gunnery only)
  - Vehicle damage resistance (Body + Armor)
  - Maneuver tests

### 5. Contacts/Legwork ✅
**File:** `Services/ContactsLegworkService.cs`

#### Features Implemented:
- **Contact System**
  - Contact levels (1-3): Acquaintance, Associate, Friend
  - Loyalty ratings (1-3)
  - Connection ratings (1-6)
  - Availability modifiers
  - Contact specialties
  
- **Legwork Mechanics**
  - Etiquette-based information gathering
  - Charisma + Etiquette + Contact Loyalty pool
  - Information quality levels: None, Rumors, Basic, Detailed, Comprehensive, Insider
  - Time and nuyen costs
  - Different legwork types (Street, Corporate, Matrix, Government, Underground)
  
- **Johnson Meetings**
  - Create meetings with Mr. Johnson
  - Initial payment offers
  - Negotiation mechanics (each success = +10%)
  - Accept/decline offers
  
- **Fixer Connections**
  - Find work through fixers
  - Availability tests for gear acquisition
  - Mission lead generation

### 6. Karma System ✅
**File:** `Services/KarmaHealingService.cs` (KarmaService class)

#### Features Implemented:
- **Karma Point Tracking**
  - Award karma with reasons
  - Spend karma for advancement
  - Running totals (earned, spent, current)
  
- **Karma Pool**
  - Automatic calculation (1 per 10 karma earned)
  - Use pool for rerolls
  - Refresh between sessions
  
- **Advancement Costs (SR3)**
  - Skill improvement: Rating × 2 karma
  - Attribute improvement: Rating × 3 karma
  - New skill: 2 karma
  - Specialization: 1 karma
  - New spell: 1 karma
  - Initiation: Grade × 10 karma
  - Bind focus: Force × 1 karma
  - Ally spirit: Force × 5 karma
  
- **Character Advancement**
  - Improve skills
  - Learn new skills
  - Improve attributes
  - Learn new spells
  - Initiation for magicians

### 7. Damage/Healing ✅
**File:** `Services/KarmaHealingService.cs` (DamageHealingService class)

#### Features Implemented:
- **Damage Application with Staging**
  - Damage codes (6M, 9S, 12D - Light/Moderate/Serious/Deadly)
  - Net successes stage damage up (2 successes = 1 level)
  - Physical and stun damage
  
- **Condition Monitors**
  - Physical track: (Body + 8) / 2
  - Stun track: (Willpower + 8) / 2
  - Wound modifiers (-1 per 3 boxes)
  - Stun overflow to physical
  - Unconscious/dying status
  
- **Damage Resistance**
  - Body + Armor pool
  - Reduce incoming damage
  
- **Natural Healing**
  - Physical: 1 day per box
  - Stun: 1 hour per box
  - Body test reduces time (2 hours per success)
  
- **First Aid/Biotech**
  - Biotech skill + modifiers (medkit, facility)
  - Target number based on damage level
  - Heal up to skill rating in boxes
  - Heals stun first, then physical
  
- **Magical Healing**
  - Heal spell using Sorcery + Magic
  - Can heal up to Force in boxes
  - Physical damage only

## New Models Added
**File:** `Models/EnhancedSystems.cs`

### Astral System Models:
- `AstralCombatState` - Projection tracking
- `CharacterFocus` - Foci management
- `AstralSignatureRecord` - Signature tracking

### Matrix System Models:
- `MatrixHost` - Host with SR3 ratings
- `HostICE` - IC configurations
- `MatrixRun` - Active run tracking
- `ActiveICEncounter` - IC encounter log

### Combat Pool Models:
- `CombatPoolState` - Pool allocation
- `CombatPoolUsage` - Usage tracking

### Vehicle Models:
- `Vehicle` - Vehicle stats
- `VehicleWeapon` - Mounted weapons
- `Drone` - Drone-specific data
- `DroneAutosoft` - Autosoft programs
- `VehicleCombatSession` - Vehicle combat tracking
- `VehicleCombatant` - Combatant tracking

### Contact Models:
- `CharacterContact` - Contact data
- `LegworkAttempt` - Legwork tracking
- `JohnsonMeeting` - Meeting records

### Karma Models:
- `KarmaRecord` - Karma tracking
- `KarmaExpenditure` - Advancement log

### Damage Models:
- `DamageRecord` - Damage with staging
- `HealingAttempt` - Healing tracking
- `HealingTimeRecord` - Natural healing

## Database Extensions
**File:** `Services/DatabaseService.Enhanced.cs`

Added database operations for:
- Astral states and signatures
- Matrix hosts, ICE, and runs
- Combat pool states
- Vehicles and drones
- Contacts and legwork
- Karma records
- Damage and healing records

## Static Helper Classes

### `KarmaCosts` (in EnhancedSystems.cs)
Provides karma cost calculations for all advancement types.

### `HealingTimes` (in EnhancedSystems.cs)
Provides healing time calculations:
- `PhysicalHealingBase(damage)` - Base hours for physical
- `StunHealingBase(damage)` - Base hours for stun
- `ApplyBodySuccesses(hours, successes)` - Reduced time
- `FirstAidMax(skill)` - Max healable boxes
- `MagicalHealingMax(force)` - Max spell healing

## Integration Notes

### Service Registration
Add these services to your DI container:

```csharp
services.AddScoped<AstralService>();
services.AddScoped<EnhancedMatrixService>();
services.AddScoped<CombatPoolService>();
services.AddScoped<HackingPoolService>();
services.AddScoped<MagicPoolService>();
services.AddScoped<TaskPoolService>();
services.AddScoped<VehicleCombatService>();
services.AddScoped<ContactsLegworkService>();
services.AddScoped<KarmaService>();
services.AddScoped<DamageHealingService>();
```

### Database Migration
After adding the new models, create a migration:

```bash
dotnet ef migrations add AddEnhancedSystems
dotnet ef database update
```

## Testing Recommendations

1. **Astral Space:**
   - Test projection start/end
   - Verify astral combat damage staging
   - Test foci bonding and activation

2. **Matrix:**
   - Test security tally escalation
   - Verify alert level changes
   - Test all IC types

3. **Combat Pool:**
   - Verify pool calculation formula
   - Test allocation limits
   - Verify pool refresh

4. **Vehicles:**
   - Test maneuver score calculation
   - Verify sensor test modifiers
   - Test all drone control modes

5. **Contacts:**
   - Test legwork with and without contacts
   - Verify negotiation mechanics
   - Test fixer operations

6. **Karma:**
   - Test all advancement costs
   - Verify karma pool rerolls
   - Test karma history tracking

7. **Damage/Healing:**
   - Test damage staging
   - Verify condition monitor calculations
   - Test all healing types

## Files Created/Modified

### New Files:
- `Models/EnhancedSystems.cs` - All new model classes
- `Services/AstralService.cs` - Astral space operations
- `Services/EnhancedMatrixService.cs` - Full Matrix system
- `Services/CombatPoolService.cs` - Combat pool management
- `Services/VehicleCombatService.cs` - Vehicle/drone combat
- `Services/ContactsLegworkService.cs` - Contacts and legwork
- `Services/KarmaHealingService.cs` - Karma and healing
- `Services/DatabaseService.Enhanced.cs` - Database operations

### Modified Files:
- `Services/DatabaseService.cs` - Added DbSets and configurations

## Next Steps

1. Create Discord commands for each system
2. Add UI/embeds for displaying results
3. Implement automated testing
4. Add command documentation
5. Create GM tools for managing these systems

---

*Implementation completed: March 9, 2026*
*Shadowrun 3rd Edition Rules Reference*
