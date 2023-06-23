using Nekoyume.Action.Loader;

namespace NineChronicles.DataProvider.Executable
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Cocona;
    using Libplanet.Action;
    using Libplanet.Action.Loader;
    using Libplanet.Crypto;
    using Libplanet.KeyStore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Executable.Commands;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.Headless;
    using NineChronicles.Headless.GraphTypes.States;
    using NineChronicles.Headless.Properties;
    using Serilog;

    [HasSubCommands(typeof(MySqlMigration), "mysql-migration")]
    [HasSubCommands(typeof(BattleArenaRankingMigration), "battle-arena-ranking-migration")]
    public class Program : CoconaLiteConsoleAppBase
    {
        public static async Task Main(string[] args)
        {
            await CoconaLiteApp.CreateHostBuilder()
                .RunAsync<Program>(args);
        }

        // EF Core uses this method at design time to access the DbContext
        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddDbContextFactory<NineChroniclesContext>(options =>
                    {
                        // Get configuration from appsettings or env
                        var configurationBuilder = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .AddEnvironmentVariables("NC_");
                        IConfiguration config = configurationBuilder.Build();
                        var headlessConfig = new Configuration();
                        config.Bind(headlessConfig);
                        if (headlessConfig.MySqlConnectionString != string.Empty)
                        {
                            args = new[] { headlessConfig.MySqlConnectionString };
                        }

                        if (args.Length == 1)
                        {
                            options.UseMySql(
                                args[0],
                                ServerVersion.AutoDetect(
                                    args[0]),
                                b => b.MigrationsAssembly("NineChronicles.DataProvider.Executable"));
                        }
                        else
                        {
                            options.UseSqlite(
                                @"Data Source=9c.gg.db",
                                b => b.MigrationsAssembly("NineChronicles.DataProvider.Executable")
                            );
                        }
                    });
                });

        [PrimaryCommand]
        public async Task Run()
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
                builder.UseStartup<GraphQLStartup>();
                builder.UseUrls($"http://{headlessConfig.GraphQLHost}:{headlessConfig.GraphQLPort}/");
            });

            var properties = NineChroniclesNodeServiceProperties
                .GenerateLibplanetNodeServiceProperties(
                    headlessConfig.AppProtocolVersionToken,
                    headlessConfig.GenesisBlockPath,
                    headlessConfig.Host,
                    headlessConfig.Port,
                    headlessConfig.SwarmPrivateKeyString,
                    headlessConfig.StoreType,
                    headlessConfig.StorePath,
                    headlessConfig.NoReduceStore,
                    100,
                    headlessConfig.IceServerStrings,
                    headlessConfig.PeerStrings,
                    headlessConfig.TrustedAppProtocolVersionSigners,
                    noMiner: headlessConfig.NoMiner,
                    confirmations: headlessConfig.Confirmations,
                    messageTimeout: headlessConfig.MessageTimeout,
                    tipTimeout: headlessConfig.TipTimeout,
                    demandBuffer: headlessConfig.DemandBuffer,
                    minimumBroadcastTarget: headlessConfig.MinimumBroadcastTarget,
                    bucketSize: headlessConfig.BucketSize,
                    render: headlessConfig.Render,
                    preload: headlessConfig.Preload);

            IActionLoader actionLoader = new NCActionLoader();

            var nineChroniclesProperties = new NineChroniclesNodeServiceProperties(actionLoader)
            {
                MinerPrivateKey = string.IsNullOrEmpty(headlessConfig.MinerPrivateKeyString)
                    ? null
                    : PrivateKey.FromString(headlessConfig.MinerPrivateKeyString),
                Libplanet = properties,
                TxQuotaPerSigner = 500,
                Dev = headlessConfig.IsDev,
                StrictRender = headlessConfig.StrictRendering,
                BlockInterval = headlessConfig.BlockInterval,
                ReorgInterval = headlessConfig.ReorgInterval,
                TxLifeTime = TimeSpan.FromMinutes(headlessConfig.TxLifeTime),
                MinerCount = headlessConfig.NoMiner ? 0 : 1,
                NetworkType = headlessConfig.NetworkType,
            };

            if (headlessConfig.LogActionRenders)
            {
                properties.LogActionRenders = true;
            }

            hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_ => context);
            });

            hostBuilder.UseNineChroniclesNode(nineChroniclesProperties, context);

            var stateContext = new StateContext(
                context.BlockChain!.ToAccountStateGetter(),
                context.BlockChain!.ToAccountBalanceGetter(),
                context.BlockChain!.Tip.Index
            );

            // ConfigureServices must come before Configure for now
            hostBuilder = hostBuilder
                .ConfigureServices((ctx, services) =>
                {
                    services.AddDbContextFactory<NineChroniclesContext>(
                        options => options.UseMySql(
                            headlessConfig.MySqlConnectionString,
                            ServerVersion.AutoDetect(headlessConfig.MySqlConnectionString),
                            mySqlOptions =>
                            {
                                mySqlOptions
                                    .EnableRetryOnFailure(
                                        maxRetryCount: 10,
                                        maxRetryDelay: TimeSpan.FromSeconds(10),
                                        errorNumbersToAdd: null);
                            }
                        )
                    );
                    services.AddHostedService<RenderSubscriber>();
                    services.AddSingleton<MySqlStore>();
                    services.Configure<Configuration>(config);
                    services.AddSingleton(stateContext);
                    services.AddHostedService<RaiderWorker>();
                });

            await hostBuilder.RunConsoleAsync(token);
        }
    }
}
