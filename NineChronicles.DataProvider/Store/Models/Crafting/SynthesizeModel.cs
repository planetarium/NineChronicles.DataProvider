namespace NineChronicles.DataProvider.Store.Models.Crafting
{
    using NineChronicles.DataProvider.Store.Models.Base;

    public class SynthesizeModel : BaseModel, IAvatar
    {
        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int MaterialGradeId { get; set; }

        public int MaterialItemSubTypeId { get; set; }

        public string? MaterialInfo { get; set; }

        public string? ResultInfo { get; set; }
    }
}
