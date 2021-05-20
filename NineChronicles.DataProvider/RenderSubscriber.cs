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

                        if (!(ev.Action is HackAndSlash4 action))
                        {
                            return;
                        }

                        Log.Debug("Storing HackAndSlash action in block #{0}", ev.BlockIndex);
                        string avatarName = ev.OutputStates.GetAvatarState(action.avatarAddress).name;
                        MySqlStore.StoreAgent(ev.Signer.ToString());
                        MySqlStore.StoreAvatar(
                            action.avatarAddress.ToString(),
                            ev.Signer.ToString(),
                            avatarName);
                        MySqlStore.StoreHackAndSlash(
                            ev.Signer.ToString(),
                            action.avatarAddress.ToString(),
                            action.stageId,
                            action.Result.IsClear,
                            isMimisbrunnr: action.stageId > 10000000
                        );
                        Log.Debug("Stored HackAndSlash action in block #{0}", ev.BlockIndex);
                    });

            _actionRenderer.EveryUnrender<ActionBase>()
                .Subscribe(
                    ev =>
                    {
                        if (ev.Exception != null)
                        {
                            return;
                        }

                        if (ev.Action is HackAndSlash4 action)
                        {
                            Log.Debug("Deleting HackAndSlash action in block #{0}", ev.BlockIndex);
                            MySqlStore.DeleteHackAndSlash(
                                ev.Signer.ToString(),
                                action.avatarAddress.ToString(),
                                action.stageId,
                                action.Result.IsClear
                            );
                            Log.Debug("Deleted HackAndSlash action in block #{0}", ev.BlockIndex);
                        }
                    });
            return Task.CompletedTask;
        }
    }
}
