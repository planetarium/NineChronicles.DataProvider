namespace NineChronicles.DataProvider.Executable
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Libplanet.KeyStore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NineChronicles.Headless;
    using NineChronicles.Headless.Properties;
    using Serilog;

    public static class Program
    {
        public static async Task Main()
        {
            // Get configuration
            var configurationBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            var configuration = configurationBuilder.Build();
            var loggerConf = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);

            // Get headless configuration
            var path = Path.GetFullPath("appsettings.json");
            var jsonString = File.ReadAllText(path);
            HeadlessConfiguration config = Newtonsoft.Json.JsonConvert.DeserializeObject<HeadlessConfiguration>(jsonString);

            Log.Logger = loggerConf.CreateLogger();

            var context = new StandaloneContext
            {
                KeyStore = Web3KeyStore.DefaultKeyStore,
            };

            IHostBuilder hostBuilder = Host.CreateDefaultBuilder();

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            hostBuilder.ConfigureWebHostDefaults(builder =>
            {
                builder.UseStartup<GraphQL.GraphQLStartup>();
                builder.UseUrls($"http://{config.GraphQLHost}:{config.GraphQLPort}/");
            });

            var properties = NineChroniclesNodeServiceProperties
                .GenerateLibplanetNodeServiceProperties(
                    config.AppProtocolVersionToken,
                    config.GenesisBlockPath,
                    config.Host,
                    config.Port,
                    config.SwarmPrivateKeyString,
                    config.MinimumDifficulty,
                    config.StoreType,
                    config.StorePath,
                    100,
                    config.IceServerStrings,
                    config.PeerStrings,
                    config.TrustedAppProtocolVersionSigners,
                    noMiner: true,
                    workers: config.Workers,
                    confirmations: config.Confirmations,
                    maximumTransactions: config.MaximumTransactions,
                    messageTimeout: config.MessageTimeout,
                    tipTimeout: config.TipTimeout,
                    demandBuffer: config.DemandBuffer,
                    staticPeerStrings: config.StaticPeerStrings);

            var nineChroniclesProperties = new NineChroniclesNodeServiceProperties()
            {
               MinerPrivateKey = null,
               Rpc = null,
               Libplanet = properties,
            };

            if (config.LogActionRenders)
            {
                properties.LogActionRenders = true;
            }

            NineChroniclesNodeService nineChroniclesNodeService =
                StandaloneServices.CreateHeadless(
                    nineChroniclesProperties,
                    context,
                    blockInterval: config.BlockInterval,
                    reorgInterval: config.ReorgInterval,
                    authorizedMiner: config.AuthorizedMiner,
                    txLifeTime: TimeSpan.FromMinutes(config.TxLifeTime));

            // ConfigureServices must come before Configure for now
            hostBuilder = hostBuilder
                .ConfigureServices((ctx, services) =>
                {
                    services.AddHostedService(provider =>
                        new DataProvider.ActionEvaluationPublisher(
                            nineChroniclesNodeService.BlockRenderer,
                            nineChroniclesNodeService.ActionRenderer,
                            nineChroniclesNodeService.ExceptionRenderer,
                            nineChroniclesNodeService.NodeStatusRenderer));
                });
            hostBuilder =
                   nineChroniclesNodeService.Configure(hostBuilder);

            await hostBuilder.RunConsoleAsync(token);
        }
    }
}
