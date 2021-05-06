namespace NineChronicles.DataProvider
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Lib9c.Renderer;
    using Microsoft.Extensions.Hosting;
    using Nekoyume.Action;
    using NineChronicles.Headless;
    using Serilog;

    public class ActionEvaluationPublisher : BackgroundService
    {
        private readonly BlockRenderer _blockRenderer;
        private readonly ActionRenderer _actionRenderer;
        private readonly ExceptionRenderer _exceptionRenderer;
        private readonly NodeStatusRenderer _nodeStatusRenderer;

        public ActionEvaluationPublisher(
            BlockRenderer blockRenderer,
            ActionRenderer actionRenderer,
            ExceptionRenderer exceptionRenderer,
            NodeStatusRenderer nodeStatusRenderer
        )
        {
            _blockRenderer = blockRenderer;
            _actionRenderer = actionRenderer;
            _exceptionRenderer = exceptionRenderer;
            _nodeStatusRenderer = nodeStatusRenderer;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _actionRenderer.EveryRender<ActionBase>()
                .Subscribe(
                    ev =>
                    {
                        Log.Debug("***********ACTION: {0}", ev.Action.PlainValue.ToString());
                    },
                    stoppingToken
                );
            return Task.CompletedTask;
        }
    }
}
