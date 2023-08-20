using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bencodex.Types;
using GraphQL.Execution;
using Libplanet;
using Libplanet.Action.State;
using Libplanet.Types.Assets;
using Libplanet.Crypto;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class AgentQueryTest : TestBase, IDisposable
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task AgentCount(int expected)
    {
        const string query = @"query {
        agentCount
    }";
        for (int i = 0; i < expected; i++)
        {
            var model = new AgentModel
            {
                Address = new PrivateKey().ToAddress().ToHex(),
            };
            Context.Agents.Add(model);
        }

        await Context.SaveChangesAsync();
        var result = await ExecuteAsync(query);
        var count =
            (int) ((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["agentCount"];
        Assert.Equal(expected, count);
    }

    public void Dispose()
    {
        CleanUp();
    }

    protected override IAccountState GetMockState()
    {
        return MockState.Empty;
    }
}
