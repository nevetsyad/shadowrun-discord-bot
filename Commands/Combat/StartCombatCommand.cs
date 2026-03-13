using MediatR;

namespace ShadowrunDiscordBot.Commands.Combat;

/// <summary>
/// Command to start a new combat session
/// </summary>
public class StartCombatCommand : IRequest<StartCombatResponse>
{
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
}

/// <summary>
/// Response from starting combat
/// </summary>
public class StartCombatResponse
{
    public bool Success { get; set; }
    public int SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
}
