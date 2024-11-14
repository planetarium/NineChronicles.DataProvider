namespace NineChronicles.DataProvider.Store.Models.Summon
{
    [Microsoft.EntityFrameworkCore.IndexAttribute(nameof(Date))]

    public class RuneSummonModel
    {
        [System.ComponentModel.DataAnnotations.KeyAttribute]
        public string? Id { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int GroupId { get; set; }

        public int SummonCount { get; set; }

        public long BlockIndex { get; set; }

        public string? SummonResult { get; set; }

        public System.DateOnly Date { get; set; }

        public System.DateTimeOffset TimeStamp { get; set; }
    }
}
