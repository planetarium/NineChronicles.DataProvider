namespace NineChronicles.DataProvider.DataRendering
{
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class RaidData
    {
        public static RaiderModel GetRaidInfo(
            int raidId,
            RaiderState raiderState
        )
        {
            var raiderModel = new RaiderModel(
                raidId,
                raiderState.AvatarName,
                raiderState.HighScore,
                raiderState.TotalScore,
                raiderState.Cp,
                raiderState.IconId,
                raiderState.Level,
                raiderState.AvatarAddress.ToString(),
                raiderState.PurchaseCount);

            return raiderModel;
        }
    }
}
