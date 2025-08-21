namespace NineChronicles.DataProvider.Store.Models
{
    public class WorldBossRankingModel
    {
        public int Ranking { get; set; }

        public string? AvatarName { get; set; }

        public long HighScore { get; set; }

        public long TotalScore { get; set; }

        public long Cp { get; set; }

        public int Level { get; set; }

        public string? Address { get; set; }

        public int IconId { get; set; }
    }
}
