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
    using Nekoyume.TableData;
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
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                Log.Debug(
                                    "AvatarName: {0}, AvatarLevel: {1}, ArmorId: {2}, TitleId: {3}, CP: {4}",
                                    avatarName,
                                    avatarLevel,
                                    avatarArmorId,
                                    avatarTitleId,
                                    avatarCp);

                                bool isClear = avatarState.stageMap.ContainsKey(has.stageId);
                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    has.avatarAddress,
                                    ev.Signer,
                                    avatarName,
                                    avatarLevel,
                                    avatarTitleId,
                                    avatarArmorId,
                                    avatarCp);
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

                            if (ev.Action is RankingBattle rb)
                            {
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(rb.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                Log.Debug(
                                    "AvatarName: {0}, AvatarLevel: {1}, ArmorId: {2}, TitleId: {3}, CP: {4}",
                                    avatarName,
                                    avatarLevel,
                                    avatarArmorId,
                                    avatarTitleId,
                                    avatarCp);

                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    rb.avatarAddress,
                                    ev.Signer,
                                    avatarName,
                                    avatarLevel,
                                    avatarTitleId,
                                    avatarArmorId,
                                    avatarCp);

                                Log.Debug("Stored RankingBattle avatar data in block #{index}", ev.BlockIndex);
                            }

                            if (ev.Action is CombinationConsumable combinationConsumable)
                            {
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(combinationConsumable.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    combinationConsumable.avatarAddress,
                                    ev.Signer,
                                    avatarName,
                                    avatarLevel,
                                    avatarTitleId,
                                    avatarArmorId,
                                    avatarCp);
                                MySqlStore.StoreCombinationConsumable(
                                    combinationConsumable.Id,
                                    ev.Signer,
                                    combinationConsumable.avatarAddress,
                                    combinationConsumable.recipeId,
                                    combinationConsumable.slotIndex,
                                    ev.BlockIndex
                                );
                                Log.Debug("Stored CombinationConsumable action in block #{index}", ev.BlockIndex);
                            }

                            if (ev.Action is CombinationEquipment combinationEquipment)
                            {
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(combinationEquipment.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    combinationEquipment.avatarAddress,
                                    ev.Signer,
                                    avatarName,
                                    avatarLevel,
                                    avatarTitleId,
                                    avatarArmorId,
                                    avatarCp);
                                MySqlStore.StoreCombinationEquipment(
                                    combinationEquipment.Id,
                                    ev.Signer,
                                    combinationEquipment.avatarAddress,
                                    combinationEquipment.recipeId,
                                    combinationEquipment.slotIndex,
                                    combinationEquipment.subRecipeId,
                                    ev.BlockIndex
                                );
                                Log.Debug("Stored CombinationEquipment action in block #{index}", ev.BlockIndex);

                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    combinationEquipment.avatarAddress,
                                    combinationEquipment.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        combinationEquipment.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        avatarName,
                                        avatarLevel,
                                        avatarTitleId,
                                        avatarArmorId,
                                        avatarCp);
                                }

                                Log.Debug(
                                    "Stored avatar {address}'s equipment in block #{index}",
                                    combinationEquipment.avatarAddress,
                                    ev.BlockIndex);
                            }

                            if (ev.Action is ItemEnhancement itemEnhancement)
                            {
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(itemEnhancement.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    itemEnhancement.avatarAddress,
                                    ev.Signer,
                                    avatarName,
                                    avatarLevel,
                                    avatarTitleId,
                                    avatarArmorId,
                                    avatarCp);
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
                                        avatarName,
                                        avatarLevel,
                                        avatarTitleId,
                                        avatarArmorId,
                                        avatarCp);
                                }

                                Log.Debug(
                                    "Stored avatar {address}'s equipment in block #{index}",
                                    itemEnhancement.avatarAddress,
                                    ev.BlockIndex);
                            }

                            if (ev.Action is Buy buy)
                            {
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(buy.buyerAvatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                MySqlStore.StoreAgent(ev.Signer);
                                MySqlStore.StoreAvatar(
                                    buy.buyerAvatarAddress,
                                    ev.Signer,
                                    avatarName,
                                    avatarLevel,
                                    avatarTitleId,
                                    avatarArmorId,
                                    avatarCp);
                                var buyerInventory = avatarState.inventory;
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
                                                avatarName,
                                                avatarLevel,
                                                avatarTitleId,
                                                avatarArmorId,
                                                avatarCp);
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
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(combinationEquipment.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;
                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    combinationEquipment.avatarAddress,
                                    combinationEquipment.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        combinationEquipment.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        avatarName,
                                        avatarLevel,
                                        avatarTitleId,
                                        avatarArmorId,
                                        avatarCp);
                                }

                                Log.Debug(
                                    "Reverted avatar {address}'s equipments in block #{index}",
                                    combinationEquipment.avatarAddress,
                                    ev.BlockIndex);
                            }

                            if (ev.Action is ItemEnhancement itemEnhancement)
                            {
                                MySqlStore.DeleteItemEnhancement(itemEnhancement.Id);
                                Log.Debug("Deleted ItemEnhancement action in block #{index}", ev.BlockIndex);
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(itemEnhancement.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;
                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    itemEnhancement.avatarAddress,
                                    itemEnhancement.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        itemEnhancement.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable,
                                        avatarName,
                                        avatarLevel,
                                        avatarTitleId,
                                        avatarArmorId,
                                        avatarCp);
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
                                        AvatarState sellerState = ev.OutputStates.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;
                                        var previousStates = ev.PreviousStates;
                                        var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                        var avatarLevel = sellerState.level;
                                        var avatarArmorId = sellerState.GetArmorId();
                                        var avatarTitleCostume = sellerState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                        int? avatarTitleId = null;
                                        if (avatarTitleCostume != null)
                                        {
                                            avatarTitleId = avatarTitleCostume.Id;
                                        }

                                        var avatarCp = CPHelper.GetCP(sellerState, characterSheet);
                                        string avatarName = sellerState.name;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        MySqlStore.StoreAgent(ev.Signer);
                                        MySqlStore.StoreAvatar(
                                            purchaseInfo.SellerAvatarAddress,
                                            purchaseInfo.SellerAgentAddress,
                                            avatarName,
                                            avatarLevel,
                                            avatarTitleId,
                                            avatarArmorId,
                                            avatarCp);
                                        Equipment? equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.TradableId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.TradableId == purchaseInfo.TradableId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            ProcessEquipmentData(
                                                purchaseInfo.SellerAvatarAddress,
                                                purchaseInfo.SellerAgentAddress,
                                                equipmentNotNull,
                                                avatarName,
                                                avatarLevel,
                                                avatarTitleId,
                                                avatarArmorId,
                                                avatarCp);
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
            string avatarName,
            int? avatarLevel,
            int? avatarTitleId,
            int? avatarArmorId,
            int? avatarCp)
        {
            MySqlStore.StoreAgent(agentAddress);
            MySqlStore.StoreAvatar(
                avatarAddress,
                agentAddress,
                avatarName,
                avatarLevel,
                avatarTitleId,
                avatarArmorId,
                avatarCp);
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
