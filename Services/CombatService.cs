using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Combat system service for managing turn-based Shadowrun combat
/// </summary>
public class CombatService
{
    private readonly DiceService _diceService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<CombatService> _logger;

    public CombatService(
        DiceService diceService,
        DatabaseService databaseService,
        ILogger<CombatService> logger)
    {
        _diceService = diceService;
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Start a new combat session in a channel
    /// </summary>
    public async Task<CombatSession> StartCombatAsync(ulong guildId, ulong channelId)
    {
        // Check if combat already active in this channel
        var existingSession = await _databaseService.GetActiveCombatSessionAsync(channelId);
        if (existingSession != null)
        {
            throw new InvalidOperationException("Combat is already in progress in this channel!");
        }

        var session = new CombatSession
        {
            DiscordGuildId = guildId,
            DiscordChannelId = channelId,
            IsActive = true,
            CurrentPass = 1,
            CurrentTurn = 1,
            StartedAt = DateTime.UtcNow
        };

        await _databaseService.AddCombatSessionAsync(session);
        _logger.LogInformation("Combat session {SessionId} started in channel {ChannelId}", session.Id, channelId);

        return session;
    }

    /// <summary>
    /// End an active combat session
    /// </summary>
    public async Task<CombatSession> EndCombatAsync(ulong channelId)
    {
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId);
        if (session == null)
        {
            throw new InvalidOperationException("No active combat session in this channel.");
        }

        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;
        await _databaseService.UpdateCombatSessionAsync(session);

        _logger.LogInformation("Combat session {SessionId} ended", session.Id);
        return session;
    }

    /// <summary>
    /// Add a participant to combat
    /// </summary>
    public async Task<CombatParticipant> AddCombatantAsync(
        ulong channelId,
        int? characterId,
        string? name,
        bool isNPC,
        int initiativeDice = 1)
    {
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId);
        if (session == null)
        {
            throw new InvalidOperationException("No active combat session. Use `/combat start` first.");
        }

        string combatantName = name;
        int reaction = 3; // Default for NPCs

