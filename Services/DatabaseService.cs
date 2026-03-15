using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Exceptions; // GPT-5.4 FIX: Added for CharacterAlreadyExistsException
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Infrastructure.Data;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Database service with Entity Framework Core and SQLite
/// </summary>
public partial class DatabaseService : IAsyncDisposable
{
    private readonly ShadowrunDbContext _context;
    private readonly ILogger<DatabaseService> _logger;
    private bool _disposed;

    public DatabaseService(BotConfig config, ILogger<DatabaseService> logger)
    {
        _logger = logger;

        var options = new DbContextOptionsBuilder<ShadowrunDbContext>()
            .UseSqlite(config.Database.ConnectionString)
            .EnableSensitiveDataLogging(config.Database.EnableSensitiveDataLogging)
            .Options;

        _context = new ShadowrunDbContext(options);
    }

    #region Transaction Support

    /// <summary>
    /// Begin a new database transaction for atomic operations
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Execute operations within a transaction with automatic commit/rollback
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        await using var transaction = await BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            var result = await operation().ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Execute operations within a transaction with automatic commit/rollback (void version)
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        await using var transaction = await BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            await operation().ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }

    #endregion

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database...");

            // Ensure database is created
            await _context.Database.EnsureCreatedAsync().ConfigureAwait(false);

            _logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    #region Character Operations

    public async Task<ShadowrunCharacter?> GetCharacterAsync(int characterId)
    {
        return await _context.Characters
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(c => c.Skills)
            .Include(c => c.Cyberware)
            .Include(c => c.Spells)
            .Include(c => c.Spirits)
            .Include(c => c.Gear)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            .ConfigureAwait(false);
    }

    public async Task<ShadowrunCharacter?> GetCharacterByNameAsync(ulong userId, string name)
    {
        return await _context.Characters
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(c => c.Skills)
            .Include(c => c.Cyberware)
            .Include(c => c.Spells)
            .Include(c => c.Spirits)
            .Include(c => c.Gear)
            .FirstOrDefaultAsync(c => c.DiscordUserId == userId && c.Name == name)
            .ConfigureAwait(false);
    }


    public async Task<List<ShadowrunCharacter>> GetUserCharactersAsync(ulong userId)
    {
        return await _context.Characters
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Where(c => c.DiscordUserId == userId)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id) // GPT-5.4 FIX: Stable ordering for deterministic results
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<ShadowrunCharacter> CreateCharacterAsync(ShadowrunCharacter character)
    {
        character.CreatedAt = DateTime.UtcNow;
        character.UpdatedAt = DateTime.UtcNow;

        _context.Characters.Add(character);

        // GPT-5.4 FIX: Catch unique constraint violation and translate to domain exception
        try
        {
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (
            ex.InnerException is Microsoft.Data.Sqlite.SqliteException sqliteEx &&
            sqliteEx.SqliteErrorCode == 19) // SQLite constraint violation
        {
            _logger.LogWarning(ex, "Character creation failed - duplicate name for user {UserId}", character.DiscordUserId);
            throw new CharacterAlreadyExistsException(character.DiscordUserId, character.Name);
        }

        _logger.LogInformation("Created character {CharacterName} for user {UserId}",
            character.Name, character.DiscordUserId);

        return character;
    }

    public async Task<ShadowrunCharacter> UpdateCharacterAsync(ShadowrunCharacter character)
    {
        character.UpdatedAt = DateTime.UtcNow;

        _context.Characters.Update(character);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return character;
    }

    public async Task<bool> DeleteCharacterAsync(int characterId)
    {
        var character = await _context.Characters.FindAsync(characterId).ConfigureAwait(false);
        if (character == null)
            return false;

        _context.Characters.Remove(character);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Deleted character {CharacterName} (ID: {CharacterId})",
            character.Name, character.Id);

        return true;
    }

    /// <summary>
    /// Get all characters.
    /// GPT-5.4 FIX: Restored original non-paginated semantics for backward compatibility.
    /// </summary>
    public async Task<List<ShadowrunCharacter>> GetAllCharactersAsync()
    {
        return await _context.Characters
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(c => c.Skills)
            .Include(c => c.Cyberware)
            .Include(c => c.Spells)
            .Include(c => c.Spirits)
            .Include(c => c.Gear)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id) // GPT-5.4 FIX: Stable ordering for deterministic results
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get a paginated list of characters
    /// GPT-5.4 FIX: New method for explicit pagination use cases
    /// </summary>
    /// <param name="skip">Number of records to skip (for pagination)</param>
    /// <param name="take">Number of records to take (for pagination). Max: 100</param>
    /// <returns>Paginated list of characters</returns>
    public async Task<List<ShadowrunCharacter>> GetCharactersPageAsync(int skip, int take)
    {
        // GPT-5.4 FIX: Validate pagination parameters
        if (skip < 0) skip = 0;
        if (take <= 0) take = 50;
        if (take > 100) take = 100; // Prevent excessive data retrieval

        return await _context.Characters
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(c => c.Skills)
            .Include(c => c.Cyberware)
            .Include(c => c.Spells)
            .Include(c => c.Spirits)
            .Include(c => c.Gear)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id) // GPT-5.4 FIX: Add ThenBy for stable ordering
            .Skip(skip)
            .Take(take)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    #endregion

    #region Combat Operations

    public async Task<CombatSession> CreateCombatSessionAsync(ulong channelId, ulong guildId)
    {
        var session = new CombatSession
        {
            DiscordChannelId = channelId,
            DiscordGuildId = guildId,
            IsActive = true,
            StartedAt = DateTime.UtcNow
        };

        _context.CombatSessions.Add(session);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Created combat session {SessionId} in channel {ChannelId}",
            session.Id, channelId);

        return session;
    }

    public async Task<CombatSession> AddCombatSessionAsync(CombatSession session)
    {
        _context.CombatSessions.Add(session);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Added combat session {SessionId} in channel {ChannelId}",
            session.Id, session.DiscordChannelId);

        return session;
    }

    public async Task<CombatSession?> GetActiveCombatSessionAsync(ulong channelId)
    {
        return await _context.CombatSessions
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(s => s.Participants)
                .ThenInclude(p => p.Character)
            .FirstOrDefaultAsync(s => s.DiscordChannelId == channelId && s.IsActive)
            .ConfigureAwait(false);
    }

    public async Task<CombatSession?> GetCombatSessionAsync(int sessionId)
    {
        return await _context.CombatSessions
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(s => s.Participants)
                .ThenInclude(p => p.Character)
            .FirstOrDefaultAsync(s => s.Id == sessionId)
            .ConfigureAwait(false);
    }

    public async Task UpdateCombatSessionAsync(CombatSession session)
    {
        _context.CombatSessions.Update(session);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task EndCombatSessionAsync(int sessionId)
    {
        var session = await _context.CombatSessions.FindAsync(sessionId).ConfigureAwait(false);
        if (session != null)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Ended combat session {SessionId}", sessionId);
        }
    }

    public async Task<CombatParticipant> AddCombatParticipantAsync(CombatParticipant participant)
    {
        _context.CombatParticipants.Add(participant);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Added combat participant {ParticipantName} to session {SessionId}",
            participant.Name, participant.CombatSessionId);

        return participant;
    }

    public async Task UpdateCombatParticipantAsync(CombatParticipant participant)
    {
        _context.CombatParticipants.Update(participant);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task RemoveCombatParticipantAsync(CombatParticipant participant)
    {
        _context.CombatParticipants.Remove(participant);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Removed combat participant {ParticipantName}", participant.Name);
    }

    public async Task<CombatAction> AddCombatActionAsync(CombatAction action)
    {
        _context.CombatActions.Add(action);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Logged combat action: {ActionType} by actor {ActorId}",
            action.ActionType, action.ActorId);

        return action;
    }

    public async Task<CombatSession?> GetAnyActiveCombatSessionAsync()
    {
        return await _context.CombatSessions
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(s => s.Participants)
                .ThenInclude(p => p.Character)
            .FirstOrDefaultAsync(s => s.IsActive)
            .ConfigureAwait(false);
    }

    public async Task<List<CombatSession>> GetRecentCombatSessionsAsync(int limit = 10)
    {
        return await _context.CombatSessions
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(s => s.Participants)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<CombatParticipant?> GetCombatParticipantAsync(int participantId)
    {
        return await _context.CombatParticipants
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(p => p.Character)
            .FirstOrDefaultAsync(p => p.Id == participantId)
            .ConfigureAwait(false);
    }

    public async Task<List<CombatAction>> GetCombatActionsAsync(int sessionId, int limit = 50)
    {
        return await _context.CombatActions
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Where(a => a.CombatSessionId == sessionId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<ShadowrunCharacter?> GetCharacterByIdAsync(int characterId)
    {
        return await _context.Characters
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(c => c.Skills)
            .Include(c => c.Cyberware)
            .Include(c => c.Spells)
            .Include(c => c.Spirits)
            .Include(c => c.Gear)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            .ConfigureAwait(false);
    }

    #endregion

    #region Cyberware Operations

    public async Task AddCyberwareAsync(int characterId, CharacterCyberware cyberware)
    {
        cyberware.CharacterId = characterId;
        _context.CharacterCyberware.Add(cyberware);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added cyberware {CyberwareName} to character {CharacterId}",
            cyberware.Name, characterId);
    }

    public async Task RemoveCyberwareAsync(int cyberwareId)
    {
        var cyberware = await _context.CharacterCyberware.FindAsync(cyberwareId);
        if (cyberware != null)
        {
            _context.CharacterCyberware.Remove(cyberware);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed cyberware {CyberwareName} from character {CharacterId}",
                cyberware.Name, cyberware.CharacterId);
        }
    }

    #endregion

    #region Skills Operations

    public async Task AddOrUpdateSkillAsync(int characterId, string skillName, int rating, string? specialization = null)
    {
        var skill = await _context.CharacterSkills
            .FirstOrDefaultAsync(s => s.CharacterId == characterId && s.SkillName == skillName);

        if (skill == null)
        {
            skill = new CharacterSkill
            {
                CharacterId = characterId,
                SkillName = skillName,
                Rating = rating,
                Specialization = specialization
            };
            _context.CharacterSkills.Add(skill);
        }
        else
        {
            skill.Rating = rating;
            skill.Specialization = specialization;
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Matrix Operations

    public async Task<MatrixRun?> GetActiveMatrixRunAsync(int characterId)
    {
        return await _context.MatrixRuns
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(r => r.ICEncounters)
            .FirstOrDefaultAsync(r => r.CharacterId == characterId && r.EndedAt == null);
    }

    public async Task<MatrixRun?> GetMatrixRunAsync(int runId)
    {
        return await _context.MatrixRuns
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(r => r.ICEncounters)
            .FirstOrDefaultAsync(r => r.Id == runId);
    }

    public async Task<MatrixRun> CreateMatrixRunAsync(MatrixRun run)
    {
        _context.MatrixRuns.Add(run);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created matrix run {RunId} for character {CharacterId}",
            run.Id, run.CharacterId);

        return run;
    }

    public async Task<MatrixRun> UpdateMatrixRunAsync(MatrixRun run)
    {
        _context.MatrixRuns.Update(run);
        await _context.SaveChangesAsync();
        return run;
    }

    public async Task<List<ActiveICEncounter>> GetICEncountersAsync(int runId)
    {
        return await _context.ActiveICEncounters
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Where(e => e.MatrixRunId == runId)
            .ToListAsync();
    }

    public async Task<ActiveICEncounter?> GetICEncounterAsync(int encounterId)
    {
        return await _context.ActiveICEncounters
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .FirstOrDefaultAsync(e => e.Id == encounterId);
    }

    public async Task<ActiveICEncounter> AddICEncounterAsync(ActiveICEncounter encounter)
    {
        _context.ActiveICEncounters.Add(encounter);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Added IC encounter {EncounterId} to matrix run {RunId}",
            encounter.Id, encounter.MatrixRunId);
        
        return encounter;
    }

    public async Task RemoveICEncounterAsync(ActiveICEncounter encounter)
    {
        _context.ActiveICEncounters.Remove(encounter);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Removed IC encounter {EncounterId}", encounter.Id);
    }

    public async Task<Cyberdeck?> GetCyberdeckAsync(int deckId)
    {
        return await _context.Cyberdecks
            .AsNoTracking() // GPT-5.4 FIX: Read-only query should not track entities
            .Include(d => d.InstalledPrograms)
            .FirstOrDefaultAsync(d => d.Id == deckId);
    }

    public async Task<Cyberdeck> CreateCyberdeckAsync(Cyberdeck deck)
    {
        _context.Cyberdecks.Add(deck);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created cyberdeck {DeckId} for character {CharacterId}",
            deck.Id, deck.CharacterId);
        
        return deck;
    }

    public async Task<Cyberdeck> UpdateCyberdeckAsync(Cyberdeck deck)
    {
        _context.Cyberdecks.Update(deck);
        await _context.SaveChangesAsync();
        return deck;
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await _context.DisposeAsync();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Entity Framework DbContext for Shadowrun bot
/// </summary>
