using Microsoft.Extensions.Logging;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Services;

/// <summary>
/// Contacts and Legwork system for SR3
/// </summary>
public class ContactsLegworkService
{
    private readonly DiceService _diceService;
    private readonly DatabaseService _databaseService;
    private readonly ILogger<ContactsLegworkService> _logger;

    public ContactsLegworkService(
        DiceService diceService,
        DatabaseService databaseService,
        ILogger<ContactsLegworkService> logger)
    {
        _diceService = diceService;
        _databaseService = databaseService;
        _logger = logger;
    }

    #region Contact Management

    /// <summary>
    /// Add a contact to a character
    /// </summary>
    public async Task<ContactResult> AddContactAsync(
        int characterId,
        string name,
        string contactType,
        int level,
        int loyalty,
        int connection,
        string? specialties = null)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return ContactResult.Fail("Character not found.");

        // Validate level and loyalty (1-3)
        level = Math.Clamp(level, 1, 3);
        loyalty = Math.Clamp(loyalty, 1, 3);

        var contact = new CharacterContact
        {
            CharacterId = characterId,
            Name = name,
            ContactType = contactType,
            Level = level,
            Loyalty = loyalty,
            Connection = connection,
            Specialties = specialties
        };

        await _databaseService.AddContactAsync(contact);

        _logger.LogInformation("Added contact {Name} ({Type}) for character {CharId}",
            name, contactType, characterId);

