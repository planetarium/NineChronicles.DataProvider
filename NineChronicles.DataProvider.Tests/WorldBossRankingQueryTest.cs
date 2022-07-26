using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Execution;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class WorldBossRankingQueryTest : TestBase, IDisposable
{
    [Fact]
    public async Task WorldBossRanking()
    {
        const string query = @"query {
        worldBossRanking(raidId: 1) {
            ranking
            avatarName
        }
    }";
        var context = CreateContext();
        for (int idx = 0; idx < 2; idx++)
        {
            for (int i = 0; i < 200; i++)
            {
                var model = new RaiderModel(
                    idx + 1,
                    i.ToString(),
                    i,
                    i + 1,
                    i + 2,
                    GameConfig.DefaultAvatarArmorId,
                    i,
                    new PrivateKey().ToAddress().ToHex()
                );
                context.Raiders.Add(model);
            }
        }

        await context.SaveChangesAsync();
        var result = await ExecuteAsync(query);
        var models = (object[])((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossRanking"];
        Assert.Equal(100, models.Length);
    }

    public void Dispose()
    {
        CleanUp();

    }
}
