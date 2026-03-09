using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

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

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database...");

            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

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
            .Include(c => c.Skills)
            .Include(c => c.Cyberware)
            .Include(c => c.Spells)
            .Include(c => c.Spirits)
            .Include(c => c.Gear)
            .FirstOrDefaultAsync(c => c.Id == characterId);
    }

    public async Task<ShadowrunCharacter?> GetCharacterByNameAsync(ulong userId, string name)
    {
        return await _context.Characters
            .Include(c => c.Skills)
            .Include(c => c.Cyberware)
            .Include(c => c.Spells)
            .Include(c => c.Spirits)
            .Include(c => c.Gear)
            .FirstOrDefaultAsync(c => c.DiscordUserId == userId && c.Name == name);
    }

    public async Task<List<ShadowrunCharacter>> GetUserCharactersAsync(ulong userId)
    {
        return await _context.Characters
            .Where(c => c.DiscordUserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<ShadowrunCharacter> CreateCharacterAsync(ShadowrunCharacter character)
    {
        character.CreatedAt = DateTime.UtcNow;
        character.UpdatedAt = DateTime.UtcNow;

        _context.Characters.Add(character);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created character {CharacterName} for user {UserId}",
            character.Name, character.DiscordUserId);

        return character;
    }

    public async Task<ShadowrunCharacter> UpdateCharacterAsync(ShadowrunCharacter character)
    {
        character.UpdatedAt = DateTime.UtcNow;

        _context.Characters.Update(character);
        await _context.SaveChangesAsync();

        return character;
    }

    public async Task<bool> DeleteCharacterAsync(int characterId)
    {
        var character = await _context.Characters.FindAsync(characterId);
        if (character == null)
            return false;

        _context.Characters.Remove(character);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted character {CharacterName} (ID: {CharacterId})",
            character.Name, character.Id);

        return true;
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
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created combat session {SessionId} in channel {ChannelId}",
            session.Id, channelId);

        return session;
    }

    public async Task<CombatSession> AddCombatSessionAsync(CombatSession session)
    {
        _context.CombatSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added combat session {SessionId} in channel {ChannelId}",
            session.Id, session.DiscordChannelId);

        return session;
    }

    public async Task<CombatSession?> GetActiveCombatSessionAsync(ulong channelId)
    {
        return await _context.CombatSessions
            .Include(s => s.Participants)
                .ThenInclude(p => p.Character)
            .FirstOrDefaultAsync(s => s.DiscordChannelId == channelId && s.IsActive);
    }

    public async Task<CombatSession?> GetCombatSessionAsync(int sessionId)
    {
        return await _context.CombatSessions
            .Include(s => s.Participants)
                .ThenInclude(p => p.Character)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task UpdateCombatSessionAsync(CombatSession session)
    {
        _context.CombatSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task EndCombatSessionAsync(int sessionId)
    {
        var session = await _context.CombatSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ended combat session {SessionId}", sessionId);
        }
    }

    public async Task<CombatParticipant> AddCombatParticipantAsync(CombatParticipant participant)
    {
        _context.CombatParticipants.Add(participant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added combat participant {ParticipantName} to session {SessionId}",
            participant.Name, participant.CombatSessionId);

        return participant;
    }

    public async Task UpdateCombatParticipantAsync(CombatParticipant participant)
    {
        _context.CombatParticipants.Update(participant);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveCombatParticipantAsync(CombatParticipant participant)
    {
        _context.CombatParticipants.Remove(participant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed combat participant {ParticipantName}", participant.Name);
    }

    public async Task<CombatAction> AddCombatActionAsync(CombatAction action)
    {
        _context.CombatActions.Add(action);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Logged combat action: {ActionType} by actor {ActorId}",
            action.ActionType, action.ActorId);

        return action;
    }

    public async Task<ShadowrunCharacter?> GetCharacterByIdAsync(int characterId)
    {
        return await _context.Characters
            .Include(c => c.Skills)
            .Include(c => c.Cyberware)
            .Include(c => c.Spells)
            .Include(c => c.Spirits)
            .Include(c => c.Gear)
            .FirstOrDefaultAsync(c => c.Id == characterId);
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
public class ShadowrunDbContext : DbContext
{
    public DbSet<ShadowrunCharacter> Characters { get; set; } = null!;
    public DbSet<CharacterSkill> CharacterSkills { get; set; } = null!;
    public DbSet<CharacterCyberware> CharacterCyberware { get; set; } = null!;
    public DbSet<CharacterSpell> CharacterSpells { get; set; } = null!;
    public DbSet<CharacterSpirit> CharacterSpirits { get; set; } = null!;
    public DbSet<CharacterGear> CharacterGear { get; set; } = null!;
    public DbSet<CombatSession> CombatSessions { get; set; } = null!;
    public DbSet<CombatParticipant> CombatParticipants { get; set; } = null!;
    public DbSet<CombatAction> CombatActions { get; set; } = null!;
    public DbSet<CombatPoolAllocation> CombatPoolAllocations { get; set; } = null!;
    public DbSet<Cyberdeck> Cyberdecks { get; set; } = null!;
    public DbSet<DeckProgram> DeckPrograms { get; set; } = null!;
    public DbSet<MatrixSession> MatrixSessions { get; set; } = null!;
    public DbSet<ActiveICE> ActiveICE { get; set; } = null!;
    public DbSet<AstralState> AstralStates { get; set; } = null!;
    public DbSet<AstralSignature> AstralSignatures { get; set; } = null!;

    // Enhanced Systems - Astral
    public DbSet<AstralCombatState> AstralCombatStates { get; set; } = null!;
    public DbSet<CharacterFocus> CharacterFoci { get; set; } = null!;
    public DbSet<AstralSignatureRecord> AstralSignatureRecords { get; set; } = null!;

    // Enhanced Systems - Matrix
    public DbSet<MatrixHost> MatrixHosts { get; set; } = null!;
    public DbSet<HostICE> HostICE { get; set; } = null!;
    public DbSet<MatrixRun> MatrixRuns { get; set; } = null!;
    public DbSet<ActiveICEncounter> ActiveICEncounters { get; set; } = null!;

    // Enhanced Systems - Combat Pool
    public DbSet<CombatPoolState> CombatPoolStates { get; set; } = null!;
    public DbSet<CombatPoolUsage> CombatPoolUsages { get; set; } = null!;

    // Enhanced Systems - Vehicles
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<VehicleWeapon> VehicleWeapons { get; set; } = null!;
    public DbSet<Drone> Drones { get; set; } = null!;
    public DbSet<DroneAutosoft> DroneAutosofts { get; set; } = null!;
    public DbSet<VehicleCombatSession> VehicleCombatSessions { get; set; } = null!;
    public DbSet<VehicleCombatant> VehicleCombatants { get; set; } = null!;

    // Enhanced Systems - Contacts
    public DbSet<CharacterContact> CharacterContacts { get; set; } = null!;
    public DbSet<LegworkAttempt> LegworkAttempts { get; set; } = null!;
    public DbSet<JohnsonMeeting> JohnsonMeetings { get; set; } = null!;

    // Enhanced Systems - Karma
    public DbSet<KarmaRecord> KarmaRecords { get; set; } = null!;
    public DbSet<KarmaExpenditure> KarmaExpenditures { get; set; } = null!;

    // Enhanced Systems - Damage/Healing
    public DbSet<DamageRecord> DamageRecords { get; set; } = null!;
    public DbSet<HealingAttempt> HealingAttempts { get; set; } = null!;
    public DbSet<HealingTimeRecord> HealingTimeRecords { get; set; } = null!;

    // Game Session Management
    public DbSet<GameSession> GameSessions { get; set; } = null!;
    public DbSet<SessionParticipant> SessionParticipants { get; set; } = null!;
    public DbSet<NarrativeEvent> NarrativeEvents { get; set; } = null!;
    public DbSet<PlayerChoice> PlayerChoices { get; set; } = null!;
    public DbSet<NPCRelationship> NPCRelationships { get; set; } = null!;
    public DbSet<ActiveMission> ActiveMissions { get; set; } = null!;
    public DbSet<MissionDefinition> MissionDefinitions { get; set; } = null!;
    
    // Session Management (Phase 4)
    public DbSet<SessionBreak> SessionBreaks { get; set; } = null!;
    public DbSet<SessionTag> SessionTags { get; set; } = null!;
    public DbSet<SessionNote> SessionNotes { get; set; } = null!;
    public DbSet<CompletedSession> CompletedSessions { get; set; } = null!;
    public DbSet<CompletedSessionTag> CompletedSessionTags { get; set; } = null!;
    public DbSet<CompletedSessionNote> CompletedSessionNotes { get; set; } = null!;

    // Dynamic Content Engine (Phase 5)
    public DbSet<SessionContentData> SessionContentData { get; set; } = null!;
    public DbSet<NPCPersonalityData> NPCPersonalityData { get; set; } = null!;
    public DbSet<NPCLearningEvent> NPCLearningEvents { get; set; } = null!;
    public DbSet<GeneratedContent> GeneratedContent { get; set; } = null!;
    public DbSet<PerformanceMetricsRecord> PerformanceMetricsRecords { get; set; } = null!;
    public DbSet<StoryPreferencesRecord> StoryPreferencesRecords { get; set; } = null!;
    public DbSet<ContentRegeneration> ContentRegenerations { get; set; } = null!;
    public DbSet<CampaignArcRecord> CampaignArcRecords { get; set; } = null!;

    public ShadowrunDbContext(DbContextOptions<ShadowrunDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<ShadowrunCharacter>()
            .HasMany(c => c.Skills)
            .WithOne(s => s.Character)
            .HasForeignKey(s => s.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShadowrunCharacter>()
            .HasMany(c => c.Cyberware)
            .WithOne(c => c.Character)
            .HasForeignKey(c => c.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShadowrunCharacter>()
            .HasMany(c => c.Spells)
            .WithOne(s => s.Character)
            .HasForeignKey(s => s.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShadowrunCharacter>()
            .HasMany(c => c.Spirits)
            .WithOne(s => s.Character)
            .HasForeignKey(s => s.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShadowrunCharacter>()
            .HasMany(c => c.Gear)
            .WithOne(g => g.Character)
            .HasForeignKey(g => g.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CombatSession>()
            .HasMany(s => s.Participants)
            .WithOne(p => p.CombatSession)
            .HasForeignKey(p => p.CombatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CombatSession>()
            .HasMany(s => s.Actions)
            .WithOne(a => a.CombatSession)
            .HasForeignKey(a => a.CombatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cyberdeck>()
            .HasMany(d => d.InstalledPrograms)
            .WithOne(p => p.Cyberdeck)
            .HasForeignKey(p => p.CyberdeckId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        modelBuilder.Entity<ShadowrunCharacter>()
            .HasIndex(c => c.DiscordUserId);

        modelBuilder.Entity<ShadowrunCharacter>()
            .HasIndex(c => new { c.DiscordUserId, c.Name });

        modelBuilder.Entity<CombatSession>()
            .HasIndex(s => new { s.DiscordChannelId, s.IsActive });

        // Game Session relationships
        modelBuilder.Entity<GameSession>()
            .HasMany(s => s.Participants)
            .WithOne(p => p.GameSession)
            .HasForeignKey(p => p.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameSession>()
            .HasMany(s => s.NarrativeEvents)
            .WithOne(e => e.GameSession)
            .HasForeignKey(e => e.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameSession>()
            .HasMany(s => s.PlayerChoices)
            .WithOne(c => c.GameSession)
            .HasForeignKey(c => c.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameSession>()
            .HasMany(s => s.NPCRelationships)
            .WithOne(r => r.GameSession)
            .HasForeignKey(r => r.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameSession>()
            .HasMany(s => s.ActiveMissions)
            .WithOne(m => m.GameSession)
            .HasForeignKey(m => m.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SessionParticipant>()
            .HasOne(p => p.Character)
            .WithMany()
            .HasForeignKey(p => p.CharacterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Game Session indexes
        modelBuilder.Entity<GameSession>()
            .HasIndex(s => new { s.DiscordChannelId, s.Status });

        modelBuilder.Entity<GameSession>()
            .HasIndex(s => s.DiscordGuildId);

        modelBuilder.Entity<NarrativeEvent>()
            .HasIndex(e => e.GameSessionId);

        modelBuilder.Entity<PlayerChoice>()
            .HasIndex(c => new { c.GameSessionId, c.DiscordUserId });

        modelBuilder.Entity<NPCRelationship>()
            .HasIndex(r => new { r.GameSessionId, r.NPCName });

        modelBuilder.Entity<ActiveMission>()
            .HasIndex(m => new { m.GameSessionId, m.Status });

        // Mission Definition relationships
        modelBuilder.Entity<MissionDefinition>()
            .HasOne(m => m.GameSession)
            .WithMany()
            .HasForeignKey(m => m.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MissionDefinition>()
            .HasIndex(m => new { m.GameSessionId, m.Status });

        // Session Break relationships
        modelBuilder.Entity<SessionBreak>()
            .HasOne(b => b.GameSession)
            .WithMany()
            .HasForeignKey(b => b.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SessionBreak>()
            .HasIndex(b => new { b.GameSessionId, b.BreakEndedAt });

        // Session Tag relationships
        modelBuilder.Entity<SessionTag>()
            .HasOne(t => t.GameSession)
            .WithMany()
            .HasForeignKey(t => t.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SessionTag>()
            .HasIndex(t => new { t.GameSessionId, t.TagName });

        // Session Note relationships
        modelBuilder.Entity<SessionNote>()
            .HasOne(n => n.GameSession)
            .WithMany()
            .HasForeignKey(n => n.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SessionNote>()
            .HasIndex(n => n.GameSessionId);

        // Completed Session relationships
        modelBuilder.Entity<CompletedSession>()
            .HasMany(s => s.Tags)
            .WithOne(t => t.CompletedSession)
            .HasForeignKey(t => t.CompletedSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompletedSession>()
            .HasMany(s => s.Notes)
            .WithOne(n => n.CompletedSession)
            .HasForeignKey(n => n.CompletedSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Completed Session indexes
        modelBuilder.Entity<CompletedSession>()
            .HasIndex(s => new { s.DiscordGuildId, s.StartedAt });

        modelBuilder.Entity<CompletedSession>()
            .HasIndex(s => s.OriginalSessionId);

        modelBuilder.Entity<CompletedSession>()
            .HasIndex(s => new { s.DiscordChannelId, s.ArchivedAt });

        // Phase 5: Session Content Data
        modelBuilder.Entity<SessionContentData>()
            .HasOne<GameSession>()
            .WithMany()
            .HasForeignKey(d => d.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SessionContentData>()
            .HasIndex(d => d.GameSessionId)
            .IsUnique();

        // Phase 5: NPC Personality Data
        modelBuilder.Entity<NPCPersonalityData>()
            .HasOne<GameSession>()
            .WithMany()
            .HasForeignKey(d => d.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NPCPersonalityData>()
            .HasIndex(d => new { d.GameSessionId, d.NPCName })
            .IsUnique();

        // Phase 5: NPC Learning Events
        modelBuilder.Entity<NPCLearningEvent>()
            .HasOne<GameSession>()
            .WithMany()
            .HasForeignKey(e => e.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NPCLearningEvent>()
            .HasIndex(e => new { e.GameSessionId, e.NPCName });

        // Phase 5: Generated Content
        modelBuilder.Entity<GeneratedContent>()
            .HasOne<GameSession>()
            .WithMany()
            .HasForeignKey(c => c.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GeneratedContent>()
            .HasIndex(c => new { c.GameSessionId, c.ContentType });

        // Phase 5: Performance Metrics
        modelBuilder.Entity<PerformanceMetricsRecord>()
            .HasOne<GameSession>()
            .WithMany()
            .HasForeignKey(m => m.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PerformanceMetricsRecord>()
            .HasIndex(m => new { m.GameSessionId, m.RecordedAt });

        // Phase 5: Story Preferences
        modelBuilder.Entity<StoryPreferencesRecord>()
            .HasOne<GameSession>()
            .WithMany()
            .HasForeignKey(p => p.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StoryPreferencesRecord>()
            .HasIndex(p => p.GameSessionId)
            .IsUnique();

        // Phase 5: Content Regeneration
        modelBuilder.Entity<ContentRegeneration>()
            .HasOne<GameSession>()
            .WithMany()
            .HasForeignKey(r => r.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ContentRegeneration>()
            .HasIndex(r => new { r.GameSessionId, r.ContentType });

        // Phase 5: Campaign Arc Records
        modelBuilder.Entity<CampaignArcRecord>()
            .HasOne<GameSession>()
            .WithMany()
            .HasForeignKey(a => a.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CampaignArcRecord>()
            .HasIndex(a => new { a.GameSessionId, a.IsCompleted });

        #region Enhanced Systems Configuration

        // Astral Combat State
        modelBuilder.Entity<AstralCombatState>()
            .HasOne<ShadowrunCharacter>()
            .WithMany()
            .HasForeignKey(a => a.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Character Focus
        modelBuilder.Entity<CharacterFocus>()
            .HasOne(f => f.Character)
            .WithMany()
            .HasForeignKey(f => f.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Astral Signature Record
        modelBuilder.Entity<AstralSignatureRecord>()
            .HasOne(s => s.Character)
            .WithMany()
            .HasForeignKey(s => s.CharacterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Matrix Host
        modelBuilder.Entity<MatrixHost>()
            .HasMany(h => h.InstalledICE)
            .WithOne(i => i.Host)
            .HasForeignKey(i => i.HostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Host ICE
        modelBuilder.Entity<HostICE>()
            .HasOne(i => i.Host)
            .WithMany(h => h.InstalledICE)
            .HasForeignKey(i => i.HostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Matrix Run
        modelBuilder.Entity<MatrixRun>()
            .HasOne(r => r.Character)
            .WithMany()
            .HasForeignKey(r => r.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatrixRun>()
            .HasOne(r => r.Host)
            .WithMany()
            .HasForeignKey(r => r.HostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatrixRun>()
            .HasOne(r => r.Cyberdeck)
            .WithMany()
            .HasForeignKey(r => r.CyberdeckId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatrixRun>()
            .HasMany(r => r.ICEncounters)
            .WithOne(e => e.MatrixRun)
            .HasForeignKey(e => e.MatrixRunId)
            .OnDelete(DeleteBehavior.Cascade);

        // Active ICEncounter
        modelBuilder.Entity<ActiveICEncounter>()
            .HasOne(e => e.MatrixRun)
            .WithMany(r => r.ICEncounters)
            .HasForeignKey(e => e.MatrixRunId)
            .OnDelete(DeleteBehavior.Cascade);

        // Combat Pool State
        modelBuilder.Entity<CombatPoolState>()
            .HasOne(p => p.Character)
            .WithMany()
            .HasForeignKey(p => p.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CombatPoolState>()
            .HasOne(p => p.CombatSession)
            .WithMany()
            .HasForeignKey(p => p.CombatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CombatPoolState>()
            .HasMany(p => p.PoolUsages)
            .WithOne(u => u.CombatPoolState)
            .HasForeignKey(u => u.CombatPoolStateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Vehicle
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Character)
            .WithMany()
            .HasForeignKey(v => v.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vehicle>()
            .HasMany(v => v.Weapons)
            .WithOne(w => w.Vehicle)
            .HasForeignKey(w => w.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Vehicle Weapon
        modelBuilder.Entity<VehicleWeapon>()
            .HasOne(w => w.Vehicle)
            .WithMany(v => v.Weapons)
            .HasForeignKey(w => w.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Drone
        modelBuilder.Entity<Drone>()
            .HasOne<ShadowrunCharacter>()
            .WithMany()
            .HasForeignKey(d => d.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Drone>()
            .HasMany(d => d.Autosofts)
            .WithOne(a => a.Drone)
            .HasForeignKey(a => a.DroneId)
            .OnDelete(DeleteBehavior.Cascade);

        // Vehicle Combat Session
        modelBuilder.Entity<VehicleCombatSession>()
            .HasOne(v => v.CombatSession)
            .WithMany()
            .HasForeignKey(v => v.CombatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VehicleCombatSession>()
            .HasMany(v => v.VehicleCombatants)
            .WithOne(c => c.VehicleCombatSession)
            .HasForeignKey(c => c.VehicleCombatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Vehicle Combatant
        modelBuilder.Entity<VehicleCombatant>()
            .HasOne(c => c.Vehicle)
            .WithMany()
            .HasForeignKey(c => c.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Character Contact
        modelBuilder.Entity<CharacterContact>()
            .HasOne(c => c.Character)
            .WithMany()
            .HasForeignKey(c => c.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CharacterContact>()
            .HasIndex(c => new { c.CharacterId, c.Name });

        // Legwork Attempt
        modelBuilder.Entity<LegworkAttempt>()
            .HasOne(l => l.Character)
            .WithMany()
            .HasForeignKey(l => l.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LegworkAttempt>()
            .HasOne(l => l.Contact)
            .WithMany()
            .HasForeignKey(l => l.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        // Johnson Meeting
        modelBuilder.Entity<JohnsonMeeting>()
            .HasOne(j => j.GameSession)
            .WithMany()
            .HasForeignKey(j => j.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Karma Record
        modelBuilder.Entity<KarmaRecord>()
            .HasOne(k => k.Character)
            .WithMany()
            .HasForeignKey(k => k.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KarmaRecord>()
            .HasMany(k => k.Expenditures)
            .WithOne(e => e.KarmaRecord)
            .HasForeignKey(e => e.KarmaRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KarmaRecord>()
            .HasIndex(k => new { k.CharacterId, k.RecordedAt });

        // Damage Record
        modelBuilder.Entity<DamageRecord>()
            .HasOne(d => d.Character)
            .WithMany()
            .HasForeignKey(d => d.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Healing Attempt
        modelBuilder.Entity<HealingAttempt>()
            .HasOne(h => h.Character)
            .WithMany()
            .HasForeignKey(h => h.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HealingAttempt>()
            .HasOne(h => h.Healer)
            .WithMany()
            .HasForeignKey(h => h.HealerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Healing Time Record
        modelBuilder.Entity<HealingTimeRecord>()
            .HasOne(h => h.Character)
            .WithMany()
            .HasForeignKey(h => h.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion
    }
}
