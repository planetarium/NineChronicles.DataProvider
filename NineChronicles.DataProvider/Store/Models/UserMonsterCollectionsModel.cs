namespace NineChronicles.DataProvider.Store.Models
{
    using System;

    public class UserMonsterCollectionsModel
    {
        public long? BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public decimal? MonsterCollectionAmount { get; set; }

        public int? Level { get; set; }

        public long? RewardLevel { get; set; }

        public long? StartedBlockIndex { get; set; }

        public long? ReceivedBlockIndex { get; set; }

        public long? ExpiredBlockIndex { get; set; }

        public DateTimeOffset? TimeStamp { get; set; }
    }
}
