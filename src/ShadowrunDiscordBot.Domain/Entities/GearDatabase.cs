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
            // Cyberdecks - All models from SR3 rulebook
            new GearItem
            {
                Id = "cyberdeck-allegience-alpha",
                Name = "Allegience Alpha",
                Category = "Electronics",
                SubCategory = "Cyberdecks",
                Cost = 0, // Found in the game world, not for sale
                Stats = new Dictionary<string, int>
                {
                    ["MPCP"] = 3,
                    ["Hardening"] = 0,
                    ["Response"] = 0,
                    ["ActiveMemory"] = 30,
                    ["Storage"] = 100,
                    ["Load"] = 10
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "cyberdeck-pcd-500",
                Name = "PCD-500",
                Category = "Electronics",
                SubCategory = "Cyberdecks",
                Cost = 5000,
                Stats = new Dictionary<string, int>
                {
                    ["MPCP"] = 4,
                    ["Hardening"] = 1,
                    ["Response"] = 0,
                    ["ActiveMemory"] = 50,
                    ["Storage"] = 100,
                    ["Load"] = 20
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "cyberdeck-fuchi-cyber-5",
                Name = "Fuchi Cyber-5",
                Category = "Electronics",
                SubCategory = "Cyberdecks",
                Cost = 25000,
                Stats = new Dictionary<string, int>
                {
                    ["MPCP"] = 6,
                    ["Hardening"] = 2,
                    ["Response"] = 1,
                    ["ActiveMemory"] = 100,
                    ["Storage"] = 500,
                    ["Load"] = 20
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "cyberdeck-sega-cty-360",
                Name = "SEGA CTY-360",
                Category = "Electronics",
                SubCategory = "Cyberdecks",
                Cost = 60000,
                Stats = new Dictionary<string, int>
                {
                    ["MPCP"] = 8,
                    ["Hardening"] = 3,
                    ["Response"] = 1,
                    ["ActiveMemory"] = 200,
                    ["Storage"] = 500,
                    ["Load"] = 50
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "cyberdeck-fuchi-cyber-7",
                Name = "Fuchi Cyber-7",
                Category = "Electronics",
                SubCategory = "Cyberdecks",
                Cost = 125000,
                Stats = new Dictionary<string, int>
                {
                    ["MPCP"] = 10,
                    ["Hardening"] = 4,
                    ["Response"] = 2,
                    ["ActiveMemory"] = 300,
                    ["Storage"] = 1000,
                    ["Load"] = 50
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "cyberdeck-fairlight-excalibur",
                Name = "Fairlight Excalibur",
                Category = "Electronics",
                SubCategory = "Cyberdecks",
                Cost = 250000,
                Stats = new Dictionary<string, int>
                {
                    ["MPCP"] = 12,
                    ["Hardening"] = 5,
                    ["Response"] = 3,
                    ["ActiveMemory"] = 500,
                    ["Storage"] = 1000,
                    ["Load"] = 100
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

        // VEHICLES - Ground Vehicles (SR3)
        ["Vehicles"] = new List<GearItem>
        {
            // Cars
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
                    ["Speed"] = 100,
                    ["Handling"] = 3
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
                    ["Speed"] = 140,
                    ["Handling"] = 4
                },
                IsLegal = true
            },
            new GearItem
            {
                Id = "toyota-camry",
                Name = "Toyota Camry",
                Category = "Vehicles",
                SubCategory = "Ground Vehicles",
                Cost = 25000,
                Stats = new Dictionary<string, int>
                {
                    ["Body"] = 2,
                    ["Armor"] = 1,
                    ["Speed"] = 80,
                    ["Handling"] = 2
                },
                IsLegal = true
            },
            
            // Trucks
            new GearItem
            {
                Id = "ford-f150",
                Name = "Ford F-150",
                Category = "Vehicles",
                SubCategory = "Ground Vehicles",
                Cost = 45000,
                Stats = new Dictionary<string, int>
                {
                    ["Body"] = 4,
                    ["Armor"] = 3,
                    ["Speed"] = 70,
                    ["Handling"] = 2
                },
                IsLegal = true
            },
            
            // Luxury Vehicles
            new GearItem
            {
                Id = "mercedes-s500",
                Name = "Mercedes S500",
                Category = "Vehicles",
                SubCategory = "Ground Vehicles",
                Cost = 65000,
                Stats = new Dictionary<string, int>
                {
                    ["Body"] = 3,
                    ["Armor"] = 2,
                    ["Speed"] = 110,
                    ["Handling"] = 4
                },
                IsLegal = true
            },
            
            // Military Vehicles
            new GearItem
            {
                Id = "m1a2-tank",
                Name = "M1A2 Tank",
                Category = "Vehicles",
                SubCategory = "Ground Vehicles",
                Cost = 250000,
                Stats = new Dictionary<string, int>
                {
                    ["Body"] = 8,
                    ["Armor"] = 6,
                    ["Speed"] = 40,
                    ["Handling"] = 1
                },
                IsLegal = false
            },
            
            // Aircraft - SR3 Air Vehicles
            new GearItem
            {
                Id = "cessna-citation",
                Name = "Cessna Citation",
                Category = "Vehicles",
                SubCategory = "Air Vehicles",
                Cost = 450000,
                Stats = new Dictionary<string, int>
                {
                    ["Body"] = 2,
                    ["Armor"] = 1,
                    ["Speed"] = 300,
                    ["Handling"] = 5
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "f22-raptor",
                Name = "F-22 Raptor",
                Category = "Vehicles",
                SubCategory = "Air Vehicles",
                Cost = 2500000,
                Stats = new Dictionary<string, int>
                {
                    ["Body"] = 5,
                    ["Armor"] = 4,
                    ["Speed"] = 800,
                    ["Handling"] = 6
                },
                IsLegal = false
            },
            
            // Water Vehicles - SR3 Watercraft
            new GearItem
            {
                Id = "sea-doo",
                Name = "Sea-Doo Spark",
                Category = "Vehicles",
                SubCategory = "Water Vehicles",
                Cost = 15000,
                Stats = new Dictionary<string, int>
                {
                    ["Body"] = 1,
                    ["Armor"] = 0,
                    ["Speed"] = 80,
                    ["Handling"] = 3
                },
                IsLegal = true
            },
            new GearItem
            {
                Id = "yacht-45",
                Name = "Yacht 45ft",
                Category = "Vehicles",
                SubCategory = "Water Vehicles",
                Cost = 150000,
                Stats = new Dictionary<string, int>
                {
                    ["Body"] = 4,
                    ["Armor"] = 2,
                    ["Speed"] = 30,
                    ["Handling"] = 1
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
        },

        // SOFTWARE PROGRAMS - All programs available for installation on cyberdecks
        ["Software"] = new List<GearItem>
        {
            // Hacking Programs (Utility type)
            new GearItem
            {
                Id = "program-scan",
                Name = "Scan",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 500,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 1
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-crack",
                Name = "Crack",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 1000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 2
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-probe",
                Name = "Probe",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 1500,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 3
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-clone",
                Name = "Clone",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 2000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 4
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-bypass",
                Name = "Bypass",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 2500,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 5
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-fade",
                Name = "Fade",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 3000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 6
                },
                IsLegal = false
            },
            
            // Combat Programs (Attack type)
            new GearItem
            {
                Id = "program-damage",
                Name = "Damage",
                Category = "Software",
                SubCategory = "Combat",
                Cost = 1000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 2
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-destroy",
                Name = "Destroy",
                Category = "Software",
                SubCategory = "Combat",
                Cost = 2000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 3
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-infect",
                Name = "Infect",
                Category = "Software",
                SubCategory = "Combat",
                Cost = 3000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 4
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-attack",
                Name = "Attack",
                Category = "Software",
                SubCategory = "Combat",
                Cost = 4000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 5
                },
                IsLegal = false
            },
            
            // Defense Programs (Defense type)
            new GearItem
            {
                Id = "program-shield",
                Name = "Shield",
                Category = "Software",
                SubCategory = "Defense",
                Cost = 1000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 2
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-guard",
                Name = "Guard",
                Category = "Software",
                SubCategory = "Defense",
                Cost = 2000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 3
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-protect",
                Name = "Protect",
                Category = "Software",
                SubCategory = "Defense",
                Cost = 3000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 4
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-stasis",
                Name = "Stasis",
                Category = "Software",
                SubCategory = "Defense",
                Cost = 4000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 5
                },
                IsLegal = false
            },
            
            // Utility Programs (Utility type)
            new GearItem
            {
                Id = "program-data-analysis",
                Name = "Data Analysis",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 500,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 1
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-hacking-tools",
                Name = "Hacking Tools",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 1000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 2
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-encryption",
                Name = "Encryption",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 1500,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 3
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-decoy",
                Name = "Decoy",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 2000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 4
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-misc",
                Name = "Miscellaneous",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 2500,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 5
                },
                IsLegal = false
            },
            
            // Special Programs (Special type)
            new GearItem
            {
                Id = "program-stealth",
                Name = "Stealth",
                Category = "Software",
                SubCategory = "Special",
                Cost = 3000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 4
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-infiltration",
                Name = "Infiltration",
                Category = "Software",
                SubCategory = "Special",
                Cost = 4000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 5
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-matrix-armor",
                Name = "Matrix Armor",
                Category = "Software",
                SubCategory = "Special",
                Cost = 5000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 6
                },
                IsLegal = false
            },
            new GearItem
            {
                Id = "program-spyware",
                Name = "Spyware",
                Category = "Software",
                SubCategory = "Special",
                Cost = 6000,
                Stats = new Dictionary<string, int>
                {
                    ["Memory"] = 7
                },
                IsLegal = false
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
