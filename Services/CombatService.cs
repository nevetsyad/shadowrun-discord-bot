using System.Text;
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

    // Constants
    private const int DefaultNPCReaction = 3;
    private const int DefaultInitiativeDice = 1;
    private const int DefaultTargetNumber = 4;

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
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var existingSession = await _databaseService.GetActiveCombatSessionAsync(channelId).ConfigureAwait(false);
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

        await _databaseService.AddCombatSessionAsync(session).ConfigureAwait(false);
        _logger.LogInformation("Combat session {SessionId} started in channel {ChannelId}", session.Id, channelId);

        return session;
    }

    /// <summary>
    /// End an active combat session
    /// </summary>
    public async Task<CombatSession> EndCombatAsync(ulong channelId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("No active combat session in this channel.");
        }

        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;
        await _databaseService.UpdateCombatSessionAsync(session).ConfigureAwait(false);

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
        int initiativeDice = DefaultInitiativeDice)
    {
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("No active combat session. Use `/combat start` first.");
        }

        var (combatantName, reaction) = await ResolveCombatantInfoAsync(characterId, name).ConfigureAwait(false);

        // Check for duplicate
        if (session.Participants.Any(p => p.Name?.Equals(combatantName, StringComparison.OrdinalIgnoreCase) == true ||
            (p.CharacterId == characterId && characterId.HasValue)))
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

        await _databaseService.AddCombatParticipantAsync(participant).ConfigureAwait(false);
        _logger.LogInformation("Added combatant {Name} to session {SessionId} with initiative {Init}", 
            combatantName, session.Id, initResult.Total);

        return participant;
    }

    /// <summary>
    /// Resolve combatant name and reaction from character ID or name
    /// </summary>
    private async Task<(string name, int reaction)> ResolveCombatantInfoAsync(int? characterId, string? name)
    {
        if (characterId.HasValue)
        {
            var character = await _databaseService.GetCharacterByIdAsync(characterId.Value).ConfigureAwait(false);
            if (character == null)
            {
                throw new InvalidOperationException($"Character with ID {characterId} not found.");
            }
            return (character.Name, character.Reaction);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return (name, DefaultNPCReaction);
        }

        throw new InvalidOperationException("Either characterId or name must be provided.");
    }

    /// <summary>
    /// Remove a participant from combat
    /// </summary>
    public async Task RemoveCombatantAsync(ulong channelId, string name)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException("No active combat session.");
        }

        // FIX: CRIT-001 - Add null check for Participants
        var participants = session.Participants ?? new List<CombatParticipant>();
        var participant = participants.FirstOrDefault(p => 
            p.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);

        if (participant == null)
        {
            throw new InvalidOperationException($"Combatant '{name}' not found.");
        }

        await _databaseService.RemoveCombatParticipantAsync(participant).ConfigureAwait(false);
        _logger.LogInformation("Removed combatant {Name} from session {SessionId}", name, session.Id);
    }

    /// <summary>
    /// Get combat status
    /// </summary>
    public async Task<CombatSession> GetCombatStatusAsync(ulong channelId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId).ConfigureAwait(false);
        return session;
    }

    /// <summary>
    /// Advance to next turn
    /// </summary>
    public async Task<CombatParticipant> NextTurnAsync(ulong channelId)
    {
        // FIX: CRIT-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId).ConfigureAwait(false);
        if (session == null || !session.IsActive)
        {
            throw new InvalidOperationException("No active combat session.");
        }

        // FIX: CRIT-002 - Add null check and empty collection handling
        var participants = session.Participants ?? new List<CombatParticipant>();
        if (participants.Count == 0)
        {
            throw new InvalidOperationException("No combatants in this combat.");
        }

        // Sort by initiative (descending), then by has acted
        var sortedParticipants = participants
            .OrderByDescending(p => p.Initiative)
            .ThenBy(p => p.HasActed)
            .ToList();

        // Find next participant who hasn't acted
        var nextParticipant = sortedParticipants.FirstOrDefault(p => !p.HasActed);

        if (nextParticipant == null)
        {
            // All have acted - advance to next pass or round
            await AdvancePassOrRoundAsync(session).ConfigureAwait(false);
            // FIX: CRIT-002 - Re-get participants after round change and handle empty case
            session = await _databaseService.GetActiveCombatSessionAsync(channelId).ConfigureAwait(false);
            participants = session?.Participants ?? new List<CombatParticipant>();
            sortedParticipants = participants
                .OrderByDescending(p => p.Initiative)
                .ToList();
            // FIX: CRIT-002 - Use FirstOrDefault with null check instead of First()
            nextParticipant = sortedParticipants.FirstOrDefault();
            if (nextParticipant == null)
            {
                throw new InvalidOperationException("No combatants available after round change.");
            }
        }

        // FIX: MED-001 - Wrap multiple database updates in a transaction
        await _databaseService.ExecuteInTransactionAsync(async () =>
        {
            nextParticipant.HasActed = true;
            await _databaseService.UpdateCombatParticipantAsync(nextParticipant).ConfigureAwait(false);

            session!.CurrentTurn++;
            await _databaseService.UpdateCombatSessionAsync(session).ConfigureAwait(false);
        }).ConfigureAwait(false);

        return nextParticipant;
    }

    /// <summary>
    /// Advance pass or start new round
    /// </summary>
    private async Task AdvancePassOrRoundAsync(CombatSession session)
    {
        // FIX: CRIT-001 - Add null check for Participants
        var participants = session.Participants ?? new List<CombatParticipant>();
        
        // Reset has acted for all participants and increment pass
        foreach (var participant in participants)
        {
            participant.HasActed = false;
            participant.CurrentPass++;
        }

        // FIX: CRIT-001 - Handle empty participants
        if (participants.Count == 0)
        {
            _logger.LogWarning("Combat session {SessionId} has no participants during pass advance", session.Id);
            return;
        }

        var maxPasses = participants.Max(p => p.InitiativePasses);
        var currentPass = participants.Min(p => p.CurrentPass);

        if (currentPass > maxPasses)
        {
            await StartNewRoundAsync(session).ConfigureAwait(false);
        }
        else
        {
            session.CurrentPass = currentPass;
            await _databaseService.UpdateCombatSessionAsync(session).ConfigureAwait(false);
            _logger.LogInformation("Combat session {SessionId} advanced to pass {Pass}", session.Id, session.CurrentPass);
        }
    }

    /// <summary>
    /// Start a new combat round with initiative reroll
    /// </summary>
    private async Task StartNewRoundAsync(CombatSession session)
    {
        session.CurrentPass = 1;
        session.CurrentTurn = 1;

        // FIX: CRIT-001 - Add null check for Participants
        var participants = session.Participants ?? new List<CombatParticipant>();
        
        // FIX: MED-001 - Wrap multiple database updates in a transaction
        await _databaseService.ExecuteInTransactionAsync(async () =>
        {
            foreach (var participant in participants)
            {
                var reaction = participant.Character?.Reaction ?? DefaultNPCReaction;
                var initResult = _diceService.RollInitiative(reaction, DefaultInitiativeDice);
                
                participant.Initiative = initResult.Total;
                participant.InitiativePasses = initResult.Passes;
                participant.CurrentPass = 1;
                await _databaseService.UpdateCombatParticipantAsync(participant).ConfigureAwait(false);
            }

            await _databaseService.UpdateCombatSessionAsync(session).ConfigureAwait(false);
        }).ConfigureAwait(false);
        
        _logger.LogInformation("Combat session {SessionId} advanced to new round", session.Id);
    }

    /// <summary>
    /// Execute an attack
    /// </summary>
    public async Task<CombatAction> ExecuteAttackAsync(
        ulong channelId,
        string attackerName,
        string targetName,
        int attackPool,
        int defensePool = 0,
        string weapon = "Attack",
        int damageBase = 0,
        string damageType = "Physical")
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetActiveCombatSessionAsync(channelId).ConfigureAwait(false);
        if (session == null || !session.IsActive)
        {
            throw new InvalidOperationException("No active combat session.");
        }

        // FIX: CRIT-001 - Add null check for Participants
        var participants = session.Participants ?? new List<CombatParticipant>();
        
        var attacker = participants.FirstOrDefault(p =>
            p.Name?.Equals(attackerName, StringComparison.OrdinalIgnoreCase) == true);
        var target = participants.FirstOrDefault(p =>
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

        await _databaseService.AddCombatActionAsync(combatAction).ConfigureAwait(false);

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

        var participantCount = Math.Max(1, session.Participants?.Count ?? 1);
        var round = session.CurrentTurn / participantCount + 1;
        
        var sb = new StringBuilder();
        sb.AppendLine("**⚔️ Combat Status**");
        sb.AppendLine($"Session ID: {session.Id}");
        sb.AppendLine($"Round: {round}");
        sb.AppendLine($"Turn: {session.CurrentTurn}");
        sb.AppendLine($"Pass: {session.CurrentPass}");
        sb.AppendLine($"Started: {session.StartedAt:HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("**Initiative Order:**");

        if (session.Participants != null)
        {
            var sortedParticipants = session.Participants
                .OrderByDescending(p => p.Initiative)
                .ToList();

            foreach (var p in sortedParticipants)
            {
                var indicator = p.HasActed ? "✓" : "▶";
                var type = p.IsNPC ? "NPC" : "Player";
                sb.AppendLine($"{indicator} {p.Initiative} - {p.Name} ({type})");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format attack result for display
    /// </summary>
    public string FormatAttackResult(CombatAction attack)
    {
        var result = $"**⚔️ Attack: {attack.Description}**\n";
        result += $"Dice Rolled: {attack.DiceRolled}\n";
        result += $"Successes: {attack.Successes}\n";
        result += $"Net Hits: {attack.Successes}\n";
        result += $"Damage: {attack.Damage} {attack.DamageType}\n";
        return result;
    }

    #region API Methods

    /// <summary>
    /// Get active combat session (API version)
    /// </summary>
    public async Task<CombatSessionDto?> GetActiveCombatAsync(ulong? channelId = null)
    {
        CombatSession? session;
        
        if (channelId.HasValue)
        {
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            session = await _databaseService.GetActiveCombatSessionAsync(channelId.Value).ConfigureAwait(false);
        }
        else
        {
            // Get any active combat
            // FIX: HIGH-001 - Added ConfigureAwait(false)
            session = await _databaseService.GetAnyActiveCombatSessionAsync().ConfigureAwait(false);
        }

        if (session == null)
        {
            return null;
        }

        return MapToCombatSessionDto(session);
    }

    /// <summary>
    /// Get all combat sessions
    /// </summary>
    public async Task<List<CombatSessionSummaryDto>> GetAllCombatSessionsAsync(int limit = 10)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var sessions = await _databaseService.GetRecentCombatSessionsAsync(limit).ConfigureAwait(false);
        return sessions.Select(s => new CombatSessionSummaryDto
        {
            Id = s.Id,
            DiscordChannelId = s.DiscordChannelId,
            IsActive = s.IsActive,
            StartedAt = s.StartedAt,
            EndedAt = s.EndedAt,
            Round = s.CurrentTurn / Math.Max(1, s.Participants?.Count ?? 1) + 1,
            ParticipantCount = s.Participants?.Count ?? 0
        }).ToList();
    }

    /// <summary>
    /// Get combat session by ID
    /// </summary>
    public async Task<CombatSessionDto?> GetCombatSessionByIdAsync(int sessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetCombatSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            return null;
        }

        return MapToCombatSessionDto(session);
    }

    /// <summary>
    /// Start combat (API version)
    /// </summary>
    public async Task<CombatSessionDto> StartCombatApiAsync(ulong channelId, ulong guildId)
    {
        // FIX: HIGH-001 - Method already uses StartCombatAsync which has ConfigureAwait
        var session = await StartCombatAsync(guildId, channelId).ConfigureAwait(false);
        return MapToCombatSessionDto(session);
    }

    /// <summary>
    /// End combat (API version)
    /// </summary>
    public async Task<EndCombatResultDto> EndCombatApiAsync(int sessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetCombatSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Combat session {sessionId} not found");
        }

        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;
        await _databaseService.UpdateCombatSessionAsync(session).ConfigureAwait(false);

        _logger.LogInformation("Ended combat session {SessionId}", sessionId);

        return new EndCombatResultDto
        {
            Success = true,
            Message = "Combat ended",
            Duration = session.EndedAt.Value - session.StartedAt
        };
    }

    /// <summary>
    /// Add participant to combat (API version)
    /// </summary>
    public async Task<CombatParticipantDto> AddParticipantApiAsync(
        int sessionId,
        string name,
        string? type = null,
        int? initiative = null,
        int? initiativeDice = null,
        int? wounds = null,
        int? characterId = null)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetCombatSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Combat session {sessionId} not found");
        }

        if (!session.IsActive)
        {
            throw new InvalidOperationException("Combat session is not active");
        }

        // FIX: CRIT-001 - Add null check for Participants
        if (session.Participants?.Any(p => p.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true) == true)
        {
            throw new InvalidOperationException("Participant with this name already exists");
        }

        // Roll initiative if not provided
        var finalInitiative = initiative ?? _diceService.RollShadowrun(initiativeDice ?? 1, 4).Successes;
        var tiebreaker = _diceService.Roll(1, 6).Total;

        var participant = new CombatParticipant
        {
            CombatSessionId = sessionId,
            Name = name,
            Type = type ?? "NPC",
            Initiative = finalInitiative,
            Tiebreaker = tiebreaker,
            HasActed = false,
            Wounds = wounds ?? 0,
            CharacterId = characterId,
            IsNPC = type != "PC"
        };

        await _databaseService.AddCombatParticipantAsync(participant).ConfigureAwait(false);
        _logger.LogInformation("Added participant {ParticipantName} to combat {SessionId}",
            participant.Name, sessionId);

        return MapToCombatParticipantDto(participant);
    }

    /// <summary>
    /// Remove participant from combat
    /// </summary>
    public async Task<bool> RemoveParticipantAsync(int sessionId, int participantId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var participant = await _databaseService.GetCombatParticipantAsync(participantId).ConfigureAwait(false);
        if (participant == null || participant.CombatSessionId != sessionId)
        {
            return false;
        }

        await _databaseService.RemoveCombatParticipantAsync(participant).ConfigureAwait(false);
        _logger.LogInformation("Removed participant {ParticipantName} from combat {SessionId}",
            participant.Name, sessionId);

        return true;
    }

    /// <summary>
    /// Get participants for a combat session
    /// </summary>
    public async Task<List<CombatParticipantDto>> GetParticipantsAsync(int sessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetCombatSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Combat session {sessionId} not found");
        }

        // FIX: CRIT-001 - Add null check for Participants
        return (session.Participants ?? new List<CombatParticipant>())
            .OrderByDescending(p => p.Initiative)
            .ThenBy(p => p.Tiebreaker)
            .Select(MapToCombatParticipantDto)
            .ToList();
    }

    /// <summary>
    /// Advance to next turn (API version)
    /// </summary>
    public async Task<NextTurnResultDto> NextTurnApiAsync(int sessionId)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetCombatSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Combat session {sessionId} not found");
        }

        if (!session.IsActive)
        {
            throw new InvalidOperationException("Combat session is not active");
        }

        // FIX: CRIT-001 - Add null check for Participants
        var participants = session.Participants ?? new List<CombatParticipant>();

        // Mark current participant as having acted
        var currentParticipant = participants
            .OrderByDescending(p => p.Initiative)
            .ThenBy(p => p.Tiebreaker)
            .Skip(session.CurrentTurn)
            .FirstOrDefault();

        // FIX: MED-001 - Wrap multiple database updates in a transaction
        await _databaseService.ExecuteInTransactionAsync(async () =>
        {
            if (currentParticipant != null)
            {
                currentParticipant.HasActed = true;
                await _databaseService.UpdateCombatParticipantAsync(currentParticipant).ConfigureAwait(false);
            }

            // Advance turn
            session.CurrentTurn++;

            // Check if round is complete
            var remainingActors = participants
                .Where(p => !p.HasActed)
                .OrderByDescending(p => p.Initiative)
                .ThenBy(p => p.Tiebreaker)
                .ToList();

            if (remainingActors.Count == 0)
            {
                // New round
                session.CurrentTurn = 0;
                foreach (var p in participants)
                {
                    p.HasActed = false;
                    await _databaseService.UpdateCombatParticipantAsync(p).ConfigureAwait(false);
                }
            }

            await _databaseService.UpdateCombatSessionAsync(session).ConfigureAwait(false);
        }).ConfigureAwait(false);

        // Get next participant
        var nextParticipant = (session.Participants ?? new List<CombatParticipant>())
            .Where(p => !p.HasActed)
            .OrderByDescending(p => p.Initiative)
            .ThenBy(p => p.Tiebreaker)
            .FirstOrDefault();

        return new NextTurnResultDto
        {
            Round = session.CurrentTurn / Math.Max(1, participants.Count) + 1,
            Turn = session.CurrentTurn,
            NextParticipant = nextParticipant != null ? new CombatParticipantSimpleDto
            {
                Id = nextParticipant.Id,
                Name = nextParticipant.Name ?? "Unknown",
                Initiative = nextParticipant.Initiative
            } : null
        };
    }

    /// <summary>
    /// Log a combat action
    /// </summary>
    public async Task<CombatActionDto> LogActionAsync(
        int sessionId,
        int? actorId,
        string? actorName,
        string actionType,
        int? targetId,
        string? targetName,
        string? description,
        int? damage)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var session = await _databaseService.GetCombatSessionAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Combat session {sessionId} not found");
        }

        var action = new CombatAction
        {
            CombatSessionId = sessionId,
            ActorId = actorId,
            ActorName = actorName,
            ActionType = actionType,
            TargetId = targetId,
            TargetName = targetName,
            Description = description,
            Damage = damage,
            Timestamp = DateTime.UtcNow
        };

        await _databaseService.AddCombatActionAsync(action).ConfigureAwait(false);
        return MapToCombatActionDto(action);
    }

    /// <summary>
    /// Get combat actions for a session
    /// </summary>
    public async Task<List<CombatActionDto>> GetCombatActionsAsync(int sessionId, int limit = 50)
    {
        // FIX: HIGH-001 - Added ConfigureAwait(false)
        var actions = await _databaseService.GetCombatActionsAsync(sessionId, limit).ConfigureAwait(false);
        return actions.Select(MapToCombatActionDto).ToList();
    }

    #endregion

    #region Private Mapping Methods

    private CombatSessionDto MapToCombatSessionDto(CombatSession session)
    {
        return new CombatSessionDto
        {
            Id = session.Id,
            DiscordChannelId = session.DiscordChannelId,
            DiscordGuildId = session.DiscordGuildId,
            IsActive = session.IsActive,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Round = session.CurrentTurn / Math.Max(1, session.Participants?.Count ?? 1) + 1,
            CurrentTurn = session.CurrentTurn,
            Participants = session.Participants?
                .OrderByDescending(p => p.Initiative)
                .ThenBy(p => p.Tiebreaker)
                .Select(MapToCombatParticipantDto)
                .ToList()
        };
    }

    private CombatParticipantDto MapToCombatParticipantDto(CombatParticipant p)
    {
        return new CombatParticipantDto
        {
            Id = p.Id,
            Name = p.Name ?? "Unknown",
            Type = p.Type ?? "NPC",
            Initiative = p.Initiative,
            Tiebreaker = p.Tiebreaker,
            HasActed = p.HasActed,
            Wounds = p.Wounds,
            CharacterId = p.CharacterId,
            CharacterName = p.Character?.Name
        };
    }

    private CombatActionDto MapToCombatActionDto(CombatAction a)
    {
        return new CombatActionDto
        {
            Id = a.Id,
            ActorId = a.ActorId,
            ActorName = a.ActorName,
            ActionType = a.ActionType ?? "Unknown",
            TargetId = a.TargetId,
            TargetName = a.TargetName,
            Description = a.Description,
            Damage = a.Damage,
            Timestamp = a.Timestamp
        };
    }

    #endregion
}

