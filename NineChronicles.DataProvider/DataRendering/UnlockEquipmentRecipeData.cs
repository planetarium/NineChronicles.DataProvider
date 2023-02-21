namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using NineChronicles.DataProvider.Store.Models;

    public static class UnlockEquipmentRecipeData
    {
        public static List<UnlockEquipmentRecipeModel> GetUnlockEquipmentRecipeInfo(
            UnlockEquipmentRecipe unlockEquipmentRecipe,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
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
            foreach (var recipeId in unlockEquipmentRecipe.RecipeIds)
            {
                unlockEquipmentRecipeList.Add(new UnlockEquipmentRecipeModel()
                {
                    Id = unlockEquipmentRecipe.Id.ToString(),
                    BlockIndex = blockIndex,
                    AgentAddress = signer.ToString(),
                    AvatarAddress = unlockEquipmentRecipe.AvatarAddress.ToString(),
                    UnlockEquipmentRecipeId = recipeId,
                    BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                    TimeStamp = blockTime,
                });
            }

            return unlockEquipmentRecipeList;
        }
    }
}
