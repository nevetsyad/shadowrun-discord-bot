namespace ShadowrunDiscordBot.Infrastructure.Data
{
    public class Cyberdeck
    {
        public List<DeckProgram> InstalledPrograms { get; set; } = new();
        public int Id { get; set; }  //used for database consider bigger if needed
        public int? CharacterId { get; set; }
        public string? Name { get; set; }
        public int MPCP { get; set; }
        public string? DeckType { get; set; }
        public int ActiveMemory { get; set; }
        public int StorageMemory { get; set; }
        public int LoadRating { get; set; }
        public int ResponseRating { get; set; }
        public int Hardening { get; set; }
        public int Value { get; set; }
    }

    public class DeckProgram
    {
        public int Rating { get; set; }
        public int MemoryCost { get; set; }
        public int CyberdeckId { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public bool IsLoaded { get; set; }
    }
}
