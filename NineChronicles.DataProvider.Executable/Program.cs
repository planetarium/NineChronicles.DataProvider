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
            var headlessConfig = new Configuration();
            config.Bind(headlessConfig);

            var loggerConf = new LoggerConfiguration()
                .ReadFrom.Configuration(config);
            Log.Logger = loggerConf.CreateLogger();

            // FIXME: quick and dirty workaround.
            // Please remove it after fixing Libplanet.Net.Swarm<T> and NetMQTransport...
            if (string.IsNullOrEmpty(headlessConfig.Host))
            {
                headlessConfig.Host = null;
            }

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
               Libplanet = properties,
            };

            if (headlessConfig.LogActionRenders)
            {
                properties.LogActionRenders = true;
            }

            hostBuilder.UseNineChroniclesNode(nineChroniclesProperties, context);

            // ConfigureServices must come before Configure for now
            hostBuilder = hostBuilder
                .ConfigureServices((ctx, services) =>
                {
                    services.AddHostedService<RenderSubscriber>();
                    services.AddSingleton<MySqlStore>();
                    services.Configure<Configuration>(config);
                });

            await hostBuilder.RunConsoleAsync(token);
        }
    }
}
