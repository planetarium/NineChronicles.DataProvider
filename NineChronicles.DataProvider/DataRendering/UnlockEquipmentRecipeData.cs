namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Helper;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;

    public static class UnlockEquipmentRecipeData
    {
        public static List<UnlockEquipmentRecipeModel> GetUnlockEquipmentRecipeInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            List<int> recipeIds,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                signer,
                crystalCurrency);
            var outputCrystalBalance = outputStates.GetBalance(
                signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var unlockEquipmentRecipeList = new List<UnlockEquipmentRecipeModel>();
            foreach (var recipeId in recipeIds)
            {
                unlockEquipmentRecipeList.Add(new UnlockEquipmentRecipeModel()
                {
                    Id = actionId.ToString(),
                    BlockIndex = blockIndex,
                    AgentAddress = signer.ToString(),
                    AvatarAddress = avatarAddress.ToString(),
                    UnlockEquipmentRecipeId = recipeId,
                    BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                    Date = DateOnly.FromDateTime(blockTime.DateTime),
                    TimeStamp = blockTime,
                });
            }

            return unlockEquipmentRecipeList;
        }
    }
}
