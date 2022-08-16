using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bencodex.Types;
using GraphQL.Execution;
using Libplanet;
using Libplanet.Assets;
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
                Context.Raiders.Add(model);
            }
        }

        await Context.SaveChangesAsync();
        var result = await ExecuteAsync(query);
        var models = (object[])((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossRanking"];
        Assert.Equal(100, models.Length);
    }

    [Fact]
    public async Task WorldBossTotalUsers()
    {
        const string query = @"query {
        worldBossTotalUsers(raidId: 1)
    }";
        for (int i = 0; i < 200; i++)
        {
            var model = new RaiderModel(
                1,
                i.ToString(),
                i,
                i + 1,
                i + 2,
                GameConfig.DefaultAvatarArmorId,
                i,
                new PrivateKey().ToAddress().ToHex()
            );
            Context.Raiders.Add(model);
        }

        await Context.SaveChangesAsync();
        var result = await ExecuteAsync(query);
        var count = (int)((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["worldBossTotalUsers"];
        Assert.Equal(200, count);

    }

    public void Dispose()
    {
        CleanUp();

    }

    protected override IValue? GetStateMock(Address address)
    {
        throw new NotImplementedException();
    }

    protected override FungibleAssetValue GetBalanceMock(Address address, Currency currency)
    {
        throw new NotImplementedException();
    }
}
