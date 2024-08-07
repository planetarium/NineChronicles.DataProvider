namespace NineChronicles.DataProvider.DataRendering.CustomCraft
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
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Item;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.CustomEquipmentCraft;
    using NineChronicles.DataProvider.Store.Models.CustomCraft;
    using Serilog;

    public static class CustomEquipmentCraftData
    {
        public static List<CustomEquipmentCraftModel> GetCraftInfo(
            IWorld prevStates,
            long blockIndex,
            DateTimeOffset blockTime,
            IRandom random,
            CustomEquipmentCraft craftData
        )
        {
            Log.Verbose($"[CustomEquipmentCraft] GetCraftData");
            var craftList = new List<CustomEquipmentCraftModel>();

            var i = 0;
            foreach (var craft in craftData.CraftList)
            {
                var guid = Guid.NewGuid().ToString();

                Dictionary<Type, (Address, ISheet)> sheets = prevStates.GetSheets(sheetTypes: new[]
                {
                    typeof(CustomEquipmentCraftRecipeSheet),
                    typeof(CustomEquipmentCraftRelationshipSheet),
                    typeof(CustomEquipmentCraftIconSheet),
                    typeof(CustomEquipmentCraftCostSheet),
                    typeof(EquipmentItemSheet),
                    typeof(MaterialItemSheet),
                });

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
                    .First(row => row.ItemSubType == ItemSubType.Drawing).Id;
                var drawingToolItemId = materialItemSheet.OrderedList!
                    .First(row => row.ItemSubType == ItemSubType.DrawingTool).Id;

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

                // Create equipment with ItemFactory
                var uid = random.GenerateRandomGuid();
                var equipmentItemId = relationshipRow.GetItemId(recipeRow.ItemSubType);
                var equipmentRow = sheets.GetSheet<EquipmentItemSheet>()[equipmentItemId];
                var equipment =
                    (Equipment)ItemFactory.CreateItemUsable(equipmentRow, uid, 0L);

                // Set Icon
                equipment.IconId = ItemFactory.SelectIconId(
                    craft.IconId,
                    craft.IconId == CustomEquipmentCraft.RandomIconId,
                    equipmentRow,
                    relationship,
                    sheets.GetSheet<CustomEquipmentCraftIconSheet>(),
                    random
                );

                // Set Elemental Type
                var elementalList = (ElementalType[])Enum.GetValues(typeof(ElementalType));
                equipment.ElementalType = elementalList[random.Next(elementalList.Length)];

                craftList.Add(new CustomEquipmentCraftModel
                {
                    Id = $"{guid}_{i++}",
                    BlockIndex = blockIndex,
                    AvatarAddress = craftData.AvatarAddress.ToString(),
                    EquipmentItemId = 1,
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
