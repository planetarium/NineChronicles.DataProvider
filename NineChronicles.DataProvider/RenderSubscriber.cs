namespace NineChronicles.DataProvider
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Lib9c.Renderer;
    using Microsoft.Extensions.Hosting;
    using Nekoyume.Action;
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
                            MySqlStore.StoreAgent(ev.Signer.ToString());
                            MySqlStore.StoreAvatar(
                                has.avatarAddress.ToString(),
                                ev.Signer.ToString(),
                                avatarName);
                            MySqlStore.StoreHackAndSlash(
                                has.Id.ToString(),
                                ev.Signer.ToString(),
                                has.avatarAddress.ToString(),
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
                            MySqlStore.StoreAgent(ev.Signer.ToString());
                            MySqlStore.StoreAvatar(
                                combinationConsumable.AvatarAddress.ToString(),
                                ev.Signer.ToString(),
                                avatarName);
                            MySqlStore.StoreCombinationConsumable(
                                combinationConsumable.Id.ToString(),
                                ev.Signer.ToString(),
                                combinationConsumable.AvatarAddress.ToString(),
                                combinationConsumable.recipeId,
                                combinationConsumable.slotIndex,
                                ev.BlockIndex
                            );
                            Log.Debug("Stored CombinationConsumable action in block #{index}", ev.BlockIndex);
                        }

                        if (ev.Action is CombinationEquipment combinationEquipment)
                        {
                            string avatarName = ev.OutputStates.GetAvatarState(combinationEquipment.AvatarAddress).name;
                            MySqlStore.StoreAgent(ev.Signer.ToString());
                            MySqlStore.StoreAvatar(
                                combinationEquipment.AvatarAddress.ToString(),
                                ev.Signer.ToString(),
                                avatarName);
                            MySqlStore.StoreCombinationEquipment(
                                combinationEquipment.Id.ToString(),
                                ev.Signer.ToString(),
                                combinationEquipment.AvatarAddress.ToString(),
                                combinationEquipment.RecipeId,
                                combinationEquipment.SlotIndex,
                                combinationEquipment.SubRecipeId,
                                ev.BlockIndex
                            );
                            Log.Debug("Stored CombinationEquipment action in block #{index}", ev.BlockIndex);
                        }

                        if (ev.Action is ItemEnhancement itemEnhancement)
                        {
                            string avatarName = ev.OutputStates.GetAvatarState(itemEnhancement.avatarAddress).name;
                            MySqlStore.StoreAgent(ev.Signer.ToString());
                            MySqlStore.StoreAvatar(
                                itemEnhancement.avatarAddress.ToString(),
                                ev.Signer.ToString(),
                                avatarName);
                            MySqlStore.StoreItemEnhancement(
                                itemEnhancement.Id.ToString(),
                                ev.Signer.ToString(),
                                itemEnhancement.avatarAddress.ToString(),
                                itemEnhancement.itemId.ToString(),
                                itemEnhancement.materialId.ToString(),
                                itemEnhancement.slotIndex,
                                ev.BlockIndex
                            );
                            Log.Debug("Stored ItemEnhancement action in block #{index}", ev.BlockIndex);
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
                            MySqlStore.DeleteHackAndSlash(has.Id.ToString());
                            Log.Debug("Deleted HackAndSlash action in block #{index}", ev.BlockIndex);
                        }

                        if (ev.Action is CombinationConsumable combinationConsumable)
                        {
                            MySqlStore.DeleteCombinationConsumable(combinationConsumable.Id.ToString());
                            Log.Debug("Deleted CombinationConsumable action in block #{index}", ev.BlockIndex);
                        }

                        if (ev.Action is CombinationEquipment combinationEquipment)
                        {
                            MySqlStore.DeleteCombinationEquipment(combinationEquipment.Id.ToString());
                            Log.Debug("Deleted CombinationEquipment action in block #{index}", ev.BlockIndex);
                        }

                        if (ev.Action is ItemEnhancement itemEnhancement)
                        {
                            MySqlStore.DeleteItemEnhancement(itemEnhancement.Id.ToString());
                            Log.Debug("Deleted ItemEnhancement action in block #{index}", ev.BlockIndex);
                        }
                    });
            return Task.CompletedTask;
        }
    }
}
