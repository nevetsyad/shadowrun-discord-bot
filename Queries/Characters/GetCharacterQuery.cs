using MediatR;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Queries.Characters;

/// <summary>
/// Query to get a character by ID
/// </summary>
public class GetCharacterQuery : IRequest<GetCharacterResponse>
{
    public int CharacterId { get; set; }
}

/// <summary>
/// Response with character data
/// </summary>
public class GetCharacterResponse
{
    public bool Success { get; set; }
    public ShadowrunCharacter? Character { get; set; }
    public string? Error { get; set; }
}
