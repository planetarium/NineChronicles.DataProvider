namespace NineChronicles.DataProvider.DataRendering.Crafting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action.CustomEquipmentCraft;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.CustomEquipmentCraft;
    using NineChronicles.DataProvider.Store.Models.Crafting;

    public static class CustomEquipmentCraftData
    {
        public static List<CustomEquipmentCraftModel> GetCustomEquipmentCraftInfo(
            IWorld prevState,
            IWorld outputState,
            IRandom random,
            Address signer,
            Guid actionId,
            CustomEquipmentCraft action,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var info = new List<CustomEquipmentCraftModel>();
            var outputCombinationSlots = outputState.GetAllCombinationSlotState(action.AvatarAddress);
            var relationship = prevState.GetRelationship(action.AvatarAddress);
            var sheets = prevState.GetSheets(sheetTypes: new List<Type>
            {
                typeof(CustomEquipmentCraftRecipeSheet),
                typeof(CustomEquipmentCraftRelationshipSheet),
                typeof(CustomEquipmentCraftOptionSheet),
                typeof(MaterialItemSheet),
            });
            var recipeSheet = sheets.GetSheet<CustomEquipmentCraftRecipeSheet>();
            var relationshipSheet = sheets.GetSheet<CustomEquipmentCraftRelationshipSheet>();
            var optionSheet = sheets.GetSheet<CustomEquipmentCraftOptionSheet>();
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            var gameConfig = prevState.GetGameConfigState();

            for (var i = 0; i < action.CraftList.Count; i++)
            {
                var craftData = action.CraftList[i];
                var recipeRow = recipeSheet.OrderedList!.First(row => row.Id == craftData.RecipeId);
                var relationshipRow = relationshipSheet.OrderedList!.Reverse()
                    .First(row => row.Relationship <= relationship);
                var slot = outputCombinationSlots.GetSlot(craftData.SlotIndex);
                var equipment = (Equipment)slot.Result.itemUsable;
                var (ncgCost, materialCosts) = CustomCraftHelper.CalculateCraftCost(
                    craftData.IconId,
                    relationship,
                    sheets.GetSheet<MaterialItemSheet>(),
                    recipeRow,
                    relationshipRow,
                    gameConfig.CustomEquipmentCraftIconCostMultiplier
                );

                var scrollId = materialSheet.Values.First(row => row.ItemSubType == ItemSubType.Scroll).Id;
                var circleId = materialSheet.Values.First(row => row.ItemSubType == ItemSubType.Circle).Id;
                var scrollCost = 0;
                var circleCost = 0;
                var additional = new List<string>();

                foreach (var cost in materialCosts)
                {
                    if (cost.Key == scrollId)
                    {
                        scrollCost = cost.Value;
                    }
                    else if (cost.Key == circleId)
                    {
                        circleCost = cost.Value;
                    }
                    else
                    {
                        additional.Add($"{cost.Key}:{cost.Value}");
                    }
                }

                info.Add(new CustomEquipmentCraftModel
                    {
                        Id = $"{actionId}_{i}",
                        AgentAddress = signer.ToString(),
                        AvatarAddress = action.AvatarAddress.ToString(),
                        RecipeId = craftData.RecipeId,
                        SlotIndex = craftData.SlotIndex,
                        Scroll = scrollCost,
                        Circle = circleCost,
                        NcgCost = (decimal)ncgCost,
                        AdditionalCost = string.Join(",", additional),
                        Relationship = relationship,
                        ItemSubType = equipment.ItemSubType.ToString(),
                        EquipmentItemId = equipment.Id,
                        IconId = equipment.IconId,
                        ElementalType = equipment.ElementalType.ToString(),
                        OptionId = ItemFactory.SelectOption(recipeRow.ItemSubType, optionSheet, random).Id,
                        TotalCP = CustomCraftHelper.SelectCp(relationshipRow, random),
                        CraftWithRandom = equipment.CraftWithRandom,
                        HasRandomOnlyIcon = equipment.HasRandomOnlyIcon,
                        BlockIndex = blockIndex,
                        Date = DateOnly.FromDateTime(blockTime.DateTime),
                        TimeStamp = blockTime.UtcDateTime,
                    }
                );

                relationship++;
            }

            return info;
        }
    }
}
