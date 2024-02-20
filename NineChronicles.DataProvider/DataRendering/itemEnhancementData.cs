namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;

    public static class ItemEnhancementData
    {
        public static ItemEnhancementModel GetItemEnhancementInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            int slotIndex,
            Guid materialId,
            List<Guid> materialIds,
            Guid itemId,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            Currency ncgCurrency = outputStates.GetGoldCurrency();
            var prevNCGBalance = previousStates.GetBalance(
                signer,
                ncgCurrency);
            var outputNCGBalance = outputStates.GetBalance(
                signer,
                ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;

            var equipment = outputStates.GetAvatarState(avatarAddress).inventory.Equipments
                .First(e => e.ItemId == itemId);

            var itemEnhancementModel = new ItemEnhancementModel()
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                ItemId = itemId.ToString(),
                MaterialId = materialId.ToString(),
                MaterialIdsCount = materialIds.Count,
                SlotIndex = slotIndex,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                BlockIndex = blockIndex,
                SheetId = equipment.Id,
                Level = equipment.level,
                Exp = equipment.Exp,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };

            return itemEnhancementModel;
        }
    }
}
