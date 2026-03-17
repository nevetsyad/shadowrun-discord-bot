namespace ShadowrunDiscordBot.Infrastructure.Data
{
    public class CombatAction
    {
        public string? ActionType { get; set; }
        public string? ActorId { get; set; }
        public int CombatSessionId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
