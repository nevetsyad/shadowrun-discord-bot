using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Karma tracking and advancement system for SR3
/// </summary>
public class KarmaService
{
    private readonly DiceService _diceService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<KarmaService> _logger;

    public KarmaService(
        DiceService diceService,
        DatabaseService databaseService,
        ILogger<KarmaService> logger)
    {
        _diceService = diceService;
        _databaseService = databaseService;
        _logger = logger;
    }

    #region Karma Tracking

    /// <summary>
    /// Award karma to a character
    /// </summary>
    public async Task<KarmaResult> AwardKarmaAsync(
        int characterId,
        int amount,
        string reason,
        string source = "Mission")
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return KarmaResult.Fail("Character not found.");

        // Get current karma state
        var currentState = await GetOrCreateKarmaRecordAsync(characterId);

        currentState.KarmaChange = amount;
        currentState.TotalEarned += amount;
        currentState.CurrentKarma += amount;
        currentState.Reason = reason;
        currentState.Source = source;

        // Update Karma Pool max (1 per 10 karma earned)
        currentState.KarmaPoolMax = currentState.TotalEarned / 10;

        character.Karma = currentState.CurrentKarma;

        await _databaseService.UpdateKarmaRecordAsync(currentState);
        await _databaseService.UpdateCharacterAsync(character);

        _logger.LogInformation("Awarded {Amount} karma to {CharId} for: {Reason}",
            amount, characterId, reason);

