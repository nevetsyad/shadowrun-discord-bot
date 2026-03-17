using ShadowrunDiscordBot.Domain.Entities;

namespace ShadowrunDiscordBot.Infrastructure.Data
{
    public class CombatParticipant
    {
        public string? Name { get; set; }
        public Character? Character { get; set; }
        public int? CombatSessionId { get; set; }
        public int Id { get; set; }
    }
}
