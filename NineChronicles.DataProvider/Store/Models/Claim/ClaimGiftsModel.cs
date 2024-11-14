namespace NineChronicles.DataProvider.Store.Models.Claim
{
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models.Base;

    [Index(nameof(Date))]
    public class ClaimGiftsModel : BaseModel, IAvatar
    {
        public string? AvatarAddress { get; set; }

        public AvatarModel? Avatar { get; set; }

        public int GiftId { get; set; }
    }
}
