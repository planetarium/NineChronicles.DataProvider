namespace NineChronicles.DataProvider.Store.Models
{
    using System;

    public class UserRunesModel
    {
        public long? BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public string? Ticker { get; set; }

        public decimal? RuneBalance { get; set; }

        public DateTimeOffset? TimeStamp { get; set; }
    }
}
