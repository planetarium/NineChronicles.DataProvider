namespace NineChronicles.DataProvider.Store.Models.Summon
{
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models.Base;

    [Index(nameof(Date))]
    public class CostumeSummonModel : BaseModel, IAvatar
    {
        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int GroupId { get; set; }

        public int SummonCount { get; set; }

        public string? SummonResult { get; set; }
    }
}
