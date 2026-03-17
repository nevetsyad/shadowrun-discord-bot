namespace ShadowrunDiscordBot.Infrastructure.Data
{
    public class MatrixRun
    {
        public List<string> ICEncounters { get; set; } = new();//likely linkls to something else string for now
        public int CharacterId { get; set; }
        public DateTime? EndedAt { get; set; }
        public int Id { get; set; }
        public int SecurityTally { get; set; }
        public string? AlertStatus { get; set; }
        public int HostId { get; set; }
        public int CyberdeckId { get; set; }
        public int PassiveThreshold { get; set; }
        public int ActiveThreshold { get; set; }
        public int ShutdownThreshold { get; set; }
        public DateTime StartedAt { get; set; }
    }
}