        return new KarmaResult
        {
            Success = true,
            KarmaChange = amount,
            CurrentKarma = currentState.CurrentKarma,
            TotalEarned = currentState.TotalEarned,
            KarmaPoolMax = currentState.KarmaPoolMax,
            Details = $"Awarded {amount} karma for: {reason}"
        };
    }

    /// <summary>
    /// Spend karma
    /// </summary>
    public async Task<KarmaResult> SpendKarmaAsync(
        int characterId,
        int amount,
        string reason)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return KarmaResult.Fail("Character not found.");

        var currentState = await GetOrCreateKarmaRecordAsync(characterId);

        if (currentState.CurrentKarma < amount)
            return KarmaResult.Fail($"Insufficient karma. Have {currentState.CurrentKarma}, need {amount}.");

        currentState.KarmaChange = -amount;
        currentState.TotalSpent += amount;
        currentState.CurrentKarma -= amount;
        currentState.Reason = reason;
        currentState.Source = "Expenditure";

        character.Karma = currentState.CurrentKarma;

        await _databaseService.UpdateKarmaRecordAsync(currentState);
        await _databaseService.UpdateCharacterAsync(character);

        return new KarmaResult
        {
            Success = true,
            KarmaChange = -amount,
            CurrentKarma = currentState.CurrentKarma,
            TotalSpent = currentState.TotalSpent,
            Details = $"Spent {amount} karma on: {reason}"
        };
    }

    /// <summary>
    /// Get karma status
    /// </summary>
    public async Task<KarmaStatus> GetKarmaStatusAsync(int characterId)
    {
        var record = await GetOrCreateKarmaRecordAsync(characterId);

        return new KarmaStatus
        {
            CurrentKarma = record.CurrentKarma,
            TotalEarned = record.TotalEarned,
            TotalSpent = record.TotalSpent,
            KarmaPool = record.KarmaPool,
            KarmaPoolMax = record.KarmaPoolMax
        };
    }

    private async Task<KarmaRecord> GetOrCreateKarmaRecordAsync(int characterId)
    {
        var record = await _databaseService.GetLatestKarmaRecordAsync(characterId);

        if (record == null)
        {
            record = new KarmaRecord
            {
                CharacterId = characterId,
                TotalEarned = 0,
                TotalSpent = 0,
                CurrentKarma = 0,
                KarmaPool = 0,
                KarmaPoolMax = 0,
                Reason = "Initial record",
                Source = "Creation"
            };
            await _databaseService.AddKarmaRecordAsync(record);
        }

        return record;
    }

    #endregion

    #region Karma Pool (Rerolls)

    /// <summary>
    /// Use karma pool for reroll
    /// </summary>
    public async Task<KarmaPoolResult> UseKarmaPoolAsync(
        int characterId,
        int diceToReroll,
        int originalTargetNumber)
    {
        var record = await GetOrCreateKarmaRecordAsync(characterId);

        if (record.KarmaPool < 1)
            return KarmaPoolResult.Fail("No karma pool dice available.");

        if (diceToReroll < 1)
            return KarmaPoolResult.Fail("Must reroll at least 1 die.");

        // Can reroll up to Karma Pool dice
        var actualRerolled = Math.Min(diceToReroll, record.KarmaPool);

        var result = _diceService.RollShadowrun(actualRerolled, originalTargetNumber);

        // Reduce karma pool
        record.KarmaPool -= actualRerolled;
        await _databaseService.UpdateKarmaRecordAsync(record);

        return new KarmaPoolResult
        {
            Success = true,
            DiceRerolled = actualRerolled,
            NewSuccesses = result.Successes,
            RemainingPool = record.KarmaPool,
            Details = $"Rerolled {actualRerolled} dice, got {result.Successes} successes. Pool remaining: {record.KarmaPool}"
        };
    }

    /// <summary>
    /// Refresh karma pool (typically done between sessions)
    /// </summary>
    public async Task RefreshKarmaPoolAsync(int characterId)
    {
        var record = await GetOrCreateKarmaRecordAsync(characterId);
        record.KarmaPool = record.KarmaPoolMax;
        await _databaseService.UpdateKarmaRecordAsync(record);
    }

    #endregion

    #region Character Advancement

    /// <summary>
    /// Improve a skill using karma
    /// </summary>
    public async Task<AdvancementResult> ImproveSkillAsync(
        int characterId,
        string skillName,
        int currentRating)
    {
        var cost = KarmaCosts.SkillImprovement(currentRating);

        var karmaResult = await SpendKarmaAsync(characterId, cost, $"Improve {skillName}");
        if (!karmaResult.Success)
            return AdvancementResult.Fail(karmaResult.Details);

        var expenditure = new KarmaExpenditure
        {
            KarmaRecordId = (await GetOrCreateKarmaRecordAsync(characterId)).Id,
            ExpenditureType = "Skill",
            TargetName = skillName,
            PreviousRating = currentRating,
            NewRating = currentRating + 1,
            KarmaCost = cost
        };
        await _databaseService.AddKarmaExpenditureAsync(expenditure);

        _logger.LogInformation("Character {CharId} improved {Skill} from {Old} to {New}",
            characterId, skillName, currentRating, currentRating + 1);

        return new AdvancementResult
        {
            Success = true,
            Type = "Skill",
            Target = skillName,
            PreviousRating = currentRating,
            NewRating = currentRating + 1,
            KarmaCost = cost,
            Details = $"Improved {skillName} from {currentRating} to {currentRating + 1}"
        };
    }

    /// <summary>
    /// Learn a new skill using karma
    /// </summary>
    public async Task<AdvancementResult> LearnNewSkillAsync(
        int characterId,
        string skillName,
        string? specialization = null)
    {
        var cost = KarmaCosts.NewSkill;

        var karmaResult = await SpendKarmaAsync(characterId, cost, $"Learn {skillName}");
        if (!karmaResult.Success)
            return AdvancementResult.Fail(karmaResult.Details);

        // Add the skill to character
        await _databaseService.AddOrUpdateSkillAsync(characterId, skillName, 1, specialization);

        return new AdvancementResult
        {
            Success = true,
            Type = "New Skill",
            Target = skillName,
            PreviousRating = 0,
            NewRating = 1,
            KarmaCost = cost,
            Details = $"Learned new skill: {skillName}"
        };
    }

    /// <summary>
    /// Improve an attribute using karma
    /// </summary>
    public async Task<AdvancementResult> ImproveAttributeAsync(
        int characterId,
        string attributeName,
        int currentRating)
    {
        var cost = KarmaCosts.AttributeImprovement(currentRating);

        var karmaResult = await SpendKarmaAsync(characterId, cost, $"Improve {attributeName}");
        if (!karmaResult.Success)
            return AdvancementResult.Fail(karmaResult.Details);

        // Update the attribute
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character != null)
        {
            switch (attributeName.ToLower())
            {
                case "body": character.Body++; break;
                case "quickness": character.Quickness++; break;
                case "strength": character.Strength++; break;
                case "charisma": character.Charisma++; break;
                case "intelligence": character.Intelligence++; break;
                case "willpower": character.Willpower++; break;
            }
            await _databaseService.UpdateCharacterAsync(character);
        }

        return new AdvancementResult
        {
            Success = true,
            Type = "Attribute",
            Target = attributeName,
            PreviousRating = currentRating,
            NewRating = currentRating + 1,
            KarmaCost = cost,
            Details = $"Improved {attributeName} from {currentRating} to {currentRating + 1}"
        };
    }

    /// <summary>
    /// Learn a new spell using karma
    /// </summary>
    public async Task<AdvancementResult> LearnNewSpellAsync(
        int characterId,
        string spellName,
        string category,
        int drainModifier)
    {
        var cost = KarmaCosts.NewSpell;

        var karmaResult = await SpendKarmaAsync(characterId, cost, $"Learn spell: {spellName}");
        if (!karmaResult.Success)
            return AdvancementResult.Fail(karmaResult.Details);

        var spell = new CharacterSpell
        {
            CharacterId = characterId,
            Name = spellName,
            Category = category,
            DrainModifier = drainModifier
        };
        await _databaseService.AddCharacterSpellAsync(spell);

        return new AdvancementResult
        {
            Success = true,
            Type = "Spell",
            Target = spellName,
            KarmaCost = cost,
            Details = $"Learned new spell: {spellName}"
        };
    }

    /// <summary>
    /// Initiate (for magicians)
    /// </summary>
    public async Task<AdvancementResult> InitiateAsync(int characterId, int currentGrade)
    {
        var cost = KarmaCosts.Initiation(currentGrade + 1);

        var karmaResult = await SpendKarmaAsync(characterId, cost, $"Initiation grade {currentGrade + 1}");
        if (!karmaResult.Success)
            return AdvancementResult.Fail(karmaResult.Details);

        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character != null)
        {
            character.InitiationGrade++;
            await _databaseService.UpdateCharacterAsync(character);
        }

        return new AdvancementResult
        {
            Success = true,
            Type = "Initiation",
            Target = "Initiation Grade",
            PreviousRating = currentGrade,
            NewRating = currentGrade + 1,
            KarmaCost = cost,
            Details = $"Achieved initiation grade {currentGrade + 1}"
        };
    }

    #endregion
}

