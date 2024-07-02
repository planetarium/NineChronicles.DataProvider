namespace NineChronicles.DataProvider.DataRendering.AdventureBoss
{
    using System;
    using Libplanet.Action.State;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;

    public static class AdventureBossUnlockFloorData
    {
        public static AdventureBossUnlockFloorModel GetUnlockInfo(
            IWorld prevState,
            IWorld outputState,
            long blockIndex,
            DateTimeOffset blockTime,
            UnlockFloor unlock
        )
        {
            var prevExploreBoard = prevState.GetExploreBoard(unlock.Season);
            var outputExploreBoard = outputState.GetExploreBoard(unlock.Season);
            var prevExplorer = prevState.GetExplorer(unlock.Season, unlock.AvatarAddress);
            var outputExplorer = outputState.GetExplorer(unlock.Season, unlock.AvatarAddress);
            return new AdventureBossUnlockFloorModel
            {
                Id = unlock.Id.ToString(),
                BlockIndex = blockIndex,
                Season = unlock.Season,
                AvatarAddress = unlock.AvatarAddress.ToString(),
                UnlockFloor = prevExplorer.Floor + 1,
                UsedGoldenDust = outputExplorer.UsedGoldenDust - prevExploreBoard.UsedGoldenDust,
                UsedNcg = (decimal)(outputExploreBoard.UsedNcg - prevExploreBoard.UsedNcg),
                TotalUsedGoldenDust = outputExploreBoard.UsedGoldenDust,
                TotalUsedNcg = (decimal)outputExploreBoard.UsedNcg,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
