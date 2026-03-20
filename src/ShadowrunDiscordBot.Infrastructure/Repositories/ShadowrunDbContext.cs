using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ShadowrunDiscordBot.Infrastructure.Repositories
{
    /// <summary>
    /// Entity Framework Core database context
    /// </summary>
    public class ShadowrunDbContext : DbContext
    {
        public DbSet<Character> Characters { get; set; } = null!;
        public DbSet<CharacterSkill> Skills { get; set; } = null!;
        public DbSet<CharacterCyberware> Cyberware { get; set; } = null!;
        public DbSet<CharacterSpell> Spells { get; set; } = null!;
        public DbSet<CharacterSpirit> Spirits { get; set; } = null!;
        public DbSet<CharacterGear> Gear { get; set; } = null!;
        public DbSet<CombatSession> CombatSessions { get; set; } = null!;
        
        public ShadowrunDbContext(DbContextOptions<ShadowrunDbContext> options)
            : base(options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Character configuration
            modelBuilder.Entity<Character>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DiscordUserId);
                entity.HasIndex(e => new { e.DiscordUserId, e.Name }).IsUnique(); // GPT-5.4 FIX: Enforce unique character names per user at the database level
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Metatype).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Archetype).IsRequired().HasMaxLength(50);
            });
            
            // Skill configuration
            modelBuilder.Entity<CharacterSkill>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CharacterId);
                entity.Property(e => e.SkillName).IsRequired().HasMaxLength(100);
            });
            
            // Cyberware configuration
            modelBuilder.Entity<CharacterCyberware>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CharacterId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            });
            
            // Spell configuration
            modelBuilder.Entity<CharacterSpell>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CharacterId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            });
            
            // Spirit configuration
            modelBuilder.Entity<CharacterSpirit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CharacterId);
                entity.Property(e => e.SpiritType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Tradition).IsRequired().HasMaxLength(50);
            });
            
            // Gear configuration
            modelBuilder.Entity<CharacterGear>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CharacterId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            });
            
            // Combat session configuration
            modelBuilder.Entity<CombatSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.DiscordChannelId, e.IsActive });
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });
        }
    }
}