/// <summary>
/// Damage and Healing system for SR3
/// </summary>
public class DamageHealingService
{
    private readonly DiceService _diceService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<DamageHealingService> _logger;

    public DamageHealingService(
        DiceService diceService,
        DatabaseService databaseService,
        ILogger<DamageHealingService> logger)
    {
        _diceService = diceService;
        _databaseService = databaseService;
        _logger = logger;
    }

    #region Damage Application

    /// <summary>
    /// Apply damage with proper staging
    /// </summary>
    public async Task<DamageApplicationResult> ApplyDamageAsync(
        int characterId,
        int baseDamage,
        string damageCode, // e.g., "6M", "9S", "12D"
        int netSuccesses,
        string source,
        int armorRating = 0)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return DamageApplicationResult.Fail("Character not found.");

        // Parse damage code
        var (damageBase, damageLevel) = ParseDamageCode(damageCode);

        // Stage damage based on net successes
        // 2 net successes = stage up one level
        var stagedLevel = StageDamage(damageLevel, netSuccesses);
        var finalDamage = GetDamageBoxes(stagedLevel);

        // Record the damage
        var record = new DamageRecord
        {
            CharacterId = characterId,
            DamageType = "Physical",
            BaseDamage = damageBase,
            NetSuccesses = netSuccesses,
            FinalDamage = finalDamage,
            DamageLevel = stagedLevel,
            DamageCode = damageCode,
            Source = source,
            ArmorRating = armorRating,
            InflictedAt = DateTime.UtcNow
        };

