using System.IO;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Server;
using GraphQL.Types;
using Libplanet.Action.State;
using Libplanet.KeyStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NineChronicles.DataProvider.GraphTypes;
using NineChronicles.DataProvider.Store;
using NineChronicles.Headless;
using NineChronicles.Headless.GraphTypes;
using NineChronicles.Headless.GraphTypes.States;

namespace NineChronicles.DataProvider.Tests;

public abstract class TestBase
{
    protected const string ConnectionStringFormat = "server=localhost;database={0};uid=root;port=3306;";

    protected DocumentExecuter DocumentExecuter;
    protected Schema Schema;
    protected NineChroniclesContext Context;
    protected ServiceCollection Services;

    protected TestBase()
    {
        var connectionString = string.Format(ConnectionStringFormat, GetType().Name);
        if (Context is null)
        {
            Context = CreateContext(connectionString);
            Context.Database.EnsureDeleted();
            Context.Database.EnsureCreated();
        }

        var services = new ServiceCollection();
        services.AddDbContextFactory<NineChroniclesContext>(options =>
        {
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(
                    connectionString),
                b => b.MigrationsAssembly("NineChronicles.DataProvider.Executable"));
        });
        services.AddSingleton<MySqlStore>();
        var tempKeyStorePath = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        var keyStore = new Web3KeyStore(tempKeyStorePath);
        var standaloneContext = new StandaloneContext
        {
            KeyStore = keyStore,
        };
        var stateContext = new StateContext(GetMockState(), 0L, new StateMemoryCache());
        services
            .AddSingleton(standaloneContext)
            .AddSingleton(stateContext)
            .AddGraphQL()
            .AddGraphTypes(typeof(NineChroniclesSummarySchema))
            .AddGraphTypes(typeof(StandaloneSchema))
            .AddLibplanetExplorer();
        services.AddSingleton<NineChroniclesSummarySchema>();
        var serviceProvider = services.BuildServiceProvider();
        Schema = new NineChroniclesSummarySchema(serviceProvider);
        DocumentExecuter = new DocumentExecuter();
        Services = services;
    }

    private NineChroniclesContext CreateContext(string connectionString)
        => new(
            new DbContextOptionsBuilder<NineChroniclesContext>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)).Options);

    protected void CleanUp()
    {
        Context.Database.EnsureDeleted();
    }

    public async Task<ExecutionResult> ExecuteAsync(string query)
    {
        return await DocumentExecuter.ExecuteAsync(new ExecutionOptions
        {
            Query = query,
            Schema = Schema
        });
    }

    protected abstract IWorldState GetMockState();
}
