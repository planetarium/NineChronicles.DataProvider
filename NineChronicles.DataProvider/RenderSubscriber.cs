namespace NineChronicles.DataProvider
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Lib9c.Renderer;
    using Libplanet;
    using Microsoft.Extensions.Hosting;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.Item;
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
                        if (ev.Exception != null)
                        {
                            return;
                        }

                        if (ev.Action is HackAndSlash has)
                        {
                            string avatarName = ev.OutputStates.GetAvatarState(has.avatarAddress).name;
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
                                has.Result.IsClear,
                                isMimisbrunnr: has.stageId > 10000000,
                                ev.BlockIndex
                            );
                            Log.Debug("Stored HackAndSlash action in block #{index}", ev.BlockIndex);
                        }

                        if (ev.Action is CombinationConsumable combinationConsumable)
                        {
                            string avatarName = ev.OutputStates.GetAvatarState(combinationConsumable.AvatarAddress).name;
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
                            string avatarName = ev.OutputStates.GetAvatarState(combinationEquipment.AvatarAddress).name;
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

                            var inventory = ev.OutputStates.GetAvatarState(combinationEquipment.AvatarAddress).inventory;
                            if (inventory is { } inventoryNotNull)
                            {
                                ProcessEquipments(
                                    ev.Signer,
                                    combinationEquipment.AvatarAddress,
                                    inventoryNotNull,
                                    avatarName);
                            }

                            Log.Debug(
                                "Stored avatar {address}'s equipments in block #{index}",
                                combinationEquipment.AvatarAddress,
                                ev.BlockIndex);
                        }

                        if (ev.Action is ItemEnhancement itemEnhancement)
                        {
                            string avatarName = ev.OutputStates.GetAvatarState(itemEnhancement.avatarAddress).name;
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

                            var inventory = ev.OutputStates.GetAvatarState(itemEnhancement.avatarAddress).inventory;
                            if (inventory is { } inventoryNotNull)
                            {
                                ProcessEquipments(
                                    ev.Signer,
                                    itemEnhancement.avatarAddress,
                                    inventoryNotNull,
                                    avatarName);
                            }

                            Log.Debug(
                                "Stored avatar {address}'s equipments in block #{index}",
                                itemEnhancement.avatarAddress,
                                ev.BlockIndex);
                        }

                        if (ev.Action is Buy buy)
                        {
                            string avatarName = ev.OutputStates.GetAvatarState(buy.buyerAvatarAddress).name;
                            var inventory = ev.OutputStates.GetAvatarState(buy.buyerAvatarAddress).inventory;
                            if (inventory is { } inventoryNotNull)
                            {
                                ProcessEquipments(
                                    ev.Signer,
                                    buy.buyerAvatarAddress,
                                    inventoryNotNull,
                                    avatarName);
                            }

                            Log.Debug(
                                "Stored avatar {address}'s equipments in block #{index}",
                                buy.buyerAvatarAddress,
                                ev.BlockIndex);
                        }

                        if (ev.Action is Sell sell)
                        {
                            string avatarName = ev.OutputStates.GetAvatarState(sell.sellerAvatarAddress).name;
                            MySqlStore.StoreAgent(ev.Signer);
                            MySqlStore.StoreAvatar(
                                sell.sellerAvatarAddress,
                                ev.Signer,
                                avatarName);
                            var inventory = ev.OutputStates.GetAvatarState(sell.sellerAvatarAddress).inventory;
                            if (inventory is { } inventoryNotNull)
                            {
                                ProcessEquipments(
                                    ev.Signer,
                                    sell.sellerAvatarAddress,
                                    inventoryNotNull,
                                    avatarName);
                            }

                            Log.Debug(
                                "Stored avatar {address}'s equipments in block #{index}",
                                sell.sellerAvatarAddress,
                                ev.BlockIndex);
                        }
                    });

            _actionRenderer.EveryUnrender<ActionBase>()
                .Subscribe(
                    ev =>
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
                            string avatarName = ev.OutputStates.GetAvatarState(combinationEquipment.AvatarAddress).name;
                            var inventory = ev.OutputStates.GetAvatarState(combinationEquipment.AvatarAddress).inventory;
                            if (inventory is { } inventoryNotNull)
                            {
                                ProcessEquipments(
                                    ev.Signer,
                                    combinationEquipment.AvatarAddress,
                                    inventoryNotNull,
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
                            string avatarName = ev.OutputStates.GetAvatarState(itemEnhancement.avatarAddress).name;
                            var inventory = ev.OutputStates.GetAvatarState(itemEnhancement.avatarAddress).inventory;
                            if (inventory is { } inventoryNotNull)
                            {
                                ProcessEquipments(
                                    ev.Signer,
                                    itemEnhancement.avatarAddress,
                                    inventoryNotNull,
                                    avatarName);
                            }

                            Log.Debug(
                                "Reverted avatar {address}'s equipments in block #{index}",
                                itemEnhancement.avatarAddress,
                                ev.BlockIndex);
                        }

                        if (ev.Action is Buy buy)
                        {
                            string avatarName = ev.OutputStates.GetAvatarState(buy.buyerAvatarAddress).name;
                            var inventory = ev.OutputStates.GetAvatarState(buy.buyerAvatarAddress).inventory;
                            if (inventory is { } inventoryNotNull)
                            {
                                ProcessEquipments(
                                    ev.Signer,
                                    buy.buyerAvatarAddress,
                                    inventoryNotNull,
                                    avatarName);
                            }

                            Log.Debug(
                                "Reverted avatar {address}'s equipments in block #{index}",
                                buy.buyerAvatarAddress,
                                ev.BlockIndex);
                        }

                        if (ev.Action is Sell sell)
                        {
                            string avatarName = ev.OutputStates.GetAvatarState(sell.sellerAvatarAddress).name;
                            var inventory = ev.OutputStates.GetAvatarState(sell.sellerAvatarAddress).inventory;
                            if (inventory is { } inventoryNotNull)
                            {
                                ProcessEquipments(
                                    ev.Signer,
                                    sell.sellerAvatarAddress,
                                    inventoryNotNull,
                                    avatarName);
                            }

                            Log.Debug(
                                "Reverted avatar {address}'s equipments in block #{index}",
                                sell.sellerAvatarAddress,
                                ev.BlockIndex);
                        }
                    });
            return Task.CompletedTask;
        }

        private void ProcessEquipments(
            Address agentAddress,
            Address avatarAddress,
            Inventory? inventory,
            string avatarName)
        {
            MySqlStore.StoreAgent(agentAddress);
            MySqlStore.StoreAvatar(
                avatarAddress,
                agentAddress,
                avatarName);
            var equipments = inventory?.Equipments;
            if (equipments is { } equipmentsNotNull)
            {
                foreach (var equipment in equipmentsNotNull)
                {
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
    }
}