        return ContactResult.Ok($"Added contact: {name} ({contactType}) - Level {level}, Loyalty {loyalty}");
    }

    /// <summary>
    /// Get all contacts for a character
    /// </summary>
    public async Task<List<CharacterContact>> GetContactsAsync(int characterId)
    {
        return await _databaseService.GetContactsAsync(characterId);
    }

    /// <summary>
    /// Improve contact level or loyalty
    /// </summary>
    public async Task<ContactResult> ImproveContactAsync(int contactId, string improvementType, int karmaSpent)
    {
        var contact = await _databaseService.GetContactAsync(contactId);
        if (contact == null)
            return ContactResult.Fail("Contact not found.");

        var cost = improvementType.ToLower() switch
        {
            "level" => 2, // 2 karma per level increase
            "loyalty" => 1, // 1 karma per loyalty increase
            _ => 0
        };

        if (karmaSpent < cost)
            return ContactResult.Fail($"Need {cost} karma, only {karmaSpent} provided.");

        switch (improvementType.ToLower())
        {
            case "level" when contact.Level < 3:
                contact.Level++;
                break;
            case "loyalty" when contact.Loyalty < 3:
                contact.Loyalty++;
                break;
            default:
                return ContactResult.Fail($"Cannot improve {improvementType} further.");
        }

        await _databaseService.UpdateContactAsync(contact);

        return ContactResult.Ok($"Improved {contact.Name}'s {improvementType}.");
    }

    /// <summary>
    /// Calculate contact availability modifier
    /// </summary>
    public int GetAvailabilityModifier(CharacterContact contact)
    {
        // Higher level contacts are harder to reach
        return contact.Level switch
        {
            1 => 0, // Acquaintance - easy to reach
            2 => 2, // Associate - moderate
            3 => 4, // Friend - busy
            _ => 0
        };
    }

    #endregion

    #region Legwork

    /// <summary>
    /// Perform legwork using a contact
    /// </summary>
    public async Task<LegworkResult> PerformLegworkAsync(
        int characterId,
        string informationSought,
        string legworkType,
        int? contactId = null,
        int etiquetteSkill = 0)
    {
        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return LegworkResult.Fail("Character not found.");

        CharacterContact? contact = null;
        if (contactId.HasValue)
        {
            contact = await _databaseService.GetContactAsync(contactId.Value);
        }

        // Calculate dice pool
        int pool;
        int targetNumber = 4;

        if (contact != null)
        {
            // Using a contact: Charisma + Etiquette + Contact bonuses
            pool = character.Charisma + etiquetteSkill + contact.Loyalty;
            targetNumber = 4 - contact.Level; // Better contacts = easier to get info

            // Apply availability modifier
            targetNumber += GetAvailabilityModifier(contact);
        }
        else
        {
            // Street legwork without contact: Charisma + Etiquette
            pool = character.Charisma + etiquetteSkill;
            targetNumber = 6; // Harder without a contact
        }

        // Environmental/story modifiers
        targetNumber += GetLegworkModifier(legworkType);

        // Roll
        var result = _diceService.RollShadowrun(pool, Math.Max(2, targetNumber));

        // Determine information quality
        var infoQuality = DetermineInfoQuality(result.Successes);

        // Calculate time and cost
        var timeHours = Math.Max(1, 8 - result.Successes);
        var nuyenCost = contact?.Level * 100 ?? 200; // Base cost

        var attempt = new LegworkAttempt
        {
            CharacterId = characterId,
            ContactId = contactId,
            InformationSought = informationSought,
            LegworkType = legworkType,
            SkillUsed = "Etiquette",
            DiceRolled = pool,
            Successes = result.Successes,
            TargetNumber = targetNumber,
            NuyenCost = nuyenCost,
            TimeHours = timeHours,
            InformationGained = GenerateLegworkResult(informationSought, infoQuality, result.Successes)
        };

        await _databaseService.AddLegworkAttemptAsync(attempt);

        _logger.LogInformation("Legwork by {CharId}: {Info}, Successes: {Successes}",
            characterId, informationSought, result.Successes);

        return new LegworkResult
        {
            Success = result.Successes > 0,
            ContactUsed = contact?.Name,
            InformationSought = informationSought,
            InformationQuality = infoQuality,
            DiceRolled = pool,
            TargetNumber = targetNumber,
            Successes = result.Successes,
            NuyenCost = nuyenCost,
            TimeHours = timeHours,
            InformationGained = attempt.InformationGained,
            Details = FormatLegworkResult(attempt, result)
        };
    }

    private int GetLegworkModifier(string legworkType)
    {
        return legworkType.ToLower() switch
        {
            "street" => 0,
            "corporate" => 2,
            "matrix" => 1,
            "government" => 4,
            "underground" => 1,
            _ => 0
        };
    }

    private string DetermineInfoQuality(int successes)
    {
        return successes switch
        {
            0 => "None",
            1 => "Rumors",
            2 => "Basic",
            3 => "Detailed",
            4 => "Comprehensive",
            _ => "Insider"
        };
    }

    private string GenerateLegworkResult(string sought, string quality, int successes)
    {
        if (successes == 0)
            return "No useful information obtained.";

        return $"[{quality}] Information regarding: {sought}";
    }

    private string FormatLegworkResult(LegworkAttempt attempt, ShadowrunDiceResult roll)
    {
        var contactInfo = attempt.ContactId.HasValue ? " via contact" : " (street)";
        return $"**Legwork{contactInfo}**\n" +
               $"Target: {attempt.InformationSought}\n" +
               $"Roll: {roll.Details}\n" +
               $"Quality: {DetermineInfoQuality(roll.Successes)}\n" +
               $"Time: {attempt.TimeHours} hours\n" +
               $"Cost: {attempt.NuyenCost}¥";
    }

    #endregion

    #region Johnson Meetings

    /// <summary>
    /// Create a Johnson meeting
    /// </summary>
    public async Task<JohnsonMeetingResult> CreateJohnsonMeetingAsync(
        int gameSessionId,
        string johnsonName,
        string corporation,
        int initialOffer,
        string? missionBriefing = null)
    {
        var meeting = new JohnsonMeeting
        {
            GameSessionId = gameSessionId,
            JohnsonName = johnsonName,
            Corporation = corporation,
            InitialOffer = initialOffer,
            FinalOffer = initialOffer,
            MissionBriefing = missionBriefing
        };

        await _databaseService.AddJohnsonMeetingAsync(meeting);

        return new JohnsonMeetingResult
        {
            Success = true,
            MeetingId = meeting.Id,
            JohnsonName = johnsonName,
            Corporation = corporation,
            InitialOffer = initialOffer,
            Details = $"Mr. Johnson ({johnsonName}) from {corporation} is ready to meet."
        };
    }

    /// <summary>
    /// Negotiate payment with Mr. Johnson
    /// </summary>
    public async Task<NegotiationResult> NegotiateWithJohnsonAsync(
        int meetingId,
        int negotiationSkill,
        int charismaModifier = 0)
    {
        var meeting = await _databaseService.GetJohnsonMeetingAsync(meetingId);
        if (meeting == null)
            return NegotiationResult.Fail("Meeting not found.");

        if (meeting.Accepted)
            return NegotiationResult.Fail("Meeting already concluded.");

        // Negotiation test
        var pool = negotiationSkill + charismaModifier;
        var result = _diceService.RollShadowrun(pool, 4);

        // Each success adds 10% to offer
        var increase = (int)(meeting.InitialOffer * result.Successes * 0.1);
        meeting.FinalOffer = meeting.InitialOffer + increase;
        meeting.NegotiationSuccesses = result.Successes;

        await _databaseService.UpdateJohnsonMeetingAsync(meeting);

        return new NegotiationResult
        {
            Success = true,
            InitialOffer = meeting.InitialOffer,
            FinalOffer = meeting.FinalOffer,
            Increase = increase,
            Successes = result.Successes,
            Details = $"Negotiated {increase:N0}¥ increase! Final offer: {meeting.FinalOffer:N0}¥"
        };
    }

    /// <summary>
    /// Accept or reject the Johnson's offer
    /// </summary>
    public async Task<JohnsonMeetingResult> RespondToOfferAsync(int meetingId, bool accept)
    {
        var meeting = await _databaseService.GetJohnsonMeetingAsync(meetingId);
        if (meeting == null)
            return JohnsonMeetingResult.Fail("Meeting not found.");

        meeting.Accepted = accept;
        await _databaseService.UpdateJohnsonMeetingAsync(meeting);

        return new JohnsonMeetingResult
        {
            Success = true,
            MeetingId = meetingId,
            JohnsonName = meeting.JohnsonName,
            Corporation = meeting.Corporation,
            InitialOffer = meeting.InitialOffer,
            FinalOffer = meeting.FinalOffer,
            Accepted = accept,
            Details = accept
                ? $"Accepted the job for {meeting.FinalOffer:N0}¥!"
                : "Declined the offer."
        };
    }

    #endregion

    #region Fixer Connections

    /// <summary>
    /// Use fixer to find work
    /// </summary>
    public async Task<FixerResult> FindWorkThroughFixerAsync(
        int characterId,
        int fixerContactId,
        int desiredPaymentLevel)
    {
        var fixer = await _databaseService.GetContactAsync(fixerContactId);
        if (fixer == null || fixer.ContactType != "Fixer")
            return FixerResult.Fail("Fixer contact not found.");

        var character = await _databaseService.GetCharacterByIdAsync(characterId);
        if (character == null)
            return FixerResult.Fail("Character not found.");

        // Fixer check = Loyalty + Connection
        var pool = fixer.Loyalty + fixer.Connection;
        var targetNumber = 4 + (desiredPaymentLevel / 10000); // Harder for better pay

        var result = _diceService.RollShadowrun(pool, targetNumber);

        if (result.Successes == 0)
            return FixerResult.Fail("Fixer couldn't find any suitable work.");

        // Generate mission leads based on successes
        var missions = GenerateMissionLeads(result.Successes, desiredPaymentLevel);

        return new FixerResult
        {
            Success = true,
            FixerName = fixer.Name,
            MissionsFound = missions.Count,
            Missions = missions,
            Details = $"{fixer.Name} found {missions.Count} potential job(s)."
        };
    }

    private List<MissionLead> GenerateMissionLeads(int successes, int targetPay)
    {
        var leads = new List<MissionLead>();

        for (int i = 0; i < successes; i++)
        {
            leads.Add(new MissionLead
            {
                Title = $"Job #{i + 1}",
                Employer = "Mr. Johnson",
                EstimatedPay = (int)(targetPay * (0.8 + (i * 0.2))),
                Difficulty = i + 1
            });
        }

        return leads;
    }

    /// <summary>
    /// Use fixer to acquire gear
    /// </summary>
    public async Task<FixerResult> AcquireGearThroughFixerAsync(
        int characterId,
        int fixerContactId,
        string gearName,
        int availability,
        long cost)
    {
        var fixer = await _databaseService.GetContactAsync(fixerContactId);
        if (fixer == null || fixer.ContactType != "Fixer")
            return FixerResult.Fail("Fixer contact not found.");

        // Availability test
        var pool = fixer.Connection + fixer.Loyalty;
        var result = _diceService.RollShadowrun(pool, availability);

        var acquired = result.Successes > 0;
        var timeDays = Math.Max(1, 7 - result.Successes);
        var finalCost = acquired ? cost : 0;

        if (acquired)
        {
            // Deduct cost from character
            var character = await _databaseService.GetCharacterByIdAsync(characterId);
            if (character != null && character.Nuyen >= cost)
            {
                character.Nuyen -= cost;
                await _databaseService.UpdateCharacterAsync(character);
            }
            else
            {
                return FixerResult.Fail("Not enough nuyen.");
            }
        }

        return new FixerResult
        {
            Success = acquired,
            FixerName = fixer.Name,
            Details = acquired
                ? $"Acquired {gearName} for {cost:N0}¥. Took {timeDays} days."
                : $"Could not acquire {gearName}."
        };
    }

    #endregion
}

