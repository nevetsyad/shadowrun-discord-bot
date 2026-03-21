using System.Collections.Generic;

namespace ShadowrunDiscordBot.Domain.Entities;

/// <summary>
/// Complete list of all cyberdeck programs available in Shadowrun 3rd Edition
/// </summary>
public static class CyberdeckPrograms
{
    public class ProgramItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public long Cost { get; set; } // In nuyen
        public int MemoryCost { get; set; } // In Megapoints (Mp)
        public bool IsLegal { get; set; } = true;
    }

    public static readonly Dictionary<string, List<ProgramItem>> AllPrograms = new()
    {
        // Hacking Programs (Hacking category)
        ["Hacking"] = new List<ProgramItem>
        {
            new ProgramItem
            {
                Id = "scan",
                Name = "Scan",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 500,
                MemoryCost = 1,
                IsLegal = true
            },
            new ProgramItem
            {
                Id = "crack",
                Name = "Crack",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 1000,
                MemoryCost = 2,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "probe",
                Name = "Probe",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 1500,
                MemoryCost = 3,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "clone",
                Name = "Clone",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 2000,
                MemoryCost = 4,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "bypass",
                Name = "Bypass",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 2500,
                MemoryCost = 5,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "fade",
                Name = "Fade",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 3000,
                MemoryCost = 6,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "hack",
                Name = "Hack",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 4000,
                MemoryCost = 7,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "stealth",
                Name = "Stealth",
                Category = "Software",
                SubCategory = "Hacking",
                Cost = 5000,
                MemoryCost = 8,
                IsLegal = false
            }
        },

        // Combat Programs (Combat category)
        ["Combat"] = new List<ProgramItem>
        {
            new ProgramItem
            {
                Id = "damage",
                Name = "Damage",
                Category = "Software",
                SubCategory = "Combat",
                Cost = 1000,
                MemoryCost = 2,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "destroy",
                Name = "Destroy",
                Category = "Software",
                SubCategory = "Combat",
                Cost = 2000,
                MemoryCost = 3,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "infect",
                Name = "Infect",
                Category = "Software",
                SubCategory = "Combat",
                Cost = 3000,
                MemoryCost = 4,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "attack",
                Name = "Attack",
                Category = "Software",
                SubCategory = "Combat",
                Cost = 4000,
                MemoryCost = 5,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "shield",
                Name = "Shield",
                Category = "Software",
                SubCategory = "Combat",
                Cost = 5000,
                MemoryCost = 6,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "guard",
                Name = "Guard",
                Category = "Software",
                SubCategory = "Combat",
                Cost = 6000,
                MemoryCost = 7,
                IsLegal = false
            }
        },

        // Defense Programs (Defense category)
        ["Defense"] = new List<ProgramItem>
        {
            new ProgramItem
            {
                Id = "shield",
                Name = "Shield",
                Category = "Software",
                SubCategory = "Defense",
                Cost = 1000,
                MemoryCost = 2,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "guard",
                Name = "Guard",
                Category = "Software",
                SubCategory = "Defense",
                Cost = 2000,
                MemoryCost = 3,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "protect",
                Name = "Protect",
                Category = "Software",
                SubCategory = "Defense",
                Cost = 3000,
                MemoryCost = 4,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "stasis",
                Name = "Stasis",
                Category = "Software",
                SubCategory = "Defense",
                Cost = 4000,
                MemoryCost = 5,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "matrix-armor",
                Name = "Matrix Armor",
                Category = "Software",
                SubCategory = "Defense",
                Cost = 5000,
                MemoryCost = 6,
                IsLegal = false
            }
        },

        // Utility Programs (Utility category)
        ["Utility"] = new List<ProgramItem>
        {
            new ProgramItem
            {
                Id = "data-analysis",
                Name = "Data Analysis",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 500,
                MemoryCost = 1,
                IsLegal = true
            },
            new ProgramItem
            {
                Id = "hacking-tools",
                Name = "Hacking Tools",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 1000,
                MemoryCost = 2,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "encryption",
                Name = "Encryption",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 1500,
                MemoryCost = 3,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "decoy",
                Name = "Decoy",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 2000,
                MemoryCost = 4,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "misc",
                Name = "Miscellaneous",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 2500,
                MemoryCost = 5,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "communication",
                Name = "Communication",
                Category = "Software",
                SubCategory = "Utility",
                Cost = 3000,
                MemoryCost = 6,
                IsLegal = false
            }
        },

        // Special Programs (Special category)
        ["Special"] = new List<ProgramItem>
        {
            new ProgramItem
            {
                Id = "stealth",
                Name = "Stealth",
                Category = "Software",
                SubCategory = "Special",
                Cost = 3000,
                MemoryCost = 4,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "infiltration",
                Name = "Infiltration",
                Category = "Software",
                SubCategory = "Special",
                Cost = 4000,
                MemoryCost = 5,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "matrix-armor",
                Name = "Matrix Armor",
                Category = "Software",
                SubCategory = "Special",
                Cost = 5000,
                MemoryCost = 6,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "spyware",
                Name = "Spyware",
                Category = "Software",
                SubCategory = "Special",
                Cost = 6000,
                MemoryCost = 7,
                IsLegal = false
            },
            new ProgramItem
            {
                Id = "matrix-hack",
                Name = "Matrix Hack",
                Category = "Software",
                SubCategory = "Special",
                Cost = 7000,
                MemoryCost = 8,
                IsLegal = false
            }
        }
    };

    /// <summary>
    /// Get programs by category
    /// </summary>
    public static List<ProgramItem> GetProgramsByCategory(string category)
    {
        return AllPrograms.TryGetValue(category, out var programs) ? programs : new List<ProgramItem>();
    }

    /// <summary>
    /// Get program by ID
    /// </summary>
    public static ProgramItem? GetProgramById(string id)
    {
        foreach (var category in AllPrograms.Values)
        {
            var program = category.FirstOrDefault(p => p.Id == id);
            if (program != null)
                return program;
        }
        return null;
    }

    /// <summary>
    /// Get all categories of programs
    /// </summary>
    public static List<string> GetAllCategories()
    {
        return AllPrograms.Keys.ToList();
    }
}