namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class ItemEnhancementData
    {
        public static ItemEnhancementModel GetItemEnhancementInfo(
            ItemEnhancement itemEnhancement,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex
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

            var itemenhancementModel = new ItemEnhancementModel()
            {
                Id = itemEnhancement.Id.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = itemEnhancement.avatarAddress.ToString(),
                ItemId = itemEnhancement.itemId.ToString(),
                MaterialId = itemEnhancement.materialId.ToString(),
                SlotIndex = itemEnhancement.slotIndex,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                BlockIndex = blockIndex,
            };

            return itemenhancementModel;
        }
    }
}
