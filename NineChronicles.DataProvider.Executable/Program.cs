using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Libplanet;
using Libplanet.KeyStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NineChronicles.Headless;
using NineChronicles.Headless.Properties;
using Org.BouncyCastle.Security;
using Serilog;

namespace NineChronicles.DataProvider.Executable
{
    public class Program
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

            var tasks = new List<Task>();
            var standaloneContext = new StandaloneContext
            {
                KeyStore = Web3KeyStore.DefaultKeyStore,
            };

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            if (config.GraphQLServer)
            {
                IHostBuilder graphQLHostBuilder = Host.CreateDefaultBuilder();
                string? secretToken = null;
                if (config.GraphQLSecretTokenPath is { })
                {
                    var buffer = new byte[40];
                    new SecureRandom().NextBytes(buffer);
                    secretToken = Convert.ToBase64String(buffer);
                    await File.WriteAllTextAsync(config.GraphQLSecretTokenPath, secretToken);
                }

                var graphQLNodeServiceProperties = new GraphQLNodeServiceProperties
                {
                    GraphQLServer = config.GraphQLServer,
                    GraphQLListenHost = config.GraphQLHost,
                    GraphQLListenPort = config.GraphQLPort,
                    SecretToken = secretToken,
                    NoCors = config.NoCors,
                };

                var graphQLService = new GraphQLService(graphQLNodeServiceProperties);
                graphQLHostBuilder =
                    graphQLService.Configure(graphQLHostBuilder, standaloneContext);

                tasks.Add(graphQLHostBuilder.RunConsoleAsync(token));
                await WaitForGraphQLService(graphQLNodeServiceProperties, source.Token);
            }

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
                    staticPeerStrings: config.StaticPeerStrings
                    );

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
                   standaloneContext,
                   blockInterval: config.BlockInterval,
                   reorgInterval: config.ReorgInterval,
                   authorizedMiner: config.AuthorizedMiner,
                   txLifeTime: TimeSpan.FromMinutes(config.TxLifeTime));

            IHostBuilder nineChroniclesNodeHostBuilder = Host.CreateDefaultBuilder();
            nineChroniclesNodeHostBuilder =
                   nineChroniclesNodeService.Configure(nineChroniclesNodeHostBuilder);
            tasks.Add(nineChroniclesNodeHostBuilder.RunConsoleAsync(token));

            await Task.WhenAll(tasks);
        }

        private static async Task WaitForGraphQLService(
            GraphQLNodeServiceProperties properties,
            CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            while (!cancellationToken.IsCancellationRequested)
            {
                Log.Debug("Trying to check GraphQL server started...");
                try
                {
                    await httpClient.GetAsync($"http://{IPAddress.Loopback}:{properties.GraphQLListenPort}/health-check", cancellationToken);
                    break;
                }
                catch (HttpRequestException e)
                {
                    Log.Error(e, "An exception occurred during connecting to GraphQL server. {e}", e);
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
