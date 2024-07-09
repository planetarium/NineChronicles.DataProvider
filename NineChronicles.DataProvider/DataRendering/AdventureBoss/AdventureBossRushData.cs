namespace NineChronicles.DataProvider.DataRendering.AdventureBoss
{
    using System;
    using Libplanet.Action.State;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;
    using Serilog;

    public static class AdventureBossRushData
    {
        public static AdventureBossRushModel GetRushInfo(
            IWorld prevStates,
            IWorld outputStates,
            long blockIndex,
            DateTimeOffset blockTime,
            SweepAdventureBoss rush
        )
        {
            var prevExplorer = prevStates.GetExplorer(rush.Season, rush.AvatarAddress);
            var outputExplorer = outputStates.GetExplorer(rush.Season, rush.AvatarAddress);
            var outputExploreBoard = outputStates.GetExploreBoard(rush.Season);

            Log.Debug(
                $"[Adventure Boss] GetChallengeData: {rush.Season}::{outputExplorer.Floor}::{outputExplorer.UsedApPotion - prevExplorer.UsedApPotion}::{outputExploreBoard.TotalPoint}"
            );
            return new AdventureBossRushModel
            {
                Id = rush.Id.ToString(),
                BlockIndex = blockIndex,
                Season = rush.Season,
                AvatarAddress = rush.AvatarAddress.ToString(),
                EndFloor = outputExplorer.Floor,
                UsedApPotion = outputExplorer.UsedApPotion - prevExplorer.UsedApPotion,
                Point = outputExplorer.Score - prevExplorer.Score,
                TotalPoint = outputExploreBoard.TotalPoint,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
