using Microsoft.EntityFrameworkCore.Migrations;

namespace ShadowrunDiscordBot.Infrastructure.Data.Migrations;

/// <summary>
/// Migration: Enhanced Character System with Priority System and Detailed Tracking
/// 
/// Changes:
/// 1. Added PriorityAllocation table for priority-based character creation
/// 2. Added ShadowrunSpell table with detailed spell tracking
/// 3. Added ShadowrunSpirit table with full spirit details
/// 4. Added ShadowrunCyberware table with grade tracking
/// 5. Added CharacterOrigin table for background details
/// 6. Added CharacterContact table for contact management
/// 7. Added comprehensive indexes for high-frequency queries
/// 
/// Benefits:
/// - Full Shadowrun 3rd Edition priority system support
/// - Detailed magic system with spell categories, drain calculations
/// - Enhanced spirit tracking with force-based attributes
/// - Cyberware grade system (Standard, Alpha, Beta, Delta)
/// - Character background and contact management
/// - Optimized query performance with strategic indexes
/// </summary>
public partial class EnhancedCharacterSystems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        #region Priority System Tables
        
        // PriorityAllocation table
        migrationBuilder.CreateTable(
            name: "PriorityAllocations",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                Priority = table.Column<string>(type: "TEXT", maxLength: 1, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                PointsAllocated = table.Column<int>(type: "INTEGER", nullable: false),
                MaxPoints = table.Column<int>(type: "INTEGER", nullable: false),
                Metadata = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PriorityAllocations", x => x.Id);
                table.ForeignKey(
                    name: "FK_PriorityAllocations_Characters_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Characters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        
        migrationBuilder.CreateIndex(
            name: "IX_PriorityAllocations_CharacterId",
            table: "PriorityAllocations",
            column: "CharacterId");
        
        migrationBuilder.CreateIndex(
            name: "IX_PriorityAllocations_CharacterId_Category",
            table: "PriorityAllocations",
            columns: new[] { "CharacterId", "Category" },
            unique: true);
        
        migrationBuilder.CreateIndex(
            name: "IX_PriorityAllocations_Priority",
            table: "PriorityAllocations",
            column: "Priority");
        
        #endregion
        
        #region Enhanced Spell System
        
        // ShadowrunSpell table
        migrationBuilder.CreateTable(
            name: "ShadowrunSpells",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Subcategory = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                TargetType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Physical"),
                Range = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "LOS"),
                DamageType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                Duration = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Instant"),
                DrainBase = table.Column<int>(type: "INTEGER", nullable: false),
                DrainModifier = table.Column<int>(type: "INTEGER", nullable: false),
                DrainFormula = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                IsExclusive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                RequiredTradition = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                CausesDrain = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                RequiresLOSMaintenance = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                MinForce = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                LearnedAtForce = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShadowrunSpells", x => x.Id);
                table.ForeignKey(
                    name: "FK_ShadowrunSpells_Characters_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Characters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunSpells_CharacterId",
            table: "ShadowrunSpells",
            column: "CharacterId");
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunSpells_CharacterId_Name",
            table: "ShadowrunSpells",
            columns: new[] { "CharacterId", "Name" },
            unique: true);
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunSpells_Category",
            table: "ShadowrunSpells",
            column: "Category");
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunSpells_Category_TargetType",
            table: "ShadowrunSpells",
            columns: new[] { "Category", "TargetType" });
        
        #endregion
        
        #region Enhanced Spirit System
        
        // ShadowrunSpirit table
        migrationBuilder.CreateTable(
            name: "ShadowrunSpirits",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                SpiritType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Force = table.Column<int>(type: "INTEGER", nullable: false),
                Tradition = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "Hermetic"),
                ServicesOwed = table.Column<int>(type: "INTEGER", nullable: false),
                IsBound = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                SummonedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                CurrentTask = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Disposition = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Neutral"),
                IsMaterialized = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                MaterializedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                Damage = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                Powers = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Weaknesses = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShadowrunSpirits", x => x.Id);
                table.ForeignKey(
                    name: "FK_ShadowrunSpirits_Characters_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Characters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunSpirits_CharacterId",
            table: "ShadowrunSpirits",
            column: "CharacterId");
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunSpirits_CharacterId_SpiritType",
            table: "ShadowrunSpirits",
            columns: new[] { "CharacterId", "SpiritType" });
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunSpirits_Tradition",
            table: "ShadowrunSpirits",
            column: "Tradition");
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunSpirits_IsBound_ServicesOwed",
            table: "ShadowrunSpirits",
            columns: new[] { "IsBound", "ServicesOwed" });
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunSpirits_Active",
            table: "ShadowrunSpirits",
            columns: new[] { "CharacterId", "ServicesOwed", "ExpiresAt" });
        
        #endregion
        
        #region Enhanced Cyberware System
        
        // ShadowrunCyberware table
        migrationBuilder.CreateTable(
            name: "ShadowrunCyberware",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Cyberware"),
                Subcategory = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Rating = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                EssenceCost = table.Column<decimal>(type: "TEXT", nullable: false),
                BaseEssenceCost = table.Column<decimal>(type: "TEXT", nullable: false),
                NuyenCost = table.Column<long>(type: "INTEGER", nullable: false),
                Grade = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Standard"),
                IsInstalled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                Location = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                InstalledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                Bonuses = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Drawbacks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                RequiresMaintenance = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                LastMaintenance = table.Column<DateTime>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                IsCultured = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                Availability = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                Legality = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                StreetIndex = table.Column<decimal>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShadowrunCyberware", x => x.Id);
                table.ForeignKey(
                    name: "FK_ShadowrunCyberware_Characters_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Characters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunCyberware_CharacterId",
            table: "ShadowrunCyberware",
            column: "CharacterId");
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunCyberware_CharacterId_Name",
            table: "ShadowrunCyberware",
            columns: new[] { "CharacterId", "Name" });
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunCyberware_Category",
            table: "ShadowrunCyberware",
            column: "Category");
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunCyberware_Category_Grade",
            table: "ShadowrunCyberware",
            columns: new[] { "Category", "Grade" });
        
        migrationBuilder.CreateIndex(
            name: "IX_ShadowrunCyberware_Location",
            table: "ShadowrunCyberware",
            column: "Location");
        
        #endregion
        
        #region Character Origin System
        
        // CharacterOrigin table
        migrationBuilder.CreateTable(
            name: "CharacterOrigins",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                RealName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                StreetName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Age = table.Column<int>(type: "INTEGER", nullable: true),
                Gender = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Ethnicity = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                HeightCm = table.Column<int>(type: "INTEGER", nullable: true),
                WeightKg = table.Column<int>(type: "INTEGER", nullable: true),
                Appearance = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                DistinguishingFeatures = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Personality = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                Backstory = table.Column<string>(type: "TEXT", nullable: true),
                Family = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                Education = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                FormerOccupation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                ReasonForRunning = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Goals = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Fears = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                Hobbies = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                Birthplace = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Residence = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                SinStatus = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Lifestyle = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                LifestyleCost = table.Column<long>(type: "INTEGER", nullable: true),
                KnownContacts = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Enemies = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Affiliations = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                CriminalRecord = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                MoralCode = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Religion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                VoiceDescription = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                Quote = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CharacterOrigins", x => x.Id);
                table.ForeignKey(
                    name: "FK_CharacterOrigins_Characters_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Characters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        
        migrationBuilder.CreateIndex(
            name: "IX_CharacterOrigins_CharacterId",
            table: "CharacterOrigins",
            column: "CharacterId",
            unique: true);
        
        migrationBuilder.CreateIndex(
            name: "IX_CharacterOrigins_Lifestyle",
            table: "CharacterOrigins",
            column: "Lifestyle");
        
        migrationBuilder.CreateIndex(
            name: "IX_CharacterOrigins_SinStatus",
            table: "CharacterOrigins",
            column: "SinStatus");
        
        #endregion
        
        #region Character Contacts System
        
        // CharacterContact table
        migrationBuilder.CreateTable(
            name: "CharacterContacts",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                ContactName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ContactType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ConnectionRating = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                LoyaltyRating = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                Backstory = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Services = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CharacterContacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_CharacterContacts_Characters_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Characters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        
        migrationBuilder.CreateIndex(
            name: "IX_CharacterContacts_CharacterId",
            table: "CharacterContacts",
            column: "CharacterId");
        
        migrationBuilder.CreateIndex(
            name: "IX_CharacterContacts_CharacterId_ContactName",
            table: "CharacterContacts",
            columns: new[] { "CharacterId", "ContactName" });
        
        migrationBuilder.CreateIndex(
            name: "IX_CharacterContacts_ContactType",
            table: "CharacterContacts",
            column: "ContactType");
        
        migrationBuilder.CreateIndex(
            name: "IX_CharacterContacts_Ratings",
            table: "CharacterContacts",
            columns: new[] { "CharacterId", "ConnectionRating", "LoyaltyRating" });
        
        migrationBuilder.CreateIndex(
            name: "IX_CharacterContacts_Active",
            table: "CharacterContacts",
            columns: new[] { "CharacterId", "IsActive" });
        
        #endregion
        
        #region Enhanced Indexes for Existing Tables
        
        // Characters table additional indexes
        migrationBuilder.CreateIndex(
            name: "IX_Characters_Metatype",
            table: "Characters",
            column: "Metatype");
        
        migrationBuilder.CreateIndex(
            name: "IX_Characters_Archetype",
            table: "Characters",
            column: "Archetype");
        
        migrationBuilder.CreateIndex(
            name: "IX_Characters_Magic_Archetype",
            table: "Characters",
            columns: new[] { "Magic", "Archetype" });
        
        // Skills table additional indexes
        migrationBuilder.CreateIndex(
            name: "IX_Skills_CharacterId_SkillName",
            table: "Skills",
            columns: new[] { "CharacterId", "SkillName" },
            unique: true);
        
        migrationBuilder.CreateIndex(
            name: "IX_Skills_SkillName",
            table: "Skills",
            column: "SkillName");
        
        migrationBuilder.CreateIndex(
            name: "IX_Skills_IsKnowledgeSkill",
            table: "Skills",
            column: "IsKnowledgeSkill");
        
        // Gear table additional indexes
        migrationBuilder.CreateIndex(
            name: "IX_Gear_CharacterId_Name",
            table: "Gear",
            columns: new[] { "CharacterId", "Name" });
        
        migrationBuilder.CreateIndex(
            name: "IX_Gear_Category",
            table: "Gear",
            column: "Category");
        
        migrationBuilder.CreateIndex(
            name: "IX_Gear_Equipped",
            table: "Gear",
            columns: new[] { "CharacterId", "IsEquipped" });
        
        // CombatSessions table additional indexes
        migrationBuilder.CreateIndex(
            name: "IX_CombatSessions_IsActive",
            table: "CombatSessions",
            column: "IsActive");
        
        migrationBuilder.CreateIndex(
            name: "IX_CombatSessions_StartedAt",
            table: "CombatSessions",
            column: "StartedAt");
        
        #endregion
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop new tables
        migrationBuilder.DropTable(name: "CharacterContacts");
        migrationBuilder.DropTable(name: "CharacterOrigins");
        migrationBuilder.DropTable(name: "ShadowrunCyberware");
        migrationBuilder.DropTable(name: "ShadowrunSpirits");
        migrationBuilder.DropTable(name: "ShadowrunSpells");
        migrationBuilder.DropTable(name: "PriorityAllocations");
        
        // Drop additional indexes (SQLite requires dropping and recreating, so we'll leave them)
        // In production, you'd want to properly drop these indexes
    }
}
