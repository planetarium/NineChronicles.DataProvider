namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using NineChronicles.DataProvider.Store.Models;

    public static class EventConsumableItemCraftsData
    {
        public static EventConsumableItemCraftsModel GetEventConsumableItemCraftsInfo(
            ActionBase.ActionEvaluation<EventConsumableItemCrafts> ev,
            EventConsumableItemCrafts eventConsumableItemCrafts,
            DateTimeOffset blockTime
        )
        {
            var previousStates = ev.PreviousStates;
            var addressesHex =
                RenderSubscriber.GetSignerAndOtherAddressesHex(ev.Signer, eventConsumableItemCrafts.AvatarAddress);
            var requiredFungibleItems = new Dictionary<int, int>();
            Dictionary<string, int> requiredItemData = new Dictionary<string, int>();
            for (var i = 1; i < 11; i++)
            {
                requiredItemData.Add($"requiredItem{i}Id", 0);
                requiredItemData.Add($"requiredItem{i}Count", 0);
            }

            int itemNumber = 1;
            var sheets = previousStates.GetSheets(
                sheetTypes: new[]
                {
                    typeof(EventScheduleSheet),
                    typeof(EventConsumableItemRecipeSheet),
                });
            var scheduleSheet = sheets.GetSheet<EventScheduleSheet>();
            scheduleSheet.ValidateFromActionForRecipe(
                ev.BlockIndex,
                eventConsumableItemCrafts.EventScheduleId,
                eventConsumableItemCrafts.EventConsumableItemRecipeId,
                "event_consumable_item_crafts",
                addressesHex);
            var recipeSheet = sheets.GetSheet<EventConsumableItemRecipeSheet>();
            var recipeRow = recipeSheet.ValidateFromAction(
                eventConsumableItemCrafts.EventConsumableItemRecipeId,
                "event_consumable_item_crafts",
                addressesHex);
            var materialItemSheet = previousStates.GetSheet<MaterialItemSheet>();
            materialItemSheet.ValidateFromAction(
                recipeRow.Materials,
                requiredFungibleItems,
                addressesHex);
            foreach (var pair in requiredFungibleItems)
            {
                if (materialItemSheet.TryGetValue(pair.Key, out var materialRow))
                {
                    requiredItemData[$"requiredItem{itemNumber}Id"] = materialRow.Id;
                    requiredItemData[$"requiredItem{itemNumber}Count"] = pair.Value;
                }

                itemNumber++;
            }

            var eventConsumableItemCraftsModel = new EventConsumableItemCraftsModel()
            {
                Id = eventConsumableItemCrafts.Id.ToString(),
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = eventConsumableItemCrafts.AvatarAddress.ToString(),
                SlotIndex = eventConsumableItemCrafts.SlotIndex,
                EventScheduleId = eventConsumableItemCrafts.EventScheduleId,
                EventConsumableItemRecipeId = eventConsumableItemCrafts.EventConsumableItemRecipeId,
                RequiredItem1Id = requiredItemData["requiredItem1Id"],
                RequiredItem1Count = requiredItemData["requiredItem1Count"],
                RequiredItem2Id = requiredItemData["requiredItem2Id"],
                RequiredItem2Count = requiredItemData["requiredItem2Count"],
                RequiredItem3Id = requiredItemData["requiredItem3Id"],
                RequiredItem3Count = requiredItemData["requiredItem3Count"],
                RequiredItem4Id = requiredItemData["requiredItem4Id"],
                RequiredItem4Count = requiredItemData["requiredItem4Count"],
                RequiredItem5Id = requiredItemData["requiredItem5Id"],
                RequiredItem5Count = requiredItemData["requiredItem5Count"],
                RequiredItem6Id = requiredItemData["requiredItem6Id"],
                RequiredItem6Count = requiredItemData["requiredItem6Count"],
                BlockIndex = ev.BlockIndex,
                Timestamp = blockTime,
            };

            return eventConsumableItemCraftsModel;
        }
    }
}
