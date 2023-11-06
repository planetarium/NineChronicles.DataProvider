namespace NineChronicles.DataProvider.Store.Models
{
    using System;

    public class UserConsumablesModel
    {
        public long? BlockIndex { get; set; }

        public string? ItemId { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public string? ItemType { get; set; }

        public string? ItemSubType { get; set; }

        public int? Id { get; set; }

        public int? BuffSkillCount { get; set; }

        public string? ElementalType { get; set; }

        public int? Grade { get; set; }

        public int? SkillsCount { get; set; }

        public long? RequiredBlockIndex { get; set; }

        public string? NonFungibleId { get; set; }

        public string? TradableId { get; set; }

        public string? MainStat { get; set; }

        public DateTimeOffset? TimeStamp { get; set; }
    }
}
