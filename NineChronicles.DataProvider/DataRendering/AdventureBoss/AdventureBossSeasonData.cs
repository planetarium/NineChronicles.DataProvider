namespace NineChronicles.DataProvider.DataRendering.AdventureBoss
{
    using System;
    using Libplanet.Action.State;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;

    public static class AdventureBossSeasonData
    {
        public static AdventureBossSeasonModel GetAdventureBossSeasonInfo(
            IWorld outputStates,
            long season,
            DateTimeOffset blockTime
        )
        {
            var gameConfig = outputStates.GetGameConfigState();
            var seasonInfo = outputStates.GetSeasonInfo(season);
            var bountyBoard = outputStates.GetBountyBoard(season);
            var exploreBoard = outputStates.GetExploreBoard(season);

            return new AdventureBossSeasonModel
            {
                Season = season,
                StartBlockIndex = seasonInfo.StartBlockIndex,
                EndBlockIndex = seasonInfo.EndBlockIndex,
                NextSeasonBlockIndex = seasonInfo.NextStartBlockIndex,
                ClaimableBlockIndex = seasonInfo.EndBlockIndex + gameConfig.AdventureBossClaimInterval,
                BossId = seasonInfo.BossId,
                FixedRewardData = (bountyBoard.FixedRewardItemId ?? bountyBoard.FixedRewardFavId).ToString(),
                RandomRewardData = (bountyBoard.RandomRewardItemId ?? bountyBoard.RandomRewardFavId).ToString(),
                RaffleWinnerAddress = exploreBoard.RaffleWinner is null ? null : exploreBoard.RaffleWinner.ToString(),
                RaffleReward = Convert.ToDecimal(exploreBoard.RaffleReward?.GetQuantityString()),
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
