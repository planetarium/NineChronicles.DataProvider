namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using NineChronicles.DataProvider.Store.Models;

    public static class UnlockEquipmentRecipeData
    {
        public static List<UnlockEquipmentRecipeModel> GetUnlockEquipmentRecipeInfo(
            ActionBase.ActionEvaluation<UnlockEquipmentRecipe> ev,
            UnlockEquipmentRecipe unlockEquipmentRecipe,
            DateTimeOffset blockTime
        )
        {
            var previousStates = ev.PreviousStates;
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var outputCrystalBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var unlockEquipmentRecipeList = new List<UnlockEquipmentRecipeModel>();
            foreach (var recipeId in unlockEquipmentRecipe.RecipeIds)
            {
                unlockEquipmentRecipeList.Add(new UnlockEquipmentRecipeModel()
                {
                    Id = unlockEquipmentRecipe.Id.ToString(),
                    BlockIndex = ev.BlockIndex,
                    AgentAddress = ev.Signer.ToString(),
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
