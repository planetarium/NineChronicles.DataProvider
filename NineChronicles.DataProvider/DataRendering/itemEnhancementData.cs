namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.State;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class ItemEnhancementData
    {
        public static ItemEnhancementModel GetItemEnhancementInfo(
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            Address avatarAddress,
            int slotIndex,
            Guid materialId,
            Guid itemId,
            Guid actionId,
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
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                ItemId = itemId.ToString(),
                MaterialId = materialId.ToString(),
                SlotIndex = slotIndex,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                BlockIndex = blockIndex,
            };

            return itemenhancementModel;
        }
    }
}
