namespace NineChronicles.DataProvider.Store.Models.Base
{
    public interface IAvatar
    {
        string? AvatarAddress { get; set; }

        AvatarModel? Avatar { get; set; }
    }
}
