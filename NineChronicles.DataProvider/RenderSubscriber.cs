namespace NineChronicles.DataProvider
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Lib9c.Renderer;
    using Libplanet;
    using Microsoft.Extensions.Hosting;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.Headless;
    using Serilog;

    public class RenderSubscriber : BackgroundService
    {
        private readonly BlockRenderer _blockRenderer;
        private readonly ActionRenderer _actionRenderer;
        private readonly ExceptionRenderer _exceptionRenderer;
        private readonly NodeStatusRenderer _nodeStatusRenderer;

        public RenderSubscriber(
            NineChroniclesNodeService nodeService,
            MySqlStore mySqlStore
        )
        {
            _blockRenderer = nodeService.BlockRenderer;
            _actionRenderer = nodeService.ActionRenderer;
            _exceptionRenderer = nodeService.ExceptionRenderer;
            _nodeStatusRenderer = nodeService.NodeStatusRenderer;
            MySqlStore = mySqlStore;
        }

        internal MySqlStore MySqlStore { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _actionRenderer.EveryRender<ActionBase>()
                .Subscribe(
                    ev =>
                    {
                        try
                        {
                            if (ev.Exception != null)
                            {
                                return;
                            }

                            if (ev.Action is HackAndSlash has)
                            {
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(has.avatarAddress);
                                string avatarName = avatarState.name;
                                bool isClear = avatarState.stageMap.ContainsKey(has.stageId);
                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    has.avatarAddress,
                                    ev.Signer,
                                    avatarName);
                                MySqlStore.StoreHackAndSlash(
                                    has.Id,
                                    ev.Signer,
                                    has.avatarAddress,
                                    has.stageId,
                                    isClear,
                                    isMimisbrunnr: has.stageId > 10000000,
                                    ev.BlockIndex
                                );
                                Log.Debug("Stored HackAndSlash action in block #{index}", ev.BlockIndex);
                            }

                            if (ev.Action is CombinationConsumable combinationConsumable)
                            {
                                string avatarName = ev.OutputStates.GetAvatarStateV2(combinationConsumable.AvatarAddress).name;
                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    combinationConsumable.AvatarAddress,
                                    ev.Signer,
                                    avatarName);
                                MySqlStore.StoreCombinationConsumable(
                                    combinationConsumable.Id,
                                    ev.Signer,
                                    combinationConsumable.AvatarAddress,
                                    combinationConsumable.recipeId,
                                    combinationConsumable.slotIndex,
                                    ev.BlockIndex
                                );
                                Log.Debug("Stored CombinationConsumable action in block #{index}", ev.BlockIndex);
                            }

                            if (ev.Action is CombinationEquipment combinationEquipment)
                            {
                                string avatarName = ev.OutputStates.GetAvatarStateV2(combinationEquipment.AvatarAddress).name;
                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    combinationEquipment.AvatarAddress,
                                    ev.Signer,
                                    avatarName);
                                MySqlStore.StoreCombinationEquipment(
                                    combinationEquipment.Id,
                                    ev.Signer,
                                    combinationEquipment.AvatarAddress,
                                    combinationEquipment.RecipeId,
                                    combinationEquipment.SlotIndex,
                                    combinationEquipment.SubRecipeId,
                                    ev.BlockIndex
                                );
                                Log.Debug("Stored CombinationEquipment action in block #{index}", ev.BlockIndex);

                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    combinationEquipment.AvatarAddress,
                                    combinationEquipment.SlotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        combinationEquipment.AvatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        avatarName);
                                }

                                Log.Debug(
                                    "Stored avatar {address}'s equipment in block #{index}",
                                    combinationEquipment.AvatarAddress,
                                    ev.BlockIndex);
                            }

                            if (ev.Action is ItemEnhancement itemEnhancement)
                            {
                                string avatarName = ev.OutputStates.GetAvatarStateV2(itemEnhancement.avatarAddress).name;
                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    itemEnhancement.avatarAddress,
                                    ev.Signer,
                                    avatarName);
                                MySqlStore.StoreItemEnhancement(
                                    itemEnhancement.Id,
                                    ev.Signer,
                                    itemEnhancement.avatarAddress,
                                    itemEnhancement.itemId,
                                    itemEnhancement.materialId,
                                    itemEnhancement.slotIndex,
                                    ev.BlockIndex
                                );
                                Log.Debug("Stored ItemEnhancement action in block #{index}", ev.BlockIndex);

                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    itemEnhancement.avatarAddress,
                                    itemEnhancement.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        itemEnhancement.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        avatarName);
                                }

                                Log.Debug(
                                    "Stored avatar {address}'s equipment in block #{index}",
                                    itemEnhancement.avatarAddress,
                                    ev.BlockIndex);
                            }

                            if (ev.Action is Buy buy)
                            {
                                var buyerState = ev.OutputStates.GetAvatarStateV2(buy.buyerAvatarAddress);
                                string avatarName = buyerState.name;
                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    buy.buyerAvatarAddress,
                                    ev.Signer,
                                    avatarName);
                                var buyerInventory = buyerState.inventory;
                                foreach (var purchaseInfo in buy.purchaseInfos)
                                {
                                    if (purchaseInfo.ItemSubType == ItemSubType.Armor
                                        || purchaseInfo.ItemSubType == ItemSubType.Belt
                                        || purchaseInfo.ItemSubType == ItemSubType.Necklace
                                        || purchaseInfo.ItemSubType == ItemSubType.Ring
                                        || purchaseInfo.ItemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ev.OutputStates.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        Equipment? equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.TradableId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.TradableId == purchaseInfo.TradableId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            ProcessEquipmentData(
                                                ev.Signer,
                                                buy.buyerAvatarAddress,
                                                equipmentNotNull,
                                                avatarName);
                                        }
                                    }
                                }

                                Log.Debug(
                                    "Stored avatar {address}'s equipment in block #{index}",
                                    buy.buyerAvatarAddress,
                                    ev.BlockIndex);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RenderSubscriber: {message}", ex.Message);
                        }
                    });

            _actionRenderer.EveryUnrender<ActionBase>()
                .Subscribe(
                    ev =>
                    {
                        try
                        {
                            if (ev.Exception != null)
                            {
                                return;
                            }

                            if (ev.Action is HackAndSlash has)
                            {
                                MySqlStore.DeleteHackAndSlash(has.Id);
                                Log.Debug("Deleted HackAndSlash action in block #{index}", ev.BlockIndex);
                            }

                            if (ev.Action is CombinationConsumable combinationConsumable)
                            {
                                MySqlStore.DeleteCombinationConsumable(combinationConsumable.Id);
                                Log.Debug("Deleted CombinationConsumable action in block #{index}", ev.BlockIndex);
                            }

                            if (ev.Action is CombinationEquipment combinationEquipment)
                            {
                                MySqlStore.DeleteCombinationEquipment(combinationEquipment.Id);
                                Log.Debug("Deleted CombinationEquipment action in block #{index}", ev.BlockIndex);
                                string avatarName = ev.OutputStates.GetAvatarStateV2(combinationEquipment.AvatarAddress)
                                    .name;
                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    combinationEquipment.AvatarAddress,
                                    combinationEquipment.SlotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        combinationEquipment.AvatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        avatarName);
                                }

                                Log.Debug(
                                    "Reverted avatar {address}'s equipments in block #{index}",
                                    combinationEquipment.AvatarAddress,
                                    ev.BlockIndex);
                            }

                            if (ev.Action is ItemEnhancement itemEnhancement)
                            {
                                MySqlStore.DeleteItemEnhancement(itemEnhancement.Id);
                                Log.Debug("Deleted ItemEnhancement action in block #{index}", ev.BlockIndex);
                                string avatarName = ev.OutputStates.GetAvatarStateV2(itemEnhancement.avatarAddress)
                                    .name;
                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    itemEnhancement.avatarAddress,
                                    itemEnhancement.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        itemEnhancement.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        avatarName);
                                }

                                Log.Debug(
                                    "Reverted avatar {address}'s equipments in block #{index}",
                                    itemEnhancement.avatarAddress,
                                    ev.BlockIndex);
                            }

                            if (ev.Action is Buy buy)
                            {
                                var buyerInventory = ev.OutputStates.GetAvatarStateV2(buy.buyerAvatarAddress).inventory;

                                foreach (var purchaseInfo in buy.purchaseInfos)
                                {
                                    if (purchaseInfo.ItemSubType == ItemSubType.Armor
                                        || purchaseInfo.ItemSubType == ItemSubType.Belt
                                        || purchaseInfo.ItemSubType == ItemSubType.Necklace
                                        || purchaseInfo.ItemSubType == ItemSubType.Ring
                                        || purchaseInfo.ItemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ev.OutputStates.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        string avatarName = sellerState.name;
                                        MySqlStore.StoreAgent(ev.Signer);
                                        MySqlStore.StoreAvatar(
                                            purchaseInfo.SellerAvatarAddress,
                                            purchaseInfo.SellerAgentAddress,
                                            avatarName);
                                        Equipment? equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.TradableId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.TradableId == purchaseInfo.TradableId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            ProcessEquipmentData(
                                                purchaseInfo.SellerAvatarAddress,
                                                purchaseInfo.SellerAgentAddress,
                                                equipmentNotNull,
                                                avatarName);
                                        }
                                    }
                                }

                                Log.Debug(
                                    "Reverted avatar {address}'s equipment in block #{index}",
                                    buy.buyerAvatarAddress,
                                    ev.BlockIndex);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RenderSubscriber: {message}", ex.Message);
                        }
                    });
            return Task.CompletedTask;
        }

        private void ProcessEquipmentData(
            Address agentAddress,
            Address avatarAddress,
            Equipment equipment,
            string avatarName)
        {
            MySqlStore.StoreAgent(agentAddress);
            MySqlStore.StoreAvatar(
                avatarAddress,
                agentAddress,
                avatarName);
            var cp = CPHelper.GetCP(equipment);
            MySqlStore.ProcessEquipment(
                equipment.ItemId,
                agentAddress,
                avatarAddress,
                equipment.Id,
                cp,
                equipment.level,
                equipment.ItemSubType);
        }
    }
}
