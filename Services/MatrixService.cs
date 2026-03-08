using System;
using System.Collections.Generic;
using System.Linq;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services
{
    /// <summary>
    /// Service for handling Matrix/cyberdeck operations for deckers and netrunners
    /// </summary>
    public class MatrixService
    {
        private readonly Cyberdeck _cyberdeck;
        private readonly MatrixSession _session;
        private readonly DiceService _diceService;

        public MatrixService(Cyberdeck cyberdeck, DiceService diceService, MatrixSession? session = null)
        {
            _cyberdeck = cyberdeck;
            _diceService = diceService;
            _session = session ?? new MatrixSession();
        }

        /// <summary>
        /// Get the current cyberdeck status display
        /// </summary>
        public string GetDeckStatus()
        {
            var status = $"**Cyberdeck Status**\n";
            status += $"Name: {_cyberdeck.Name}\n";
            status += $"Type: {_cyberdeck.DeckType}\n";
            status += $"MPCP Rating: {_cyberdeck.MPCP}\n";
            status += $"Active Memory: {_cyberdeck.ActiveMemory} Mp\n";
            status += $"Storage Memory: {_cyberdeck.StorageMemory} Mp\n";
            status += $"Load Rating: {_cyberdeck.LoadRating}\n";
            status += $"Response Rating: {_cyberdeck.ResponseRating}\n";
            status += $"Hardening: {_cyberdeck.Hardening}\n";
            status += $"Value: {_cyberdeck.Value:N0}¥\n";

            return status;
        }

        /// <summary>
        /// Get the list of installed programs
        /// </summary>
        public string GetProgramsList()
        {
            var programs = _cyberdeck.InstalledPrograms?.ToList() ?? new List<DeckProgram>();
            
            if (programs.Count == 0)
                return "No programs installed on this cyberdeck.";

            var programList = "**Installed Programs:**\n";
            foreach (var program in programs)
            {
                var loadedStatus = program.IsLoaded ? "✅ [Loaded]" : "⬜ [Stored]";
                programList += $"- {program.Name} ({program.Type}) - Rating {program.Rating}, {program.MemoryCost} Mp {loadedStatus}\n";
            }

            var totalMemory = programs.Sum(p => p.MemoryCost);
            var loadedMemory = programs.Where(p => p.IsLoaded).Sum(p => p.MemoryCost);
            programList += $"\n**Memory Usage:** {loadedMemory}/{_cyberdeck.ActiveMemory} Mp active, {totalMemory}/{_cyberdeck.StorageMemory} Mp stored";

            return programList;
        }

        /// <summary>
        /// Get the Matrix session status
        /// </summary>
        public string GetSessionStatus()
        {
            var status = $"**Matrix Session Status**\n";
            status += $"Mode: {(_session.IsInVR ? "VR (Hot Sim)" : "AR")}\n";
            status += $"Security Tally: {_session.SecurityTally}\n";
            status += $"Alert Level: {_session.AlertLevel}\n";
            status += $"Initiative: {_session.CurrentInitiative}\n";
            status += $"Initiative Passes: {_session.InitiativePasses}\n";

            var activeICE = _session.ActiveICE?.Where(i => i.IsActivated).ToList() ?? new List<ActiveICE>();
            status += $"Active ICE: {activeICE.Count}\n";

            return status;
        }

        /// <summary>
        /// Get the list of active ICE
        /// </summary>
        public string GetICEList()
        {
            var iceList = _session.ActiveICE?.ToList() ?? new List<ActiveICE>();

            if (iceList.Count == 0)
                return "No ICE encountered in current session.";

            var result = "**ICE Status:**\n";
            foreach (var ice in iceList)
            {
                var activationStatus = ice.IsActivated ? "🔴 ACTIVE" : "🟡 Standby";
                result += $"- {ice.ICEType} ICE (Rating {ice.Rating}) - {activationStatus}\n";
                if (ice.SecurityTallyThreshold > 0)
                    result += $"  Triggers at Security Tally: {ice.SecurityTallyThreshold}\n";
            }

            return result;
        }

        /// <summary>
        /// Roll Matrix initiative
        /// </summary>
        public string RollMatrixInitiative()
        {
            // Matrix initiative = MPCP + 4D6 (VR) or 2D6 (AR)
            var baseInit = _cyberdeck.MPCP;
            var diceCount = _session.IsInVR ? 4 : 2;

            var result = _diceService.RollInitiative(baseInit, diceCount);

            _session.CurrentInitiative = result.Total;
            _session.InitiativePasses = result.Passes;

            var output = $"**Matrix Initiative Roll**\n";
            output += $"MPCP: {baseInit}\n";
            output += $"Mode: {(_session.IsInVR ? "VR (4D6)" : "AR (2D6)")}\n";
            output += $"Roll: {result.Details}\n";
            output += $"Initiative Passes: {result.Passes}\n";

            return output;
        }

        /// <summary>
        /// Attempt to crack ICE
        /// </summary>
        public string CrackICE(string iceType, int iceRating)
        {
            // Pool = MPCP + Hacking Skill (simplified)
            var pool = _cyberdeck.MPCP + 6; // Assuming base hacking skill of 6

            var result = _diceService.RollShadowrun(pool, 4);

            var output = $"**Cracking ICE: {iceType}**\n";
            output += $"ICE Rating: {iceRating}\n";
            output += $"Attack Pool: {pool}\n";
            output += $"Roll: {result.Details}\n";
            output += $"Successes: {result.Successes}\n";

            if (result.Successes >= iceRating)
            {
                output += "✅ **ICE CRACKED!** Access granted.\n";
                // Reduce security tally on success
                _session.SecurityTally = Math.Max(0, _session.SecurityTally - 1);
            }
            else
            {
                output += "❌ **ICE Resisted!** Access denied.\n";
                // Increase security tally on failure
                _session.SecurityTally += 1;

                // Check for alert escalation
                if (_session.SecurityTally >= 10 && _session.AlertLevel == "None")
                {
                    _session.AlertLevel = "Passive";
                    output += "⚠️ **Alert Level increased to Passive!**\n";
                }
                else if (_session.SecurityTally >= 20 && _session.AlertLevel == "Passive")
                {
                    _session.AlertLevel = "Active";
                    output += "🚨 **Alert Level increased to Active!**\n";
                }
            }

            return output;
        }

        /// <summary>
        /// Attempt to bypass security
        /// </summary>
        public string BypassSecurity(string systemType, int systemRating)
        {
            var pool = _cyberdeck.MPCP + 4; // Simplified pool

            var result = _diceService.RollShadowrun(pool, systemRating);

            var output = $"**Bypassing Security: {systemType}**\n";
            output += $"System Rating: {systemRating}\n";
            output += $"Pool: {pool}\n";
            output += $"Roll: {result.Details}\n";
            output += $"Successes: {result.Successes}\n";

            if (result.Successes > 0)
            {
                output += "✅ **Security Bypassed!** Access granted.\n";
            }
            else
            {
                output += "❌ **Bypass Failed!** Security alerted.\n";
                _session.SecurityTally += 2;
            }

            return output;
        }

        /// <summary>
        /// Attack another icon in the Matrix
        /// </summary>
        public string MatrixAttack(string targetName, int attackProgramRating)
        {
            var pool = _cyberdeck.MPCP + attackProgramRating;

            var result = _diceService.RollShadowrun(pool, 4);

            var output = $"**Matrix Attack: {targetName}**\n";
            output += $"Attack Program Rating: {attackProgramRating}\n";
            output += $"Pool: {pool}\n";
            output += $"Roll: {result.Details}\n";
            output += $"Successes: {result.Successes}\n";

            if (result.Successes > 0)
            {
                var damage = result.Successes * 2; // Simplified damage calculation
                output += $"💥 **Attack Successful!** Dealt {damage} Matrix damage.\n";
            }
            else
            {
                output += "❌ **Attack Failed!** Target defended.\n";
            }

            return output;
        }

        /// <summary>
        /// Defend against Matrix attack
        /// </summary>
        public string MatrixDefense(int incomingDamage)
        {
            var pool = _cyberdeck.Hardening + _cyberdeck.Firewall; // Firewall property simulation

            var result = _diceService.RollShadowrun(pool, 4);

            var output = $"**Matrix Defense**\n";
            output += $"Incoming Damage: {incomingDamage}\n";
            output += $"Defense Pool: {pool}\n";
            output += $"Roll: {result.Details}\n";
            output += $"Successes: {result.Successes}\n";

            var actualDamage = Math.Max(0, incomingDamage - result.Successes);
            output += $"🛡️ **Damage Soaked:** {result.Successes}\n";
            output += $"❤️ **Damage Taken:** {actualDamage}\n";

            return output;
        }

        /// <summary>
        /// Load a program into active memory
        /// </summary>
        public string LoadProgram(string programName)
        {
            var program = _cyberdeck.InstalledPrograms?.FirstOrDefault(p => p.Name.ToLower() == programName.ToLower());

            if (program == null)
                return $"Program '{programName}' not found on this cyberdeck.";

            if (program.IsLoaded)
                return $"Program '{programName}' is already loaded.";

            var currentlyLoaded = _cyberdeck.InstalledPrograms?.Where(p => p.IsLoaded).Sum(p => p.MemoryCost) ?? 0;

            if (currentlyLoaded + program.MemoryCost > _cyberdeck.ActiveMemory)
                return $"Insufficient active memory to load '{programName}'. Need {program.MemoryCost} Mp, have {_cyberdeck.ActiveMemory - currentlyLoaded} Mp available.";

            program.IsLoaded = true;
            return $"✅ Loaded '{program.Name}' into active memory. ({program.MemoryCost} Mp)";
        }

        /// <summary>
        /// Unload a program from active memory
        /// </summary>
        public string UnloadProgram(string programName)
        {
            var program = _cyberdeck.InstalledPrograms?.FirstOrDefault(p => p.Name.ToLower() == programName.ToLower());

            if (program == null)
                return $"Program '{programName}' not found on this cyberdeck.";

            if (!program.IsLoaded)
                return $"Program '{programName}' is not currently loaded.";

            program.IsLoaded = false;
            return $"⬜ Unloaded '{program.Name}' from active memory.";
        }

        /// <summary>
        /// Install a new program
        /// </summary>
        public string InstallProgram(string name, string type, int rating, int memoryCost)
        {
            var totalStorage = _cyberdeck.InstalledPrograms?.Sum(p => p.MemoryCost) ?? 0;

            if (totalStorage + memoryCost > _cyberdeck.StorageMemory)
                return $"Insufficient storage memory. Need {memoryCost} Mp, have {_cyberdeck.StorageMemory - totalStorage} Mp available.";

            var program = new DeckProgram
            {
                CyberdeckId = _cyberdeck.Id,
                Name = name,
                Type = type,
                Rating = rating,
                MemoryCost = memoryCost,
                IsLoaded = false
            };

            _cyberdeck.InstalledPrograms?.Add(program);

            return $"✅ Installed '{name}' ({type}) - Rating {rating}, {memoryCost} Mp";
        }

        /// <summary>
        /// Switch between AR and VR mode
        /// </summary>
        public string ToggleVRMode()
        {
            _session.IsInVR = !_session.IsInVR;

            return _session.IsInVR
                ? "🌐 Switched to VR mode (Hot Sim). +2 Initiative Dice, biofeedback damage possible."
                : "👓 Switched to AR mode. Normal initiative, no biofeedback risk.";
        }

        /// <summary>
        /// Check if the character can perform Matrix operations
        /// </summary>
        public bool CanAccessMatrix()
        {
            return _cyberdeck.MPCP >= 3;
        }
    }

    /// <summary>
    /// Database of common Matrix programs
    /// </summary>
    public static class MatrixProgramDatabase
    {
        public static readonly List<ProgramInfo> Programs = new List<ProgramInfo>
        {
            // Attack Programs
            new ProgramInfo { Name = "Attack", Type = "Attack", BaseRating = 1, MemoryCost = 50, Description = "Standard attack program" },
            new ProgramInfo { Name = "Black Hammer", Type = "Attack", BaseRating = 1, MemoryCost = 100, Description = "Lethal biofeedback attack" },
            new ProgramInfo { Name = "Blaster", Type = "Attack", BaseRating = 1, MemoryCost = 75, Description = "High-damage attack program" },

            // Defense Programs
            new ProgramInfo { Name = "Armor", Type = "Defense", BaseRating = 1, MemoryCost = 25, Description = "Reduces Matrix damage" },
            new ProgramInfo { Name = "Shield", Type = "Defense", BaseRating = 1, MemoryCost = 50, Description = "Active defense program" },
            new ProgramInfo { Name = "Medic", Type = "Defense", BaseRating = 1, MemoryCost = 40, Description = "Repairs deck damage" },

            // Utility Programs
            new ProgramInfo { Name = "Browse", Type = "Utility", BaseRating = 1, MemoryCost = 20, Description = "Search databases" },
            new ProgramInfo { Name = "Decrypt", Type = "Utility", BaseRating = 1, MemoryCost = 60, Description = "Break encryption" },
            new ProgramInfo { Name = "Defuse", Type = "Utility", BaseRating = 1, MemoryCost = 45, Description = "Disarm data bombs" },
            new ProgramInfo { Name = "Read/Write", Type = "Utility", BaseRating = 1, MemoryCost = 15, Description = "File manipulation" },
            new ProgramInfo { Name = "Sleaze", Type = "Utility", BaseRating = 1, MemoryCost = 50, Description = "Stealth operations" },
            new ProgramInfo { Name = "Track", Type = "Utility", BaseRating = 1, MemoryCost = 35, Description = "Trace icons in Matrix" },

            // Special Programs
            new ProgramInfo { Name = "Decompiler", Type = "Special", BaseRating = 1, MemoryCost = 80, Description = "Analyze and break programs" },
            new ProgramInfo { Name = "Scanner", Type = "Special", BaseRating = 1, MemoryCost = 55, Description = "Detect hidden nodes" },
            new ProgramInfo { Name = "Validate", Type = "Special", BaseRating = 1, MemoryCost = 30, Description = "Verify authentication" }
        };

        public static ProgramInfo? GetProgram(string name)
        {
            return Programs.FirstOrDefault(p => p.Name.ToLower() == name.ToLower());
        }
    }

    /// <summary>
    /// Information about a Matrix program
    /// </summary>
    public class ProgramInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int BaseRating { get; set; } = 1;
        public int MemoryCost { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
    }
}
