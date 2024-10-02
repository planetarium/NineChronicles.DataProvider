namespace NineChronicles.DataProvider.DataRendering.Crafting
{
    using System;
    using System.Collections.Generic;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store.Models.Crafting;

    public static class UnlockCombinationSlotData
    {
        private const int GoldenDustId = 600201;
        private const int RubyDustId = 600202;

        public static UnlockCombinationSlotModel GetUnlockCombinationSlotInfo(
            IWorld prevStates,
            Address signer,
            UnlockCombinationSlot action,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var cost = prevStates.GetSheet<UnlockCombinationSlotCostSheet>()[action.SlotIndex];
            var materialCost = new List<string>();
            if (cost.GoldenDustPrice > 0)
            {
                materialCost.Add($"{GoldenDustId}:{cost.GoldenDustPrice}");
            }

            if (cost.RubyDustPrice > 0)
            {
                materialCost.Add($"{RubyDustId}:{cost.RubyDustPrice}");
            }

            return new UnlockCombinationSlotModel
            {
                Id = Guid.NewGuid().ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = action.AvatarAddress.ToString(),
                SlotIndex = action.SlotIndex,
                NcgCost = (decimal)cost.NcgPrice,
                CrystalCost = cost.CrystalPrice,
                MaterialCosts = string.Join(",", materialCost),
                BlockIndex = blockIndex,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
