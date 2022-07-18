namespace NineChronicles.DataProvider.Store.Models
{
    using System.ComponentModel.DataAnnotations;

    public class AbilityRankingModel
    {
        [Key]
        public string? AvatarAddress { get; set; }

        public string? AgentAddress { get; set; }

        public string? Name { get; set; }

        public int? AvatarLevel { get; set; }

        public int? TitleId { get; set; }

        public int? ArmorId { get; set; }

        public int? Cp { get; set; }

        public int Ranking { get; set; }
    }
}
