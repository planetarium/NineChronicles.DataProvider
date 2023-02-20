namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Event;
    using Nekoyume.Model.State;
    using Nekoyume.TableData.Event;
    using NineChronicles.DataProvider.Store.Models;

    public static class EventDungeonBattleData
    {
        public static EventDungeonBattleModel GetEventDungeonBattleInfo(
            ActionBase.ActionEvaluation<EventDungeonBattle> ev,
            EventDungeonBattle eventDungeonBattle,
            DateTimeOffset blockTime
        )
        {
            var previousStates = ev.PreviousStates;
            var outputStates = ev.OutputStates;
            AvatarState prevAvatarState = previousStates.GetAvatarStateV2(eventDungeonBattle.AvatarAddress);
            AvatarState outputAvatarState = outputStates.GetAvatarStateV2(eventDungeonBattle.AvatarAddress);
            var prevAvatarItems = prevAvatarState.inventory.Items;
            var outputAvatarItems = outputAvatarState.inventory.Items;
            var addressesHex =
                RenderSubscriber.GetSignerAndOtherAddressesHex(ev.Signer, eventDungeonBattle.AvatarAddress);
            var scheduleSheet = previousStates.GetSheet<EventScheduleSheet>();
            var scheduleRow = scheduleSheet.ValidateFromActionForDungeon(
                ev.BlockIndex,
                eventDungeonBattle.EventScheduleId,
                eventDungeonBattle.EventDungeonId,
                "event_dungeon_battle",
                addressesHex);
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(
                eventDungeonBattle.AvatarAddress,
                eventDungeonBattle.EventDungeonId);
            var eventDungeonInfo = ev.OutputStates.GetState(eventDungeonInfoAddr)
                is Bencodex.Types.List serializedEventDungeonInfoList
                ? new EventDungeonInfo(serializedEventDungeonInfoList)
                : new EventDungeonInfo(remainingTickets: scheduleRow.DungeonTicketsMax);
            bool isClear = eventDungeonInfo.IsCleared(eventDungeonBattle.EventDungeonStageId);
            Currency ncgCurrency = ev.OutputStates.GetGoldCurrency();
            var prevNCGBalance = previousStates.GetBalance(
                ev.Signer,
                ncgCurrency);
            var outputNCGBalance = ev.OutputStates.GetBalance(
                ev.Signer,
                ncgCurrency);
            var burntNCG = prevNCGBalance - outputNCGBalance;
            var newItems = outputAvatarItems.Except(prevAvatarItems);
            Dictionary<string, int> rewardItemData = new Dictionary<string, int>();
            for (var i = 1; i < 11; i++)
            {
                rewardItemData.Add($"rewardItem{i}Id", 0);
                rewardItemData.Add($"rewardItem{i}Count", 0);
            }

            int itemNumber = 1;
            foreach (var newItem in newItems)
            {
                rewardItemData[$"rewardItem{itemNumber}Id"] = newItem.item.Id;
                if (prevAvatarItems.Any(x => x.item.Equals(newItem.item)))
                {
                    var prevItemCount = prevAvatarItems.Single(x => x.item.Equals(newItem.item)).count;
                    var rewardCount = newItem.count - prevItemCount;
                    rewardItemData[$"rewardItem{itemNumber}Count"] = rewardCount;
                }
                else
                {
                    rewardItemData[$"rewardItem{itemNumber}Count"] = newItem.count;
                }

                itemNumber++;
            }

            var eventDungeonBattleModel = new EventDungeonBattleModel
            {
                Id = eventDungeonBattle.Id.ToString(),
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = eventDungeonBattle.AvatarAddress.ToString(),
                EventDungeonId = eventDungeonBattle.EventDungeonId,
                EventScheduleId = eventDungeonBattle.EventScheduleId,
                EventDungeonStageId = eventDungeonBattle.EventDungeonStageId,
                RemainingTickets = eventDungeonInfo.RemainingTickets,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                Cleared = isClear,
                FoodsCount = eventDungeonBattle.Foods.Count,
                CostumesCount = eventDungeonBattle.Costumes.Count,
                EquipmentsCount = eventDungeonBattle.Equipments.Count,
                RewardItem1Id = rewardItemData["rewardItem1Id"],
                RewardItem1Count = rewardItemData["rewardItem1Count"],
                RewardItem2Id = rewardItemData["rewardItem2Id"],
                RewardItem2Count = rewardItemData["rewardItem2Count"],
                RewardItem3Id = rewardItemData["rewardItem3Id"],
                RewardItem3Count = rewardItemData["rewardItem3Count"],
                RewardItem4Id = rewardItemData["rewardItem4Id"],
                RewardItem4Count = rewardItemData["rewardItem4Count"],
                RewardItem5Id = rewardItemData["rewardItem5Id"],
                RewardItem5Count = rewardItemData["rewardItem5Count"],
                RewardItem6Id = rewardItemData["rewardItem6Id"],
                RewardItem6Count = rewardItemData["rewardItem6Count"],
                RewardItem7Id = rewardItemData["rewardItem7Id"],
                RewardItem7Count = rewardItemData["rewardItem7Count"],
                RewardItem8Id = rewardItemData["rewardItem8Id"],
                RewardItem8Count = rewardItemData["rewardItem8Count"],
                RewardItem9Id = rewardItemData["rewardItem9Id"],
                RewardItem9Count = rewardItemData["rewardItem9Count"],
                RewardItem10Id = rewardItemData["rewardItem10Id"],
                RewardItem10Count = rewardItemData["rewardItem10Count"],
                BlockIndex = ev.BlockIndex,
                Timestamp = blockTime,
            };

            return eventDungeonBattleModel;
        }
    }
}