        // Apply to character
        character.PhysicalDamage += finalDamage;
        await _databaseService.AddDamageRecordAsync(record);
        await _databaseService.UpdateCharacterAsync(character);

        _logger.LogInformation("Applied {Damage} {Level} damage to {CharId} from {Source}",
            finalDamage, stagedLevel, characterId, source);

        return new DamageApplicationResult
        {
            Success = true,
            BaseDamage = damageBase,
            NetSuccesses = netSuccesses,
            StagedLevel = stagedLevel,
            FinalDamage = finalDamage,
            CurrentPhysical = character.PhysicalDamage,
            MaxPhysical = character.PhysicalConditionMonitor,
            Details = $"Took {finalDamage} {stagedLevel} damage from {source}"
        };
    }

    private (int baseDamage, string level) ParseDamageCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length < 2)
            return (6, "Moderate");

        var numPart = code[..^1];
        var levelPart = code[^1..].ToUpper();

        if (!int.TryParse(numPart, out int baseDamage))
            baseDamage = 6;

        var level = levelPart switch
        {
            "L" => "Light",
            "M" => "Moderate",
            "S" => "Serious",
            "D" => "Deadly",
            _ => "Moderate"
        };

        return (baseDamage, level);
    }

    private string StageDamage(string baseLevel, int netSuccesses)
    {
        var levels = new[] { "Light", "Moderate", "Serious", "Deadly" };
        var currentIndex = Array.IndexOf(levels, baseLevel);

        if (currentIndex < 0) currentIndex = 1;

        // Stage up 1 level per 2 net successes
        var stagesUp = netSuccesses / 2;
        var newIndex = Math.Min(levels.Length - 1, currentIndex + stagesUp);

        return levels[newIndex];
    }

    private int GetDamageBoxes(string level)
    {
        return level switch
        {
            "Light" => 1,
            "Moderate" => 3,
            "Serious" => 6,
            "Deadly" => 10,
            _ => 3
        };
    }

    /// <summary>
    /// Apply stun damage
    /// </summary>
    public async Task<DamageApplicationResult> ApplyStunDamageAsync(
        int characterId,
        int damage,
        string source)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return DamageApplicationResult.Fail("Character not found.");

        character.StunDamage += damage;

        // Check for stun overflow to physical
        if (character.StunDamage > character.StunConditionMonitor)
        {
            var overflow = character.StunDamage - character.StunConditionMonitor;
            character.StunDamage = character.StunConditionMonitor;
            character.PhysicalDamage += overflow;
        }

        await _databaseService.UpdateCharacterAsync(character);

        return new DamageApplicationResult
        {
            Success = true,
            FinalDamage = damage,
            DamageType = "Stun",
            CurrentStun = character.StunDamage,
            MaxStun = character.StunConditionMonitor,
            CurrentPhysical = character.PhysicalDamage,
            Details = $"Took {damage} stun damage from {source}"
        };
    }

    #endregion

    #region Damage Resistance

    /// <summary>
    /// Roll damage resistance
    /// </summary>
    public async Task<ResistanceResult> ResistDamageAsync(
        int characterId,
        int incomingDamage,
        string damageType,
        int armorRating = 0)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return ResistanceResult.Fail("Character not found.");

        // Resistance pool = Body + Armor
        var pool = character.Body + armorRating;
        var result = _diceService.RollShadowrun(pool, 4);

        var damageTaken = Math.Max(0, incomingDamage - result.Successes);

        // Apply the damage
        if (damageType == "Stun")
        {
            character.StunDamage += damageTaken;
        }
        else
        {
            character.PhysicalDamage += damageTaken;
        }

        await _databaseService.UpdateCharacterAsync(character);

        return new ResistanceResult
        {
            Success = true,
            Pool = pool,
            Successes = result.Successes,
            IncomingDamage = incomingDamage,
            ResistedDamage = result.Successes,
            DamageTaken = damageTaken,
            Details = $"Resisted {result.Successes} damage. Took {damageTaken}."
        };
    }

    #endregion

    #region Natural Healing

    /// <summary>
    /// Begin natural healing process
    /// </summary>
    public async Task<HealingResult> BeginNaturalHealingAsync(
        int characterId,
        string damageType)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return HealingResult.Fail("Character not found.");

        var currentDamage = damageType == "Physical"
            ? character.PhysicalDamage
            : character.StunDamage;

        if (currentDamage == 0)
            return HealingResult.Fail("No damage to heal.");

        // Body test to reduce healing time
        var bodyResult = _diceService.RollShadowrun(character.Body, 4);

        var baseHours = damageType == "Physical"
            ? HealingTimes.PhysicalHealingBase(currentDamage)
            : HealingTimes.StunHealingBase(currentDamage);

        var modifiedHours = HealingTimes.ApplyBodySuccesses(baseHours, bodyResult.Successes);

        var healingRecord = new HealingTimeRecord
        {
            CharacterId = characterId,
            DamageType = damageType,
            DamageAmount = currentDamage,
            BaseHours = baseHours,
            ModifiedHours = modifiedHours,
            BodySuccesses = bodyResult.Successes,
            HealingStarted = DateTime.UtcNow,
            HealingComplete = DateTime.UtcNow.AddHours(modifiedHours)
        };

        await _databaseService.AddHealingTimeRecordAsync(healingRecord);

        return new HealingResult
        {
            Success = true,
            HealingType = "Natural",
            DamageType = damageType,
            DamageToHeal = currentDamage,
            BodySuccesses = bodyResult.Successes,
            BaseTimeHours = baseHours,
            ModifiedTimeHours = modifiedHours,
            CompletionTime = healingRecord.HealingComplete,
            Details = $"Natural healing started. Will take {modifiedHours} hours."
        };
    }

    /// <summary>
    /// Check if healing is complete
    /// </summary>
    public async Task<HealingCompletionResult> CheckHealingCompletionAsync(int characterId)
    {
        var records = await _databaseService.GetActiveHealingRecordsAsync(characterId);
        var completed = new List<HealingTimeRecord>();

        foreach (var record in records)
        {
            if (record.HealingComplete.HasValue && DateTime.UtcNow >= record.HealingComplete.Value)
            {
                // Apply healing
                var character = await _databaseService.GetCharacterByIdAsync(characterId);
                if (character != null)
                {
                    if (record.DamageType == "Physical")
                    {
                        character.PhysicalDamage = Math.Max(0, character.PhysicalDamage - record.DamageAmount);
                    }
                    else
                    {
                        character.StunDamage = Math.Max(0, character.StunDamage - record.DamageAmount);
                    }
                    await _databaseService.UpdateCharacterAsync(character);
                }

                completed.Add(record);
            }
        }

        return new HealingCompletionResult
        {
            Success = true,
            CompletedHealings = completed.Count,
            Details = completed.Any()
                ? $"Completed {completed.Count} healing process(es)."
                : "No healing completed yet."
        };
    }

    #endregion

    #region First Aid / Biotech

    /// <summary>
    /// Apply first aid to heal damage
    /// </summary>
    public async Task<HealingResult> ApplyFirstAidAsync(
        int characterId,
        int? healerId,
        int biotechSkill,
        bool hasMedkit = false,
        bool hasFacility = false)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return HealingResult.Fail("Character not found.");

        var totalDamage = character.PhysicalDamage + character.StunDamage;
        if (totalDamage == 0)
            return HealingResult.Fail("No damage to heal.");

        // First aid pool = Biotech + applicable modifiers
        var pool = biotechSkill;
        if (hasMedkit) pool += 2;
        if (hasFacility) pool += 4;

        // Target number based on damage level
        var targetNumber = GetFirstAidTargetNumber(totalDamage);

        var result = _diceService.RollShadowrun(pool, targetNumber);

        // Can heal up to Biotech skill rating in boxes
        var maxHeal = HealingTimes.FirstAidMax(biotechSkill);
        var actualHeal = Math.Min(result.Successes, maxHeal);

        // Apply healing to stun first, then physical
        var stunHealed = Math.Min(actualHeal, character.StunDamage);
        var physicalHealed = actualHeal - stunHealed;

        character.StunDamage -= stunHealed;
        character.PhysicalDamage -= physicalHealed;

        var attempt = new HealingAttempt
        {
            CharacterId = characterId,
            HealerId = healerId,
            HealingType = "FirstAid",
            SkillUsed = "Biotech",
            DiceRolled = pool,
            Successes = result.Successes,
            TargetNumber = targetNumber,
            DamageHealed = actualHeal,
            UsedMedkit = hasMedkit,
            UsedMedicalFacility = hasFacility
        };

        await _databaseService.AddHealingAttemptAsync(attempt);
        await _databaseService.UpdateCharacterAsync(character);

        return new HealingResult
        {
            Success = result.Successes > 0,
            HealingType = "First Aid",
            DiceRolled = pool,
            Successes = result.Successes,
            DamageHealed = actualHeal,
            StunHealed = stunHealed,
            PhysicalHealed = physicalHealed,
            Details = $"First aid healed {actualHeal} boxes ({stunHealed} stun, {physicalHealed} physical)."
        };
    }

    private int GetFirstAidTargetNumber(int totalDamage)
    {
        return totalDamage switch
        {
            <= 3 => 2, // Light damage
            <= 6 => 3, // Moderate
            <= 9 => 4, // Serious
            _ => 5    // Deadly
        };
    }

    #endregion

    #region Magical Healing

    /// <summary>
    /// Apply magical healing (Heal spell)
    /// </summary>
    public async Task<HealingResult> ApplyMagicalHealingAsync(
        int characterId,
        int healerId,
        int spellForce,
        int sorcerySkill,
        int magicRating)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return HealingResult.Fail("Character not found.");

        if (character.PhysicalDamage == 0)
            return HealingResult.Fail("No physical damage to heal magically.");

        // Spellcasting test
        var pool = sorcerySkill + magicRating;
        var result = _diceService.RollShadowrun(pool, 4);

        // Can heal up to Force boxes
        var maxHeal = HealingTimes.MagicalHealingMax(spellForce);
        var actualHeal = Math.Min(result.Successes, maxHeal);
        actualHeal = Math.Min(actualHeal, character.PhysicalDamage);

        character.PhysicalDamage -= actualHeal;

        var attempt = new HealingAttempt
        {
            CharacterId = characterId,
            HealerId = healerId,
            HealingType = "Magical",
            SkillUsed = "Sorcery",
            DiceRolled = pool,
            Successes = result.Successes,
            DamageHealed = actualHeal
        };

        await _databaseService.AddHealingAttemptAsync(attempt);
        await _databaseService.UpdateCharacterAsync(character);

        return new HealingResult
        {
            Success = result.Successes > 0,
            HealingType = "Magical",
            DiceRolled = pool,
            Successes = result.Successes,
            DamageHealed = actualHeal,
            PhysicalHealed = actualHeal,
            Details = $"Heal spell restored {actualHeal} physical damage."
        };
    }

    #endregion

    #region Condition Monitors

    /// <summary>
    /// Get current condition monitor status
    /// </summary>
    public async Task<ConditionMonitorStatus> GetConditionMonitorStatusAsync(int characterId)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return new ConditionMonitorStatus();

        var physicalWounds = character.PhysicalDamage / 3;
        var stunWounds = character.StunDamage / 3;
        var woundModifier = -(physicalWounds + stunWounds);

        return new ConditionMonitorStatus
        {
            PhysicalDamage = character.PhysicalDamage,
            PhysicalMax = character.PhysicalConditionMonitor,
            StunDamage = character.StunDamage,
            StunMax = character.StunConditionMonitor,
            WoundModifier = woundModifier,
            IsUnconscious = character.StunDamage >= character.StunConditionMonitor,
            IsDying = character.PhysicalDamage >= character.PhysicalConditionMonitor,
            ConditionMonitorDisplay = FormatConditionMonitors(character)
        };
    }

    private string FormatConditionMonitors(ShadowrunCharacter character)
    {
        var physical = new string('■', character.PhysicalDamage) +
                       new string('□', character.PhysicalConditionMonitor - character.PhysicalDamage);
        var stun = new string('■', character.StunDamage) +
                   new string('□', character.StunConditionMonitor - character.StunDamage);

        return $"Physical: [{physical}]\nStun:     [{stun}]";
    }

    #endregion
}

