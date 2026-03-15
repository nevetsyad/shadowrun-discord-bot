using System.Collections.Generic;

namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// SR3 Gear Database with categories: Weapons, Armor, Cyberware, Bioware, Electronics, Vehicles, etc.
/// Contains SR3-compliant gear stats, costs, and availability
/// </summary>
public static class GearDatabase
{
    public class GearItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public long Cost { get; set; } // In nuyen
        public decimal EssenceCost { get; set; } // For cyberware/bioware
        public int Availability { get; set; } // Street index
        public string? Description { get; set; }
        public Dictionary<string, int> Stats { get; set; } = new();
        public List<string> Requirements { get; set; } = new();
        public bool IsLegal { get; set; } = true;
    }

    public static readonly Dictionary<string, List<GearItem>> AllGear = new()
    {
        // WEAPONS
        ["Weapons"] = new List<GearItem>
        {
            // Pistols
            new GearItem
            {
                Id = "heavy-pistol",
                Name = "Heavy Pistol",
                Category = "Weapons",
                SubCategory = "Pistols",
                Cost = 500,
                Stats = new Dictionary<string, int>
                {
                    ["Damage"] = 9,
                    ["Range"] = 25,
                    ["Conceal"] = 4
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "light-pistol",
                Name = "Light Pistol",
                Category = "Weapons",
                SubCategory = "Pistols",
                Cost = 300,
                Stats = new Dictionary<string, int>
                {
                    ["Damage"] = 6,
                    ["Range"] = 20,
                    ["Conceal"] = 6
                },
                IsLegal = true
            },
            
            // Edged Weapons
            new GearItem
            {
                Id = "combat-knife",
                Name = "Combat Knife",
                Category = "Weapons",
                SubCategory = "Edged Weapons",
                Cost = 200,
                Stats = new Dictionary<string, int>
                {
                    ["Damage"] = 4,
                    ["Reach"] = 0,
                    ["Conceal"] = 6
                },
                IsLegal = true
            },
            new GearItem
            {
                Id = "katana",
                Name = "Katana",
                Category = "Weapons",
                SubCategory = "Edged Weapons",
                Cost = 1000,
                Stats = new Dictionary<string, int>
                {
                    ["Damage"] = 6,
                    ["Reach"] = 1,
                    ["Conceal"] = 2
                },
                IsLegal = false
            },
            
            // Assault Rifles
            new GearItem
            {
                Id = "assault-rifle",
                Name = "Assault Rifle",
                Category = "Weapons",
                SubCategory = "Assault Rifles",
                Cost = 1500,
                Stats = new Dictionary<string, int>
                {
                    ["Damage"] = 8,
                    ["Range"] = 150,
                    ["Conceal"] = 0
                },
                IsLegal = false
            }
        },

        // ARMOR
        ["Armor"] = new List<GearItem>
        {
            new GearItem
            {
                Id = "armor-vest",
                Name = "Armor Vest",
                Category = "Armor",
                SubCategory = "Body Armor",
                Cost = 400,
                Stats = new Dictionary<string, int>
                {
                    ["Ballistic"] = 3,
                    ["Impact"] = 2,
                    ["Conceal"] = 8
                },
                IsLegal = true
            },
            new GearItem
            {
                Id = "armor-jacket",
                Name = "Armor Jacket",
                Category = "Armor",
                SubCategory = "Body Armor",
                Cost = 800,
                Stats = new Dictionary<string, int>
                {
                    ["Ballistic"] = 5,
                    ["Impact"] = 4,
                    ["Conceal"] = 4
                },
                IsLegal = true
            },
            new GearItem
            {
                Id = "full-body-armor",
                Name = "Full Body Armor",
                Category = "Armor",
                SubCategory = "Body Armor",
                Cost = 2000,
                Stats = new Dictionary<string, int>
                {
                    ["Ballistic"] = 8,
                    ["Impact"] = 6,
                    ["Conceal"] = 0
                },
                IsLegal = false
            }
        },

        // CYBERWARE
        ["Cyberware"] = new List<GearItem>
        {
            new GearItem
            {
                Id = "wired-reflexes-1",
                Name = "Wired Reflexes (Rating 1)",
                Category = "Cyberware",
                SubCategory = "Reflex Enhancers",
                Cost = 20000,
                EssenceCost = 2.0m,
                Stats = new Dictionary<string, int>
                {
                    ["ReactionBonus"] = 1,
                    ["InitiativeDice"] = 1
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "wired-reflexes-2",
                Name = "Wired Reflexes (Rating 2)",
                Category = "Cyberware",
                SubCategory = "Reflex Enhancers",
                Cost = 55000,
                EssenceCost = 3.0m,
                Stats = new Dictionary<string, int>
                {
                    ["ReactionBonus"] = 2,
                    ["InitiativeDice"] = 2
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "cybereyes-basic",
                Name = "Cybereyes (Basic)",
                Category = "Cyberware",
                SubCategory = "Sensory Enhancements",
                Cost = 5000,
                EssenceCost = 0.2m,
                Stats = new Dictionary<string, int>
                {
                    ["VisualMods"] = 0
                },
                IsLegal = true
            },
            new GearItem
            {
                Id = "smartlink",
                Name = "Smartlink",
                Category = "Cyberware",
                SubCategory = "Weapon Enhancements",
                Cost = 2500,
                EssenceCost = 0.5m,
                Stats = new Dictionary<string, int>
                {
                    ["ToHitBonus"] = 1
                },
                IsLegal = true
            },
            new GearItem
            {
                Id = "muscle-replacement-1",
                Name = "Muscle Replacement (Rating 1)",
                Category = "Cyberware",
                SubCategory = "Physical Enhancements",
                Cost = 15000,
                EssenceCost = 1.0m,
                Stats = new Dictionary<string, int>
                {
                    ["StrengthBonus"] = 1,
                    ["QuicknessBonus"] = 1
                },
                IsLegal = false
            }
        },

        // BIOWARE
        ["Bioware"] = new List<GearItem>
        {
            new GearItem
            {
                Id = "enhanced-articulation",
                Name = "Enhanced Articulation",
                Category = "Bioware",
                SubCategory = "Physical Enhancements",
                Cost = 25000,
                EssenceCost = 0.4m,
                Stats = new Dictionary<string, int>
                {
                    ["QuicknessBonus"] = 1
                },
                IsLegal = true
            },
            new GearItem
            {
                Id = "synthacardium",
                Name = "Synthacardium",
                Category = "Bioware",
                SubCategory = "Physical Enhancements",
                Cost = 12000,
                EssenceCost = 0.3m,
                Stats = new Dictionary<string, int>
                {
                    ["BodyBonus"] = 1
                },
                IsLegal = true
            }
        },

        // ELECTRONICS
        ["Electronics"] = new List<GearItem>
        {
            new GearItem
            {
                Id = "cyberdeck-base",
                Name = "Cyberdeck (Base Model)",
                Category = "Electronics",
                SubCategory = "Cyberdecks",
                Cost = 100000,
                Stats = new Dictionary<string, int>
                {
                    ["MPCP"] = 4,
                    ["Hardening"] = 4,
                    ["ActiveMemory"] = 50,
                    ["Storage"] = 100
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "commlink",
                Name = "Commlink",
                Category = "Electronics",
                SubCategory = "Communications",
                Cost = 500,
                Stats = new Dictionary<string, int>
                {
                    ["Range"] = 5
                },
                IsLegal = true
            }
        },

        // VEHICLES
        ["Vehicles"] = new List<GearItem>
        {
            new GearItem
            {
                Id = "citymaster",
                Name = "Citymaster",
                Category = "Vehicles",
                SubCategory = "Ground Vehicles",
                Cost = 35000,
                Stats = new Dictionary<string, int>
                {
                    ["Body"] = 3,
                    ["Armor"] = 2,
                    ["Speed"] = 100
                },
                IsLegal = true
            },
            new GearItem
            {
                Id = "harley-davidson",
                Name = "Harley-Davidson Scorpion",
                Category = "Vehicles",
                SubCategory = "Bikes",
                Cost = 18000,
                Stats = new Dictionary<string, int>
                {
                    ["Body"] = 2,
                    ["Armor"] = 0,
                    ["Speed"] = 140
                },
                IsLegal = true
            }
        },

        // GENERAL EQUIPMENT
        ["General"] = new List<GearItem>
        {
            new GearItem
            {
                Id = "medkit-rating-6",
                Name = "Medkit (Rating 6)",
                Category = "General",
                SubCategory = "Medical",
                Cost = 600,
                Stats = new Dictionary<string, int>
                {
                    ["Rating"] = 6
                },
                IsLegal = true
            },
            new GearItem
            {
                Id = "flashlight",
                Name = "Flashlight",
                Category = "General",
                SubCategory = "Tools",
                Cost = 20,
                Stats = new Dictionary<string, int>
                {
                    ["Range"] = 50
                },
                IsLegal = true
            }
        }
    };

    /// <summary>
    /// Get gear by category
    /// </summary>
    public static List<GearItem> GetGearByCategory(string category)
    {
        return AllGear.TryGetValue(category, out var gear) ? gear : new List<GearItem>();
    }

    /// <summary>
    /// Get gear by ID
    /// </summary>
    public static GearItem? GetGearById(string id)
    {
        foreach (var category in AllGear.Values)
        {
            var item = category.FirstOrDefault(g => g.Id == id);
            if (item != null)
                return item;
        }
        return null;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    public static List<string> GetAllCategories()
    {
        return AllGear.Keys.ToList();
    }
}