#region Result Types

public record ContactResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ContactResult Ok(string message) => new() { Success = true, Message = message };
    public static ContactResult Fail(string message) => new() { Success = false, Message = message };
}

public record LegworkResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string? ContactUsed { get; init; }
    public string InformationSought { get; init; } = string.Empty;
    public string InformationQuality { get; init; } = "None";
    public int DiceRolled { get; init; }
    public int TargetNumber { get; init; }
    public int Successes { get; init; }
    public int NuyenCost { get; init; }
    public int TimeHours { get; init; }
    public string? InformationGained { get; init; }

    public static LegworkResult Fail(string details) => new() { Success = false, Details = details };
}

public record JohnsonMeetingResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public int MeetingId { get; init; }
    public string JohnsonName { get; init; } = "Mr. Johnson";
    public string Corporation { get; init; } = "Unknown";
    public int InitialOffer { get; init; }
    public int FinalOffer { get; init; }
    public bool Accepted { get; init; }

    public static JohnsonMeetingResult Fail(string details) => new() { Success = false, Details = details };
}

public record NegotiationResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public int InitialOffer { get; init; }
    public int FinalOffer { get; init; }
    public int Increase { get; init; }
    public int Successes { get; init; }

    public static NegotiationResult Fail(string details) => new() { Success = false, Details = details };
}

public record FixerResult
{
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public string FixerName { get; init; } = string.Empty;
    public int MissionsFound { get; init; }
    public List<MissionLead>? Missions { get; init; }

    public static FixerResult Fail(string details) => new() { Success = false, Details = details };
}

public class MissionLead
{
    public string Title { get; set; } = string.Empty;
    public string Employer { get; set; } = "Mr. Johnson";
    public int EstimatedPay { get; set; }
    public int Difficulty { get; set; }
}

#endregion
