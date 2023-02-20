namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class RapidCombinationData
    {
        public static RapidCombinationModel GetRapidCombinationInfo(
            ActionBase.ActionEvaluation<RapidCombination> ev,
            RapidCombination rapidCombination,
            DateTimeOffset blockTime
        )
        {
            var states = ev.PreviousStates;
            var slotState = states.GetCombinationSlotState(rapidCombination.avatarAddress, rapidCombination.slotIndex);
            var diff = slotState.Result.itemUsable.RequiredBlockIndex - ev.BlockIndex;
            var gameConfigState = states.GetGameConfigState();
            var count = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
            var rapidCombinationModel = new RapidCombinationModel()
            {
                Id = rapidCombination.Id.ToString(),
                BlockIndex = ev.BlockIndex,
                AgentAddress = ev.Signer.ToString(),
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
