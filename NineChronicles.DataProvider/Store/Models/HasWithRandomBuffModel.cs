namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class HasWithRandomBuffModel
    {
        [Key]
        public string? Id { get; set; }

        public long BlockIndex { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int StageId { get; set; }

        public int BuffId { get; set; }

        public bool Cleared { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
