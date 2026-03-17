namespace ShadowrunDiscordBot.Infrastructure.Data
{
    public class ActiveICEncounter
    {
        public int? Id { get; set; }
        public int MatrixRunId { get; set; }
        public int HostICEId { get; set; }
        public bool IsDefeated { get; set; }
        public int DamageToDeck { get; set; }
        public int DamageToCharacter { get; set; }
        public string? EncounterLog { get; set; }
    }
}
