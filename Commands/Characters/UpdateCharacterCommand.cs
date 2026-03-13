using MediatR;

namespace ShadowrunDiscordBot.Commands.Characters;

/// <summary>
/// Command to update an existing character
/// </summary>
public class UpdateCharacterCommand : IRequest<UpdateCharacterResponse>
{
    public int CharacterId { get; set; }
    public ulong DiscordUserId { get; set; }
    
    // Optional updates (null = no change)
    public string? Name { get; set; }
    public int? Body { get; set; }
    public int? Quickness { get; set; }
    public int? Strength { get; set; }
    public int? Charisma { get; set; }
    public int? Intelligence { get; set; }
    public int? Willpower { get; set; }
    public int? Karma { get; set; }
    public long? Nuyen { get; set; }
    public int? PhysicalDamage { get; set; }
    public int? StunDamage { get; set; }
}

/// <summary>
/// Response from character update
/// </summary>
public class UpdateCharacterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
}
