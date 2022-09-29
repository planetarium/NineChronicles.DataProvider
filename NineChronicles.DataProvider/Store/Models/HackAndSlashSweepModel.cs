namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class HackAndSlashSweepModel
    {
        [Key]
        public string? Id { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int WorldId { get; set; }

        public int StageId { get; set; }

        public int ApStoneCount { get; set; }

        public int ActionPoint { get; set; }

        public int CostumesCount { get; set; }

        public int EquipmentsCount { get; set; }

        public bool Cleared { get; set; }

        public bool Mimisbrunnr { get; set; }

        public long BlockIndex { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
