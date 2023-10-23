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
    using Nekoyume.Model.Event;
    using Nekoyume.Model.State;
    using Nekoyume.TableData.Event;
    using NineChronicles.DataProvider.Store.Models;

    public static class EventDungeonBattleData
    {
        public static EventDungeonBattleModel GetEventDungeonBattleInfo(
            IAccount previousStates,
            IAccount outputStates,
            Address signer,
            Address avatarAddress,
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId,
            int foodsCount,
            int costumesCount,
            int equipmentsCount,
            Guid actionId,
            string actionType,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState prevAvatarState = previousStates.GetAvatarStateV2(avatarAddress);
            AvatarState outputAvatarState = outputStates.GetAvatarStateV2(avatarAddress);
            var prevAvatarItems = prevAvatarState.inventory.Items;
            var outputAvatarItems = outputAvatarState.inventory.Items;
            var addressesHex =
                RenderSubscriber.GetSignerAndOtherAddressesHex(signer, avatarAddress);
            var scheduleSheet = previousStates.GetSheet<EventScheduleSheet>();
            var scheduleRow = scheduleSheet.ValidateFromActionForDungeon(
                blockIndex,
                eventScheduleId,
                eventDungeonId,
                actionType,
                addressesHex);
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(
                avatarAddress,
                eventDungeonId);
            var eventDungeonInfo = outputStates.GetState(eventDungeonInfoAddr)
                is Bencodex.Types.List serializedEventDungeonInfoList
                ? new EventDungeonInfo(serializedEventDungeonInfoList)
                : new EventDungeonInfo(remainingTickets: scheduleRow.DungeonTicketsMax);
            bool isClear = eventDungeonInfo.IsCleared(eventDungeonStageId);
            Currency ncgCurrency = outputStates.GetGoldCurrency();
            var prevNCGBalance = previousStates.GetBalance(
                signer,
                ncgCurrency);
            var outputNCGBalance = outputStates.GetBalance(
                signer,
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
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                EventDungeonId = eventDungeonId,
                EventScheduleId = eventScheduleId,
                EventDungeonStageId = eventDungeonStageId,
                RemainingTickets = eventDungeonInfo.RemainingTickets,
                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                Cleared = isClear,
                FoodsCount = foodsCount,
                CostumesCount = costumesCount,
                EquipmentsCount = equipmentsCount,
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
                BlockIndex = blockIndex,
                Timestamp = blockTime,
            };

            return eventDungeonBattleModel;
        }
    }
}
