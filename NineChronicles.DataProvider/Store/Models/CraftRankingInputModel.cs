namespace NineChronicles.DataProvider.Store.Models
{
    using System.ComponentModel.DataAnnotations;

    public class CraftRankingInputModel
    {
        [Key]
        public string? AvatarAddress { get; set; }

        public string? AgentAddress { get; set; }

        public long BlockIndex { get; set; }

        public int CraftCount { get; set; }

        public int Ranking { get; set; }
    }
}
