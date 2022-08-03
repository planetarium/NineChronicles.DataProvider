using System.IO;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Server;
using GraphQL.Types;
using Libplanet.KeyStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NineChronicles.DataProvider.GraphTypes;
using NineChronicles.DataProvider.Store;
using NineChronicles.Headless;

namespace NineChronicles.DataProvider.Tests;

public class TestBase
{
    private const string ConnectionString = "server=localhost;database=dp-test;uid=root;port=3306;";

    protected DocumentExecuter DocumentExecuter;
    protected Schema Schema;

    protected TestBase()
    {
        using var ctx = CreateContext();
        {
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
        }

        var services = new ServiceCollection();
        services.AddDbContextFactory<NineChroniclesContext>(options =>
        {
            options.UseMySql(
                ConnectionString,
                ServerVersion.AutoDetect(
                    ConnectionString),
                b => b.MigrationsAssembly("NineChronicles.DataProvider.Executable"));
        });
        services.AddSingleton<MySqlStore>();
        var tempKeyStorePath = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        var keyStore = new Web3KeyStore(tempKeyStorePath);
        var context = new StandaloneContext
        {
            KeyStore = keyStore,
        };
        services
            .AddSingleton(context)
            .AddGraphQL()
            .AddGraphTypes(typeof(NineChroniclesSummarySchema));
        services.AddSingleton<NineChroniclesSummarySchema>();
        var serviceProvider = services.BuildServiceProvider();
        Schema = new NineChroniclesSummarySchema(serviceProvider);
        DocumentExecuter = new DocumentExecuter();
    }

    protected NineChroniclesContext CreateContext()
        => new(
            new DbContextOptionsBuilder<NineChroniclesContext>()
                .UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString)).Options);

    protected void CleanUp()
    {
        using var ctx = CreateContext();
        {
            ctx.Database.EnsureDeleted();
        }
    }

    public async Task<ExecutionResult> ExecuteAsync(string query)
    {
        return await DocumentExecuter.ExecuteAsync(new ExecutionOptions
        {
            Query = query,
            Schema = Schema
        });
    }
}
