namespace NineChronicles.DataProvider.Executable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Libplanet.KeyStore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.Headless;
    using NineChronicles.Headless.Properties;
    using Serilog;

    public static class Program
    {
        public static async Task Main()
        {
            // Get configuration
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables("NC_");
            IConfiguration config = configurationBuilder.Build();
            var headlessConfig = new HeadlessConfiguration();
            config.Bind(headlessConfig);

            var loggerConf = new LoggerConfiguration()
                .ReadFrom.Configuration(config);
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
                builder.UseUrls($"http://{headlessConfig.GraphQLHost}:{headlessConfig.GraphQLPort}/");
            });

            var properties = NineChroniclesNodeServiceProperties
                .GenerateLibplanetNodeServiceProperties(
                    headlessConfig.AppProtocolVersionToken,
                    headlessConfig.GenesisBlockPath,
                    headlessConfig.Host,
                    headlessConfig.Port,
                    headlessConfig.SwarmPrivateKeyString,
                    headlessConfig.MinimumDifficulty,
                    headlessConfig.StoreType,
                    headlessConfig.StorePath,
                    100,
                    headlessConfig.IceServerStrings,
                    headlessConfig.PeerStrings,
                    headlessConfig.TrustedAppProtocolVersionSigners,
                    noMiner: true,
                    workers: headlessConfig.Workers,
                    confirmations: headlessConfig.Confirmations,
                    maximumTransactions: headlessConfig.MaximumTransactions,
                    messageTimeout: headlessConfig.MessageTimeout,
                    tipTimeout: headlessConfig.TipTimeout,
                    demandBuffer: headlessConfig.DemandBuffer,
                    staticPeerStrings: headlessConfig.StaticPeerStrings,
                    render: true);

            var nineChroniclesProperties = new NineChroniclesNodeServiceProperties()
            {
               MinerPrivateKey = null,
               Rpc = null,
               Libplanet = properties,
            };

            if (headlessConfig.LogActionRenders)
            {
                properties.LogActionRenders = true;
            }

            var mySqlStore = new MySqlStore(headlessConfig.MySqlConnectionString);

            NineChroniclesNodeService nineChroniclesNodeService =
                StandaloneServices.CreateHeadless(
                    nineChroniclesProperties,
                    context,
                    blockInterval: headlessConfig.BlockInterval,
                    reorgInterval: headlessConfig.ReorgInterval,
                    authorizedMiner: headlessConfig.AuthorizedMiner,
                    txLifeTime: TimeSpan.FromMinutes(headlessConfig.TxLifeTime));

            // ConfigureServices must come before Configure for now
            hostBuilder = hostBuilder
                .ConfigureServices((ctx, services) =>
                {
                    services.AddHostedService(provider =>
                        new DataProvider.RenderSubscriber(
                            nineChroniclesNodeService.BlockRenderer,
                            nineChroniclesNodeService.ActionRenderer,
                            nineChroniclesNodeService.ExceptionRenderer,
                            nineChroniclesNodeService.NodeStatusRenderer,
                            mySqlStore));
                });
            hostBuilder =
                   nineChroniclesNodeService.Configure(hostBuilder);

            await hostBuilder.RunConsoleAsync(token);
        }
    }
}
