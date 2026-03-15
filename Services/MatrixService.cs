using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine("**Cyberdeck Status**");
            sb.AppendLine($"Name: {_cyberdeck.Name}");
            sb.AppendLine($"Type: {_cyberdeck.DeckType}");
            sb.AppendLine($"MPCP Rating: {_cyberdeck.MPCP}");
            sb.AppendLine($"Active Memory: {_cyberdeck.ActiveMemory} Mp");
            sb.AppendLine($"Storage Memory: {_cyberdeck.StorageMemory} Mp");
            sb.AppendLine($"Load Rating: {_cyberdeck.LoadRating}");
            sb.AppendLine($"Response Rating: {_cyberdeck.ResponseRating}");
            sb.AppendLine($"Hardening: {_cyberdeck.Hardening}");
            sb.AppendLine($"Value: {_cyberdeck.Value:N0}¥");

            return sb.ToString();
        }

        /// <summary>
        /// Get the list of installed programs
        /// </summary>
        public string GetProgramsList()
        {
            var programs = _cyberdeck.InstalledPrograms?.ToList() ?? new List<DeckProgram>();
            
            if (programs.Count == 0)
                return "No programs installed on this cyberdeck.";

            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine("**Installed Programs:**");
            foreach (var program in programs)
            {
                var loadedStatus = program.IsLoaded ? "✅ [Loaded]" : "⬜ [Stored]";
                sb.AppendLine($"- {program.Name} ({program.Type}) - Rating {program.Rating}, {program.MemoryCost} Mp {loadedStatus}");
            }

            var totalMemory = programs.Sum(p => p.MemoryCost);
            var loadedMemory = programs.Where(p => p.IsLoaded).Sum(p => p.MemoryCost);
            sb.AppendLine();
            sb.Append($"**Memory Usage:** {loadedMemory}/{_cyberdeck.ActiveMemory} Mp active, {totalMemory}/{_cyberdeck.StorageMemory} Mp stored");

            return sb.ToString();
        }

        /// <summary>
        /// Get the Matrix session status
        /// </summary>
        public string GetSessionStatus()
        {
            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine("**Matrix Session Status**");
            sb.AppendLine($"Mode: {(_session.IsInVR ? "VR (Hot Sim)" : "AR")}");
            sb.AppendLine($"Security Tally: {_session.SecurityTally}");
            sb.AppendLine($"Alert Level: {_session.AlertLevel}");
            sb.AppendLine($"Initiative: {_session.CurrentInitiative}");
            sb.AppendLine($"Initiative Passes: {_session.InitiativePasses}");

            var activeICE = _session.ActiveICE?.Where(i => i.IsActivated).ToList() ?? new List<ActiveICE>();
            sb.AppendLine($"Active ICE: {activeICE.Count}");

            return sb.ToString();
        }

        /// <summary>
        /// Get the list of active ICE
        /// </summary>
        public string GetICEList()
        {
            var iceList = _session.ActiveICE?.ToList() ?? new List<ActiveICE>();

            if (iceList.Count == 0)
                return "No ICE encountered in current session.";

            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine("**ICE Status:**");
            foreach (var ice in iceList)
            {
                var activationStatus = ice.IsActivated ? "🔴 ACTIVE" : "🟡 Standby";
                sb.AppendLine($"- {ice.ICEType} ICE (Rating {ice.Rating}) - {activationStatus}");
                if (ice.SecurityTallyThreshold > 0)
                    sb.AppendLine($"  Triggers at Security Tally: {ice.SecurityTallyThreshold}");
            }

            return sb.ToString();
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

            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine("**Matrix Initiative Roll**");
            sb.AppendLine($"MPCP: {baseInit}");
            sb.AppendLine($"Mode: {(_session.IsInVR ? "VR (4D6)" : "AR (2D6)")}");
            sb.AppendLine($"Roll: {result.Details}");
            sb.AppendLine($"Initiative Passes: {result.Passes}");

            return sb.ToString();
        }

        /// <summary>
        /// Attempt to crack ICE
        /// </summary>
        public string CrackICE(string iceType, int iceRating)
        {
            // Pool = MPCP + Hacking Skill (simplified)
            var pool = _cyberdeck.MPCP + 6; // Assuming base hacking skill of 6

            var result = _diceService.RollShadowrun(pool, 4);

            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine($"**Cracking ICE: {iceType}**");
            sb.AppendLine($"ICE Rating: {iceRating}");
            sb.AppendLine($"Attack Pool: {pool}");
            sb.AppendLine($"Roll: {result.Details}");
            sb.AppendLine($"Successes: {result.Successes}");

            if (result.Successes >= iceRating)
            {
                sb.AppendLine("✅ **ICE CRACKED!** Access granted.");
                // Reduce security tally on success
                _session.SecurityTally = Math.Max(0, _session.SecurityTally - 1);
            }
            else
            {
                sb.AppendLine("❌ **ICE Resisted!** Access denied.");
                // Increase security tally on failure
                _session.SecurityTally += 1;

                // Check for alert escalation
                if (_session.SecurityTally >= 10 && _session.AlertLevel == "None")
                {
                    _session.AlertLevel = "Passive";
                    sb.AppendLine("⚠️ **Alert Level increased to Passive!**");
                }
                else if (_session.SecurityTally >= 20 && _session.AlertLevel == "Passive")
                {
                    _session.AlertLevel = "Active";
                    sb.AppendLine("🚨 **Alert Level increased to Active!**");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Attempt to bypass security
        /// </summary>
        public string BypassSecurity(string systemType, int systemRating)
        {
            var pool = _cyberdeck.MPCP + 4; // Simplified pool

            var result = _diceService.RollShadowrun(pool, systemRating);

            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine($"**Bypassing Security: {systemType}**");
            sb.AppendLine($"System Rating: {systemRating}");
            sb.AppendLine($"Pool: {pool}");
            sb.AppendLine($"Roll: {result.Details}");
            sb.AppendLine($"Successes: {result.Successes}");

            if (result.Successes > 0)
            {
                sb.AppendLine("✅ **Security Bypassed!** Access granted.");
            }
            else
            {
                sb.AppendLine("❌ **Bypass Failed!** Security alerted.");
                _session.SecurityTally += 2;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Attack another icon in the Matrix
        /// </summary>
        public string MatrixAttack(string targetName, int attackProgramRating)
        {
            var pool = _cyberdeck.MPCP + attackProgramRating;

            var result = _diceService.RollShadowrun(pool, 4);

            // FIX: HIGH-002 - Use StringBuilder instead of string concatenation
            var sb = new StringBuilder();
            sb.AppendLine($"**Matrix Attack: {targetName}**");
            sb.AppendLine($"Attack Program Rating: {attackProgramRating}");
            sb.AppendLine($"Pool: {pool}");
            sb.AppendLine($"Roll: {result.Details}");
            sb.AppendLine($"Successes: {result.Successes}");

            if (result.Successes > 0)
            {
                var damage = result.Successes * 2; // Simplified damage calculation
                sb.AppendLine($"💥 **Attack Successful!** Dealt {damage} Matrix damage.");
            }
            else
            {
                sb.AppendLine("❌ **Attack Failed!** Target defended.");
            }

            return sb.ToString();
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
    }

    /// <summary>
    /// API-focused Matrix service for managing matrix runs via database
    /// </summary>
    public class MatrixApiService
    {
        private readonly DatabaseService _databaseService;
        private readonly DiceService _diceService;
        private readonly ILogger<MatrixApiService> _logger;

        public MatrixApiService(
            DatabaseService databaseService,
            DiceService diceService,
            ILogger<MatrixApiService> logger)
        {
            _databaseService = databaseService;
            _diceService = diceService;
            _logger = logger;
        }

        #region Matrix Run Operations

        /// <summary>
        /// Get active matrix run for a character
        /// </summary>
        public async Task<MatrixRunDto?> GetActiveRunAsync(int characterId)
        {
            var run = await _databaseService.GetActiveMatrixRunAsync(characterId);
            return run != null ? MapToMatrixRunDto(run) : null;
        }

        /// <summary>
        /// Create a new matrix run
        /// </summary>
        public async Task<MatrixRunDto> CreateRunAsync(CreateMatrixRunDto createDto)
        {
            var run = new MatrixRun
            {
                CharacterId = createDto.CharacterId,
                HostId = createDto.HostId,
                CyberdeckId = createDto.CyberdeckId,
                SecurityTally = 0,
                AlertStatus = "None",
                StartedAt = DateTime.UtcNow
            };

            var created = await _databaseService.CreateMatrixRunAsync(run);
            _logger.LogInformation("Created matrix run {RunId} for character {CharacterId}",
                created.Id, createDto.CharacterId);

            return MapToMatrixRunDto(created);
        }

        /// <summary>
        /// Update a matrix run
        /// </summary>
        public async Task<MatrixRunDto?> UpdateRunAsync(int runId, UpdateMatrixRunDto updateDto)
        {
            var run = await _databaseService.GetMatrixRunAsync(runId);
            if (run == null) return null;

            if (updateDto.SecurityTally.HasValue) run.SecurityTally = updateDto.SecurityTally.Value;
            if (updateDto.AlertStatus != null) run.AlertStatus = updateDto.AlertStatus;

            var updated = await _databaseService.UpdateMatrixRunAsync(run);
            return MapToMatrixRunDto(updated);
        }

        /// <summary>
        /// End a matrix run
        /// </summary>
        public async Task<bool> EndRunAsync(int runId)
        {
            var run = await _databaseService.GetMatrixRunAsync(runId);
            if (run == null) return false;

            run.EndedAt = DateTime.UtcNow;
            await _databaseService.UpdateMatrixRunAsync(run);
            
            _logger.LogInformation("Ended matrix run {RunId}", runId);
            return true;
        }

        #endregion

        #region ICE Operations

        /// <summary>
        /// Get ICE encounters for a matrix run
        /// </summary>
        public async Task<List<ICEDto>> GetICEAsync(int runId)
        {
            var encounters = await _databaseService.GetICEncountersAsync(runId);
            return encounters.Select(MapToICEDto).ToList();
        }

        /// <summary>
        /// Add ICE encounter to a matrix run
        /// </summary>
        public async Task<ICEDto> AddICEAsync(int runId, CreateICEDto createDto)
        {
            var encounter = new ActiveICEncounter
            {
                MatrixRunId = runId,
                HostICEId = createDto.HostICEId,
                IsDefeated = false,
                DamageToDeck = 0,
                DamageToCharacter = 0
            };

            var created = await _databaseService.AddICEncounterAsync(encounter);
            _logger.LogInformation("Added ICE encounter {EncounterId} to run {RunId}",
                created.Id, runId);

            return MapToICEDto(created);
        }

        /// <summary>
        /// Remove ICE encounter from a matrix run
        /// </summary>
        public async Task<bool> RemoveICEAsync(int runId, int encounterId)
        {
            var encounter = await _databaseService.GetICEncounterAsync(encounterId);
            if (encounter == null || encounter.MatrixRunId != runId) return false;

            await _databaseService.RemoveICEncounterAsync(encounter);
            _logger.LogInformation("Removed ICE encounter {EncounterId} from run {RunId}",
                encounterId, runId);

            return true;
        }

        #endregion

        #region Cyberdeck Operations

        /// <summary>
        /// Get cyberdeck by ID
        /// </summary>
        public async Task<CyberdeckDto?> GetDeckAsync(int deckId)
        {
            var deck = await _databaseService.GetCyberdeckAsync(deckId);
            return deck != null ? MapToCyberdeckDto(deck) : null;
        }

        /// <summary>
        /// Create a new cyberdeck
        /// </summary>
        public async Task<CyberdeckDto> CreateDeckAsync(CreateCyberdeckDto createDto)
        {
            var deck = new Cyberdeck
            {
                CharacterId = createDto.CharacterId,
                Name = createDto.Name,
                DeckType = createDto.DeckType ?? "Custom",
                MPCP = createDto.MPCP,
                ActiveMemory = createDto.ActiveMemory,
                StorageMemory = createDto.StorageMemory,
                LoadRating = createDto.LoadRating,
                ResponseRating = createDto.ResponseRating,
                Hardening = createDto.Hardening,
                Value = createDto.Value
            };

            var created = await _databaseService.CreateCyberdeckAsync(deck);
            _logger.LogInformation("Created cyberdeck {DeckId} for character {CharacterId}",
                created.Id, createDto.CharacterId);

            return MapToCyberdeckDto(created);
        }

        /// <summary>
        /// Update a cyberdeck
        /// </summary>
        public async Task<CyberdeckDto?> UpdateDeckAsync(int deckId, UpdateCyberdeckDto updateDto)
        {
            var deck = await _databaseService.GetCyberdeckAsync(deckId);
            if (deck == null) return null;

            if (updateDto.Name != null) deck.Name = updateDto.Name;
            if (updateDto.MPCP.HasValue) deck.MPCP = updateDto.MPCP.Value;
            if (updateDto.ActiveMemory.HasValue) deck.ActiveMemory = updateDto.ActiveMemory.Value;
            if (updateDto.StorageMemory.HasValue) deck.StorageMemory = updateDto.StorageMemory.Value;
            if (updateDto.LoadRating.HasValue) deck.LoadRating = updateDto.LoadRating.Value;
            if (updateDto.ResponseRating.HasValue) deck.ResponseRating = updateDto.ResponseRating.Value;
            if (updateDto.Hardening.HasValue) deck.Hardening = updateDto.Hardening.Value;
            if (updateDto.Value.HasValue) deck.Value = updateDto.Value.Value;

            var updated = await _databaseService.UpdateCyberdeckAsync(deck);
            return MapToCyberdeckDto(updated);
        }

        #endregion

        #region Private Mapping Methods

        private MatrixRunDto MapToMatrixRunDto(MatrixRun run)
        {
            return new MatrixRunDto
            {
                Id = run.Id,
                CharacterId = run.CharacterId,
                HostId = run.HostId,
                CyberdeckId = run.CyberdeckId,
                SecurityTally = run.SecurityTally,
                AlertStatus = run.AlertStatus,
                PassiveThreshold = run.PassiveThreshold,
                ActiveThreshold = run.ActiveThreshold,
                ShutdownThreshold = run.ShutdownThreshold,
                StartedAt = run.StartedAt,
                EndedAt = run.EndedAt
            };
        }

        private ICEDto MapToICEDto(ActiveICEncounter encounter)
        {
            return new ICEDto
            {
                Id = encounter.Id,
                MatrixRunId = encounter.MatrixRunId,
                HostICEId = encounter.HostICEId,
                IsDefeated = encounter.IsDefeated,
                DamageToDeck = encounter.DamageToDeck,
                DamageToCharacter = encounter.DamageToCharacter,
                EncounterLog = encounter.EncounterLog
            };
        }

        private CyberdeckDto MapToCyberdeckDto(Cyberdeck deck)
        {
            return new CyberdeckDto
            {
                Id = deck.Id,
                CharacterId = deck.CharacterId,
                Name = deck.Name,
                DeckType = deck.DeckType,
                MPCP = deck.MPCP,
                ActiveMemory = deck.ActiveMemory,
                StorageMemory = deck.StorageMemory,
                LoadRating = deck.LoadRating,
                ResponseRating = deck.ResponseRating,
                Hardening = deck.Hardening,
                Value = deck.Value
            };
        }

        #endregion
    }

    #region Matrix DTOs

    public class MatrixRunDto
    {
        public int Id { get; set; }
        public int CharacterId { get; set; }
        public int HostId { get; set; }
        public int CyberdeckId { get; set; }
        public int SecurityTally { get; set; }
        public string AlertStatus { get; set; } = "None";
        public int PassiveThreshold { get; set; }
        public int ActiveThreshold { get; set; }
        public int ShutdownThreshold { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }

    public class CreateMatrixRunDto
    {
        public int CharacterId { get; set; }
        public int HostId { get; set; }
        public int CyberdeckId { get; set; }
    }

    public class UpdateMatrixRunDto
    {
        public int? SecurityTally { get; set; }
        public string? AlertStatus { get; set; }
    }

    public class ICEDto
    {
        public int Id { get; set; }
        public int MatrixRunId { get; set; }
        public int HostICEId { get; set; }
        public bool IsDefeated { get; set; }
        public int DamageToDeck { get; set; }
        public int DamageToCharacter { get; set; }
        public string? EncounterLog { get; set; }
    }

    public class CreateICEDto
    {
        public int HostICEId { get; set; }
    }

    public class CyberdeckDto
    {
        public int Id { get; set; }
        public int CharacterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DeckType { get; set; } = string.Empty;
        public int MPCP { get; set; }
        public int ActiveMemory { get; set; }
        public int StorageMemory { get; set; }
        public int LoadRating { get; set; }
        public int ResponseRating { get; set; }
        public int Hardening { get; set; }
        public int Value { get; set; }
    }

    public class CreateCyberdeckDto
    {
        public int CharacterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DeckType { get; set; }
        public int MPCP { get; set; }
        public int ActiveMemory { get; set; }
        public int StorageMemory { get; set; }
        public int LoadRating { get; set; }
        public int ResponseRating { get; set; }
        public int Hardening { get; set; }
        public int Value { get; set; }
    }

    public class UpdateCyberdeckDto
    {
        public string? Name { get; set; }
        public int? MPCP { get; set; }
        public int? ActiveMemory { get; set; }
        public int? StorageMemory { get; set; }
        public int? LoadRating { get; set; }
        public int? ResponseRating { get; set; }
        public int? Hardening { get; set; }
        public int? Value { get; set; }
    }

    #endregion

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
