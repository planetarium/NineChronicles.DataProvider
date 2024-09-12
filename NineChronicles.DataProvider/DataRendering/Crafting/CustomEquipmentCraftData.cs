namespace NineChronicles.DataProvider.DataRendering.Crafting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    using Serilog;

    public static class CustomEquipmentCraftData
    {
        public static List<CustomEquipmentCraftModel> GetCraftInfo(
            IWorld prevStates,
            IWorld outputStates,
            long blockIndex,
            DateTimeOffset blockTime,
            CustomEquipmentCraft craftData
        )
        {
            Log.Verbose($"[CustomEquipmentCraft] GetCraftData");

            Dictionary<Type, (Address, ISheet)> sheets = prevStates.GetSheets(sheetTypes: new[]
            {
                typeof(CustomEquipmentCraftRecipeSheet),
                typeof(CustomEquipmentCraftRelationshipSheet),
                typeof(CustomEquipmentCraftIconSheet),
                typeof(CustomEquipmentCraftCostSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet),
            });

            var craftList = new List<CustomEquipmentCraftModel>();
            var i = 0;
            var guid = Guid.NewGuid().ToString();

            foreach (var craft in craftData.CraftList)
            {
                var equipment = (Equipment)outputStates.GetAllCombinationSlotState(craftData.AvatarAddress)
                    .GetSlot(craft.SlotIndex).Result!.itemUsable!;

                var relationship = prevStates.GetRelationship(craftData.AvatarAddress);
                var recipeSheet = sheets.GetSheet<CustomEquipmentCraftRecipeSheet>();
                var recipeRow = recipeSheet[craft.RecipeId];
                var relationshipRow = sheets.GetSheet<CustomEquipmentCraftRelationshipSheet>()
                    .OrderedList!.First(row => row.Relationship >= relationship);
                var (ncgCost, materialCosts) = CustomCraftHelper.CalculateCraftCost(
                    craft.IconId,
                    sheets.GetSheet<MaterialItemSheet>(),
                    recipeRow,
                    relationshipRow,
                    sheets.GetSheet<CustomEquipmentCraftCostSheet>().Values
                        .FirstOrDefault(r => r.Relationship == relationship),
                    prevStates.GetGameConfigState().CustomEquipmentCraftIconCostMultiplier
                );

                var drawingAmount = 0;
                var drawingToolAmount = 0;
                var sb = new List<string>();
                var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
                var drawingItemId = materialItemSheet.OrderedList!
                    .First(row => row.ItemSubType == ItemSubType.Scroll).Id;
                var drawingToolItemId = materialItemSheet.OrderedList!
                    .First(row => row.ItemSubType == ItemSubType.Circle).Id;

                foreach (var (itemId, amount) in materialCosts)
                {
                    if (itemId == drawingItemId)
                    {
                        drawingAmount = amount;
                    }
                    else if (itemId == drawingToolItemId)
                    {
                        drawingToolAmount = amount;
                    }
                    else
                    {
                        sb.Add($"{itemId}:{amount}");
                    }
                }

                craftList.Add(new CustomEquipmentCraftModel
                {
                    Id = $"{guid}_{i++}",
                    BlockIndex = blockIndex,
                    AvatarAddress = craftData.AvatarAddress.ToString(),
                    EquipmentItemId = equipment.Id,
                    RecipeId = craft.RecipeId,
                    SlotIndex = craft.SlotIndex,
                    ItemSubType = equipment.ItemSubType.ToString(),
                    IconId = equipment.IconId,
                    ElementalType = equipment.ElementalType.ToString(),
                    DrawingAmount = drawingAmount,
                    DrawingToolAmount = drawingToolAmount,
                    NcgCost = (decimal)ncgCost,
                    AdditionalCost = string.Join(",", sb),
                    Date = DateOnly.FromDateTime(blockTime.DateTime),
                    TimeStamp = blockTime,
                });
            }

            return craftList;
        }
    }
}
