namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;
    using static Lib9c.SerializeKeys;

    public static class ClaimStakeRewardData
    {
        public static ClaimStakeRewardModel GetClaimStakeRewardInfo(
            IClaimStakeReward claimStakeReward,
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            long claimStakeStartBlockIndex;
            long claimStakeEndBlockIndex;
            if (previousStates.TryGetStakeState(signer, out var prevStakeStateV2))
            {
                claimStakeStartBlockIndex = prevStakeStateV2.StartedBlockIndex;
                claimStakeEndBlockIndex = prevStakeStateV2.ReceivedBlockIndex;
            }
            else
            {
                claimStakeStartBlockIndex = 0;
                claimStakeEndBlockIndex = 0;
            }

            var plainValue = (Dictionary)claimStakeReward.PlainValue;
            var avatarAddress = ((Dictionary)plainValue["values"])[AvatarAddressKey].ToAddress();

            var previousAvatarState = previousStates.GetAvatarState(avatarAddress);
            var previousApPotionCount = previousAvatarState.inventory.Items
                .Where(x => x.item.ItemSubType == ItemSubType.ApStone)
                .Sum(potion => potion.count);
            var previousHourGlassCount = previousAvatarState.inventory.Items
                .Where(x => x.item.ItemSubType == ItemSubType.Hourglass)
                .Sum(hourGlass => hourGlass.count);

            var outputAvatarState = outputStates.GetAvatarState(avatarAddress);
            var outputApPotionCount = outputAvatarState.inventory.Items
                .Where(x => x.item.ItemSubType == ItemSubType.ApStone)
                .Sum(potion => potion.count);
            var outputHourGlassCount = outputAvatarState.inventory.Items
                .Where(x => x.item.ItemSubType == ItemSubType.Hourglass)
                .Sum(hourGlass => hourGlass.count);

            var claimStakeRewardModel = new ClaimStakeRewardModel
            {
                Id = ((GameAction)claimStakeReward).Id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                ClaimRewardAvatarAddress = avatarAddress.ToString(),
                HourGlassCount = outputHourGlassCount - previousHourGlassCount,
                ApPotionCount = outputApPotionCount - previousApPotionCount,
                ClaimStakeStartBlockIndex = claimStakeStartBlockIndex,
                ClaimStakeEndBlockIndex = claimStakeEndBlockIndex,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return claimStakeRewardModel;
        }
    }
}