#region Result Types

public record KarmaResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public int KarmaChange { get; init; }
    public int CurrentKarma { get; init; }
    public int TotalEarned { get; init; }
    public int TotalSpent { get; init; }
    public int KarmaPoolMax { get; init; }

    public static KarmaResult Fail(string details) => new() { Success = false, Details = details };
}

public record KarmaStatus
{
    public int CurrentKarma { get; init; }
    public int TotalEarned { get; init; }
    public int TotalSpent { get; init; }
    public int KarmaPool { get; init; }
    public int KarmaPoolMax { get; init; }
}

public record KarmaPoolResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public int DiceRerolled { get; init; }
    public int NewSuccesses { get; init; }
    public int RemainingPool { get; init; }

    public static KarmaPoolResult Fail(string details) => new() { Success = false, Details = details };
}

public record AdvancementResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public int PreviousRating { get; init; }
    public int NewRating { get; init; }
    public int KarmaCost { get; init; }

    public static AdvancementResult Fail(string details) => new() { Success = false, Details = details };
}

public record DamageApplicationResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string DamageType { get; init; } = "Physical";
    public int BaseDamage { get; init; }
    public int NetSuccesses { get; init; }
    public string StagedLevel { get; init; } = "Moderate";
    public int FinalDamage { get; init; }
    public int CurrentPhysical { get; init; }
    public int MaxPhysical { get; init; }
    public int CurrentStun { get; init; }
    public int MaxStun { get; init; }

    public static DamageApplicationResult Fail(string details) => new() { Success = false, Details = details };
}

