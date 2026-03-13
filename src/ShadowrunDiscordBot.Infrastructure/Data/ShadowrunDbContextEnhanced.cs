using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Domain.Entities;

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
            
            entity.HasIndex(e => new { e.CharacterId, e.Category })
                .IsUnique()
                .HasDatabaseName("IX_PriorityAllocations_CharacterId_Category");
            
            entity.HasIndex(e => e.Priority)
                .HasDatabaseName("IX_PriorityAllocations_Priority");
            
            // Properties
            entity.Property(e => e.Priority)
                .IsRequired()
                .HasMaxLength(1);
            
            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(20);
            
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
            
            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_CombatSessions_StartedAt");
            
            // Properties
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });
        
        #endregion
    }
}
