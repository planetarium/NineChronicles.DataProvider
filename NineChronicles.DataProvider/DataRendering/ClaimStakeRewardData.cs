namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store.Models;
    using static Lib9c.SerializeKeys;

    public static class ClaimStakeRewardData
    {
        public static ClaimStakeRewardModel GetClaimStakeRewardInfo(
            ActionBase.ActionEvaluation<ActionBase> ev,
            IClaimStakeReward claimStakeReward,
            DateTimeOffset blockTime
        )
        {
            var plainValue = (Bencodex.Types.Dictionary)claimStakeReward.PlainValue;
            var avatarAddress = plainValue[AvatarAddressKey].ToAddress();
            var id = ((GameAction)claimStakeReward).Id;
            ev.PreviousStates.TryGetStakeState(ev.Signer, out StakeState prevStakeState);

            var claimStakeStartBlockIndex = prevStakeState.StartedBlockIndex;
            var claimStakeEndBlockIndex = prevStakeState.ReceivedBlockIndex;
            var currency = ev.OutputStates.GetGoldCurrency();
            var stakeStateAddress = StakeState.DeriveAddress(ev.Signer);
            var stakedAmount = ev.OutputStates.GetBalance(stakeStateAddress, currency);

            var sheets = ev.PreviousStates.GetSheets(
                typeof(StakeRegularRewardSheet),
                typeof(ConsumableItemSheet),
                typeof(CostumeItemSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet));
            StakeRegularRewardSheet stakeRegularRewardSheet = sheets.GetSheet<StakeRegularRewardSheet>();
            int level = stakeRegularRewardSheet.FindLevelByStakedAmount(ev.Signer, stakedAmount);
            var rewards = stakeRegularRewardSheet[level].Rewards;
            var accumulatedRewards = prevStakeState.CalculateAccumulatedRewards(ev.BlockIndex);
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

            if (ev.PreviousStates.TryGetSheet<StakeRegularFixedRewardSheet>(
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
                BlockIndex = ev.BlockIndex,
                AgentAddress = ev.Signer.ToString(),
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
