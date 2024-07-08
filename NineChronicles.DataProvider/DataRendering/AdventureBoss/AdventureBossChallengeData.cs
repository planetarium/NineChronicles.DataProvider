namespace NineChronicles.DataProvider.DataRendering.AdventureBoss
{
    using System;
    using Libplanet.Action.State;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;

    public static class AdventureBossChallengeData
    {
        public static AdventureBossChallengeModel GetChallengeInfo(
            IWorld prevStates,
            IWorld outputStates,
            long blockIndex,
            DateTimeOffset blockTime,
            ExploreAdventureBoss challenge
        )
        {
            var prevExplorer = prevStates.TryGetExplorer(challenge.Season, challenge.AvatarAddress, out var exp)
                ? exp
                : new Explorer(challenge.AvatarAddress, "name");
            var outputExplorer = outputStates.GetExplorer(challenge.Season, challenge.AvatarAddress);
            var exploreBoard = outputStates.GetExploreBoard(challenge.Season);

            return new AdventureBossChallengeModel
            {
                Id = challenge.Id.ToString(),
                BlockIndex = blockIndex,
                Season = challenge.Season,
                AvatarAddress = challenge.AvatarAddress.ToString(),
                StartFloor = prevExplorer.Floor,
                EndFloor = outputExplorer.Floor,
                UsedApPotion = outputExplorer.UsedApPotion - prevExplorer.UsedApPotion,
                Point = outputExplorer.Score - prevExplorer.Score,
                TotalPoint = exploreBoard.TotalPoint,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
