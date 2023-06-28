namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
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
            prevStakeState.CalculateAccumulatedItemRewards(
                blockIndex,
                out var itemV1Step,
                out var itemV2Step);

            int hourGlassCount = 0;
            int apPotionCount = 0;
            if (itemV1Step > 0)
            {
                StakeRegularRewardSheet stakeRegularRewardSheetV1 = new StakeRegularRewardSheet();
                stakeRegularRewardSheetV1.Set(ClaimStakeReward.V1.StakeRegularRewardSheetCsv);
                StakeRegularFixedRewardSheet stakeRegularFixedRewardSheetV1 = new StakeRegularFixedRewardSheet();
                stakeRegularFixedRewardSheetV1.Set(ClaimStakeReward.V1.StakeRegularFixedRewardSheetCsv);
                var (hourGlass, apPotion) = CalculateItemCount(
                    stakedAmount,
                    currency,
                    level,
                    itemV1Step,
                    stakeRegularRewardSheetV1,
                    stakeRegularFixedRewardSheetV1
                );
                hourGlassCount += hourGlass;
                apPotionCount += apPotion;
            }

            if (itemV2Step > 0)
            {
                if (!previousStates.TryGetSheet<StakeRegularFixedRewardSheet>(out var stakeRegularFixedRewardSheet))
                {
                    stakeRegularFixedRewardSheet = new StakeRegularFixedRewardSheet();
                }

                var (hourGlass, apPotion) = CalculateItemCount(
                    stakedAmount,
                    currency,
                    level,
                    itemV2Step,
                    stakeRegularRewardSheet,
                    stakeRegularFixedRewardSheet
                );
                hourGlassCount += hourGlass;
                apPotionCount += apPotion;
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

        private static (int hourGlassCount, int apPotionCount) CalculateItemCount(
            FungibleAssetValue stakedAmount,
            Currency currency,
            int level,
            int itemRewardStep,
            StakeRegularRewardSheet stakeRegularRewardSheet,
            StakeRegularFixedRewardSheet stakeRegularFixedRewardSheet
        )
        {
            var rewards = stakeRegularRewardSheet[level].Rewards;
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
                    hourGlassCount += (int)quantity * itemRewardStep;
                }

                if (reward.ItemId == 500000)
                {
                    apPotionCount += (int)quantity * itemRewardStep;
                }
            }

            if (stakeRegularFixedRewardSheet.TryGetValue(level, out var row))
            {
                var fixedRewards = row.Rewards;
                foreach (var reward in fixedRewards)
                {
                    if (reward.ItemId == 400000)
                    {
                        hourGlassCount += reward.Count * itemRewardStep;
                    }

                    if (reward.ItemId == 500000)
                    {
                        apPotionCount += reward.Count * itemRewardStep;
                    }
                }
            }

            return (hourGlassCount, apPotionCount);
        }
    }
}