        if (characterId.HasValue)
        {
            var character = await _databaseService.GetCharacterByIdAsync(characterId.Value);
            if (character == null)
            {
                throw new InvalidOperationException($"Character with ID {characterId} not found.");
            }
            combatantName = character.Name;
            reaction = character.Reaction;
        }
        else if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Either characterId or name must be provided.");
        }

        // Check for duplicate
        if (session.Participants.Any(p => p.Name == combatantName || (p.CharacterId == characterId && characterId.HasValue)))
        {
            throw new InvalidOperationException($"Combatant '{combatantName}' already exists in this combat.");
        }

        // Roll initiative
        var initResult = _diceService.RollInitiative(reaction, initiativeDice);

        var participant = new CombatParticipant
        {
            CombatSessionId = session.Id,
            CharacterId = characterId,
            Name = combatantName,
            Initiative = initResult.Total,
            InitiativePasses = initResult.Passes,
            CurrentPass = 1,
            HasActed = false,
            IsNPC = isNPC
        };

        await _databaseService.AddCombatParticipantAsync(participant);
        _logger.LogInformation("Added combatant {Name} to session {SessionId} with initiative {Init}", 
            combatantName, session.Id, initResult.Total);

        return participant;
    }

    /// <summary>
    /// Remove a participant from combat
    /// </summary>
    public async Task RemoveCombatantAsync(ulong channelId, string name)
    {
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId);
        if (session == null)
        {
            throw new InvalidOperationException("No active combat session.");
        }

        var participant = session.Participants.FirstOrDefault(p => 
            p.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);

        if (participant == null)
        {
            throw new InvalidOperationException($"Combatant '{name}' not found.");
        }

        await _databaseService.RemoveCombatParticipantAsync(participant);
        _logger.LogInformation("Removed combatant {Name} from session {SessionId}", name, session.Id);
    }

    /// <summary>
    /// Get combat status
    /// </summary>
    public async Task<CombatSession> GetCombatStatusAsync(ulong channelId)
    {
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId);
        return session;
    }

    /// <summary>
    /// Advance to next turn
    /// </summary>
    public async Task<CombatParticipant> NextTurnAsync(ulong channelId)
    {
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId);
        if (session == null || !session.IsActive)
        {
            throw new InvalidOperationException("No active combat session.");
        }

        if (!session.Participants.Any())
        {
            throw new InvalidOperationException("No combatants in this combat.");
        }

        // Sort by initiative (descending), then by has acted
        var sortedParticipants = session.Participants
            .OrderByDescending(p => p.Initiative)
            .ThenBy(p => p.HasActed)
            .ToList();

        // Find next participant who hasn't acted
        var nextParticipant = sortedParticipants.FirstOrDefault(p => !p.HasActed);

        if (nextParticipant == null)
        {
            // All have acted - advance to next pass or round
            await AdvancePassOrRoundAsync(session);
            sortedParticipants = session.Participants
                .OrderByDescending(p => p.Initiative)
                .ToList();
            nextParticipant = sortedParticipants.First();
        }

        nextParticipant.HasActed = true;
        await _databaseService.UpdateCombatParticipantAsync(nextParticipant);

        session.CurrentTurn++;
        await _databaseService.UpdateCombatSessionAsync(session);

        return nextParticipant;
    }

    /// <summary>
    /// Advance pass or start new round
    /// </summary>
    private async Task AdvancePassOrRoundAsync(CombatSession session)
    {
        // Reset has acted for all participants
        foreach (var participant in session.Participants)
        {
            participant.HasActed = false;
            participant.CurrentPass++;
        }

        // Check if we need to advance round
        var maxPasses = session.Participants.Max(p => p.InitiativePasses);
        var currentPass = session.Participants.Min(p => p.CurrentPass);

        if (currentPass > maxPasses)
        {
            // New round - reroll initiative
            session.CurrentPass = 1;
            session.CurrentTurn = 1;

            foreach (var participant in session.Participants)
            {
                int reaction = 3;
                if (participant.Character != null)
                {
                    reaction = participant.Character.Reaction;
                }

                var initResult = _diceService.RollInitiative(reaction, 1);
                participant.Initiative = initResult.Total;
                participant.InitiativePasses = initResult.Passes;
                participant.CurrentPass = 1;
            }

            await _databaseService.UpdateCombatSessionAsync(session);
            _logger.LogInformation("Combat session {SessionId} advanced to new round", session.Id);
        }
        else
        {
            session.CurrentPass = currentPass;
            await _databaseService.UpdateCombatSessionAsync(session);
            _logger.LogInformation("Combat session {SessionId} advanced to pass {Pass}", session.Id, session.CurrentPass);
        }
    }

    /// <summary>
    /// Execute an attack
    /// </summary>
    public async Task<CombatAttack> ExecuteAttackAsync(
        ulong channelId,
        string attackerName,
        string targetName,
        int attackPool,
        int defensePool = 0,
        string weapon = "Attack",
        int damageBase = 0,
        string damageType = "Physical")
    {
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId);
        if (session == null || !session.IsActive)
        {
            throw new InvalidOperationException("No active combat session.");
        }

        var attacker = session.Participants.FirstOrDefault(p =>
            p.Name?.Equals(attackerName, StringComparison.OrdinalIgnoreCase) == true);
        var target = session.Participants.FirstOrDefault(p =>
            p.Name?.Equals(targetName, StringComparison.OrdinalIgnoreCase) == true);

        if (attacker == null)
        {
            throw new InvalidOperationException($"Attacker '{attackerName}' not found.");
        }

        if (target == null)
        {
            throw new InvalidOperationException($"Target '{targetName}' not found.");
        }

        // Roll attack
        var attackResult = _diceService.RollShadowrun(attackPool, 4);

        // Roll defense (if pool provided)
        int defenseSuccesses = 0;
        ShadowrunDiceResult? defenseResult = null;
        if (defensePool > 0)
        {
            defenseResult = _diceService.RollShadowrun(defensePool, 4);
            defenseSuccesses = defenseResult.Successes;
        }

        int netHits = attackResult.Successes - defenseSuccesses;
        int damage = netHits > 0 ? damageBase + netHits : 0;

        var combatAction = new CombatAction
        {
            CombatSessionId = session.Id,
            ActorId = attacker.Id,
            TargetId = target.Id,
            ActionType = "Attack",
            Description = $"{attacker.Name} attacks {target.Name} with {weapon}",
            DiceRolled = attackPool,
            Successes = attackResult.Successes,
            Damage = damage,
            DamageType = damageType
        };

        await _databaseService.AddCombatActionAsync(combatAction);

        _logger.LogInformation("Attack: {Attacker} -> {Target}, Net Hits: {NetHits}, Damage: {Damage}",
            attacker.Name, target.Name, netHits, damage);

        return combatAction;
    }

    /// <summary>
    /// Format combat status for display
    /// </summary>
    public string FormatCombatStatus(CombatSession session)
    {
        if (session == null || !session.IsActive)
        {
            return "No active combat session. Use `/combat start` to begin.";
        }

        var status = $"**⚔️ Combat Status**\n";
        status += $"Session ID: {session.Id}\n";
        status += $"Round: {session.CurrentTurn / Math.Max(1, session.Participants.Count) + 1}\n";
        status += $"Turn: {session.CurrentTurn}\n";
        status += $"Pass: {session.CurrentPass}\n";
        status += $"Started: {session.StartedAt:HH:mm:ss}\n\n";

        status += "**Initiative Order:**\n";

        var sortedParticipants = session.Participants
            .OrderByDescending(p => p.Initiative)
            .ToList();

        for (int i = 0; i < sortedParticipants.Count; i++)
        {
            var p = sortedParticipants[i];
            var indicator = p.HasActed ? "✓" : "▶";
            status += $"{indicator} {p.Initiative} - {p.Name} ({(p.IsNPC ? "NPC" : "Player")})\n";
        }

        return status;
    }

    /// <summary>
    /// Format attack result for display
    /// </summary>
    public string FormatAttackResult(CombatAttack attack)
    {
        var result = $"**⚔️ Attack: {attack.Description}**\n";
        result += $"Dice Rolled: {attack.DiceRolled}\n";
        result += $"Successes: {attack.Successes}\n";
        result += $"Net Hits: {attack.Successes}\n";
        result += $"Damage: {attack.Damage} {attack.DamageType}\n";
        return result;
    }
}