#region Combat DTOs

public class CombatSessionDto
{
    public int Id { get; set; }
    public ulong DiscordChannelId { get; set; }
    public ulong DiscordGuildId { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int Round { get; set; }
    public int CurrentTurn { get; set; }
    public List<CombatParticipantDto>? Participants { get; set; }
}

public class CombatSessionSummaryDto
{
    public int Id { get; set; }
    public ulong DiscordChannelId { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int Round { get; set; }
    public int ParticipantCount { get; set; }
}

public class CombatParticipantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Initiative { get; set; }
    public int Tiebreaker { get; set; }
    public bool HasActed { get; set; }
    public int Wounds { get; set; }
    public int? CharacterId { get; set; }
    public string? CharacterName { get; set; }
}

public class CombatParticipantSimpleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Initiative { get; set; }
}

public class CombatActionDto
{
    public int Id { get; set; }
    public int? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int? TargetId { get; set; }
    public string? TargetName { get; set; }
    public string? Description { get; set; }
    public int? Damage { get; set; }
    public DateTime Timestamp { get; set; }
}

public class EndCombatResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

public class NextTurnResultDto
{
    public int Round { get; set; }
    public int Turn { get; set; }
    public CombatParticipantSimpleDto? NextParticipant { get; set; }
}

#endregion
