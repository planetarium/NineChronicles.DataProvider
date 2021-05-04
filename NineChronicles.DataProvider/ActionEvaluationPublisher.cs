using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Lib9c.Renderer;
using Microsoft.Extensions.Hosting;
using Nekoyume.Action;
using NineChronicles.Headless;
using Serilog;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.DataProvider
{
    public class ActionEvaluationPublisher : BackgroundService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly BlockRenderer _blockRenderer;
        private readonly ActionRenderer _actionRenderer;
        private readonly ExceptionRenderer _exceptionRenderer;
        private readonly NodeStatusRenderer _nodeStatusRenderer;
        private RpcContext _context;

        public ActionEvaluationPublisher(
            BlockRenderer blockRenderer,
            ActionRenderer actionRenderer,
            ExceptionRenderer exceptionRenderer,
            NodeStatusRenderer nodeStatusRenderer,
            string host,
            int port,
            RpcContext context
        )
        {
            _blockRenderer = blockRenderer;
            _actionRenderer = actionRenderer;
            _exceptionRenderer = exceptionRenderer;
            _nodeStatusRenderer = nodeStatusRenderer;
            _host = host;
            _port = port;
            _context = context;
        }

        public override async Task StartAsync(CancellationToken stoppingToken)
        {
            await base.StartAsync(stoppingToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _blockRenderer.EveryBlock().Subscribe(
                 pair =>
                {
                    try
                    {
                        foreach (var i in pair.NewTip.Transactions)
                        {
                            foreach (var j in i.Actions)
                            {
                                Log.Error("***********EVERY BLOCK: {0}", j.PlainValue.ToString());
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // FIXME add logger as property
                        Log.Error(e, "Skip broadcasting block render due to the unexpected exception");
                    }

                },
                stoppingToken
            );

            _blockRenderer.EveryReorg().Subscribe(
                 ev =>
                {
                    try
                    {
                    }
                    catch (Exception e)
                    {
                        // FIXME add logger as property
                        Log.Error(e, "Skip broadcasting reorg due to the unexpected exception");
                    }
                },
                stoppingToken
            );

            _blockRenderer.EveryReorgEnd().Subscribe(
                 ev =>
                {
                    try
                    {
                    }
                    catch (Exception e)
                    {
                        // FIXME add logger as property
                        Log.Error(e, "Skip broadcasting reorg end due to the unexpected exception");
                    }
                },
                stoppingToken
            );

            _actionRenderer.EveryRender<ActionBase>()
                .Where(ContainsAddressToBroadcast)
                .Subscribe(
                 ev =>
                {
                    var formatter = new BinaryFormatter();
                    using var c = new MemoryStream();
                    using var df = new DeflateStream(c, System.IO.Compression.CompressionLevel.Fastest);

                    try
                    {
                        Log.Error("***********ACTION: {0}", ev.Action.PlainValue.ToString());
                    }
                    catch (SerializationException se)
                    {
                        // FIXME add logger as property
                        Log.Error(se, "Skip broadcasting render since the given action isn't serializable.");
                    }
                    catch (Exception e)
                    {
                        // FIXME add logger as property
                        Log.Error(e, "Skip broadcasting render due to the unexpected exception");
                    }
                },
                stoppingToken
            );

            _actionRenderer.EveryUnrender<ActionBase>()
                .Where(ContainsAddressToBroadcast)
                .Subscribe(
                 ev =>
                {
                    var formatter = new BinaryFormatter();
                    using var c = new MemoryStream();
                    using var df = new DeflateStream(c, System.IO.Compression.CompressionLevel.Fastest);

                    try
                    {
                    }
                    catch (SerializationException se)
                    {
                        // FIXME add logger as property
                        Log.Error(se, "Skip broadcasting unrender since the given action isn't serializable.");
                    }
                    catch (Exception e)
                    {
                        // FIXME add logger as property
                        Log.Error(e, "Skip broadcasting unrender due to the unexpected exception");
                    }
                },
                stoppingToken
            );
            
            _exceptionRenderer.EveryException().Subscribe(
                 tuple =>
                {
                    try
                    {
                    }
                    catch (Exception e)
                    {
                        // FIXME add logger as property
                        Log.Error(e, "Skip broadcasting exception due to the unexpected exception");
                    }
                },
                stoppingToken
            );
            
            _nodeStatusRenderer.EveryChangedStatus().Subscribe(
                 isPreloadStarted =>
                {
                    try
                    {
                    }
                    catch (Exception e)
                    {
                        // FIXME add logger as property
                        Log.Error(e, "Skip broadcasting status change due to the unexpected exception");
                    }
                },
                stoppingToken
            );

            return Task.CompletedTask;
        }

        private bool ContainsAddressToBroadcast(ActionBase.ActionEvaluation<ActionBase> ev)
        {
            var updatedAddresses =
                ev.OutputStates.UpdatedAddresses.Union(ev.OutputStates.UpdatedFungibleAssets.Keys);
            return _context.AddressesToSubscribe.Any(address =>
                ev.Signer.Equals(address) || updatedAddresses.Contains(address));
        }
    }
}
