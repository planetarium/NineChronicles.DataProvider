namespace NineChronicles.DataProvider.DataRendering.Crafting
{
    using System;
    using System.Collections.Generic;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models.Crafting;

    public static class RapidCombinationData
    {
        public static List<RapidCombinationModel> GetRapidCombinationInfo(
            IWorld previousStates,
            Address signer,
            Address avatarAddress,
            List<int> slotIndexList,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var states = previousStates;
            var slotStates = states.GetAllCombinationSlotState(avatarAddress);
            var gameConfigState = states.GetGameConfigState();
            var combinationList = new List<RapidCombinationModel>();
            for (var i = 0; i < slotIndexList.Count; i++)
            {
                var slotIndex = slotIndexList[i];
                var slotState = slotStates.GetSlot(slotIndex);
                var diff = slotState.Result.itemUsable.RequiredBlockIndex - blockIndex;
                var count = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
                combinationList.Add(new RapidCombinationModel
                {
                    Id = $"{actionId}_{i}",
                    BlockIndex = blockIndex,
                    AgentAddress = signer.ToString(),
                    AvatarAddress = avatarAddress.ToString(),
                    SlotIndex = slotIndex,
                    HourglassCount = count,
                    Date = DateOnly.FromDateTime(blockTime.DateTime),
                    TimeStamp = blockTime,
                });
            }

            return combinationList;
        }
    }
}
