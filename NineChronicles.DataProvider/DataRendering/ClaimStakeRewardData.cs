namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.State;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store.Models;
    using static Lib9c.SerializeKeys;

    public static class ClaimStakeRewardData
    {
        public static ClaimStakeRewardModel GetClaimStakeRewardInfo(
            IClaimStakeReward claimStakeReward,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var plainValue = (Bencodex.Types.Dictionary)claimStakeReward.PlainValue;
            var avatarAddress = plainValue[AvatarAddressKey].ToAddress();
            var id = ((GameAction)claimStakeReward).Id;
            previousStates.TryGetStakeState(signer, out StakeState prevStakeState);

            var claimStakeStartBlockIndex = prevStakeState.StartedBlockIndex;
            var claimStakeEndBlockIndex = prevStakeState.ReceivedBlockIndex;
            var currency = outputStates.GetGoldCurrency();
            var stakeStateAddress = StakeState.DeriveAddress(signer);
            var stakedAmount = outputStates.GetBalance(stakeStateAddress, currency);

            var sheets = previousStates.GetSheets(
                typeof(StakeRegularRewardSheet),
                typeof(ConsumableItemSheet),
                typeof(CostumeItemSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet));
            StakeRegularRewardSheet stakeRegularRewardSheet = sheets.GetSheet<StakeRegularRewardSheet>();
            int level = stakeRegularRewardSheet.FindLevelByStakedAmount(signer, stakedAmount);
            var rewards = stakeRegularRewardSheet[level].Rewards;
            var accumulatedRewards = prevStakeState.CalculateAccumulatedRewards(blockIndex);
            int hourGlassCount = 0;
            int apPotionCount = 0;
            foreach (var reward in rewards)
            {
                var (quantity, _) = stakedAmount.DivRem(currency * reward.Rate);
                if (quantity < 1)
                {
                    // If the quantity is zero, it doesn't add the item into inventory.
                    continue;
                }

                if (reward.ItemId == 400000)
                {
                    hourGlassCount += (int)quantity * accumulatedRewards;
                }

                if (reward.ItemId == 500000)
                {
                    apPotionCount += (int)quantity * accumulatedRewards;
                }
            }

            if (previousStates.TryGetSheet<StakeRegularFixedRewardSheet>(
                    out var stakeRegularFixedRewardSheet))
            {
                var fixedRewards = stakeRegularFixedRewardSheet[level].Rewards;
                foreach (var reward in fixedRewards)
                {
                    if (reward.ItemId == 400000)
                    {
                        hourGlassCount += reward.Count * accumulatedRewards;
                    }

                    if (reward.ItemId == 500000)
                    {
                        apPotionCount += reward.Count * accumulatedRewards;
                    }
                }
            }

            var claimStakeRewardModel = new ClaimStakeRewardModel
            {
                Id = id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                ClaimRewardAvatarAddress = avatarAddress.ToString(),
                HourGlassCount = hourGlassCount,
                ApPotionCount = apPotionCount,
                ClaimStakeStartBlockIndex = claimStakeStartBlockIndex,
                ClaimStakeEndBlockIndex = claimStakeEndBlockIndex,
                TimeStamp = blockTime,
            };

            return claimStakeRewardModel;
        }
    }
}
