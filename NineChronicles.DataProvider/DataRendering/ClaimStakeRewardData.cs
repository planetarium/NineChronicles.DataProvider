namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;
    using static Lib9c.SerializeKeys;

    public static class ClaimStakeRewardData
    {
        public static ClaimStakeRewardModel GetClaimStakeRewardInfo(
            IClaimStakeReward claimStakeReward,
            IAccount previousStates,
            IAccount outputStates,
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

            var previousApPotions =
                avatarPreviousState.inventory.Items.Where(x => x.item.ItemSubType == ItemSubType.ApStone);
            var previousHourGlasses =
                avatarPreviousState.inventory.Items.Where(x => x.item.ItemSubType == ItemSubType.Hourglass);
            foreach (var potion in previousApPotions)
            {
                previousApPotionCount += potion.count;
            }

            foreach (var hourGlass in previousHourGlasses)
            {
                previousHourGlassCount += hourGlass.count;
            }

            var outputApPotions =
                avatarOutputState.inventory.Items.Where(x => x.item.ItemSubType == ItemSubType.ApStone);
            var outputHourGlasses =
                avatarOutputState.inventory.Items.Where(x => x.item.ItemSubType == ItemSubType.Hourglass);

            foreach (var potion in outputApPotions)
            {
                outputApPotionCount += potion.count;
            }

            foreach (var hourGlass in outputHourGlasses)
            {
                outputHourGlassCount += hourGlass.count;
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
