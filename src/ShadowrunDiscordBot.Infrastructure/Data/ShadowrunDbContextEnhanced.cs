using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Domain.Interfaces;

namespace ShadowrunDiscordBot.Infrastructure.Data;

/// <summary>
/// Enhanced Entity Framework Core database context with priority system and detailed tracking
/// </summary>
public class ShadowrunDbContext : DbContext
{
    // Core entities
    public DbSet<Character> Characters { get; set; } = null!;
    public DbSet<CharacterSkill> Skills { get; set; } = null!;
    public DbSet<CharacterGear> Gear { get; set; } = null!;
    public DbSet<CombatSession> CombatSessions { get; set; } = null!;
    public DbSet<CombatParticipant> CombatParticipants { get; set; } = null!;
    public DbSet<CombatAction> CombatActions { get; set; } = null!;
    
    // Enhanced entities (new)
    public DbSet<PriorityAllocation> PriorityAllocations { get; set; } = null!;
    public DbSet<ShadowrunSpell> ShadowrunSpells { get; set; } = null!;
    public DbSet<ShadowrunSpirit> ShadowrunSpirits { get; set; } = null!;
    public DbSet<ShadowrunCyberware> ShadowrunCyberware { get; set; } = null!;
    public DbSet<CharacterOrigin> CharacterOrigins { get; set; } = null!;
    public DbSet<CharacterContact> CharacterContacts { get; set; } = null!;
    
    // Legacy entities (for backward compatibility)
    public DbSet<CharacterCyberware> Cyberware { get; set; } = null!;
    public DbSet<CharacterSpell> Spells { get; set; } = null!;
    public DbSet<CharacterSpirit> Spirits { get; set; } = null!;

    // Game Session entities
    public DbSet<GameSession> GameSessions { get; set; } = null!;
    public DbSet<SessionParticipant> SessionParticipants { get; set; } = null!;
    public DbSet<NarrativeEvent> NarrativeEvents { get; set; } = null!;
    public DbSet<PlayerChoice> PlayerChoices { get; set; } = null!;
    public DbSet<NPCRelationship> NPCRelationships { get; set; } = null!;
    public DbSet<ActiveMission> ActiveMissions { get; set; } = null!;
    public DbSet<SessionBreak> SessionBreaks { get; set; } = null!;
    public DbSet<SessionTag> SessionTags { get; set; } = null!;
    public DbSet<SessionNote> SessionNotes { get; set; } = null!;

    // Completed Session entities
    public DbSet<CompletedSession> CompletedSessions { get; set; } = null!;
    public DbSet<CompletedSessionTag> CompletedSessionTags { get; set; } = null!;
    public DbSet<CompletedSessionNote> CompletedSessionNotes { get; set; } = null!;

