using MediatR;

namespace ShadowrunDiscordBot.Commands.Combat;

/// <summary>
/// Command to end a combat session
/// </summary>
public class EndCombatCommand : IRequest<EndCombatResponse>
{
    public ulong ChannelId { get; set; }
}

/// <summary>
/// Response from ending combat
/// </summary>
public class EndCombatResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
}
