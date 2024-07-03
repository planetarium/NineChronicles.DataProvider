namespace NineChronicles.DataProvider.DataRendering.AdventureBoss
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Data;
    using Nekoyume.Helper;
    using Nekoyume.Module;
    using Nekoyume.TableData.AdventureBoss;
    using NineChronicles.DataProvider.Store.Models.AdventureBoss;

    public static class AdventureBossClaimRewardData
    {
        public static AdventureBossClaimRewardModel GetClaimInfo(
            IWorld prevState,
            long blockIndex,
            DateTimeOffset blockTime,
            ClaimAdventureBossReward claim
        )
        {
            var states = prevState;
            var myReward = new AdventureBossGameData.ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };
            var latestSeason = prevState.GetLatestAdventureBossSeason();

            var gameConfig = states.GetGameConfigState();
            var ncgRewardRatioSheet = states.GetSheet<AdventureBossNcgRewardRatioSheet>();

            var continueInv = true;
            var continueExp = true;

            var claimedSeasonList = new List<long>();

            for (var szn = latestSeason.Season; szn > 0; szn--)
            {
                var seasonInfo = states.GetSeasonInfo(szn);
                var bountyBoard = states.GetBountyBoard(szn);
                var exploreBoard = states.GetExploreBoard(szn);
                var investor =
                    bountyBoard.Investors.FirstOrDefault(inv => inv.AvatarAddress == claim.AvatarAddress);
                var ncgReward = 0 * bountyBoard.totalBounty().Currency;
                var explorer = states.TryGetExplorer(szn, claim.AvatarAddress, out var exp) ? exp : null;

                if (seasonInfo.EndBlockIndex > blockIndex)
                {
                    // Season in progress. Skip this season.
                    continue;
                }

                if (seasonInfo.EndBlockIndex + gameConfig.AdventureBossClaimInterval < blockIndex)
                {
                    // Claim interval expired.
                    break;
                }

                if (investor is not null)
                {
                    if (!claimedSeasonList.Contains(szn))
                    {
                        claimedSeasonList.Add(szn);
                    }

                    continueInv = AdventureBossHelper.CollectWantedReward(
                        myReward,
                        gameConfig,
                        ncgRewardRatioSheet,
                        seasonInfo,
                        bountyBoard,
                        investor,
                        blockIndex,
                        claim.AvatarAddress,
                        ref myReward
                    );
                    investor.Claimed = true;
                    states = states.SetBountyBoard(szn, bountyBoard);
                }

                if (explorer is not null)
                {
                    if (!claimedSeasonList.Contains(szn))
                    {
                        claimedSeasonList.Add(szn);
                    }

                    continueExp = AdventureBossHelper.CollectExploreReward(
                        myReward,
                        gameConfig,
                        ncgRewardRatioSheet,
                        seasonInfo,
                        bountyBoard,
                        exploreBoard,
                        explorer,
                        blockIndex,
                        claim.AvatarAddress,
                        ref myReward,
                        out ncgReward
                    );
                    explorer.Claimed = true;
                    states = states.SetExplorer(szn, explorer);
                }

                if (!continueInv && !continueExp)
                {
                    break;
                }
            }

            var rewardData = new List<string>();
            foreach (var (itemId, amount) in myReward.ItemReward)
            {
                rewardData.Add($"{itemId}:{amount}");
            }

            foreach (var (ticker, amount) in myReward.FavReward)
            {
                rewardData.Add($"{ticker}:{amount}");
            }

            return new AdventureBossClaimRewardModel
            {
                Id = claim.Id.ToString(),
                BlockIndex = blockIndex,
                AvatarAddress = claim.AvatarAddress.ToString(),
                ClaimedSeason = string.Join(",", claimedSeasonList),
                NcgReward = Convert.ToDecimal(myReward.NcgReward?.GetQuantityString()),
                RewardData = string.Join(",", rewardData),
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
