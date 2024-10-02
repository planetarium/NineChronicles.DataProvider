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

            bool continueInv;
            bool continueExp;

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

                // Skip not participated season
                if (investor is null && explorer is null)
                {
                    continue;
                }

                if (investor is not null)
                {
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
                else
                {
                    continueInv = false;
                }

                if (explorer is not null)
                {
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
                else
                {
                    continueExp = false;
                }

                // Stop if stop both investor and explorer
                if (!continueInv && !continueExp)
                {
                    break;
                }

                // Add claimed season if not breaking loop
                if (!claimedSeasonList.Contains(szn))
                {
                    claimedSeasonList.Add(szn);
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
