using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Execution;
using Libplanet;
using Libplanet.Crypto;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class AgentQueryTest : TestBase, IDisposable
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ShopEquipmentCount(int expected)
    {
        const string query = @"query {
        agentCount
    }";
        var context = CreateContext();
        for (int i = 0; i < expected; i++)
        {
            var model = new AgentModel
            {
                Address = new PrivateKey().ToAddress().ToHex(),
            };
            context.Agents.Add(model);
        }

        await context.SaveChangesAsync();
        var result = await ExecuteAsync(query);
        var count =
            (int) ((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["agentCount"];
        Assert.Equal(expected, count);
    }

    public void Dispose()
    {
        CleanUp();
    }
}
