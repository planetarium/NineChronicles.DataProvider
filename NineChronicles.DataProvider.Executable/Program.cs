#nullable enable
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using Libplanet.Headless.Hosting;
using Nekoyume.Action.Loader;
using IPAddress = System.Net.IPAddress;

namespace NineChronicles.DataProvider.Executable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Cocona;
    using Libplanet.Action.Loader;
    using Libplanet.Crypto;
    using Libplanet.KeyStore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NineChronicles.DataProvider.Executable.Commands;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.Headless;
    using NineChronicles.Headless.GraphTypes.States;
    using NineChronicles.Headless.Properties;
    using Serilog;

    [HasSubCommands(typeof(MySqlMigration), "mysql-migration")]
    [HasSubCommands(typeof(BattleArenaRankingMigration), "battle-arena-ranking-migration")]
    [HasSubCommands(typeof(UserStakingMigration), "user-staking-migration")]
    [HasSubCommands(typeof(UserDataMigration), "user-data-migration")]
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
        public async Task Run(
            [Option(Description = "The path of the appsettings JSON file.")] string? configPath = null)
        {
            // Get configuration
            var configurationBuilder = new ConfigurationBuilder();
            if (configPath != null)
            {
                if (Uri.IsWellFormedUriString(configPath, UriKind.Absolute))
                {
                    HttpClient client = new HttpClient();
                    HttpResponseMessage resp = await client.GetAsync(configPath);
                    resp.EnsureSuccessStatusCode();
                    Stream body = await resp.Content.ReadAsStreamAsync();
                    configurationBuilder.AddJsonStream(body)
                        .AddEnvironmentVariables("NC_");
                }
                else
                {
                    configurationBuilder.AddJsonFile(configPath!)
                        .AddEnvironmentVariables("NC_");
                }
            }
            else
            {
                configurationBuilder
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables("NC_");
            }

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

            IActionEvaluatorConfiguration GetActionEvaluatorConfiguration(IConfiguration configuration)
            {
                if (!(configuration.GetValue<ActionEvaluatorType>("Type") is { } actionEvaluatorType))
                {
                    return null;
                }

                return actionEvaluatorType switch
                {
                    ActionEvaluatorType.Default => new DefaultActionEvaluatorConfiguration(),
                    ActionEvaluatorType.RemoteActionEvaluator => new RemoteActionEvaluatorConfiguration
                    {
                        StateServiceEndpoint = configuration.GetValue<string>("StateServiceEndpoint"),
                    },
                    ActionEvaluatorType.ForkableActionEvaluator => new ForkableActionEvaluatorConfiguration
                    {
                        Pairs = (configuration.GetSection("Pairs") ??
                            throw new KeyNotFoundException()).GetChildren().Select(pair =>
                        {
                            var range = new ForkableActionEvaluatorRange();
                            pair.Bind("Range", range);
                            var actionEvaluatorConfiguration =
                                GetActionEvaluatorConfiguration(pair.GetSection("ActionEvaluator")) ??
                                throw new KeyNotFoundException();
                            return (range, actionEvaluatorConfiguration);
                        }).ToImmutableArray()
                    },
                    _ => throw new InvalidOperationException("Unexpected type."),
                };
            }

            var actionEvaluatorConfiguration =
                GetActionEvaluatorConfiguration(config.GetSection("Headless").GetSection("ActionEvaluator"));

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
                    preload: headlessConfig.Preload,
                    actionEvaluatorConfiguration: actionEvaluatorConfiguration);

            IActionLoader actionLoader = new NCActionLoader();

            var nineChroniclesProperties = new NineChroniclesNodeServiceProperties(
                actionLoader, headlessConfig.StateServiceManagerService, null)
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
            NineChroniclesNodeService service =
                NineChroniclesNodeService.Create(nineChroniclesProperties, context);
            ActionEvaluationPublisher publisher = new ActionEvaluationPublisher(
                context.NineChroniclesNodeService!.BlockRenderer,
                context.NineChroniclesNodeService!.ActionRenderer,
                context.NineChroniclesNodeService!.ExceptionRenderer,
                context.NineChroniclesNodeService!.NodeStatusRenderer,
                context.NineChroniclesNodeService!.BlockChain,
                IPAddress.Loopback.ToString(),
                0,
                new RpcContext
                {
                    RpcRemoteSever = false
                },
                new ConcurrentDictionary<string, Sentry.ITransaction>()
            );

            if (headlessConfig.LogActionRenders)
            {
                properties.LogActionRenders = true;
            }

            hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton(_ => context);
            });

            hostBuilder.UseNineChroniclesNode(nineChroniclesProperties, context, publisher, service);

            var stateContext = new StateContext(
                context.BlockChain!.GetAccountState(context.BlockChain!.Tip.Hash),
                context.BlockChain!.Tip.Index,
                new ArenaMemoryCache()
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
                                        maxRetryCount: 20,
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
