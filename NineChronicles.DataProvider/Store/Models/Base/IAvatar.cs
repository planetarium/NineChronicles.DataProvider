namespace NineChronicles.DataProvider.Store.Models
{
    public interface IAvatar
    {
        string? AvatarAddress { get; set; }

        AvatarModel? Avatar { get; set; }
    }
}
