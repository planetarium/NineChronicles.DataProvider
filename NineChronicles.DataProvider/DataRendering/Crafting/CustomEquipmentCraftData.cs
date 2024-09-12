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
    using Nekoyume.Model.Item;
    using Nekoyume.Module;
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
            });
            var recipeSheet = sheets.GetSheet<CustomEquipmentCraftRecipeSheet>();
            var relationshipSheet = sheets.GetSheet<CustomEquipmentCraftRelationshipSheet>();
            var optionSheet = sheets.GetSheet<CustomEquipmentCraftOptionSheet>();

            for (var i = 0; i < action.CraftList.Count; i++)
            {
                var craftData = action.CraftList[i];
                var gameConfig = prevState.GetGameConfigState();
                var recipeRow = recipeSheet.OrderedList!.First(row => row.Id == craftData.RecipeId);
                var relationshipRow = relationshipSheet.OrderedList!.Reverse()
                    .First(row => row.Relationship <= relationship);
                var slot = outputCombinationSlots.GetSlot(craftData.SlotIndex);
                var equipment = (Equipment)slot.Result.itemUsable;

                var circleCost = (decimal)recipeRow.CircleAmount * relationshipRow.CostMultiplier / 10000m;
                if (craftData.RecipeId == 0)
                {
                    // Random
                    circleCost = circleCost * gameConfig.CustomEquipmentCraftIconCostMultiplier / 10000m;
                }

                info.Add(new CustomEquipmentCraftModel
                    {
                        Id = $"{actionId}_{i}",
                        AgentAddress = signer.ToString(),
                        AvatarAddress = action.AvatarAddress.ToString(),
                        RecipeId = craftData.RecipeId,
                        SlotIndex = craftData.SlotIndex,
                        Scroll = (int)Math.Floor(recipeRow.ScrollAmount * relationshipRow.CostMultiplier / 10000m),
                        Circle = (int)Math.Floor(circleCost),
                        AdditionalMaterials = string.Empty,
                        Relationship = relationship,
                        EquipmentId = equipment.Id,
                        IconId = equipment.IconId,
                        ElementalType = equipment.ElementalType.ToString(),
                        OptionId = ItemFactory.SelectOption(recipeRow.ItemSubType, optionSheet, random).Id,
                        TotalCP = random.Next(relationshipRow.MinCp, relationshipRow.MaxCp + 1),
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
