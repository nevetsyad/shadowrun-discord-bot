using MediatR;
using ShadowrunDiscordBot.Models;

namespace ShadowrunDiscordBot.Queries.Combat;

/// <summary>
/// Query to get active combat session for a channel
/// </summary>
public class GetActiveCombatQuery : IRequest<GetActiveCombatResponse>
{
    public ulong ChannelId { get; set; }
}

/// <summary>
/// Response with combat session data
/// </summary>
public class GetActiveCombatResponse
{
    public bool Success { get; set; }
    public CombatSession? Session { get; set; }
    public string? Error { get; set; }
}
