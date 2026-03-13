using MediatR;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Queries.Characters;

/// <summary>
/// Query to get all characters for a user
/// </summary>
public class GetUserCharactersQuery : IRequest<GetUserCharactersResponse>
{
    public ulong DiscordUserId { get; set; }
}

/// <summary>
/// Response with user's characters
/// </summary>
public class GetUserCharactersResponse
{
    public bool Success { get; set; }
    public List<ShadowrunCharacter> Characters { get; set; } = new();
    public string? Error { get; set; }
}
