namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models;

    public static class ItemEnhancementData
    {
        public static ItemEnhancementModel GetItemEnhancementInfo(
            ActionBase.ActionEvaluation<ItemEnhancement> ev,
            ItemEnhancement itemEnhancement
        )
        {
            Currency ncgCurrency = ev.OutputStates.GetGoldCurrency();
            var prevNCGBalance = ev.PreviousStates.GetBalance(
                ev.Signer,
                ncgCurrency);
            var outputNCGBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;

            var itemenhancementModel = new ItemEnhancementModel()
            {
                Id = itemEnhancement.Id.ToString(),
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = itemEnhancement.avatarAddress.ToString(),
                ItemId = itemEnhancement.itemId.ToString(),
                MaterialId = itemEnhancement.materialId.ToString(),
                SlotIndex = itemEnhancement.slotIndex,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                BlockIndex = ev.BlockIndex,
            };

            return itemenhancementModel;
        }
    }
}
