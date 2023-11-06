namespace NineChronicles.DataProvider.Store.Models
{
    using System;

    public class UserStakingsModel
    {
        public long? BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public decimal? StakeAmount { get; set; }

        public long? StartedBlockIndex { get; set; }

        public long? ReceivedBlockIndex { get; set; }

        public long? CancellableBlockIndex { get; set; }

        public DateTimeOffset? TimeStamp { get; set; }
    }
}
