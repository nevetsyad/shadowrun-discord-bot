using MediatR;

namespace ShadowrunDiscordBot.Commands.Characters;

/// <summary>
/// Command to create a new Shadowrun character
/// </summary>
public class CreateCharacterCommand : IRequest<CreateCharacterResponse>
{
    public ulong DiscordUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Metatype { get; set; } = "Human";
    public string Archetype { get; set; } = "Street Samurai";
    
    // Attributes
    public int Body { get; set; } = 3;
    public int Quickness { get; set; } = 3;
    public int Strength { get; set; } = 3;
    public int Charisma { get; set; } = 3;
    public int Intelligence { get; set; } = 3;
    public int Willpower { get; set; } = 3;
    
    // Resources
    public int Karma { get; set; } = 0;
    public long Nuyen { get; set; } = 0;
}

/// <summary>
/// Response from character creation
/// </summary>
public class CreateCharacterResponse
{
    public bool Success { get; set; }
    public int CharacterId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
}
