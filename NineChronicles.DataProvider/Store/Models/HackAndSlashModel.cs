namespace NineChronicles.DataProvider.Store.Models
{
    using System.ComponentModel.DataAnnotations;

    public class HackAndSlashModel
    {
        [Key]
        public string? Id { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int StageId { get; set; }

        public bool Cleared { get; set; }

        public bool Mimisbrunnr { get; set; }

        public long BlockIndex { get; set; }
    }
}
