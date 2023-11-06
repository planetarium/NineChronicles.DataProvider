namespace NineChronicles.DataProvider.Store.Models
{
    using System;

    public class UserNCGsModel
    {
        public long? BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public decimal? NCGBalance { get; set; }

        public DateTimeOffset? TimeStamp { get; set; }
    }
}
