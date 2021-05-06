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
            BlockRenderer blockRenderer,
            ActionRenderer actionRenderer,
            ExceptionRenderer exceptionRenderer,
            NodeStatusRenderer nodeStatusRenderer,
            MySqlStore mySqlStore
        )
        {
            _blockRenderer = blockRenderer;
            _actionRenderer = actionRenderer;
            _exceptionRenderer = exceptionRenderer;
            _nodeStatusRenderer = nodeStatusRenderer;
            MySqlStore = mySqlStore;
        }

        internal MySqlStore MySqlStore { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _actionRenderer.EveryRender<HackAndSlash4>()
                .Subscribe(
                    ev =>
                    {
                        Log.Debug("Storing HackAndSlash Action in Block #{0}", ev.BlockIndex);
                        MySqlStore.StoreAgent(ev.Signer.ToString());
                        MySqlStore.StoreAvatar(
                            ev.Action.avatarAddress.ToString(),
                            ev.Signer.ToString());
                        MySqlStore.StoreHackAndSlash(
                            ev.Signer.ToString(),
                            ev.Action.avatarAddress.ToString(),
                            ev.Action.stageId,
                            ev.Action.Result.IsClear
                        );
                        Log.Debug("Stored HackAndSlash Action in Block #{0}", ev.BlockIndex);
                    },
                    stoppingToken
                );
            return Task.CompletedTask;
        }
    }
}
