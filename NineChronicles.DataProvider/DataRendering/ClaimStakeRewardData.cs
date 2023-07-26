namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.State;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
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
            var plainValue = (Dictionary)claimStakeReward.PlainValue;
            var avatarAddress = ((Dictionary)plainValue["values"])[AvatarAddressKey].ToAddress();
            var id = ((GameAction)claimStakeReward).Id;
            previousStates.TryGetStakeState(signer, out StakeState prevStakeState);

            var claimStakeStartBlockIndex = prevStakeState.StartedBlockIndex;
            var claimStakeEndBlockIndex = prevStakeState.ReceivedBlockIndex;
            var avatarPreviousState = previousStates.GetAvatarStateV2(avatarAddress);
            var avatarOutputState = outputStates.GetAvatarStateV2(avatarAddress);

            var previousApPotionCount = 0;
            var previousHourGlassCount = 0;
            var outputApPotionCount = 0;
            var outputHourGlassCount = 0;

            var previousApPotion = avatarPreviousState.inventory.Items
                .FirstOrDefault(x => x.item.ItemSubType == ItemSubType.ApStone);
            var previousHourGlass = avatarPreviousState.inventory.Items
                .FirstOrDefault(x => x.item.ItemSubType == ItemSubType.Hourglass);
            if (previousApPotion != null)
            {
                previousApPotionCount = previousApPotion.count;
            }

            if (previousHourGlass != null)
            {
                previousHourGlassCount = previousHourGlass.count;
            }

            var outputApPotion = avatarOutputState.inventory.Items
                .FirstOrDefault(x => x.item.ItemSubType == ItemSubType.ApStone);
            var outputHourGlass = avatarOutputState.inventory.Items
                .FirstOrDefault(x => x.item.ItemSubType == ItemSubType.Hourglass);

            if (outputApPotion != null)
            {
                outputApPotionCount = outputApPotion.count;
            }

            if (outputHourGlass != null)
            {
                outputHourGlassCount = outputHourGlass.count;
            }

            var claimStakeRewardModel = new ClaimStakeRewardModel
            {
                Id = id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                ClaimRewardAvatarAddress = avatarAddress.ToString(),
                HourGlassCount = outputHourGlassCount - previousHourGlassCount,
                ApPotionCount = outputApPotionCount - previousApPotionCount,
                ClaimStakeStartBlockIndex = claimStakeStartBlockIndex,
                ClaimStakeEndBlockIndex = claimStakeEndBlockIndex,
                TimeStamp = blockTime,
            };

            return claimStakeRewardModel;
        }
    }
}
