namespace NineChronicles.DataProvider.Store.Models
{
    using System.ComponentModel.DataAnnotations;

    public class StageRankingModel
    {
        [Key]
        public int Ranking { get; set; }

        public int ClearedStageId { get; set; }

        public string? AvatarAddress { get; set; }

        public string? Name { get; set; }

        public long BlockIndex { get; set; }
    }
}
