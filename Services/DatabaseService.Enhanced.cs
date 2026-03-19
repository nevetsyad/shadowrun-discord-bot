using Microsoft.EntityFrameworkCore;
using ShadowrunDiscordBot.Models;
using ShadowrunDiscordBot.Mappers;
using ShadowrunDiscordBot.Domain.Entities;
using Vehicle = ShadowrunDiscordBot.Domain.Entities.Vehicle;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Database service extensions for enhanced systems
/// </summary>
public partial class DatabaseService
{
    #region Astral Operations

    public async Task AddAstralStateAsync(AstralCombatState state)
    {
        _context.AstralCombatStates.Add(state);
        await _context.SaveChangesAsync();
    }

    public async Task<AstralCombatState?> GetAstralStateAsync(int characterId)
    {
        return await _context.AstralCombatStates
            .FirstOrDefaultAsync(a => a.CharacterId == characterId);
    }

    public async Task UpdateAstralStateAsync(AstralCombatState state)
    {
        _context.AstralCombatStates.Update(state);
        await _context.SaveChangesAsync();
    }

    public async Task AddAstralSignatureAsync(AstralSignatureRecord signature)
    {
        _context.AstralSignatureRecords.Add(signature);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AstralSignatureRecord>> GetAstralSignaturesAtLocationAsync(string location)
    {
        return await _context.AstralSignatureRecords
            .Where(s => s.Location == location && s.DetectedAt.AddHours(s.HoursToFade) > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task AddFocusAsync(CharacterFocus focus)
    {
        _context.CharacterFoci.Add(focus);
        await _context.SaveChangesAsync();
    }

    public async Task<CharacterFocus?> GetFocusAsync(int focusId)
    {
        return await _context.CharacterFoci.FindAsync(focusId);
    }

    public async Task UpdateFocusAsync(CharacterFocus focus)
    {
        _context.CharacterFoci.Update(focus);
        await _context.SaveChangesAsync();
    }

    public async Task<List<CharacterFocus>> GetCharacterFociAsync(int characterId)
    {
        return await _context.CharacterFoci
            .Where(f => f.CharacterId == characterId)
            .ToListAsync();
    }

    #endregion

    #region Matrix Host Operations

    public async Task AddMatrixHostAsync(MatrixHost host)
    {
        _context.MatrixHosts.Add(host);
        await _context.SaveChangesAsync();
    }

    public async Task<MatrixHost?> GetMatrixHostAsync(int hostId)
    {
        return await _context.MatrixHosts
            .Include(h => h.InstalledICE)
            .FirstOrDefaultAsync(h => h.Id == hostId);
    }

    public async Task AddHostICEAsync(HostICE ice)
    {
        _context.HostICE.Add(ice);
        await _context.SaveChangesAsync();
    }

    public async Task<List<HostICE>> GetHostICEAsync(int hostId)
    {
        return await _context.HostICE
            .Where(i => i.HostId == hostId)
            .ToListAsync();
    }

    public async Task<HostICE?> GetHostICEByIdAsync(int iceId)
    {
        return await _context.HostICE.FindAsync(iceId);
    }

    public async Task UpdateHostICEAsync(List<HostICE> ice)
    {
        _context.HostICE.UpdateRange(ice);
        await _context.SaveChangesAsync();
    }

    // Matrix operations (GetMatrixRunAsync, UpdateMatrixRunAsync, AddICEncounterAsync, GetCyberdeckAsync)
    // are defined in DatabaseService.cs - removed duplicates from this file

    #endregion

    #region Combat Pool Operations

    public async Task AddCombatPoolStateAsync(Domain.Entities.CombatPoolState state)
    {
        var model = EnhancedSystemsMapper.ToModel(state);
        _context.CombatPoolStates.Add(model);
        await _context.SaveChangesAsync();
    }

    public async Task<Domain.Entities.CombatPoolState?> GetCombatPoolStateAsync(int poolStateId)
    {
        var model = await _context.CombatPoolStates.FindAsync(poolStateId);
        return model != null ? EnhancedSystemsMapper.ToDomain(model) : null;
    }

    public async Task<Domain.Entities.CombatPoolState?> GetCombatPoolStateForCharacterAsync(int characterId, int combatSessionId)
    {
        var model = await _context.CombatPoolStates
            .FirstOrDefaultAsync(p => p.CharacterId == characterId && p.CombatSessionId == combatSessionId);
        return model != null ? EnhancedSystemsMapper.ToDomain(model) : null;
    }

    public async Task UpdateCombatPoolStateAsync(Domain.Entities.CombatPoolState state)
    {
        var model = EnhancedSystemsMapper.ToModel(state);
        _context.CombatPoolStates.Update(model);
        await _context.SaveChangesAsync();
    }

    public async Task AddCombatPoolUsageAsync(CombatPoolUsage usage)
    {
        _context.CombatPoolUsages.Add(usage);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Vehicle Operations

    public async Task AddVehicleAsync(Vehicle vehicle)
    {
        var model = EnhancedSystemsMapper.ToModel(vehicle);
        _context.Vehicles.Add(model);
        await _context.SaveChangesAsync();
    }

    public async Task<Vehicle?> GetVehicleAsync(int vehicleId)
    {
        var model = await _context.Vehicles
            .Include(v => v.Weapons)
            .FirstOrDefaultAsync(v => v.Id == vehicleId);
        return model != null ? EnhancedSystemsMapper.ToDomain(model) : null;
    }

    public async Task UpdateVehicleAsync(Vehicle vehicle)
    {
        var model = EnhancedSystemsMapper.ToModel(vehicle);
        _context.Vehicles.Update(model);
        await _context.SaveChangesAsync();
    }

    public async Task AddVehicleCombatSessionAsync(VehicleCombatSession session)
    {
        _context.VehicleCombatSessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public async Task<VehicleCombatSession?> GetVehicleCombatSessionAsync(int combatSessionId)
    {
        return await _context.VehicleCombatSessions
            .Include(v => v.VehicleCombatants)
            .FirstOrDefaultAsync(v => v.CombatSessionId == combatSessionId);
    }

    public async Task AddVehicleCombatantAsync(VehicleCombatant combatant)
    {
        _context.VehicleCombatants.Add(combatant);
        await _context.SaveChangesAsync();
    }

    public async Task AddDroneAsync(Domain.Entities.Drone drone)
    {
        var model = EnhancedSystemsMapper.ToModel(drone);
        _context.Drones.Add(model);
        await _context.SaveChangesAsync();
    }

    public async Task<Domain.Entities.Drone?> GetDroneAsync(int droneId)
    {
        var model = await _context.Drones
            .Include(d => d.Autosofts)
            .Include(d => d.Weapons)
            .FirstOrDefaultAsync(d => d.Id == droneId);
        return model != null ? EnhancedSystemsMapper.ToDomain(model) : null;
    }

    public async Task UpdateDroneAsync(Domain.Entities.Drone drone)
    {
        var model = EnhancedSystemsMapper.ToModel(drone);
        _context.Drones.Update(model);
        await _context.SaveChangesAsync();
    }

    public async Task AddDroneAutosoftAsync(DroneAutosoft autosoft)
    {
        _context.DroneAutosofts.Add(autosoft);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Vehicle>> GetCharacterVehiclesAsync(int characterId)
    {
        var models = await _context.Vehicles
            .Where(v => v.CharacterId == characterId)
            .Include(v => v.Weapons)
            .ToListAsync();
        return models.Select(m => EnhancedSystemsMapper.ToDomain(m)).ToList();
    }

    #endregion

    #region Contact Operations

    public async Task AddContactAsync(CharacterContact contact)
    {
        _context.CharacterContacts.Add(contact);
        await _context.SaveChangesAsync();
    }

    public async Task<CharacterContact?> GetContactAsync(int contactId)
    {
        return await _context.CharacterContacts.FindAsync(contactId);
    }

    public async Task<List<CharacterContact>> GetContactsAsync(int characterId)
    {
        return await _context.CharacterContacts
            .Where(c => c.CharacterId == characterId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task UpdateContactAsync(CharacterContact contact)
    {
        _context.CharacterContacts.Update(contact);
        await _context.SaveChangesAsync();
    }

    public async Task AddLegworkAttemptAsync(LegworkAttempt attempt)
    {
        _context.LegworkAttempts.Add(attempt);
        await _context.SaveChangesAsync();
    }

    public async Task AddJohnsonMeetingAsync(JohnsonMeeting meeting)
    {
        _context.JohnsonMeetings.Add(meeting);
        await _context.SaveChangesAsync();
    }

    public async Task<JohnsonMeeting?> GetJohnsonMeetingAsync(int meetingId)
    {
        return await _context.JohnsonMeetings.FindAsync(meetingId);
    }

    public async Task UpdateJohnsonMeetingAsync(JohnsonMeeting meeting)
    {
        _context.JohnsonMeetings.Update(meeting);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Karma Operations

    public async Task AddKarmaRecordAsync(KarmaRecord record)
    {
        _context.KarmaRecords.Add(record);
        await _context.SaveChangesAsync();
    }

    public async Task<KarmaRecord?> GetLatestKarmaRecordAsync(int characterId)
    {
        return await _context.KarmaRecords
            .Where(k => k.CharacterId == characterId)
            .OrderByDescending(k => k.RecordedAt)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateKarmaRecordAsync(KarmaRecord record)
    {
        _context.KarmaRecords.Update(record);
        await _context.SaveChangesAsync();
    }

    public async Task AddKarmaExpenditureAsync(KarmaExpenditure expenditure)
    {
        _context.KarmaExpenditures.Add(expenditure);
        await _context.SaveChangesAsync();
    }

    public async Task<List<KarmaRecord>> GetKarmaHistoryAsync(int characterId, int limit = 20)
    {
        return await _context.KarmaRecords
            .Where(k => k.CharacterId == characterId)
            .OrderByDescending(k => k.RecordedAt)
            .Take(limit)
            .ToListAsync();
    }

    #endregion

    #region Damage/Healing Operations

    public async Task AddDamageRecordAsync(DamageRecord record)
    {
        _context.DamageRecords.Add(record);
        await _context.SaveChangesAsync();
    }

    public async Task AddHealingAttemptAsync(HealingAttempt attempt)
    {
        _context.HealingAttempts.Add(attempt);
        await _context.SaveChangesAsync();
    }

    public async Task AddHealingTimeRecordAsync(HealingTimeRecord record)
    {
        _context.HealingTimeRecords.Add(record);
        await _context.SaveChangesAsync();
    }

    public async Task<List<HealingTimeRecord>> GetActiveHealingRecordsAsync(int characterId)
    {
        return await _context.HealingTimeRecords
            .Where(h => h.CharacterId == characterId && h.HealingComplete > DateTime.UtcNow)
            .ToListAsync();
    }

    #endregion

    #region Spell Operations

    public async Task AddCharacterSpellAsync(CharacterSpell spell)
    {
        _context.CharacterSpells.Add(spell);
        await _context.SaveChangesAsync();
    }

    #endregion
}
