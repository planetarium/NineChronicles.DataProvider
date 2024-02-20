namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;

    public static class ItemEnhancementFailData
    {
        public static ItemEnhancementFailModel? GetItemEnhancementFailInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            Guid materialId,
            List<Guid> materialIds,
            Guid itemId,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarState(avatarAddress);
            AvatarState prevAvatarState = previousStates.GetAvatarState(avatarAddress);

            int prevEquipmentLevel = 0;
            if (prevAvatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable prevEnhancementItem)
                && prevEnhancementItem is Equipment prevEnhancementEquipment)
            {
                prevEquipmentLevel = prevEnhancementEquipment.level;
            }

            int outputEquipmentLevel = 0;
            if (avatarState.inventory.TryGetNonFungibleItem(itemId, out ItemUsable outputEnhancementItem)
                && outputEnhancementItem is Equipment outputEnhancementEquipment)
            {
                outputEquipmentLevel = outputEnhancementEquipment.level;
            }

            Currency ncgCurrency = outputStates.GetGoldCurrency();
            var prevNCGBalance = previousStates.GetBalance(
                signer,
                ncgCurrency);
            var outputNCGBalance = outputStates.GetBalance(
                signer,
                ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;

            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                signer,
                crystalCurrency);
            var outputCrystalBalance = outputStates.GetBalance(
                signer,
                crystalCurrency);
            var gainedCrystal = outputCrystalBalance - prevCrystalBalance;

            if (prevEquipmentLevel == outputEquipmentLevel)
            {
                return new ItemEnhancementFailModel()
                {
                    Id = actionId.ToString(),
                    BlockIndex = blockIndex,
                    AgentAddress = signer.ToString(),
                    AvatarAddress = avatarAddress.ToString(),
                    EquipmentItemId = itemId.ToString(),
                    MaterialItemId = materialId.ToString(),
                    MaterialIdsCount = materialIds.Count,
                    EquipmentLevel = outputEquipmentLevel,
                    GainedCrystal = Convert.ToDecimal(gainedCrystal.GetQuantityString()),
                    BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                    Date = DateOnly.FromDateTime(blockTime.DateTime),
                    TimeStamp = blockTime,
                };
            }

            return null;
        }
    }
}
