using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Database service with Entity Framework Core and SQLite
/// </summary>
public class DatabaseService : IAsyncDisposable
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

    public async Task<CombatSession?> GetActiveCombatSessionAsync(ulong channelId)
    {
        return await _context.CombatSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.DiscordChannelId == channelId && s.IsActive);
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
    }
}
