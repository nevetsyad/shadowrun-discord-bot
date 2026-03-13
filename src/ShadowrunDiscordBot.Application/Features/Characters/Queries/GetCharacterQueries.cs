namespace ShadowrunDiscordBot.Application.Features.Characters.Queries;

using MediatR;
using DTOs;

/// <summary>
/// Query to get a character by ID
/// </summary>
public class GetCharacterByIdQuery : IRequest<CharacterDto?>
{
    public int CharacterId { get; set; }
}

/// <summary>
/// Query to get a character by Discord user ID and name
/// </summary>
public class GetCharacterByNameQuery : IRequest<CharacterDto?>
{
    public ulong DiscordUserId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
}

/// <summary>
/// Query to get all characters for a Discord user
/// </summary>
public class GetUserCharactersQuery : IRequest<List<CharacterDto>>
{
    public ulong DiscordUserId { get; set; }
}

/// <summary>
/// Query to check if a character exists
/// </summary>
public class CharacterExistsQuery : IRequest<bool>
{
    public ulong DiscordUserId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
}
