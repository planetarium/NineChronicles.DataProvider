namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using NineChronicles.DataProvider.Store.Models;

    public static class ReplaceCombinationEquipmentMaterialData
    {
        public static List<ReplaceCombinationEquipmentMaterialModel> GetReplaceCombinationEquipmentMaterialInfo(
            IAccount previousStates,
            IAccount outputStates,
            Address signer,
            Address avatarAddress,
            int recipeId,
            int? subRecipeId,
            bool payByCrystal,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime)
        {
            var replaceCombinationEquipmentMaterialList = new List<ReplaceCombinationEquipmentMaterialModel>();
            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
            var prevCrystalBalance = previousStates.GetBalance(
                signer,
                crystalCurrency);
            var outputCrystalBalance = outputStates.GetBalance(
                signer,
                crystalCurrency);
            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
            var requiredFungibleItems = new Dictionary<int, int>();
            Dictionary<Type, (Address, ISheet)> sheets = previousStates.GetSheets(
                sheetTypes: new[]
                {
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSheet),
                    typeof(MaterialItemSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(SkillSheet),
                    typeof(CrystalMaterialCostSheet),
                    typeof(CrystalFluctuationSheet),
                });
            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            var equipmentItemRecipeSheet = sheets.GetSheet<EquipmentItemRecipeSheet>();
            equipmentItemRecipeSheet.TryGetValue(
                recipeId,
                out var recipeRow);
            materialItemSheet.TryGetValue(recipeRow!.MaterialId, out var materialRow);
            if (requiredFungibleItems.ContainsKey(materialRow!.Id))
            {
                requiredFungibleItems[materialRow.Id] += recipeRow.MaterialCount;
            }
            else
            {
                requiredFungibleItems[materialRow.Id] = recipeRow.MaterialCount;
            }

            if (subRecipeId.HasValue)
            {
                var equipmentItemSubRecipeSheetV2 = sheets.GetSheet<EquipmentItemSubRecipeSheetV2>();
                equipmentItemSubRecipeSheetV2.TryGetValue(subRecipeId.Value, out var subRecipeRow);

                // Validate SubRecipe Material
                for (var i = subRecipeRow!.Materials.Count; i > 0; i--)
                {
                    var materialInfo = subRecipeRow.Materials[i - 1];
                    materialItemSheet.TryGetValue(materialInfo.Id, out materialRow);

                    if (requiredFungibleItems.ContainsKey(materialRow!.Id))
                    {
                        requiredFungibleItems[materialRow.Id] += materialInfo.Count;
                    }
                    else
                    {
                        requiredFungibleItems[materialRow.Id] = materialInfo.Count;
                    }
                }
            }

            var inventory = previousStates
                .GetAvatarStateV2(avatarAddress).inventory;
            foreach (var pair in requiredFungibleItems.OrderBy(pair => pair.Key))
            {
                var itemId = pair.Key;
                var requiredCount = pair.Value;
                if (materialItemSheet.TryGetValue(itemId, out materialRow))
                {
                    int itemCount = inventory.TryGetItem(itemId, out Inventory.Item item)
                        ? item.count
                        : 0;
                    if (itemCount < requiredCount && payByCrystal)
                    {
                        replaceCombinationEquipmentMaterialList.Add(
                            new ReplaceCombinationEquipmentMaterialModel()
                            {
                                Id = actionId.ToString(),
                                BlockIndex = blockIndex,
                                AgentAddress = signer.ToString(),
                                AvatarAddress =
                                    avatarAddress.ToString(),
                                ReplacedMaterialId = itemId,
                                ReplacedMaterialCount = requiredCount - itemCount,
                                BurntCrystal =
                                    Convert.ToDecimal(burntCrystal.GetQuantityString()),
                                TimeStamp = blockTime,
                            });
                    }
                }
            }

            return replaceCombinationEquipmentMaterialList;
        }
    }
}
