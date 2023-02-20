namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class ItemEnhancementFailData
    {
        public static ItemEnhancementFailModel? GetItemEnhancementFailInfo(
            ActionBase.ActionEvaluation<ItemEnhancement> ev,
            ItemEnhancement itemEnhancement,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(itemEnhancement.avatarAddress);
            var previousStates = ev.PreviousStates;
            AvatarState prevAvatarState = previousStates.GetAvatarStateV2(itemEnhancement.avatarAddress);

            int prevEquipmentLevel = 0;
            if (prevAvatarState.inventory.TryGetNonFungibleItem(itemEnhancement.itemId, out ItemUsable prevEnhancementItem)
                && prevEnhancementItem is Equipment prevEnhancementEquipment)
            {
                prevEquipmentLevel = prevEnhancementEquipment.level;
            }

            int outputEquipmentLevel = 0;
            if (avatarState.inventory.TryGetNonFungibleItem(itemEnhancement.itemId, out ItemUsable outputEnhancementItem)
                && outputEnhancementItem is Equipment outputEnhancementEquipment)
            {
                outputEquipmentLevel = outputEnhancementEquipment.level;
            }

            Currency ncgCurrency = ev.OutputStates.GetGoldCurrency();
            var prevNCGBalance = previousStates.GetBalance(
                ev.Signer,
                ncgCurrency);
            var outputNCGBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;

            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = ev.PreviousStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var outputCrystalBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var gainedCrystal = outputCrystalBalance - prevCrystalBalance;

            if (prevEquipmentLevel == outputEquipmentLevel)
            {
                return new ItemEnhancementFailModel()
                {
                    Id = itemEnhancement.Id.ToString(),
                    BlockIndex = ev.BlockIndex,
                    AgentAddress = ev.Signer.ToString(),
                    AvatarAddress = itemEnhancement.avatarAddress.ToString(),
                    EquipmentItemId = itemEnhancement.itemId.ToString(),
                    MaterialItemId = itemEnhancement.materialId.ToString(),
                    EquipmentLevel = outputEquipmentLevel,
                    GainedCrystal = Convert.ToDecimal(gainedCrystal.GetQuantityString()),
                    BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                    TimeStamp = blockTime,
                };
            }

            return null;
        }
    }
}
