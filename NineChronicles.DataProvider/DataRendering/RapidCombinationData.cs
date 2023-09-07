namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class RapidCombinationData
    {
        public static RapidCombinationModel GetRapidCombinationInfo(
            IAccount previousStates,
            IAccount outputStates,
            Address signer,
            Address avatarAddress,
            int slotIndex,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var states = previousStates;
            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            var diff = slotState.Result.itemUsable.RequiredBlockIndex - blockIndex;
            var gameConfigState = states.GetGameConfigState();
            var count = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
            var rapidCombinationModel = new RapidCombinationModel()
            {
                Id = actionId.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                SlotIndex = slotIndex,
                HourglassCount = count,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return rapidCombinationModel;
        }
    }
}