    public ShadowrunDbContext(DbContextOptions<ShadowrunDbContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        #region Character Configuration
        
        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes for common queries
            entity.HasIndex(e => e.DiscordUserId)
                .HasDatabaseName("IX_Characters_DiscordUserId");
            
            entity.HasIndex(e => new { e.DiscordUserId, e.Name })
                .IsUnique()
                .HasDatabaseName("IX_Characters_DiscordUserId_Name");
            
            entity.HasIndex(e => e.Metatype)
                .HasDatabaseName("IX_Characters_Metatype");
            
            entity.HasIndex(e => e.Archetype)
                .HasDatabaseName("IX_Characters_Archetype");
            
            // Composite index for awakened character queries
            entity.HasIndex(e => new { e.Magic, e.Archetype })
                .HasDatabaseName("IX_Characters_Magic_Archetype");
            
            // Properties
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Metatype)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Archetype)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Essence)
                .HasDefaultValue(600);
            
            // Relationships
            entity.HasMany(e => e.Skills)
                .WithOne()
                .HasForeignKey(s => s.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Cyberware)
                .WithOne()
                .HasForeignKey(c => c.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Spells)
                .WithOne()
                .HasForeignKey(s => s.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Spirits)
                .WithOne()
                .HasForeignKey(s => s.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Gear)
                .WithOne()
                .HasForeignKey(g => g.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        #endregion
        
        #region Priority System Configuration
        
        modelBuilder.Entity<PriorityAllocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes
            entity.HasIndex(e => e.CharacterId)
                .HasDatabaseName("IX_PriorityAllocations_CharacterId");
            
            entity.HasIndex(e => e.Priority)
                .HasDatabaseName("IX_PriorityAllocations_Priority");
            
            // Properties
            entity.Property(e => e.Priority)
                .IsRequired()
                .HasMaxLength(1);
            
            // Relationships
            entity.HasOne(e => e.Character)
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        #endregion
        
        #region Enhanced Spell Configuration
        
        modelBuilder.Entity<ShadowrunSpell>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes
            entity.HasIndex(e => e.CharacterId)
                .HasDatabaseName("IX_ShadowrunSpells_CharacterId");
            
            entity.HasIndex(e => new { e.CharacterId, e.Name })
                .IsUnique()
                .HasDatabaseName("IX_ShadowrunSpells_CharacterId_Name");
            
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_ShadowrunSpells_Category");
            
            entity.HasIndex(e => new { e.Category, e.TargetType })
                .HasDatabaseName("IX_ShadowrunSpells_Category_TargetType");
            
            // Properties
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.TargetType)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Physical");
            
            entity.Property(e => e.Range)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("LOS");
            
            entity.Property(e => e.Duration)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Instant");
            
            // Relationships
            entity.HasOne(e => e.Character)
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        #endregion
        
        #region Enhanced Spirit Configuration
        
        modelBuilder.Entity<ShadowrunSpirit>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes
            entity.HasIndex(e => e.CharacterId)
                .HasDatabaseName("IX_ShadowrunSpirits_CharacterId");
            
            entity.HasIndex(e => new { e.CharacterId, e.SpiritType })
                .HasDatabaseName("IX_ShadowrunSpirits_CharacterId_SpiritType");
            
            entity.HasIndex(e => e.Tradition)
                .HasDatabaseName("IX_ShadowrunSpirits_Tradition");
            
            entity.HasIndex(e => new { e.IsBound, e.ServicesOwed })
                .HasDatabaseName("IX_ShadowrunSpirits_IsBound_ServicesOwed");
            
            // Index for active spirits (common query)
            entity.HasIndex(e => new { e.CharacterId, e.ServicesOwed, e.ExpiresAt })
                .HasDatabaseName("IX_ShadowrunSpirits_Active");
            
            // Properties
            entity.Property(e => e.SpiritType)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Tradition)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Hermetic");
            
            entity.Property(e => e.Disposition)
                .HasMaxLength(20)
                .HasDefaultValue("Neutral");
            
            // Relationships
            entity.HasOne(e => e.Character)
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        #endregion
        
        #region Enhanced Cyberware Configuration
        
        modelBuilder.Entity<ShadowrunCyberware>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes
            entity.HasIndex(e => e.CharacterId)
                .HasDatabaseName("IX_ShadowrunCyberware_CharacterId");
            
            entity.HasIndex(e => new { e.CharacterId, e.Name })
                .HasDatabaseName("IX_ShadowrunCyberware_CharacterId_Name");
            
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_ShadowrunCyberware_Category");
            
            entity.HasIndex(e => new { e.Category, e.Grade })
                .HasDatabaseName("IX_ShadowrunCyberware_Category_Grade");
            
            entity.HasIndex(e => e.Location)
                .HasDatabaseName("IX_ShadowrunCyberware_Location");
            
            // Properties
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Cyberware");
            
            entity.Property(e => e.Grade)
                .HasMaxLength(20)
                .HasDefaultValue("Standard");
            
            // Relationships
            entity.HasOne(e => e.Character)
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        #endregion
        
        #region Character Origin Configuration
        
        modelBuilder.Entity<CharacterOrigin>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes
            entity.HasIndex(e => e.CharacterId)
                .IsUnique()
                .HasDatabaseName("IX_CharacterOrigins_CharacterId");
            
            entity.HasIndex(e => e.Lifestyle)
                .HasDatabaseName("IX_CharacterOrigins_Lifestyle");
            
            entity.HasIndex(e => e.SinStatus)
                .HasDatabaseName("IX_CharacterOrigins_SinStatus");
            
            // Properties
            entity.Property(e => e.RealName)
                .HasMaxLength(100);
            
            entity.Property(e => e.StreetName)
                .HasMaxLength(100);
            
            entity.Property(e => e.Lifestyle)
                .HasMaxLength(50);
            
            entity.Property(e => e.SinStatus)
                .HasMaxLength(100);
            
            // Relationships
            entity.HasOne(e => e.Character)
                .WithOne()
                .HasForeignKey<CharacterOrigin>(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        #endregion
        
        #region Character Contacts Configuration
        
        modelBuilder.Entity<CharacterContact>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes
            entity.HasIndex(e => e.CharacterId)
                .HasDatabaseName("IX_CharacterContacts_CharacterId");
            
            entity.HasIndex(e => new { e.CharacterId, e.ContactName })
                .HasDatabaseName("IX_CharacterContacts_CharacterId_ContactName");
            
            entity.HasIndex(e => e.ContactType)
                .HasDatabaseName("IX_CharacterContacts_ContactType");
            
            // Composite index for high-connection contacts (common query)
            entity.HasIndex(e => new { e.CharacterId, e.ConnectionRating, e.LoyaltyRating })
                .HasDatabaseName("IX_CharacterContacts_Ratings");
            
            // Index for active contacts
            entity.HasIndex(e => new { e.CharacterId, e.IsActive })
                .HasDatabaseName("IX_CharacterContacts_Active");
            
            // Properties
            entity.Property(e => e.ContactName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.ContactType)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.ConnectionRating)
                .HasDefaultValue(1);
            
            entity.Property(e => e.LoyaltyRating)
                .HasDefaultValue(1);
            
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
            
            // Relationships
            entity.HasOne(e => e.Character)
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        #endregion
        
        #region Skill Configuration
        
        modelBuilder.Entity<CharacterSkill>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes
            entity.HasIndex(e => e.CharacterId)
                .HasDatabaseName("IX_Skills_CharacterId");
            
            entity.HasIndex(e => new { e.CharacterId, e.SkillName })
                .IsUnique()
                .HasDatabaseName("IX_Skills_CharacterId_SkillName");
            
            entity.HasIndex(e => e.SkillName)
                .HasDatabaseName("IX_Skills_SkillName");
            
            entity.HasIndex(e => e.IsKnowledgeSkill)
                .HasDatabaseName("IX_Skills_IsKnowledgeSkill");
            
            // Properties
            entity.Property(e => e.SkillName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Rating)
                .HasDefaultValue(0);
        });
        
        #endregion
        
        #region Gear Configuration
        
        modelBuilder.Entity<CharacterGear>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes
            entity.HasIndex(e => e.CharacterId)
                .HasDatabaseName("IX_Gear_CharacterId");
            
            entity.HasIndex(e => new { e.CharacterId, e.Name })
                .HasDatabaseName("IX_Gear_CharacterId_Name");
            
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_Gear_Category");
            
            entity.HasIndex(e => new { e.CharacterId, e.IsEquipped })
                .HasDatabaseName("IX_Gear_Equipped");
            
            // Properties
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1);
            
            entity.Property(e => e.IsEquipped)
                .HasDefaultValue(false);
        });
        
        #endregion
        
        #region Legacy Entity Configurations (for backward compatibility)
        
        // Legacy Cyberware
        modelBuilder.Entity<CharacterCyberware>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CharacterId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
        });
        
        // Legacy Spells
        modelBuilder.Entity<CharacterSpell>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CharacterId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
        });
        
        // Legacy Spirits
        modelBuilder.Entity<CharacterSpirit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CharacterId);
            entity.Property(e => e.SpiritType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Tradition).IsRequired().HasMaxLength(50);
        });
        
        #endregion
        
        #region Combat Session Configuration
        
        modelBuilder.Entity<CombatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Indexes
            entity.HasIndex(e => new { e.DiscordChannelId, e.IsActive })
                .HasDatabaseName("IX_CombatSessions_Channel_Active");
            
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_CombatSessions_IsActive");
            
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_CombatSessions_CreatedAt");
            
            // Properties
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });

        #endregion

        #region Combat Participant Configuration

        modelBuilder.Entity<CombatParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.CombatSessionId)
                .HasDatabaseName("IX_CombatParticipants_CombatSessionId");

            entity.HasIndex(e => e.CharacterId)
                .HasDatabaseName("IX_CombatParticipants_CharacterId");

            entity.HasIndex(e => new { e.CombatSessionId, e.TeamId })
                .HasDatabaseName("IX_CombatParticipants_Session_Team");

            entity.HasIndex(e => new { e.CombatSessionId, e.IsEliminated })
                .HasDatabaseName("IX_CombatParticipants_Session_Active");

            // Properties
            entity.Property(e => e.TeamId)
                .HasDefaultValue(0);

            entity.Property(e => e.HitPoints)
                .HasDefaultValue(10);

            entity.Property(e => e.DamageTrack)
                .HasDefaultValue(0);

            entity.Property(e => e.IsEliminated)
                .HasDefaultValue(false);

            // Relationships
            entity.HasOne(e => e.CombatSession)
                .WithMany()
                .HasForeignKey(e => e.CombatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Character)
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        #endregion

        #region Combat Action Configuration

        modelBuilder.Entity<CombatAction>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.CombatSessionId)
                .HasDatabaseName("IX_CombatActions_CombatSessionId");

            entity.HasIndex(e => e.ActorId)
                .HasDatabaseName("IX_CombatActions_ActorId");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_CombatActions_CreatedAt");

            entity.HasIndex(e => new { e.CombatSessionId, e.CreatedAt })
                .HasDatabaseName("IX_CombatActions_Session_Time");

            // Properties
            entity.Property(e => e.ActionType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Attack");

            entity.Property(e => e.ActorName)
                .HasMaxLength(100);

            entity.Property(e => e.TargetName)
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);
        });

        #endregion

        #region Game Session Configuration

        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.DiscordChannelId)
                .HasDatabaseName("IX_GameSessions_ChannelId");

            entity.HasIndex(e => e.DiscordGuildId)
                .HasDatabaseName("IX_GameSessions_GuildId");

            entity.HasIndex(e => e.GameMasterUserId)
                .HasDatabaseName("IX_GameSessions_GMUserId");

            entity.HasIndex(e => new { e.Status, e.IsActive })
                .HasDatabaseName("IX_GameSessions_Status_Active");

            entity.HasIndex(e => e.InGameDateTime)
                .HasDatabaseName("IX_GameSessions_InGameDateTime");

            entity.HasIndex(e => e.LastActivityAt)
                .HasDatabaseName("IX_GameSessions_LastActivityAt");

            // Properties
            entity.Property(e => e.CurrentLocation)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.LocationDescription)
                .HasMaxLength(1000);

            entity.Property(e => e.SessionName)
                .HasMaxLength(200);

            entity.Property(e => e.Notes)
                .HasMaxLength(5000);

            entity.Property(e => e.Metadata)
                .HasColumnType("json");

            // Relationships
            entity.HasMany(e => e.Participants)
                .WithOne()
                .HasForeignKey(p => p.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.NarrativeEvents)
                .WithOne()
                .HasForeignKey(e => e.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.PlayerChoices)
                .WithOne()
                .HasForeignKey(c => c.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.NPCRelationships)
                .WithOne()
                .HasForeignKey(r => r.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ActiveMissions)
                .WithOne()
                .HasForeignKey(m => m.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region Session Participant Configuration

        modelBuilder.Entity<SessionParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.GameSessionId, e.DiscordUserId })
                .IsUnique()
                .HasDatabaseName("IX_SessionParticipants_Session_UserId");

            entity.HasIndex(e => e.CharacterId)
                .HasDatabaseName("IX_SessionParticipants_CharacterId");

            entity.HasIndex(e => new { e.GameSessionId, e.IsActive })
                .HasDatabaseName("IX_SessionParticipants_Session_Active");

            entity.HasIndex(e => e.SessionKarma)
                .HasDatabaseName("IX_SessionParticipants_Karma");

            // Properties
            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            // Relationships
            entity.HasOne(e => e.GameSession)
                .WithMany(s => s.Participants)
                .HasForeignKey(e => e.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Character)
                .WithMany()
                .HasForeignKey(e => e.CharacterId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        #endregion

        #region Narrative Event Configuration

        modelBuilder.Entity<NarrativeEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.GameSessionId, e.EventType })
                .HasDatabaseName("IX_NarrativeEvents_Session_Type");

            entity.HasIndex(e => new { e.GameSessionId, e.Importance })
                .HasDatabaseName("IX_NarrativeEvents_Session_Importance");

            entity.HasIndex(e => new { e.GameSessionId, e.InGameDateTime })
                .HasDatabaseName("IX_NarrativeEvents_Session_DateTime");

            entity.HasIndex(e => new { e.GameSessionId, e.RecordedAt })
                .HasDatabaseName("IX_NarrativeEvents_Session_Time");

            // Properties
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.NPCsInvolved)
                .HasMaxLength(500);

            entity.Property(e => e.Location)
                .HasMaxLength(200);

            entity.Property(e => e.Tags)
                .HasMaxLength(500);

            // Relationships
            entity.HasOne(e => e.GameSession)
                .WithMany(s => s.NarrativeEvents)
                .HasForeignKey(e => e.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RelatedNarrativeEvent)
                .WithMany()
                .HasForeignKey(e => e.RelatedNarrativeEventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        #endregion

        #region Player Choice Configuration

        modelBuilder.Entity<PlayerChoice>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.GameSessionId, e.MadeAt })
                .HasDatabaseName("IX_PlayerChoices_Session_Time");

            entity.HasIndex(e => e.DiscordUserId)
                .HasDatabaseName("IX_PlayerChoices_UserId");

            entity.HasIndex(e => e.IsResolved)
                .HasDatabaseName("IX_PlayerChoices_IsResolved");

            // Properties
            entity.Property(e => e.ChoiceDescription)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.PlayerDecision)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Consequences)
                .HasMaxLength(1000);

            // Relationships
            entity.HasOne(e => e.GameSession)
                .WithMany(s => s.PlayerChoices)
                .HasForeignKey(e => e.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RelatedNarrativeEvent)
                .WithMany()
                .HasForeignKey(e => e.RelatedNarrativeEventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        #endregion

        #region NPC Relationship Configuration

        modelBuilder.Entity<NPCRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.GameSessionId, e.NPCName })
                .IsUnique()
                .HasDatabaseName("IX_NPCRelationships_Session_Name");

            entity.HasIndex(e => e.Attitude)
                .HasDatabaseName("IX_NPCRelationships_Attitude");

            entity.HasIndex(e => e.TrustLevel)
                .HasDatabaseName("IX_NPCRelationships_Trust");

            entity.HasIndex(e => new { e.GameSessionId, e.IsActive })
                .HasDatabaseName("IX_NPCRelationships_Session_Active");

            // Properties
            entity.Property(e => e.NPCName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.NPCRole)
                .HasMaxLength(100);

            entity.Property(e => e.Organization)
                .HasMaxLength(100);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.FirstMeeting)
                .HasMaxLength(500);

            entity.Property(e => e.InteractionHistory)
                .HasMaxLength(2000);

            // Relationships
            entity.HasOne(e => e.GameSession)
                .WithMany(s => s.NPCRelationships)
                .HasForeignKey(e => e.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region Active Mission Configuration

        modelBuilder.Entity<ActiveMission>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.GameSessionId, e.Status })
                .HasDatabaseName("IX_ActiveMissions_Session_Status");

            entity.HasIndex(e => new { e.GameSessionId, e.MissionType })
                .HasDatabaseName("IX_ActiveMissions_Session_Type");

            entity.HasIndex(e => e.Deadline)
                .HasDatabaseName("IX_ActiveMissions_Deadline");

            entity.HasIndex(e => new { e.GameSessionId, e.Status, e.Deadline })
                .HasFilter("[Status] IN ('InProgress', 'Planning')")
                .HasDatabaseName("IX_ActiveMissions_Active_Deadline");

            // Properties
            entity.Property(e => e.MissionName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Johnson)
                .HasMaxLength(100);

            entity.Property(e => e.MissionType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Objective)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.TargetLocation)
                .HasMaxLength(200);

            entity.Property(e => e.TargetOrganization)
                .HasMaxLength(100);

            entity.Property(e => e.Notes)
                .HasMaxLength(2000);

            // Relationships
            entity.HasOne(e => e.GameSession)
                .WithMany(s => s.ActiveMissions)
                .HasForeignKey(e => e.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region Session Break Configuration

        modelBuilder.Entity<SessionBreak>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.GameSessionId, e.BreakStartedAt })
                .HasDatabaseName("IX_SessionBreaks_Session_Time");

            entity.HasIndex(e => e.BreakEndedAt)
                .HasDatabaseName("IX_SessionBreaks_EndedAt");

            entity.HasIndex(e => new { e.GameSessionId, e.IsAutomatic })
                .HasDatabaseName("IX_SessionBreaks_Session_Automatic");

            // Properties
            entity.Property(e => e.Reason)
                .HasMaxLength(200);

            // Relationships
            entity.HasOne(e => e.GameSession)
                .WithMany()
                .HasForeignKey(e => e.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region Session Tag Configuration

        modelBuilder.Entity<SessionTag>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.GameSessionId, e.TagName })
                .IsUnique()
                .HasDatabaseName("IX_SessionTags_Session_Name");

            entity.HasIndex(e => new { e.GameSessionId, e.Category })
                .HasDatabaseName("IX_SessionTags_Session_Category");

            // Properties
            entity.Property(e => e.TagName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Category)
                .HasMaxLength(50);

            // Relationships
            entity.HasOne(e => e.GameSession)
                .WithMany(s => s.Tags)
                .HasForeignKey(e => e.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region Session Note Configuration

        modelBuilder.Entity<SessionNote>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.GameSessionId, e.IsPinned })
                .HasDatabaseName("IX_SessionNotes_Session_Pinned");

            entity.HasIndex(e => new { e.GameSessionId, e.NoteType })
                .HasDatabaseName("IX_SessionNotes_Session_Type");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_SessionNotes_CreatedAt");

            // Properties
            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.NoteType)
                .HasMaxLength(50);

            // Relationships
            entity.HasOne(e => e.GameSession)
                .WithMany(s => s.Notes)
                .HasForeignKey(e => e.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region Completed Session Configuration

        modelBuilder.Entity<CompletedSession>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.OriginalSessionId })
                .HasDatabaseName("IX_CompletedSessions_OriginalId");

            entity.HasIndex(e => e.DiscordChannelId)
                .HasDatabaseName("IX_CompletedSessions_ChannelId");

            entity.HasIndex(e => e.DiscordGuildId)
                .HasDatabaseName("IX_CompletedSessions_GuildId");

            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_CompletedSessions_StartedAt");

            entity.HasIndex(e => e.ArchivedAt)
                .HasDatabaseName("IX_CompletedSessions_ArchivedAt");

            entity.HasIndex(e => new { e.Category, e.ArchivedAt })
                .HasDatabaseName("IX_CompletedSessions_Category_Date");

            // Properties
            entity.Property(e => e.SessionName)
                .HasMaxLength(200);

            entity.Property(e => e.Outcome)
                .HasMaxLength(2000);

            entity.Property(e => e.Category)
                .HasMaxLength(50);

            entity.Property(e => e.Metadata)
                .HasColumnType("json");

            // Relationships
            entity.HasOne(e => e.GameSession)
                .WithMany()
                .HasForeignKey(e => e.OriginalSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Tags)
                .WithOne()
                .HasForeignKey(t => t.CompletedSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Notes)
                .WithOne()
                .HasForeignKey(n => n.CompletedSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region Completed Session Tag Configuration

        modelBuilder.Entity<CompletedSessionTag>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.CompletedSessionId, e.TagName })
                .IsUnique()
                .HasDatabaseName("IX_CompletedSessionTags_Session_Name");

            // Properties
            entity.Property(e => e.TagName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Category)
                .HasMaxLength(50);

            // Relationships
            entity.HasOne(e => e.CompletedSession)
                .WithMany(s => s.Tags)
                .HasForeignKey(e => e.CompletedSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion

        #region Completed Session Note Configuration

        modelBuilder.Entity<CompletedSessionNote>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.CompletedSessionId, e.NoteType })
                .HasDatabaseName("IX_CompletedSessionNotes_Session_Type");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_CompletedSessionNotes_CreatedAt");

            // Properties
            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.NoteType)
                .HasMaxLength(50);

            // Relationships
            entity.HasOne(e => e.CompletedSession)
                .WithMany(s => s.Notes)
                .HasForeignKey(e => e.CompletedSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion
    }
}