public record ResistanceResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public int Pool { get; init; }
    public int Successes { get; init; }
    public int IncomingDamage { get; init; }
    public int ResistedDamage { get; init; }
    public int DamageTaken { get; init; }

    public static ResistanceResult Fail(string details) => new() { Success = false, Details = details };
}

public record HealingResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string HealingType { get; init; } = "Natural";
    public string DamageType { get; init; } = "Physical";
    public int DiceRolled { get; init; }
    public int Successes { get; init; }
    public int DamageToHeal { get; init; }
    public int DamageHealed { get; init; }
    public int StunHealed { get; init; }
    public int PhysicalHealed { get; init; }
    public int BodySuccesses { get; init; }
    public int BaseTimeHours { get; init; }
    public int ModifiedTimeHours { get; init; }
    public DateTime? CompletionTime { get; init; }

    public static HealingResult Fail(string details) => new() { Success = false, Details = details };
}

public record HealingCompletionResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public int CompletedHealings { get; init; }
}

public class ConditionMonitorStatus
{
    public int PhysicalDamage { get; init; }
    public int PhysicalMax { get; init; }
    public int StunDamage { get; init; }
    public int StunMax { get; init; }
    public int WoundModifier { get; init; }
    public bool IsUnconscious { get; init; }
    public bool IsDying { get; init; }
    public string ConditionMonitorDisplay { get; init; } = string.Empty;
}

#endregion
