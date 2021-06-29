namespace NineChronicles.DataProvider.Store.Models
{
    using System.ComponentModel.DataAnnotations;

    public class CombinationConsumableModel
    {
        [Key]
        public string? Id { get; set; }

        public string? AgentAddress { get; set; }

        public AgentModel? Agent { get; set; }

        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int RecipeId { get; set; }

        public int SlotIndex { get; set; }

        public long BlockIndex { get; set; }
    }
}
