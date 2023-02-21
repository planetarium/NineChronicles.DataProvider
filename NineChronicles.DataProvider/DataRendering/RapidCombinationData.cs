namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class RapidCombinationData
    {
        public static RapidCombinationModel GetRapidCombinationInfo(
            RapidCombination rapidCombination,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var states = previousStates;
            var slotState = states.GetCombinationSlotState(rapidCombination.avatarAddress, rapidCombination.slotIndex);
            var diff = slotState.Result.itemUsable.RequiredBlockIndex - blockIndex;
            var gameConfigState = states.GetGameConfigState();
            var count = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
            var rapidCombinationModel = new RapidCombinationModel()
            {
                Id = rapidCombination.Id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = rapidCombination.avatarAddress.ToString(),
                SlotIndex = rapidCombination.slotIndex,
                HourglassCount = count,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return rapidCombinationModel;
        }
    }
}
