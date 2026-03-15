using ShadowrunDiscordBot.Domain.Entities;
using ShadowrunDiscordBot.Models;
using System.Text.Json;
using CharacterSkill = ShadowrunDiscordBot.Models.CharacterSkill;
using CharacterCyberware = ShadowrunDiscordBot.Models.CharacterCyberware;
using CharacterGear = ShadowrunDiscordBot.Models.CharacterGear;

namespace ShadowrunDiscordBot.Application.Services;

/// <summary>
/// SR3 Gear Selection Service
/// Provides gear selection by category, archetype pre-loads, and priority-based gear
/// </summary>
public class GearSelectionService : IGearSelectionService
{
    /// <summary>
    /// Get all available gear categories
    /// </summary>
    public List<string> GetGearCategories()
    {
        return GearDatabase.GetAllCategories();
    }

    /// <summary>
    /// Get gear items by category (for section-by-section selection)
    /// </summary>
    public async Task<List<GearDatabase.GearItem>> GetGearByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return GearDatabase.GetGearByCategory(category);
    }

    /// <summary>
    /// Get specific gear item by ID
    /// </summary>
    public async Task<GearDatabase.GearItem?> GetGearByIdAsync(
        string gearId,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return GearDatabase.GetGearById(gearId);
    }

    /// <summary>
    /// Get pre-loaded gear package for archetype (SR3 starting gear)
    /// </summary>
    public async Task<List<GearDatabase.GearItem>> GetPreLoadByArchetypeAsync(
        string archetypeId,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        var gear = new List<GearDatabase.GearItem>();

        switch (archetypeId.ToLowerInvariant())
        {
            case "street-samurai":
            case "street samurai":
                // Street Samurai starting gear (SR3)
                gear.Add(GearDatabase.GetGearById("heavy-pistol")!);
                gear.Add(GearDatabase.GetGearById("combat-knife")!);
                gear.Add(GearDatabase.GetGearById("armor-jacket")!);
                gear.Add(GearDatabase.GetGearById("wired-reflexes-1")!);
                gear.Add(GearDatabase.GetGearById("smartlink")!);
                gear.Add(GearDatabase.GetGearById("medkit-rating-6")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                break;

            case "mage":
                // Mage starting gear (SR3)
                gear.Add(GearDatabase.GetGearById("light-pistol")!);
                gear.Add(GearDatabase.GetGearById("armor-vest")!);
                gear.Add(GearDatabase.GetGearById("medkit-rating-6")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                // TODO: Add magical supplies (libraries, fetishes, etc.)
                break;

            case "decker":
                // Decker starting gear (SR3)
                gear.Add(GearDatabase.GetGearById("light-pistol")!);
                gear.Add(GearDatabase.GetGearById("armor-vest")!);
                gear.Add(GearDatabase.GetGearById("cyberdeck-base")!);
                gear.Add(GearDatabase.GetGearById("cybereyes-basic")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                break;

            case "shaman":
                // Shaman starting gear (SR3)
                gear.Add(GearDatabase.GetGearById("light-pistol")!);
                gear.Add(GearDatabase.GetGearById("armor-vest")!);
                gear.Add(GearDatabase.GetGearById("medkit-rating-6")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                // TODO: Add shamanic supplies (lodges, fetishes, etc.)
                break;

            case "rigger":
                // Rigger starting gear (SR3)
                gear.Add(GearDatabase.GetGearById("light-pistol")!);
                gear.Add(GearDatabase.GetGearById("armor-vest")!);
                gear.Add(GearDatabase.GetGearById("citymaster")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                // TODO: Add drones, control rig
                break;

            case "face":
                // Face starting gear (SR3)
                gear.Add(GearDatabase.GetGearById("light-pistol")!);
                gear.Add(GearDatabase.GetGearById("armor-jacket")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                // TODO: Add social gear (disguise kits, etc.)
                break;

            case "physical adept":
            case "physical-adept":
                // Physical Adept starting gear (SR3)
                gear.Add(GearDatabase.GetGearById("combat-knife")!);
                gear.Add(GearDatabase.GetGearById("armor-jacket")!);
                gear.Add(GearDatabase.GetGearById("medkit-rating-6")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                break;
        }

        return gear.Where(g => g != null).ToList();
    }

    /// <summary>
    /// Get pre-loaded gear package based on priority level
    /// Higher priority = more/better starting gear
    /// </summary>
    public async Task<List<GearDatabase.GearItem>> GetPreLoadByPriorityAsync(
        string priorityLevel,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        var gear = new List<GearDatabase.GearItem>();

        switch (priorityLevel.ToUpperInvariant())
        {
            case "A": // 1,000,000¥ - High-end gear
                gear.Add(GearDatabase.GetGearById("assault-rifle")!);
                gear.Add(GearDatabase.GetGearById("full-body-armor")!);
                gear.Add(GearDatabase.GetGearById("wired-reflexes-2")!);
                gear.Add(GearDatabase.GetGearById("cybereyes-basic")!);
                gear.Add(GearDatabase.GetGearById("smartlink")!);
                gear.Add(GearDatabase.GetGearById("citymaster")!);
                gear.Add(GearDatabase.GetGearById("medkit-rating-6")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                break;

            case "B": // 400,000¥ - Good gear
                gear.Add(GearDatabase.GetGearById("heavy-pistol")!);
                gear.Add(GearDatabase.GetGearById("katana")!);
                gear.Add(GearDatabase.GetGearById("armor-jacket")!);
                gear.Add(GearDatabase.GetGearById("wired-reflexes-1")!);
                gear.Add(GearDatabase.GetGearById("smartlink")!);
                gear.Add(GearDatabase.GetGearById("harley-davidson")!);
                gear.Add(GearDatabase.GetGearById("medkit-rating-6")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                break;

            case "C": // 90,000¥ - Moderate gear
                gear.Add(GearDatabase.GetGearById("heavy-pistol")!);
                gear.Add(GearDatabase.GetGearById("combat-knife")!);
                gear.Add(GearDatabase.GetGearById("armor-jacket")!);
                gear.Add(GearDatabase.GetGearById("medkit-rating-6")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                break;

            case "D": // 20,000¥ - Basic gear
                gear.Add(GearDatabase.GetGearById("light-pistol")!);
                gear.Add(GearDatabase.GetGearById("combat-knife")!);
                gear.Add(GearDatabase.GetGearById("armor-vest")!);
                gear.Add(GearDatabase.GetGearById("commlink")!);
                break;

            case "E": // 5,000¥ - Minimal gear
                gear.Add(GearDatabase.GetGearById("light-pistol")!);
                gear.Add(GearDatabase.GetGearById("armor-vest")!);
                gear.Add(GearDatabase.GetGearById("flashlight")!);
                break;
        }

        return gear.Where(g => g != null).ToList();
    }

    /// <summary>
    /// Parse gear JSON and add to character
    /// </summary>
    public async Task<(bool Success, string Message, List<CharacterGear> AddedGear)> ParseGearJsonAndAddToCharacterAsync(
        ShadowrunCharacter character,
        string gearJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var gearIds = JsonSerializer.Deserialize<List<string>>(gearJson);
            
            if (gearIds == null || gearIds.Count == 0)
            {
                return (false, "No gear IDs found in JSON", new List<CharacterGear>());
            }

            var addedGear = new List<CharacterGear>();
            var totalCost = 0L;

            foreach (var gearId in gearIds)
            {
                var gearItem = await GetGearByIdAsync(gearId, cancellationToken);
                
                if (gearItem == null)
                {
                    return (false, $"Gear item '{gearId}' not found", new List<CharacterGear>());
                }

                // Check if character can afford it
                if (totalCost + gearItem.Cost > character.Nuyen)
                {
                    return (false, $"Cannot afford {gearItem.Name} (Cost: {gearItem.Cost:N0}¥, Available: {character.Nuyen - totalCost:N0}¥)", 
                        new List<CharacterGear>());
                }

                // Check essence for cyberware/bioware
                if (gearItem.EssenceCost > 0)
                {
                    var currentEssenceLoss = character.Cyberware?.Sum(c => c.EssenceCost) ?? 0m;
                    if (currentEssenceLoss + gearItem.EssenceCost > 6.0m)
                    {
                        return (false, $"Not enough essence for {gearItem.Name} (Required: {gearItem.EssenceCost:F1}, Available: {6.0m - currentEssenceLoss:F1})", 
                            new List<CharacterGear>());
                    }
                }

                // Add gear to character
                var characterGear = new CharacterGear
                {
                    Name = gearItem.Name,
                    Category = gearItem.Category,
                    Value = gearItem.Cost,
                    Quantity = 1,
                    Description = gearItem.Description,
                    IsEquipped = true
                };

                // Handle cyberware/bioware specially
                if (gearItem.Category == "Cyberware" || gearItem.Category == "Bioware")
                {
                    var cyberware = new CharacterCyberware
                    {
                        Name = gearItem.Name,
                        Category = gearItem.Category,
                        EssenceCost = gearItem.EssenceCost,
                        NuyenCost = gearItem.Cost,
                        Rating = gearItem.Stats.GetValueOrDefault("Rating", 0),
                        IsInstalled = true
                    };
                    character.Cyberware.Add(cyberware);
                }
                else
                {
                    character.Gear.Add(characterGear);
                }

                addedGear.Add(characterGear);
                totalCost += gearItem.Cost;
            }

            // Deduct cost from character's nuyen
            character.Nuyen -= totalCost;

            return (true, $"Successfully added {addedGear.Count} gear items (Cost: {totalCost:N0}¥)", addedGear);
        }
        catch (JsonException ex)
        {
            return (false, $"Invalid gear JSON: {ex.Message}", new List<CharacterGear>());
        }
        catch (Exception ex)
        {
            return (false, $"Error processing gear: {ex.Message}", new List<CharacterGear>());
        }
    }

    /// <summary>
    /// Add single gear item to character
    /// </summary>
    public async Task<(bool Success, string Message)> AddGearToCharacterAsync(
        ShadowrunCharacter character,
        string gearId,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var gearItem = await GetGearByIdAsync(gearId, cancellationToken);
        
        if (gearItem == null)
        {
            return (false, $"Gear item '{gearId}' not found");
        }

        var totalCost = gearItem.Cost * quantity;

        // Check if character can afford it
        if (totalCost > character.Nuyen)
        {
            return (false, $"Cannot afford {gearItem.Name} x{quantity} (Cost: {totalCost:N0}¥, Available: {character.Nuyen:N0}¥)");
        }

        // Check essence for cyberware/bioware
        if (gearItem.EssenceCost > 0)
        {
            var currentEssenceLoss = character.Cyberware?.Sum(c => c.EssenceCost) ?? 0m;
            if (currentEssenceLoss + (gearItem.EssenceCost * quantity) > 6.0m)
            {
                return (false, $"Not enough essence for {gearItem.Name} (Required: {gearItem.EssenceCost * quantity:F1}, Available: {6.0m - currentEssenceLoss:F1})");
            }
        }

        // Add gear to character
        if (gearItem.Category == "Cyberware" || gearItem.Category == "Bioware")
        {
            for (int i = 0; i < quantity; i++)
            {
                var cyberware = new CharacterCyberware
                {
                    Name = gearItem.Name,
                    Category = gearItem.Category,
                    EssenceCost = gearItem.EssenceCost,
                    NuyenCost = gearItem.Cost,
                    Rating = gearItem.Stats.GetValueOrDefault("Rating", 0),
                    IsInstalled = true
                };
                character.Cyberware.Add(cyberware);
            }
        }
        else
        {
            var characterGear = new CharacterGear
            {
                Name = gearItem.Name,
                Category = gearItem.Category,
                Value = gearItem.Cost,
                Quantity = quantity,
                Description = gearItem.Description,
                IsEquipped = true
            };
            character.Gear.Add(characterGear);
        }

        // Deduct cost
        character.Nuyen -= totalCost;

        return (true, $"Added {gearItem.Name} x{quantity} to character (Cost: {totalCost:N0}¥)");
    }

    /// <summary>
    /// Remove gear from character
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveGearFromCharacterAsync(
        ShadowrunCharacter character,
        string gearId,
        CancellationToken cancellationToken = default)
    {
        var gearItem = await GetGearByIdAsync(gearId, cancellationToken);
        
        if (gearItem == null)
        {
            return (false, $"Gear item '{gearId}' not found");
        }

        // Find gear in character's inventory
        var characterGear = character.Gear.FirstOrDefault(g => g.Name == gearItem.Name);
        
        if (characterGear != null)
        {
            character.Gear.Remove(characterGear);
            character.Nuyen += characterGear.Value; // Refund
            return (true, $"Removed {gearItem.Name} from character (Refund: {characterGear.Value:N0}¥)");
        }

        // Check cyberware/bioware
        var cyberware = character.Cyberware.FirstOrDefault(c => c.Name == gearItem.Name);
        
        if (cyberware != null)
        {
            character.Cyberware.Remove(cyberware);
            character.Nuyen += cyberware.NuyenCost; // Refund
            return (true, $"Removed {gearItem.Name} from character (Refund: {cyberware.NuyenCost:N0}¥)");
        }

        return (false, $"Character does not have {gearItem.Name}");
    }
}

/// <summary>
/// Interface for gear selection service
/// </summary>
public interface IGearSelectionService
{
    List<string> GetGearCategories();
    Task<List<GearDatabase.GearItem>> GetGearByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<GearDatabase.GearItem?> GetGearByIdAsync(string gearId, CancellationToken cancellationToken = default);
    Task<List<GearDatabase.GearItem>> GetPreLoadByArchetypeAsync(string archetypeId, CancellationToken cancellationToken = default);
    Task<List<GearDatabase.GearItem>> GetPreLoadByPriorityAsync(string priorityLevel, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, List<CharacterGear> AddedGear)> ParseGearJsonAndAddToCharacterAsync(ShadowrunCharacter character, string gearJson, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> AddGearToCharacterAsync(ShadowrunCharacter character, string gearId, int quantity = 1, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> RemoveGearFromCharacterAsync(ShadowrunCharacter character, string gearId, CancellationToken cancellationToken = default);
}
